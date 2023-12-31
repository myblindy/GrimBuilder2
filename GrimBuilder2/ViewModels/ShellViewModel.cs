﻿using CommunityToolkit.Mvvm.ComponentModel;

using GrimBuilder2.Contracts.Services;
using GrimBuilder2.Views;
using GrimBuilder2.Views.Controls;

namespace GrimBuilder2.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }
    public InstanceViewModel InstanceViewModel { get; }

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService,
        InstanceViewModel instanceViewModel)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        InstanceViewModel = instanceViewModel;
    }

    private void OnNavigated(object? sender, CustomFrameViewNavigatedArgs e)
    {
        if (e.Page is SettingsPage)
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.Page!.GetType());
        if (selectedItem != null)
            Selected = selectedItem;
    }
}
