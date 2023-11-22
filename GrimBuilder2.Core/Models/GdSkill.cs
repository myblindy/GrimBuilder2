using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Core.Models;
public class GdSkill
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int Tier { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required bool Circular { get; init; }
    public required int MaximumLevel { get; init; }
    public required int? UltimateLevel { get; init; }
    public required int? MasteryLevelRequirement { get; init; }

    public GdSkill? Dependency { get; internal set; }

    public required string BitmapUpPath { get; init; }
    public required string BitmapDownPath { get; init; }

    public required string? BitmapFrameUpPath { get; init; }
    public required string? BitmapFrameDownPath { get; init; }
    public required string? BitmapFrameInFocusPath { get; init; }
}
