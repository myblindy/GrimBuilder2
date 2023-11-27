using GrimBuilder2.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views;

public sealed partial class MasteriesPage : Page
{
    public MasteriesViewModel ViewModel { get; }

    public MasteriesPage()
    {
        ViewModel = App.GetService<MasteriesViewModel>();
        InitializeComponent();
    }
}
