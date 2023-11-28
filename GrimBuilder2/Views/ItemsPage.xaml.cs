using GrimBuilder2.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views;

public sealed partial class ItemsPage : Page
{
    public ItemsViewModel ViewModel
    {
        get;
    }

    public ItemsPage()
    {
        ViewModel = App.GetService<ItemsViewModel>();
        InitializeComponent();
    }
}
