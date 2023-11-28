using Microsoft.UI.Xaml.Controls;
using GrimBuilder2.ViewModels.Dialogs;

namespace GrimBuilder2.Views.Dialogs;

public sealed partial class OpenCharacterDialog : ContentDialog
{
    public OpenCharacterViewModel ViewModel { get; }

    public OpenCharacterDialog(OpenCharacterViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
    }
}
