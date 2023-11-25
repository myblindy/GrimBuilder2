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

    private void OnSkillPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement frameworkElement || frameworkElement.DataContext is not GdAssignableSkill skill) return;

        var props = e.GetCurrentPoint(this).Properties;
        if (skill.IsDependencyValid && skill.IsMasteryValid)
            if (props.IsLeftButtonPressed && skill.AssignedPoints < skill.MaximumLevel)
                ++skill.AssignedPoints;
            else if (props.IsRightButtonPressed && skill.AssignedPoints > 0)
                --skill.AssignedPoints;
    }
}
