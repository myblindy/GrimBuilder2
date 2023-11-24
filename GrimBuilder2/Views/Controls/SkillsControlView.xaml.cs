using GrimBuilder2.Core.Models;
using GrimBuilder2.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views.Controls;

public sealed partial class SkillsControlView : UserControl
{
    static readonly DependencyProperty ClassProperty =
        DependencyProperty.Register(nameof(Class), typeof(GdAssignableClass), typeof(SkillsControlView), new(null));

    public GdAssignableClass? Class
    {
        get => (GdAssignableClass?)GetValue(ClassProperty);
        set => SetValue(ClassProperty, value);
    }

    public SkillsControlView()
    {
        InitializeComponent();
    }
}
