using CommunityToolkit.Mvvm.ComponentModel;

namespace GrimBuilder2.ViewModels;

public partial class ItemsViewModel(InstanceViewModel instanceViewModel) : ObservableRecipient
{
    public InstanceViewModel InstanceViewModel => instanceViewModel;
}
