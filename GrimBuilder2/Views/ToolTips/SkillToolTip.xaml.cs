using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml.Documents;
using System.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using GrimBuilder2.Helpers;

namespace GrimBuilder2.Views.ToolTips;

public sealed partial class SkillToolTip : UserControl
{
    public static readonly DependencyProperty SkillProperty =
        DependencyProperty.Register(nameof(Skill), typeof(GdSkill), typeof(SkillToolTip), new(null, OnSkillPropertyChanged));

    public GdSkill? Skill
    {
        get => (GdSkill?)GetValue(SkillProperty);
        set => SetValue(SkillProperty, value);
    }

    private static void OnSkillPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tt = (SkillToolTip)d;
        if (e.NewValue is not GdSkill { } skill)
            tt.DescriptionTextBlock.Text = null;
        else
            tt.DescriptionTextBlock.SetMarkup(skill.Description);
    }

    public SkillToolTip()
    {
        InitializeComponent();
    }
}
