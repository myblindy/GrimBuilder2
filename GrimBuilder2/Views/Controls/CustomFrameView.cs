using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace GrimBuilder2.Views.Controls;

[INotifyPropertyChanged]
public partial class CustomFrameView : Grid
{
    public event EventHandler<CustomFrameViewNavigatedArgs>? Navigated;

    Page? currentPage;
    public Page? CurrentPage
    {
        get => currentPage;
        private set
        {
            currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentPageViewModel));
            Navigated?.Invoke(this, new(value));
        }
    }

    public INotifyPropertyChanged? CurrentPageViewModel =>
        CurrentPage is null ? null : ((dynamic)CurrentPage).ViewModel;

    readonly Dictionary<Type, Page> pageCache = [];
    public void Navigate(Type pageType)
    {
        if (!pageCache.TryGetValue(pageType, out var page))
            Children.Add(pageCache[pageType] = page = (Page)Activator.CreateInstance(pageType)!);

        if (CurrentPage is not null && CurrentPage != page)
            CurrentPage.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        page.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        CurrentPage = page;
    }
}

public record CustomFrameViewNavigatedArgs(Page? Page);