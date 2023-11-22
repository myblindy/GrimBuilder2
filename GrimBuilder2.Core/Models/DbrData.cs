using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GrimBuilder2.Core.Models;

public class DbrData : IReadOnlyDictionary<string, DbrValues>
{
    public string Path { get; }

    readonly Dictionary<string, DbrValues> entries = [];

    public DbrData(string path, ReadOnlySpan<byte> data, IReadOnlyList<string> strings)
    {
        while (!data.IsEmpty)
        {
            var type = (DbrValueType)BinaryPrimitives.ReadUInt16LittleEndian(data);
            var entriesCount = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
            var key = strings[BinaryPrimitives.ReadInt32LittleEndian(data[4..])];
            data = data[8..];

            DbrValues values = new(key, type);
            while (entriesCount-- > 0)
            {
                switch (type)
                {
                    case DbrValueType.Integer:
                    case DbrValueType.Boolean:
                        values.IntegerValues!.Add(BinaryPrimitives.ReadInt32LittleEndian(data));
                        break;
                    case DbrValueType.Single:
                        values.SingleValues!.Add(BinaryPrimitives.ReadSingleLittleEndian(data));
                        break;
                    case DbrValueType.String:
                        values.StringValues!.Add(strings[BinaryPrimitives.ReadInt32LittleEndian(data)]);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                data = data[4..];
            }

            entries[key] = values;
        }
        Path = path;
    }

    #region IReadOnlyDictionary<string, DbrValues> implementation

    public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, DbrValues>)entries).Keys;

    public IEnumerable<DbrValues> Values => ((IReadOnlyDictionary<string, DbrValues>)entries).Values;

    public int Count => ((IReadOnlyCollection<KeyValuePair<string, DbrValues>>)entries).Count;

    public DbrValues this[string key] => ((IReadOnlyDictionary<string, DbrValues>)entries)[key];

    public bool ContainsKey(string key) => ((IReadOnlyDictionary<string, DbrValues>)entries).ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out DbrValues value) => ((IReadOnlyDictionary<string, DbrValues>)entries).TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<string, DbrValues>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, DbrValues>>)entries).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries).GetEnumerator();

    #endregion
}

public enum DbrValueType { Integer, Single, String, Boolean }

public readonly struct DbrValues
{
    public DbrValues(string key, DbrValueType type)
    {
        Key = key;
        Type = type;

        switch (Type)
        {
            case DbrValueType.Integer:
            case DbrValueType.Boolean:
                IntegerValues = [];
                break;
            case DbrValueType.Single:
                SingleValues = [];
                break;
            case DbrValueType.String:
                StringValues = [];
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public List<int>? IntegerValues { get; }
    public int IntegerValueUnsafe => IntegerValues![0];
    public bool BooleanValueUnsafe => IntegerValueUnsafe != 0;
    public List<float>? SingleValues { get; }
    public float SingleValueUnsafe => SingleValues![0];
    public List<string>? StringValues { get; }
    public string StringValueUnsafe => StringValues![0];
    public string Key { get; }
    public DbrValueType Type { get; }
}