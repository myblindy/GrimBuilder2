using CommunityToolkit.Mvvm.ComponentModel;

namespace GrimBuilder2.ViewModels;

public partial class GlobalViewModel : ObservableObject
{
    [ObservableProperty]
    string gdPath = @"D:\Program Files (x86)\Steam\steamapps\common\Grim Dawn";

    [ObservableProperty]
    string gdSavePath = @"C:\Users\tweet\Documents\My Games\Grim Dawn\save";

}
