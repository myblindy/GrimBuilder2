namespace GrimBuilder2.Core.Models;

public class GdClass
{
    public GdClass() { }

    public string Name { get; set; } = null!;
    public int Index { get; set; }
    public string BitmapPath { get; set; } = null!;
    public List<GdSkill> Skills { get; } = [];
}
