using GrimBuilder2.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
