namespace GrimBuilder2.Core.Models.SavedFile;

public record GdsCharacter(string Name, int ClassIndex1, int ClassIndex2, int Level, GdsSkill[] Skills, GdsItem[] Items)
{
    public static GdsCharacter Empty { get; } = new("Empty", 0, 0, 0, [], new GdsItem[16]);
}
