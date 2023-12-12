using GrimBuilder2.Helpers;
using GrimBuilder2.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.Reactive.Linq;
using Windows.Foundation;

namespace GrimBuilder2.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        AppTitleBar.Loaded += (s, e) => SetRegionsForCustomTitleBar();
        AppTitleBar.SizeChanged += (s, e) => SetRegionsForCustomTitleBar();
        this.WhenAnyValue(x => x.ViewModel.InstanceViewModel.Characters.Count).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ =>
            {
                await Task.Yield(); // wait for the UI to update
                SetRegionsForCustomTitleBar();
            });

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
    }

    void SetRegionsForCustomTitleBar()
    {
        if (AppTitleBar.XamlRoot is null) return;

        // Specify the interactive regions of the title bar.
        double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(App.MainWindow.AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(App.MainWindow.AppWindow.TitleBar.LeftInset / scaleAdjustment);

        var transform = CharacterTabs.TransformToVisual(null);
        var bounds = transform.TransformBounds(new(0, 0, CharacterTabs.ActualWidth, CharacterTabs.ActualHeight));
        var toolBarRect = GetRect(bounds, scaleAdjustment);

        var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(App.MainWindow.AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, [toolBarRect]);
    }

    private static Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale) =>
        new(_X: (int)Math.Round(bounds.X * scale), _Y: (int)Math.Round(bounds.Y * scale),
            _Width: (int)Math.Round(bounds.Width * scale), _Height: (int)Math.Round(bounds.Height * scale));

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private void CharacterTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        ViewModel.InstanceViewModel.Characters.Remove((CharacterViewModel)args.Tab.DataContext);
    }
}
