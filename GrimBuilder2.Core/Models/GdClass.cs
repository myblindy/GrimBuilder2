using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Core.Models;

public class GdClass(string name, string bitmapPath)
{
    public GdClass() : this(null!, null!) { }

    public string Name { get; } = name;
    public string BitmapPath { get; } = bitmapPath;
    public List<GdSkill> Skills { get; } = [];
}
