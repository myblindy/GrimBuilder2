using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace GrimBuilder2.Helpers;

static class GdToolTip
{
    static readonly DataTemplate skillToolTipTemplate = (DataTemplate)XamlReader.Load("""
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
            ToolTipService.SetToolTip(d, new ContentControl
            {
                Content = skill,
                ContentTemplate = skillToolTipTemplate
            });
        else if (e.NewValue is null)
            ToolTipService.SetToolTip(d, null);
        else
            throw new InvalidOperationException();
    }

    static readonly DataTemplate itemToolTipTemplate = (DataTemplate)XamlReader.Load("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"                                                                    
                      xmlns:tt="using:GrimBuilder2.Views.ToolTips">
            <tt:ItemToolTip Item="{Binding}" />
        </DataTemplate>
        """);

    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.RegisterAttached("Item", typeof(GdItem), typeof(GdToolTip), new(null, OnItemChanged));
    public static GdItem? GetItem(DependencyObject obj) => (GdItem?)obj.GetValue(ItemProperty);
    public static void SetItem(DependencyObject obj, GdItem value) => obj.SetValue(ItemProperty, value);
    static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GdItem { } item)
            ToolTipService.SetToolTip(d, new ContentControl
            {
                Content = item,
                ContentTemplate = itemToolTipTemplate
            });
        else if (e.NewValue is null)
            ToolTipService.SetToolTip(d, null);
        else
            throw new InvalidOperationException();
    }
}
