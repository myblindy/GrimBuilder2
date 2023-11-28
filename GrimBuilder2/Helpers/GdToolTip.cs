using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace GrimBuilder2.Helpers;

static class GdToolTip
{
    static readonly DataTemplate toolTipTemplate = (DataTemplate)XamlReader.Load("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"                                                                    
                      xmlns:tt="using:GrimBuilder2.Views.ToolTips">
            <tt:SkillToolTip Skill="{Binding}" />
        </DataTemplate>
        """);

    public static readonly DependencyProperty SkillProperty =
        DependencyProperty.RegisterAttached("Skill", typeof(GdSkill), typeof(GdToolTip), new(null, OnSkillChanged));
    public static GdSkill? GetSkill(DependencyObject obj) => (GdSkill?)obj.GetValue(SkillProperty);
    public static void SetSkill(DependencyObject obj, GdSkill value) => obj.SetValue(SkillProperty, value);
    static void OnSkillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GdSkill { } skill)
            ToolTipService.SetToolTip(d, new ContentPresenter
            {
                DataContext = skill,
                ContentTemplate = toolTipTemplate
            });
        else
            ToolTipService.SetToolTip(d, null);
    }
}
