using CommunityToolkit.Mvvm.ComponentModel;

namespace GrimBuilder2.ViewModels;

public partial class DevotionsViewModel : ObservableRecipient
{
    public MainViewModel MainViewModel { get; }

    public DevotionsViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }
}
