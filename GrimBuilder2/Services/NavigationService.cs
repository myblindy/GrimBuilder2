using CommunityToolkit.WinUI.UI;
using GrimBuilder2.Contracts.Services;
using GrimBuilder2.Contracts.ViewModels;
using GrimBuilder2.Views.Controls;
using Microsoft.UI.Xaml.Media;

namespace GrimBuilder2.Services;

public class NavigationService(IPageService pageService) : INavigationService
{
    public event EventHandler<CustomFrameViewNavigatedArgs>? Navigated;

    CustomFrameView? frame;
    public CustomFrameView? Frame
    {
        get => frame;
        set
        {
            if (Frame is not null) Frame.Navigated -= OnFrameNavigated;
            frame = value;
            if (Frame is not null) Frame.Navigated += OnFrameNavigated;
        }
    }

    void OnFrameNavigated(object? s, CustomFrameViewNavigatedArgs e)
    {
        if (Frame?.CurrentPageViewModel is INavigationAware navigationAware)
            navigationAware.OnNavigatedTo(e.Page!);
        Navigated?.Invoke(s, e);
    }

    public void NavigateTo(string pageKey)
    {
        var pageType = pageService.GetPageType(pageKey);

        if (Frame!.CurrentPage?.GetType() != pageType)
        {
            var vmBeforeNavigation = Frame.CurrentPageViewModel;
            Frame.Navigate(pageType);
            if (vmBeforeNavigation is INavigationAware navigationAware)
                navigationAware.OnNavigatedFrom();
        }
    }
}
