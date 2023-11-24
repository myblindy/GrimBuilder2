using GrimBuilder2.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
