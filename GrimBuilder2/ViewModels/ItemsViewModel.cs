using CommunityToolkit.Mvvm.ComponentModel;

namespace GrimBuilder2.ViewModels;

public partial class ItemsViewModel(CommonViewModel commonViewModel) : ObservableRecipient
{
    public CommonViewModel CommonViewModel => commonViewModel;
}
