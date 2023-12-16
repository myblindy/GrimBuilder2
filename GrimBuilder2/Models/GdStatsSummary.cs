using GrimBuilder2.ViewModels;

namespace GrimBuilder2.Models;

public class GdStatsSummary
{
    public float Physique { get; private set; } = 50;
    public float Cunning { get; private set; } = 50;
    public float Spirit { get; private set; } = 50;

    public GdStatsSummary(CharacterViewModel character)
    {
        float physiqueModifier = 0, cunningModifier = 0, spiritModifier = 0;
        foreach (var equipSlot in character.EquipSlots!)
            if (equipSlot.Item is { } item)
            {
                Physique += item.Stats.Physique;
                physiqueModifier += item.Stats.PhysiqueModifier;
                Cunning += item.Stats.Cunning;
                cunningModifier += item.Stats.CunningModifier;
                Spirit += item.Stats.Spirit;
                spiritModifier += item.Stats.SpiritModifier;
            }

        Physique *= 1 + physiqueModifier / 100;
        Cunning *= 1 + cunningModifier / 100;
        Spirit *= 1 + spiritModifier / 100;
    }
}
