namespace GrimBuilder2.Core.Models;

public enum GdItemType
{
    Feet, Hands, Head, Legs, Relic, Shoulders, Chest,
    WeaponOneHandedAxe, WeaponOneHandedSword, WeaponOneHandedMace, WeaponOneHandedGun, WeaponDagger, WeaponScepter,
    WeaponTwoHandedAxe, WeaponTwoHandedSword, WeaponTwoHandedMace, WeaponTwoHandedGun, WeaponCrossbow,
    OffhandFocus, Shield,
    Medal, Amulet, Ring, Belt,

    SuperWeapon,
    SuperOneHandedWeapon, SuperOneHandedMeleeWeapon, SuperOffhandWeapon, SuperOneHandedRangedWeapon,
    SuperTwoHandedWeapon, SuperTwoHandedMeleeWeapon, SuperTwoHandedRangedWeapon,
}

public class GdItem
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? DbrPath { get; set; }
    public GdItemType Type { get; set; }
    public string? BitmapPath { get; set; }
}
