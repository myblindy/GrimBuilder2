using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.ViewModels;

public partial class GlobalViewModel : ObservableObject
{
    [ObservableProperty]
    string gdPath = @"D:\Program Files (x86)\Steam\steamapps\common\Grim Dawn";

    [ObservableProperty]
    string gdSavePath = @"C:\Users\tweet\Documents\My Games\Grim Dawn\save";

}
