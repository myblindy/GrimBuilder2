using CommunityToolkit.Mvvm.ComponentModel;

using GrimBuilder2.Contracts.Services;
using GrimBuilder2.ViewModels;
using GrimBuilder2.Views;

using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> pages = [];

    public PageService()
    {
        Configure<MasteriesViewModel, MasteriesPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<DevotionsViewModel, DevotionsPage>();
        Configure<ItemsViewModel, ItemsPage>();
    }

    public IEnumerable<Type> EnumeratePageTypes() => pages.Values;

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (pages)
        {
            if (!pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (pages)
        {
            var key = typeof(VM).FullName!;
            if (pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {pages.First(p => p.Value == type).Key}");
            }

            pages.Add(key, type);
        }
    }
}
