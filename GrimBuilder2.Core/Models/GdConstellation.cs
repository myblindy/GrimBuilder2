namespace GrimBuilder2.Core.Models;

public class GdConstellation
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string? BitmapPath { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    // skill requirements?
    public required GdSkill[] Skills { get; init; }
    public required (GdAffinity affinity, int quantity)[] RequiredAffinities { get; init; }
    public required (GdAffinity affinity, int quantity)[] GrantedAffinities { get; init; }
}
