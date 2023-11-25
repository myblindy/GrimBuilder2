using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Services;
using GrimBuilder2.Models;
using ReactiveUI;

namespace GrimBuilder2.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly GdService gdService;

    [ObservableProperty]
    IEnumerable<GdClass>? classes;

    [ObservableProperty]
    IEnumerable<GdConstellation>? constellations;

    [ObservableProperty]
    IEnumerable<GdAssignableSkill>? assignableConstellationSkills;

    [ObservableProperty]
    IEnumerable<GdNebula>? nebulas;

    [ObservableProperty]
    GdClass? selectedRawClass1, selectedRawClass2;

    [ObservableProperty]
    GdAssignableClass? selectedAssignableClass1, selectedAssignableClass2;

    static void OnSelectedRawClass(GdClass? @class, Action<GdAssignableClass?> setter)
    {
        var assignableClass = @class is null ? null : App.Mapper.Map<GdAssignableClass>(@class);
        setter(assignableClass);

        // hook up the active property logic in code
        if (assignableClass is not null)
        {
            foreach (var thisSkill in assignableClass.AssignableSkills!)
            {
                var dependencySkill = thisSkill.Dependency is null ? null : assignableClass.AssignableSkills!.First(x => x.Name == thisSkill.Dependency.Name);
                if (dependencySkill is not null)
                {
                    thisSkill.AssignableDependency = dependencySkill;
                    dependencySkill.WhenAnyValue(x => x.AssignedPoints).Subscribe(_ => setShouldBeActive());
                }
                assignableClass.WhenAnyValue(x => x.MasterySkill!.AssignedPoints).Subscribe(_ => setShouldBeActive());

                void setShouldBeActive()
                {
                    thisSkill.IsMasteryValid = assignableClass.MasterySkill is null ? false
                        : thisSkill.MasteryLevelRequirement <= assignableClass.MasterySkill.AssignedPoints;
                    thisSkill.IsDependencyValid = (dependencySkill is null || dependencySkill.AssignedPoints > 0);
                }
            }
        }
    }

    partial void OnSelectedRawClass1Changed(GdClass? value) =>
        OnSelectedRawClass(value, x => SelectedAssignableClass1 = x);

    partial void OnSelectedRawClass2Changed(GdClass? value) =>
        OnSelectedRawClass(value, x => SelectedAssignableClass2 = x);

    public MainViewModel(GdService gdService)
    {
        this.gdService = gdService;

        _ = InitializeAsync();
    }

    async Task InitializeAsync()
    {
        var (classes, devotions) = await TaskExtended.WhenAll(
            gdService.GetClassesAsync(),
            gdService.GetDevotionsAsync());

        Classes = classes;
        SelectedRawClass1 = Classes.First();
        SelectedRawClass2 = Classes.Skip(1).First();

        Constellations = devotions.constellations;
        AssignableConstellationSkills = devotions.constellations.SelectMany(c => c.Skills)
            .Select(App.Mapper.Map<GdAssignableSkill>);
        Nebulas = devotions.nebulas;
    }
}
