using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GrimBuilder2.Core.Models;

public class DbrData
{
    public string Path { get; }

    readonly byte[] data;
    readonly IReadOnlyList<string> strings;
    readonly Dictionary<string, (DbrValueType type, int offset, int count)> entries = [];

    public DbrData(string path, byte[] data, IReadOnlyList<string> strings)
    {
        this.data = data;
        this.strings = strings;
        Path = path;

        ReadOnlySpan<byte> dataSpan = data;
        var offset = 0;
        while (!dataSpan.IsEmpty)
        {
            var type = (DbrValueType)BinaryPrimitives.ReadUInt16LittleEndian(dataSpan);
            var entriesCount = BinaryPrimitives.ReadUInt16LittleEndian(dataSpan[2..]);
            var key = strings[BinaryPrimitives.ReadInt32LittleEndian(dataSpan[4..])];

            // store the offset to the beginning of the data entries
            entries[key] = (type, offset + 8, entriesCount);

            // all data entries are 4 bytes long
            var entryLength = 8 + entriesCount * 4;
            offset += entryLength;
            dataSpan = dataSpan[entryLength..];
        }
    }

    public IEnumerable<string> Keys => entries.Keys;

    public DbrValueType GetValueType(string key) =>
        entries.TryGetValue(key, out var entry) ? entry.type : throw new InvalidOperationException();

    public bool GetBooleanValue(string key) =>
        GetIntegerValue(key) != 0;

    public int GetIntegerValue(string key) =>
        TryGetIntegerValue(key, out var value) ? value : throw new InvalidOperationException();

    public bool TryGetIntegerValue(string key, out int value)
    {
        if (entries.TryGetValue(key, out var entry) && entry.type is DbrValueType.Integer or DbrValueType.Boolean)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(entry.offset));
            return true;
        }
        value = default;
        return false;
    }

    public int[] GetIntegerValues(string key) =>
        TryGetIntegerValues(key, out var values) ? values : throw new InvalidOperationException();

    public bool TryGetIntegerValues(string key, [NotNullWhen(true)] out int[] values)
    {
        if (entries.TryGetValue(key, out var entry) && entry.type == DbrValueType.Integer)
        {
            values = new int[entry.count];
            for (var i = 0; i < entry.count; ++i)
                values[i] = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(entry.offset + i * 4));
            return true;
        }

        values = [];
        return false;
    }

    public string GetStringValue(string key) =>
        TryGetStringValue(key, out var value) ? value : throw new InvalidOperationException();

    public bool TryGetStringValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (entries.TryGetValue(key, out var entry) && entry.type == DbrValueType.String)
        {
            value = strings[BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(entry.offset))];
            return true;
        }
        value = default;
        return false;
    }

    public string[] GetStringValues(string key) =>
        TryGetStringValues(key, out var values) ? values : throw new InvalidOperationException();

    public bool TryGetStringValues(string key, [NotNullWhen(true)] out string[]? values)
    {
        if (entries.TryGetValue(key, out var entry) && entry.type == DbrValueType.String)
        {
            values = new string[entry.count];
            for (var i = 0; i < entry.count; ++i)
                values[i] = strings[BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(entry.offset + i * 4))];
            return true;
        }

        values = [];
        return false;
    }

    public float[] GetFloatValues(string key) =>
        TryGetFloatValues(key, out var values) ? values : throw new InvalidOperationException();

    public bool TryGetFloatValues(string key, [NotNullWhen(true)] out float[] values)
    {
        if (entries.TryGetValue(key, out var entry) && entry.type == DbrValueType.Single)
        {
            values = new float[entry.count];
            for (var i = 0; i < entry.count; ++i)
                values[i] = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(entry.offset + i * 4));
            return true;
        }

        values = [];
        return false;
    }
}

public enum DbrValueType { Integer, Single, String, Boolean }