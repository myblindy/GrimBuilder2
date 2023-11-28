namespace GrimBuilder2.Core.Models;

public enum EquipSlotType
{
    Artifact,
    Chest,
    Feet,
    Finger1,
    Finger2,
    HandLeft,
    HandRight,
    Hands,
    Head,
    Legs,
    Medal,
    Neck,
    Shoulders,
    Waist,
}

public class GdEquipSlot
{
    public EquipSlotType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string SilhouetteBitmapPath { get; set; } = null!;
}
