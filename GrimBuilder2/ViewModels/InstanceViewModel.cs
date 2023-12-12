using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Models.SavedFile;
using GrimBuilder2.Models;
using GrimBuilder2.Services;
using Nito.AsyncEx;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace GrimBuilder2.ViewModels;

public partial class InstanceViewModel : ObservableObject
{
    readonly GdService gdService;
    readonly DialogService dialogService;

    public AsyncManualResetEvent InitializeFinishedEvent { get; } = new();

    [ObservableProperty]
    IList<GdClass>? classes;

    [ObservableProperty]
    IDictionary<(GdClass?, GdClass?), string?>? classCombinations;

    [ObservableProperty]
    IList<GdConstellation>? constellations;

    [ObservableProperty]
    IList<GdNebula>? nebulas;

    [ObservableProperty]
    IList<GdEquipSlot>? equipSlots;

    [ObservableProperty]
    IList<GdItem>? items, prefixes, suffixes;

    public ObservableCollection<CharacterViewModel> Characters { get; } = [];

    [ObservableProperty]
    CharacterViewModel? selectedCharacter;

    [ObservableProperty]
    bool loadFinished;

    public InstanceViewModel(GdService gdService, DialogService dialogService)
    {
        this.gdService = gdService;
        this.dialogService = dialogService;
        _ = InitializeAsync();

        // workaround for WinUI bindings breaking on nulls
        this.WhenAnyValue(x => x.SelectedCharacter!.LoadFinished).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(loadFinished => LoadFinished = loadFinished);
    }

    async Task InitializeAsync()
    {
        var (classResults, devotions, equipSlots, allItems) = await TaskExtended.WhenAll(
            Task.Run(() => gdService.GetClassesAsync()),
            Task.Run(() => gdService.GetDevotionsAsync()),
            Task.Run(() => gdService.GetEquipSlotsAsync()),
            Task.Run(() => gdService.GetItemsAsync()));

        Classes = classResults.Classes;
        ClassCombinations = classResults.ClassCombinations;

        Constellations = devotions.constellations;
        Nebulas = devotions.nebulas;

        EquipSlots = equipSlots;
        (Items, Prefixes, Suffixes) = (allItems.items, allItems.prefixes, allItems.suffixes);

        InitializeFinishedEvent.Set();

        _ = OpenAsync();
    }

    public string? GetClassCombinationName(GdClass? c1, GdClass? c2) => ClassCombinations?[(c1, c2)];

    public GdItem? BuildItem(GdsItem savedItem)
    {
        if (Items!.FirstOrDefault(i => i.DbrPath == savedItem.NameFile) is not { } baseItem) return null;
        var item = App.Mapper.Map<GdItem>(baseItem);

        if (savedItem.PrefixFile is not null
            && Prefixes!.FirstOrDefault(i => i.DbrPath == savedItem.PrefixFile) is { } prefixItem)
        {
            item.AddAffix(prefixItem, GdItemAffixType.Prefix);
        }

        if (savedItem.SuffixFile is not null
            && Suffixes!.FirstOrDefault(i => i.DbrPath == savedItem.SuffixFile) is { } suffixItem)
        {
            item.AddAffix(suffixItem, GdItemAffixType.Suffix);
        }

        return item;
    }

    [RelayCommand]
    async Task OpenAsync()
    {
        await InitializeFinishedEvent.WaitAsync();
        if (await dialogService.OpenCharacterAsync() is not { } result) return;

        var vm = App.GetService<CharacterViewModel>();
        await vm.LoadFinishedEvent.WaitAsync();

        vm.Name = result.Name;

        // clear all the assigned points
        vm.SelectedRawClass1 = vm.SelectedRawClass2 = null;

        // set the classes
        vm.SelectedRawClass1 = Classes!.FirstOrDefault(w => w.Index == result.ClassIndex1);
        vm.SelectedRawClass2 = Classes!.FirstOrDefault(w => w.Index == result.ClassIndex2);

        // set the masteries
        IEnumerable<GdAssignableSkill> allSkills = vm.AssignableConstellationSkills!;
        if (vm.SelectedAssignableClass1?.AssignableSkills is not null)
            allSkills = allSkills.Concat(vm.SelectedAssignableClass1.AssignableSkills);
        if (vm.SelectedAssignableClass2?.AssignableSkills is not null)
            allSkills = allSkills.Concat(vm.SelectedAssignableClass2.AssignableSkills);
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
            vm.EquipSlots![slotIndex].Item = savedItem is null ? null : BuildItem(savedItem);
        }

        Characters.Add(vm);
        SelectedCharacter = vm;
    }
}
