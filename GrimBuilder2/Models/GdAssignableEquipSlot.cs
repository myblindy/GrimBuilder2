using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableEquipSlot : GdEquipSlot
{
    [ObservableProperty]
    GdItem? item;

    public static ImageSource? GetRarityImageSource(GdItemRarity rarity) => rarity switch
    {
        GdItemRarity.Broken => null,
        GdItemRarity.Common => "/Assets/itembg_common.png",
        GdItemRarity.Magical => "/Assets/itembg_magical.png",
        GdItemRarity.Rare => "/Assets/itembg_rare.png",
        GdItemRarity.Epic => "/Assets/itembg_epic.png",
        GdItemRarity.Legendary => "/Assets/itembg_legendary.png",
        GdItemRarity.Relic => "/Assets/itembg_relic.png",
        GdItemRarity.Quest or GdItemRarity.Artifact or GdItemRarity.ArtifactFormula or GdItemRarity.Enchantment => null,
        _ => throw new NotImplementedException(),
    } is { } path && Uri.TryCreate($"ms-appx://{path}", UriKind.RelativeOrAbsolute, out var uri) ? new BitmapImage(uri) : null;
}
