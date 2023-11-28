using GrimBuilder2.Core.Models.SavedFile;
using GrimBuilder2.Views.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace GrimBuilder2.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "Used as a service")]
public class DialogService
{
    public async Task<string?> SelectFolderAsync()
    {
        FolderPicker picker = new();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        return await picker.PickSingleFolderAsync() is { } folder ? folder.Path : null;
    }

    public async Task<GdsCharacter?> OpenCharacterAsync()
    {
        var openCharacterDialog = App.GetService<OpenCharacterDialog>();
        openCharacterDialog.XamlRoot = App.MainWindow.Content.XamlRoot;
        if (await openCharacterDialog.ShowAsync() is not ContentDialogResult.Primary) return default;

        return openCharacterDialog.ViewModel.Result;
    }
}
