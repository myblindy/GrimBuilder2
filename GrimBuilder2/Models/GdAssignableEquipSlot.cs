using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableEquipSlot : GdEquipSlot
{
    [ObservableProperty]
    GdItem? item;
}
