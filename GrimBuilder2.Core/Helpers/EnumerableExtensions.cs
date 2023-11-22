using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Core.Helpers;

public static class EnumerableExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
    {
        foreach (var value in values)
            list.Add(value);
    }
}
