using CommunityToolkit.Mvvm.ComponentModel;

namespace GrimBuilder2.ViewModels;

public partial class DevotionsViewModel(CommonViewModel commonViewModel) : ObservableRecipient
{
    public CommonViewModel CommonViewModel => commonViewModel;
}
