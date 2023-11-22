using Windows.Storage.Pickers;

namespace GrimBuilder2.Services;

public class DialogService
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Used as a service")]
    public async Task<string?> SelectFolderAsync()
    {
        FolderPicker picker = new();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        return await picker.PickSingleFolderAsync() is { } folder ? folder.Path : null;
    }
}
