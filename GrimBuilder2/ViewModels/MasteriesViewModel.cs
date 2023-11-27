using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.ViewModels;

public partial class MasteriesViewModel(CommonViewModel commonViewModel) : ObservableRecipient
{
    public CommonViewModel CommonViewModel => commonViewModel;
}
