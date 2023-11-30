using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GrimBuilder2.ViewModels;

public partial class InstanceViewModel : ObservableObject
{
    [ObservableProperty]
    string gdPath = @"D:\Program Files (x86)\Steam\steamapps\common\Grim Dawn";

    [ObservableProperty]
    string gdSavePath = @"C:\Users\tweet\Documents\My Games\Grim Dawn\save";
}
