using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Contracts.Services;
using GrimBuilder2.Core.Helpers;
using Microsoft.UI.Xaml;
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

    public CustomFrameView()
    {
        var pageService = App.GetService<IPageService>();
        Children.AddRange(pageService.EnumeratePageTypes().Select(t => (Page)Activator.CreateInstance(t)!));
    }

    public void Navigate(Type pageType)
    {
        if (Children.FirstOrDefault(p => p.GetType() == pageType) is not Page { } page)
            Children.Add(page = (Page)Activator.CreateInstance(pageType)!);

        foreach (var childPage in Children.OfType<Page>())
            if (childPage != page)
                childPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
        CurrentPage = page;
    }
}

public record CustomFrameViewNavigatedArgs(Page? Page);