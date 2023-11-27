namespace GrimBuilder2.Core.Models;
public class GdSkill
{
    public string Name { get; set; } = null!;
    public string InternalName { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int Tier { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Circular { get; set; }
    public int MaximumLevel { get; set; }
    public int? UltimateLevel { get; set; }

    static readonly int[] masteryTiers = [0, 1, 5, 10, 15, 20, 25, 32, 40, 50];
    public int? MasteryLevelRequirement => masteryTiers[Tier];

    public GdSkill? Dependency { get; internal set; }

    public string? BitmapUpPath { get; set; } = null!;
    public string? BitmapDownPath { get; set; } = null!;

    public string? BitmapFrameUpPath { get; set; }
    public string? BitmapFrameDownPath { get; set; }
    public string? BitmapFrameInFocusPath { get; set; }
}
