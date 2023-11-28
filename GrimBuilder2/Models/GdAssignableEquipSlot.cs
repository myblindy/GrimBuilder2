using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableEquipSlot : GdEquipSlot
{
    [ObservableProperty]
    GdItem? item;
}
