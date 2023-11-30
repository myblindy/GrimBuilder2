namespace GrimBuilder2.Core.Models;

public class GdItem
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? DbrPath { get; set; }
    public GdItemType Type { get; set; }
    public GdItemRarity Rarity { get; set; }
    public string? BitmapPath { get; set; }
}

[Flags]
public enum GdItemType
{
    Feet = 1 << 0,
    Hands = 1 << 1,
    Head = 1 << 2,
    Legs = 1 << 3,
    Relic = 1 << 4,
    Shoulders = 1 << 5,
    Chest = 1 << 6,
    WeaponOneHandedAxe = 1 << 7,
    WeaponOneHandedSword = 1 << 8,
    WeaponOneHandedMace = 1 << 9,
    WeaponOneHandedGun = 1 << 10,
    WeaponDagger = 1 << 11,
    WeaponScepter = 1 << 12,
    WeaponTwoHandedAxe = 1 << 13,
    WeaponTwoHandedSword = 1 << 14,
    WeaponTwoHandedMace = 1 << 15,
    WeaponTwoHandedGun = 1 << 16,
    WeaponCrossbow = 1 << 17,
    OffhandFocus = 1 << 18,
    Shield = 1 << 19,
    Medal = 1 << 20,
    Amulet = 1 << 21,
    Ring = 1 << 22,
    Belt = 1 << 23,

    SuperWeapon = SuperOneHandedWeapon | SuperTwoHandedWeapon | SuperOffhandWeapon,
    SuperOneHandedWeapon = SuperOneHandedMeleeWeapon | SuperOneHandedRangedWeapon,
    SuperOneHandedMeleeWeapon = WeaponOneHandedAxe | WeaponOneHandedMace | WeaponOneHandedSword | WeaponScepter | WeaponDagger,
    SuperOffhandWeapon = OffhandFocus | Shield,
    SuperOneHandedRangedWeapon = WeaponOneHandedGun,
    SuperTwoHandedWeapon = SuperTwoHandedMeleeWeapon | SuperTwoHandedRangedWeapon,
    SuperTwoHandedMeleeWeapon = WeaponTwoHandedAxe | WeaponTwoHandedMace | WeaponTwoHandedSword,
    SuperTwoHandedRangedWeapon = WeaponTwoHandedGun | WeaponCrossbow,
}

public enum GdItemRarity
{
    Broken, Common, Magical, Rare, Epic, Legendary, Quest,
    Artifact, ArtifactFormula, Enchantment, Relic,
}
