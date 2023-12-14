
namespace GrimBuilder2.Core.Models;

public partial class GdStats
{
    [GdStat("characterLife", "{0} Health")]
    public float Health { get; set; }
    [GdStat("characterLifeModifier", "{0}% Health")]
    public float HealthModifier { get; set; }
    [GdStat("characterLifeRegen", "{0} Life Regen")]
    public float HealthRegen { get; set; }
    [GdStat("characterLifeRegenModifier", "{0}% Life Regen")]
    public float HealthRegenModifier { get; set; }
    [GdStat("characterMana", "{0} Energy")]
    public float Energy { get; set; }
    [GdStat("characterManaModifier", "{0}% Energy")]
    public float EnergyModifier { get; set; }
    [GdStat("characterManaRegen", "{0} Energy Regen")]
    public float EnergyRegen { get; set; }
    [GdStat("characterManaRegenModifier", "{0}% Energy Regen")]
    public float EnergyRegenModifier { get; set; }
    [GdStat("characterStrength", "{0} Physique")]
    public float Physique { get; set; }
    [GdStat("characterStrengthModifier", "{0}% Physique")]
    public float PhysiqueModifier { get; set; }
    [GdStat("characterDexterity", "{0} Cunning")]
    public float Cunning { get; set; }
    [GdStat("characterDexterityModifier", "{0}% Cunning")]
    public float CunningModifier { get; set; }
    [GdStat("characterIntelligence", "{0} Spirit")]
    public float Spirit { get; set; }
    [GdStat("characterIntelligenceModifier", "{0}% Spirit")]
    public float SpiritModifier { get; set; }
    [GdStat("defensiveProtection", "{0} Armor")]
    public float Armor { get; set; }
    [GdStat("defensiveProtectionModifier", "{0}% Armor")]
    public float ArmorModifier { get; set; }
    [GdStat("defensiveAbsorptionModifier", "{0}% Armor Absorption")]
    public float ArmorAbsorptionModifier { get; set; }
    [GdStat("defensiveBleeding", "{0}% Bleed Resistance")]
    public float ResistBleed { get; set; }
    [GdStat("defensiveBleedingMaxResist", "{0}% Max Bleed Resistance")]
    public float MaxResistBleed { get; set; }
    [GdStat("defensiveFire", "{0}% Fire Resistance")]
    public float ResistFire { get; set; }
    [GdStat("defensiveFireMaxResist", "{0}% Max Fire Resistance")]
    public float MaxResistFire { get; set; }
    [GdStat("defensiveCold", "{0}% Cold Resistance")]
    public float ResistCold { get; set; }
    [GdStat("defensiveColdMaxResist", "{0}% Max Cold Resistance")]
    public float MaxResistCold { get; set; }
    [GdStat("defensiveLightning", "{0}% Lightning Resistance")]
    public float ResistLightning { get; set; }
    [GdStat("defensiveLightningMaxResist", "{0}% Max Lightning Resistance")]
    public float MaxResistLightning { get; set; }
    [GdStat("defensiveAether", "{0}% Aether Resistance")]
    public float ResistAether { get; set; }
    [GdStat("defensiveAetherMaxResist", "{0}% Max Aether Resistance")]
    public float MaxResistAether { get; set; }
    [GdStat("defensiveChaos", "{0}% Chaos Resistance")]
    public float ResistChaos { get; set; }
    [GdStat("defensiveChaosMaxResist", "{0}% Max Chaos Resistance")]
    public float MaxResistChaos { get; set; }
    [GdStat("defensiveElemental", "{0}% Elemental Resistance")]
    public float ResistElemental { get; set; }
    [GdStat("defensiveKnockdown", "{0}% Knockdown Resistance")]
    public float ResistKnockdown { get; set; }
    [GdStat("defensiveKnockdownMaxResist", "{0}% Max Knockdown Resistance")]
    public float MaxResistKnockdown { get; set; }
    [GdStat("defensiveLife", "{0}% Vitality Resistance")]
    public float ResistVitality { get; set; }
    [GdStat("defensiveLifeMaxResist", "{0}% Max Vitality Resistance")]
    public float MaxResistVitality { get; set; }
    [GdStat("defensiveStun", "{0}% Stun Resistance")]
    public float ResistStun { get; set; }
    [GdStat("defensiveStunMaxResist", "{0}% Max Stun Resistance")]
    public float MaxResistStun { get; set; }
    [GdStat("defensiveTotalSpeedResistance", "{0}% Slow Resistance")]
    public float ResistSlow { get; set; }
    [GdStat("defensiveTotalSpeedResistanceMaxResist", "{0}% Max Slow Resistance")]
    public float MaxResistSlow { get; set; }
    [GdStat("defensivePhysical", "{0}% Physical Resistance")]
    public float ResistPhysical { get; set; }
    [GdStat("defensivePhysicalMaxResist", "{0}% Max Physical Resistance")]
    public float MaxResistPhysical { get; set; }
    [GdStat("defensivePierce", "{0}% Pierce Resistance")]
    public float ResistPierce { get; set; }
    [GdStat("defensivePierceMaxResist", "{0}% Max Pierce Resistance")]
    public float MaxResistPierce { get; set; }
    [GdStat("defensivePoison", "{0}% Poison Resistance")]
    public float ResistPoison { get; set; }
    [GdStat("defensivePoisonMaxResist", "{0}% Max Poison Resistance")]
    public float MaxResistPoison { get; set; }
    [GdStat("defensiveDisruption", "{0}% Disruption Resistance")]
    public float ResistDisruption { get; set; }
    [GdStat("defensiveDisruptionMaxResist", "{0}% Max Disruption Resistance")]
    public float MaxResistDisruption { get; set; }
}