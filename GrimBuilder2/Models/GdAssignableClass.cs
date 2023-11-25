using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;
using ReactiveUI;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableClass : GdClass
{
    [ObservableProperty]
    GdAssignableSkill[]? assignableSkills;

    [ObservableProperty]
    GdAssignableSkill? masterySkill;

    public GdAssignableClass() =>
        this.WhenAnyValue(x => x.AssignableSkills).Subscribe(_ => MasterySkill = AssignableSkills?[0]);
}
