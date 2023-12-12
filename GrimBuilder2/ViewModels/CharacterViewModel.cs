using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Models.SavedFile;
using GrimBuilder2.Models;
using GrimBuilder2.Services;
using Nito.AsyncEx;
using ReactiveUI;

namespace GrimBuilder2.ViewModels;

public partial class CharacterViewModel : ObservableRecipient
{
    private readonly GdService gdService;
    private readonly DialogService dialogService;
    private readonly InstanceViewModel instanceViewModel;

    [ObservableProperty]
    string? name;

    [ObservableProperty]
    IList<GdAssignableSkill>? assignableConstellationSkills;

    [ObservableProperty]
    IList<GdAssignableEquipSlot>? equipSlots;

    [ObservableProperty]
    GdClass? selectedRawClass1, selectedRawClass2;

    [ObservableProperty]
    GdAssignableClass? selectedAssignableClass1, selectedAssignableClass2;

    [ObservableProperty]
    bool loadFinished;
    public AsyncManualResetEvent LoadFinishedEvent { get; } = new();

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

    public CharacterViewModel(GdService gdService, DialogService dialogService, InstanceViewModel instanceViewModel)
    {
        this.gdService = gdService;
        this.dialogService = dialogService;
        this.instanceViewModel = instanceViewModel;

        _ = InitializeAsync();
    }

    async Task InitializeAsync()
    {
        await instanceViewModel.InitializeFinishedEvent.WaitAsync();

        AssignableConstellationSkills = instanceViewModel.Constellations!.SelectMany(c => c.Skills)
            .Select(App.Mapper.Map<GdAssignableSkill>)
            .ToArray();

        EquipSlots = instanceViewModel.EquipSlots!.Select(App.Mapper.Map<GdAssignableEquipSlot>).ToArray();

        LoadFinished = true;
        LoadFinishedEvent.Set();
    }
}
