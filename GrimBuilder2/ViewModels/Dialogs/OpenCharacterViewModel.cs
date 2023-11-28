using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrimBuilder2.Core.Models.SavedFile;
using GrimBuilder2.Services;

namespace GrimBuilder2.ViewModels.Dialogs;

public partial class OpenCharacterViewModel(
    InstanceViewModel instanceViewModel, CommonViewModel commonViewModel, GdService gdService) : ObservableObject
{
    public CommonViewModel CommonViewModel { get; } = commonViewModel;

    enum TypeKind { GrimDawn, GrimBuilding, New }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    int type;

    public IList<GdsCharacter> Characters { get; } =
        gdService.GetCharacterList(instanceViewModel.GdSavePath);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    GdsCharacter? selectedCharacter;

    public GdsCharacter? Result { get; private set; }

    [RelayCommand(CanExecute = nameof(CanOpen))]
    void Open()
    {
        Result = (TypeKind)Type switch
        {
            TypeKind.New => GdsCharacter.Empty,
            TypeKind.GrimDawn => SelectedCharacter,
            _ => throw new NotImplementedException(),
        };
    }

    bool CanOpen => (TypeKind)Type switch
    {
        TypeKind.New => true,
        TypeKind.GrimDawn => SelectedCharacter is not null,
        _ => false
    };
}
