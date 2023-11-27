using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;
using ReactiveUI;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableClass : GdClass
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterySkill))]
    GdAssignableSkill[]? assignableSkills;

    public GdAssignableSkill? MasterySkill => AssignableSkills?[0];
}
