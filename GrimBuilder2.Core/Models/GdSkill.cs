using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Core.Models;
public class GdSkill
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; } = null!;
    public int Tier { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public bool Circular { get; init; }
    public int MaximumLevel { get; init; }
    public int? UltimateLevel { get; init; }
    public int? MasteryLevelRequirement { get; init; }

    public GdSkill? Dependency { get; internal set; }

    public string? BitmapUpPath { get; init; } = null!;
    public string? BitmapDownPath { get; init; } = null!;

    public string? BitmapFrameUpPath { get; init; }
    public string? BitmapFrameDownPath { get; init; }
    public string? BitmapFrameInFocusPath { get; init; }

    public bool True { get; } = true;
}
