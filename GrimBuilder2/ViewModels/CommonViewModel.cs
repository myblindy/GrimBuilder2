using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Services;
using GrimBuilder2.Models;
using ReactiveUI;

namespace GrimBuilder2.ViewModels;

public partial class CommonViewModel : ObservableRecipient
{
    private readonly GdService gdService;

    [ObservableProperty]
    IList<GdClass>? classes;

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

    public CommonViewModel(GdService gdService)
    {
        this.gdService = gdService;

        _ = InitializeAsync();
    }

    async Task InitializeAsync()
    {
        var (classes, devotions, equipSlots) = await TaskExtended.WhenAll(
            gdService.GetClassesAsync(),
            gdService.GetDevotionsAsync(),
            gdService.GetEquipSlotsAsync());

        Classes = classes;

        Constellations = devotions.constellations;
        AssignableConstellationSkills = devotions.constellations.SelectMany(c => c.Skills)
            .Select(App.Mapper.Map<GdAssignableSkill>)
            .ToArray();
        Nebulas = devotions.nebulas;
        EquipSlots = equipSlots.Select(App.Mapper.Map<GdAssignableEquipSlot>).ToArray();

        await LoadSaveFile(@"C:\Users\tweet\Documents\My Games\Grim Dawn\save\main\_Red Hot JiU");
    }

    [RelayCommand]
    async Task LoadSaveFile(string path)
    {
        // clear all the assigned points
        SelectedRawClass1 = SelectedRawClass2 = null;

        // load the save file
        var saveData = await Task.Run(() => gdService.ParseSaveFile(path));

        // set the classes
        SelectedRawClass1 = Classes!.First(w => w.Index == saveData.ClassIndex1);
        SelectedRawClass2 = Classes!.First(w => w.Index == saveData.ClassIndex2);

        // set the masteries
        foreach (var skill in SelectedAssignableClass1!.AssignableSkills!.Concat(SelectedAssignableClass2!.AssignableSkills!).Concat(AssignableConstellationSkills!))
            skill.AssignedPoints = saveData.Skills.FirstOrDefault(s => s.Name == skill.InternalName)?.Level ?? 0;
    }
}
