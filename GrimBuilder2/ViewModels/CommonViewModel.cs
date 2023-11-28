using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Models;
using GrimBuilder2.Services;
using ReactiveUI;

namespace GrimBuilder2.ViewModels;

public partial class CommonViewModel : ObservableRecipient
{
    private readonly GdService gdService;
    private readonly DialogService dialogService;

    [ObservableProperty]
    IList<GdClass>? classes;

    [ObservableProperty]
    IDictionary<(GdClass?, GdClass?), string?>? classCombinations;

    [ObservableProperty]
    IList<GdConstellation>? constellations;

    [ObservableProperty]
    IList<GdAssignableSkill>? assignableConstellationSkills;

    [ObservableProperty]
    IList<GdAssignableEquipSlot>? equipSlots;

    [ObservableProperty]
    IList<GdNebula>? nebulas;

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
                    thisSkill.IsMasteryValid = assignableClass.MasterySkill is not null
                        && thisSkill.MasteryLevelRequirement <= assignableClass.MasterySkill.AssignedPoints;
                    thisSkill.IsDependencyValid = (dependencySkill is null || dependencySkill.AssignedPoints > 0);
                }
            }
        }
    }

    partial void OnSelectedRawClass1Changed(GdClass? value) =>
        OnSelectedRawClass(value, x => SelectedAssignableClass1 = x);

    partial void OnSelectedRawClass2Changed(GdClass? value) =>
        OnSelectedRawClass(value, x => SelectedAssignableClass2 = x);

    public CommonViewModel(GdService gdService, DialogService dialogService)
    {
        this.gdService = gdService;
        this.dialogService = dialogService;

        _ = InitializeAsync();
    }

    async Task InitializeAsync()
    {
        var (classResults, devotions, equipSlots) = await TaskExtended.WhenAll(
            gdService.GetClassesAsync(),
            gdService.GetDevotionsAsync(),
            gdService.GetEquipSlotsAsync());

        Classes = classResults.Classes;
        ClassCombinations = classResults.ClassCombinations;

        Constellations = devotions.constellations;
        AssignableConstellationSkills = devotions.constellations.SelectMany(c => c.Skills)
            .Select(App.Mapper.Map<GdAssignableSkill>)
            .ToArray();
        Nebulas = devotions.nebulas;
        EquipSlots = equipSlots.Select(App.Mapper.Map<GdAssignableEquipSlot>).ToArray();

        await Open();
    }

    [RelayCommand]
    async Task Open()
    {
        if (await dialogService.OpenCharacterAsync() is not { } result) return;

        // clear all the assigned points
        SelectedRawClass1 = SelectedRawClass2 = null;

        // set the classes
        SelectedRawClass1 = Classes!.FirstOrDefault(w => w.Index == result.ClassIndex1);
        SelectedRawClass2 = Classes!.FirstOrDefault(w => w.Index == result.ClassIndex2);

        // set the masteries
        IEnumerable<GdAssignableSkill> allSkills = AssignableConstellationSkills!;
        if (SelectedAssignableClass1?.AssignableSkills is not null)
            allSkills = allSkills.Concat(SelectedAssignableClass1.AssignableSkills);
        if (SelectedAssignableClass2?.AssignableSkills is not null)
            allSkills = allSkills.Concat(SelectedAssignableClass2.AssignableSkills);
        foreach (var skill in allSkills)
            skill.AssignedPoints = result.Skills.FirstOrDefault(s => s.Name == skill.InternalName)?.Level ?? 0;
    }

    public string? GetClassCombinationName(GdClass? c1, GdClass? c2) => ClassCombinations?[(c1, c2)];
}
