using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Models.SavedFile;
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
    IList<GdItem>? items;

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
        var (classResults, devotions, equipSlots, items) = await TaskExtended.WhenAll(
            Task.Run(() => gdService.GetClassesAsync()),
            Task.Run(() => gdService.GetDevotionsAsync()),
            Task.Run(() => gdService.GetEquipSlotsAsync()),
            Task.Run(() => gdService.GetItemsAsync()));

        Classes = classResults.Classes;
        ClassCombinations = classResults.ClassCombinations;

        Constellations = devotions.constellations;
        AssignableConstellationSkills = devotions.constellations.SelectMany(c => c.Skills)
            .Select(App.Mapper.Map<GdAssignableSkill>)
            .ToArray();
        Nebulas = devotions.nebulas;
        EquipSlots = equipSlots.Select(App.Mapper.Map<GdAssignableEquipSlot>).ToArray();
        Items = items;

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

        // set the equipped items
        for (var slotIndex = 0; slotIndex < EquipSlots!.Count; ++slotIndex)
        {
            var savedItem = result.Items[EquipSlots[slotIndex].Type switch
            {
                EquipSlotType.Artifact => 11,
                EquipSlotType.Chest => 2,
                EquipSlotType.Feet => 4,
                EquipSlotType.Finger1 => 6,
                EquipSlotType.Finger2 => 7,
                EquipSlotType.HandLeft => 13,
                EquipSlotType.HandRight => 12,
                EquipSlotType.Hands => 5,
                EquipSlotType.Head => 0,
                EquipSlotType.Legs => 3,
                EquipSlotType.Medal => 10,
                EquipSlotType.Neck => 1,
                EquipSlotType.Shoulders => 9,
                EquipSlotType.Waist => 8,
                _ => throw new NotImplementedException(),
            }];
            EquipSlots[slotIndex].Item = savedItem is null ? null : BuildItem(savedItem);
        }
    }

    public string? GetClassCombinationName(GdClass? c1, GdClass? c2) => ClassCombinations?[(c1, c2)];

    GdItem? BuildItem(GdsItem savedItem) => Items!.FirstOrDefault(i => i.DbrPath == savedItem.NameFile);
}
