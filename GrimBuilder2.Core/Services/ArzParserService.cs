using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using K4os.Compression.LZ4;
using Nito.AsyncEx;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GrimBuilder2.Core.Services;

public class ArzParserService
{
    const string BasePath = @"D:\Program Files (x86)\Steam\steamapps\common\Grim Dawn";

    readonly Arz[] arzFiles;
    readonly Dictionary<string, string> tags = new(StringComparer.OrdinalIgnoreCase);
    readonly AsyncManualResetEvent readyEvent = new();

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ArzFileHeader
    {
        public readonly ushort Magic;
        public readonly ushort Version;
        public readonly uint RecordTableStart, RecordTableSize, RecordTableEntryCount,
            StringTableStart, StringTableSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe readonly struct ArzRecord
    {
        public ArzRecord(byte* allMemory, byte* memory, uint byteLength, IList<string> strings)
        {
            Name = strings[BinaryPrimitives.ReadInt32LittleEndian(new(memory, 4))];
            Type = BinaryPrimitivesExtended.ReadGDStringLittleEndian(new(memory + 4, (int)byteLength - 4), out var typeByteLength);

            CompressedDataStart = allMemory + Unsafe.SizeOf<ArzFileHeader>() + BinaryPrimitives.ReadUInt32LittleEndian(new(memory + 4 + typeByteLength, 4));
            CompressedDataLength = BinaryPrimitives.ReadInt32LittleEndian(new(memory + 4 + typeByteLength + 4, 4));
            UncompressedDataLength = BinaryPrimitives.ReadInt32LittleEndian(new(memory + 4 + typeByteLength + 4 + 4, 4));

            TotalBytesRead = (uint)(4 + typeByteLength + 4 + 4 + 4 + 8);
        }

        public readonly string Name;
        public readonly string Type;

        public readonly byte* CompressedDataStart;
        public readonly int CompressedDataLength;
        public readonly int UncompressedDataLength;

        public readonly uint TotalBytesRead;

        public byte[] Data
        {
            get
            {
                var result = new byte[UncompressedDataLength];
                var decompressedLength = LZ4Codec.Decode(new(CompressedDataStart, CompressedDataLength), result);
                Debug.Assert(decompressedLength == UncompressedDataLength);

                return result;
            }
        }
    }

    sealed class Arz
    {
        public required string Path { get; init; }
        public required MemoryMappedFile File { get; init; }
        public List<string> Strings { get; } = [];
        public Dictionary<string, ArzRecord> Records { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, Func<Task<(string path, byte[] data)>>> ArcFileReaders { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ArcFileHeader
    {
        public readonly int Magic;
        public readonly int Version;
        public readonly int FileCount;
        public readonly int FilePartCount;
        public readonly int FilePartSize;
        public readonly int StringTableSize;
        public readonly int FilePartOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ArcFilePart
    {
        public readonly int Offset;
        public readonly int CompressedSize;
        public readonly int UncompressedSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ArcToc
    {
        public readonly int Type;
        public readonly int Offset;
        public readonly int CompressedSize;
        public readonly int UncompressedSize;
        public readonly int Reserved;
        public readonly long FileTime;
        public readonly int FilePartCount;
        public readonly int Index;
        public readonly int StringTableSize;
        public readonly int StringTableOffset;
    }

    public ArzParserService()
    {
        arzFiles = Directory.EnumerateDirectories(BasePath, "gdx*").OrderByDescending(path => path)
            .Append(BasePath)
            .Select(dbPath => Path.Combine(dbPath, "database"))
            .SelectMany(arzPath => Directory.EnumerateFiles(arzPath, "*.arz", SearchOption.AllDirectories))
            .Select(arzPath => new Arz() { Path = arzPath, File = MemoryMappedFile.CreateFromFile(arzPath) })
            .ToArray();
        _ = InitializeAsync();
    }

    unsafe void ParseStringTables(Arz arz, ArzFileHeader* header, byte* ptr)
    {
        for (var stp = new ReadOnlySpan<byte>(ptr + header->StringTableStart, (int)header->StringTableSize); stp.Length > 0;)
        {
            var stringCount = BinaryPrimitives.ReadUInt32LittleEndian(stp); stp = stp[4..];

            while (stringCount-- > 0)
            {
                arz.Strings.Add(BinaryPrimitivesExtended.ReadGDStringLittleEndian(stp, out var byteLength));
                stp = stp[byteLength..];
            }
        }
    }

    unsafe void ParseRecords(Arz arz, ArzFileHeader* header, byte* ptr, IList<string> strings)
    {
        var rIdx = 0;
        for (var rp = ptr + header->RecordTableStart; rIdx < header->RecordTableEntryCount; ++rIdx)
        {
            var record = new ArzRecord(ptr, rp, header->RecordTableSize, strings);
            rp += record.TotalBytesRead;
            arz.Records[record.Name] = record;
        }
    }

    async Task ParseTags()
    {
        foreach (var (path, data) in await GetFileDataAsync(new Regex(@"^tags.*\.txt$", RegexOptions.IgnoreCase)).ConfigureAwait(false))
        {
            using var reader = new StreamReader(new MemoryStream(data));
            while (reader.ReadLine() is { } line)
                if (line.IndexOf('=') is { } eqIdx && eqIdx >= 0)
                {
                    var key = line[..eqIdx];
                    if (!tags.ContainsKey(key))
                        tags[key] = line[(eqIdx + 1)..];
                }
        }
    }

    static async Task<(string path, byte[] data)> CreateArcFileReader(string arc, ArcToc toc, ArcFilePart[] fileParts)
    {
        using var file = File.OpenHandle(arc, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
        if (toc.Type == 1 && toc.CompressedSize == toc.UncompressedSize)
        {
            var buffer = new byte[toc.UncompressedSize];
            await RandomAccess.ReadAsync(file, buffer, toc.Offset).ConfigureAwait(false);
            return (arc, buffer);
        }
        else
        {
            var fullUncompressedSize = fileParts.Sum(w => w.UncompressedSize);
            var buffer = new byte[fullUncompressedSize];
            var bufferWriteIndex = 0;

            foreach (var filePart in fileParts)
                if (filePart.UncompressedSize == filePart.CompressedSize)
                {
                    await RandomAccess.ReadAsync(file, buffer.AsMemory(bufferWriteIndex, filePart.UncompressedSize), filePart.Offset).ConfigureAwait(false);
                    bufferWriteIndex += filePart.UncompressedSize;
                }
                else
                {
                    var compressedBuffer = ArrayPool<byte>.Shared.Rent(filePart.CompressedSize);
                    await RandomAccess.ReadAsync(file, compressedBuffer.AsMemory(0, filePart.CompressedSize), filePart.Offset).ConfigureAwait(false);
                    var decompressedSize = LZ4Codec.Decode(compressedBuffer.AsSpan(0, filePart.CompressedSize), buffer.AsSpan(bufferWriteIndex));
                    ArrayPool<byte>.Shared.Return(compressedBuffer);
                    Debug.Assert(decompressedSize == filePart.UncompressedSize);

                    bufferWriteIndex += decompressedSize;
                }

            return (arc, buffer);
        }
    }

    async Task InitializeAsync()
    {
        await Task.WhenAll(arzFiles.Select(async arz =>
            await Task.WhenAll(
                Directory.EnumerateFiles(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(arz.Path)!, "../resources")), "*.arc", SearchOption.AllDirectories)
                    .Select(async arc =>
                    {
                        // don't parse localization files
                        if (Regex.IsMatch(Path.GetFileName(arc), @"^text_(?:[^e]\w|e[^n])\.arc$", RegexOptions.IgnoreCase))
                            return;

                        // parse each arc file
                        unsafe
                        {
                            using var mmf = MemoryMappedFile.CreateFromFile(arc);
                            using var accessor = mmf.CreateViewAccessor();

                            byte* ptr = default;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                            var originalPtr = ptr;

                            // header
                            var header = (ArcFileHeader*)ptr;
                            var fileCount = Math.Min(header->FileCount, header->FilePartCount);
                            Debug.Assert(header->Version == 3);

                            // file parts
                            ptr = originalPtr + header->FilePartOffset;
                            var fileParts = new ArcFilePart*[header->FilePartCount];
                            for (var i = 0; i < fileParts.Length; ++i)
                            {
                                fileParts[i] = (ArcFilePart*)ptr;
                                ptr += Unsafe.SizeOf<ArcFilePart>();
                            }

                            // strings
                            var fileNames = new string[fileCount];
                            for (var i = 0; i < fileNames.Length; ++i)
                            {
                                fileNames[i] = BinaryPrimitivesExtended.ReadZeroTerminatedString(new(ptr, (int)(accessor.Capacity - (ptr - originalPtr))), out var byteLength);
                                ptr += byteLength + 1;
                            }

                            // toc
                            for (var i = 0; i < fileCount; ++i)
                            {
                                var toc = *(ArcToc*)ptr;
                                ptr += Unsafe.SizeOf<ArcToc>();

                                var filePartValues = new ArcFilePart[toc.FilePartCount];
                                for (var filePartIdx = 0; filePartIdx < toc.FilePartCount; ++filePartIdx)
                                    filePartValues[filePartIdx] = *fileParts[toc.Index + filePartIdx];

                                arz.ArcFileReaders[fileNames[i]] = () => CreateArcFileReader(arc, toc, filePartValues);
                            }

                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    })
                    .Append(Task.Run(() =>
                    {
                        // parse the arz
                        unsafe
                        {
                            using var accessor = arz.File.CreateViewAccessor();
                            byte* ptr = default;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                            var header = (ArzFileHeader*)ptr;
                            Debug.Assert(header->Magic == 2 && header->Version == 3);

                            // read string table
                            ParseStringTables(arz, header, ptr);

                            // read records
                            ParseRecords(arz, header, ptr, arz.Strings);
                        }
                    }))))).ConfigureAwait(false);

        await ParseTags().ConfigureAwait(false);

        readyEvent.Set();
    }

    public async ValueTask EnsureLoadedAsync() => await readyEvent.WaitAsync().ConfigureAwait(false);

    public DbrData? GetDbrData(string path)
    {
        foreach (var arz in arzFiles)
            if (arz.Records.TryGetValue(path, out var record))
                return new(path, record.Data, arz.Strings);

        return null;
    }

    public IList<DbrData> GetDbrData(Regex regex)
    {
        var keys = arzFiles.SelectMany(arz => arz.Records.Keys.Where(key => regex.IsMatch(key))).ToHashSet();
        return keys.Select(key => GetDbrData(key)!).ToList();
    }

    public async Task<(string path, byte[] data)> GetFileDataAsync(string path)
    {
        foreach (var arz in arzFiles)
            if (arz.ArcFileReaders.TryGetValue(path, out var reader))
                return await reader().ConfigureAwait(false);

        return default;
    }

    public async Task<IList<(string path, byte[] data)>> GetFileDataAsync(Regex regex)
    {
        var files = arzFiles.SelectMany(arz => arz.ArcFileReaders.Keys.Where(key => regex.IsMatch(key))).ToHashSet();
        return await Task.WhenAll(files.Select(key => GetFileDataAsync(key)!)).ConfigureAwait(false);
    }

    public string? GetTag(string tag) => tags.TryGetValue(tag, out var value) ? value : null;
}
