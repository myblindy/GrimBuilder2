using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GrimBuilder2.Contracts.Services;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Helpers;
using GrimBuilder2.Services;
using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

namespace GrimBuilder2.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService themeSelectorService;
    private readonly ArzParserService arzParserService;
    private readonly DialogService dialogService;

    [ObservableProperty]
    private ElementTheme elementTheme;

    [ObservableProperty]
    private string versionDescription;

    public ICommand SwitchThemeCommand { get; }

    [ObservableProperty]
    double dumpDbrDatabaseProgress;

    [RelayCommand(IncludeCancelCommand = true)]
    async Task DumpDbrDatabase(CancellationToken ct)
    {
        if (await dialogService.SelectFolderAsync() is not { } outputPath)
            return;

        await Task.Run(() => Directory.Delete(outputPath, true), ct).ConfigureAwait(false);

        DumpDbrDatabaseProgress = 0;
        var dbrFiles = await Task.Run(() => arzParserService.GetDbrData(new Regex(@""))).ConfigureAwait(false);
        var dbrIndex = 0;
        await Parallel.ForEachAsync(dbrFiles, ct, async (dbr, ct) =>
        {
            if (Interlocked.Increment(ref dbrIndex) % 25 == 0)
                App.MainDispatcherQueue.TryEnqueue(() => DumpDbrDatabaseProgress = (double)dbrIndex / dbrFiles.Count);

            var fullPath = Path.Combine(outputPath, dbr.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, string.Join(Environment.NewLine, dbr.Keys.Select(key =>
                $"{key}={string.Join(",", dbr.GetValueType(key) switch
                {
                    DbrValueType.Integer or DbrValueType.Boolean =>
                        dbr.GetIntegerValues(key).Select(w => w.ToString(CultureInfo.InvariantCulture)),
                    DbrValueType.String => dbr.GetStringValues(key),
                    DbrValueType.Single => dbr.GetFloatValues(key).Select(w => w.ToString(CultureInfo.InvariantCulture)),
                    _ => throw new NotImplementedException()
                })}")), ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ArzParserService arzParserService, DialogService dialogService)
    {
        this.themeSelectorService = themeSelectorService;
        this.arzParserService = arzParserService;
        this.dialogService = dialogService;
        elementTheme = this.themeSelectorService.Theme;
        versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await this.themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
