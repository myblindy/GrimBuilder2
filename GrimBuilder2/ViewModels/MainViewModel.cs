using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Helpers;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Services;
using System.Collections.ObjectModel;

namespace GrimBuilder2.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly GdService gdService;

    public ObservableCollection<GdClass> Classes { get; } = [];

    [ObservableProperty]
    GdClass? class1, class2;

    public MainViewModel(GdService gdService)
    {
        this.gdService = gdService;

        _ = InitializeAsync();
    }

    async Task InitializeAsync()
    {
        Classes.AddRange(await gdService.GetClassesAsync());
        Class1 = Classes[0];
        Class2 = Classes[1];
    }
}
