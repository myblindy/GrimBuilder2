using GrimBuilder2.Core.Helpers;

namespace GrimBuilder2.Core.Models.SavedFile;

public record class GdsItem(string NameFile, string PrefixFile, string SuffixFile, string Modifier, string Transmute,
    int Seed, string Relic, string RelicBonus, int RelicSeed, string Augment, int AugmentSeed, int StackCount);

public static class GdsItemReader
{
    public static GdsItem ReadItem(this BinaryReader binaryReader, bool isInventoryItem, GdEncryptionState encState)
    {
        var nameFile = binaryReader.ReadEncString(encState);
        var prefixFile = binaryReader.ReadEncString(encState);
        var suffixFile = binaryReader.ReadEncString(encState);
        var modifier = binaryReader.ReadEncString(encState);
        var transmute = binaryReader.ReadEncString(encState);
        var seed = binaryReader.ReadEncInt32(encState);
        var relic = binaryReader.ReadEncString(encState);
        var relicBonus = binaryReader.ReadEncString(encState);
        var relicSeed = binaryReader.ReadEncInt32(encState);
        var augment = binaryReader.ReadEncString(encState);
        binaryReader.ReadEncInt32(encState);
        var augmentSeed = binaryReader.ReadEncInt32(encState);
        binaryReader.ReadEncInt32(encState);
        var stackCount = binaryReader.ReadEncInt32(encState);

        if (isInventoryItem)
            binaryReader.ReadEncUInt8(encState);

        return new GdsItem(nameFile, prefixFile, suffixFile, modifier, transmute, seed, relic, relicBonus, relicSeed, augment, augmentSeed, stackCount);
    }
}
