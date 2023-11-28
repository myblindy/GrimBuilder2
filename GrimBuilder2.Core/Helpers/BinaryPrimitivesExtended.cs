using System.Buffers.Binary;
using System.Text;

namespace GrimBuilder2.Core.Helpers;

public static class BinaryPrimitivesExtended
{
    public static string ReadGDStringLittleEndian(this ReadOnlySpan<byte> span, out int byteLength)
    {
        var length = BinaryPrimitives.ReadInt32LittleEndian(span);
        byteLength = length + 4;
        return Encoding.ASCII.GetString(span[4..(4 + length)]);
    }

    public static string ReadZeroTerminatedString(this ReadOnlySpan<byte> span, out int byteLength)
    {
        byteLength = span.IndexOf((byte)0);
        return Encoding.ASCII.GetString(span[..byteLength]);
    }
}
