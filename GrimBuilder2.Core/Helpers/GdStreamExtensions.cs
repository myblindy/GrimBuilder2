using System.Runtime.CompilerServices;
using System.Text;

namespace GrimBuilder2.Core.Helpers;
public static class GdStreamExtensions
{
    public static void ReadEncryptionKey(this BinaryReader binaryReader, GdEncryptionState state)
    {
        var key = binaryReader.ReadUInt32() ^ 0x55555555;
        state.BuildKey(key);
    }

    public static uint ReadEncUInt32(this BinaryReader binaryReader, GdEncryptionState state, bool updateKey = true) =>
        (uint)ReadEncInt32(binaryReader, state, updateKey);

    public static int ReadEncInt32(this BinaryReader binaryReader, GdEncryptionState state, bool updateKey = true)
    {
        var value = binaryReader.ReadUInt32();
        var result = value ^ state.Key;
        if (updateKey)
            state.UpdateKey(value);

        return (int)result;
    }

    public static float ReadEncSingle(this BinaryReader binaryReader, GdEncryptionState state, bool updateKey = true) =>
        BitConverter.Int32BitsToSingle(binaryReader.ReadEncInt32(state, updateKey));

    public static byte ReadEncUInt8(this BinaryReader binaryReader, GdEncryptionState state, bool updateKey = true)
    {
        var value = binaryReader.ReadByte();
        var result = (byte)(value ^ (byte)state.Key);
        if (updateKey)
            state.UpdateKey(value);

        return result;
    }

    public static unsafe string ReadEncString(this BinaryReader binaryReader, GdEncryptionState state)
    {
        var length = ReadEncInt32(binaryReader, state);

        Span<byte> buffer = stackalloc byte[length];
        binaryReader.ReadEnc(state, buffer);
        return Encoding.ASCII.GetString(buffer);
    }

    public static unsafe string ReadEncWideString(this BinaryReader binaryReader, GdEncryptionState state)
    {
        var length = ReadEncInt32(binaryReader, state);

        Span<byte> buffer = stackalloc byte[length * 2];
        binaryReader.ReadEnc(state, buffer);
        return Encoding.Unicode.GetString(buffer);
    }

    public static unsafe void ReadEnc(this BinaryReader binaryReader, GdEncryptionState state, Span<byte> buffer)
    {
        foreach (ref var b in buffer)
            b = ReadEncUInt8(binaryReader, state);
    }

    public static T[] ReadEncArray<T>(this BinaryReader binaryReader, GdEncryptionState state, Func<T> entryReader)
    {
        var result = new T[binaryReader.ReadEncInt32(state)];
        for (int i = 0; i < result.Length; ++i)
            result[i] = entryReader();
        return result;
    }
}

public class GdEncryptionState
{
    internal uint Key { get; set; }
    readonly uint[] table = new uint[256];

    internal void BuildKey(uint key)
    {
        Key = key;

        for (int i = 0; i < table.Length; ++i)
        {
            key = (key >> 1) | (key << 31);
            key *= 39916801;
            table[i] = key;
        }
    }

    internal void UpdateKey(byte[] b)
    {
        for (int i = 0; i < b.Length; ++i)
            Key ^= table[b[i]];
    }

    internal unsafe void UpdateKey<T>(T value) where T : unmanaged
    {
        var p = (byte*)&value;
        var pEnd = p + Unsafe.SizeOf<T>();
        for (; p < pEnd; ++p)
            Key ^= table[*p];
    }
}