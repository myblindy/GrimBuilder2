using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

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

    public static string GetDisplayItemType(GdItemType itemType) => itemType switch
    {
        GdItemType.Feet => "Feet",
        GdItemType.Hands => "Hands",
        GdItemType.Head => "Head",
        GdItemType.Legs => "Legs",
        GdItemType.Relic => "Relic",
        GdItemType.Shoulders => "Shoulders",
        GdItemType.Chest => "Chest",
        GdItemType.WeaponOneHandedAxe => "One-Handed Axe",
        GdItemType.WeaponOneHandedSword => "One-Handed Sword",
        GdItemType.WeaponOneHandedMace => "One-Handed Mace",
        GdItemType.WeaponOneHandedGun => "One-Handed Gun",
        GdItemType.WeaponDagger => "Dagger",
        GdItemType.WeaponScepter => "Scepter",
        GdItemType.WeaponTwoHandedAxe => "Two-Handed Axe",
        GdItemType.WeaponTwoHandedSword => "Two-Handed Sword",
        GdItemType.WeaponTwoHandedMace => "Two-Handed Mace",
        GdItemType.WeaponTwoHandedGun => "Two-Handed Gun",
        GdItemType.WeaponCrossbow => "Crossbow",
        GdItemType.OffhandFocus => "Focus",
        GdItemType.Shield => "Shield",
        GdItemType.Medal => "Medal",
        GdItemType.Amulet => "Amulet",
        GdItemType.Ring => "Ring",
        GdItemType.Belt => "Belt",
        _ => throw new NotImplementedException()
    };

    public static SolidColorBrush GetNameBrush(GdItemRarity rarity) => rarity switch
    {
        GdItemRarity.Common => new(Color.FromArgb(0xff, 0xff, 0xff, 0xff)),
        GdItemRarity.Magical => new(Color.FromArgb(0xff, 0xf2, 0xe6, 0x1a)),
        GdItemRarity.Rare => new(Color.FromArgb(0xff, 0x3e, 0xea, 0x4a)),
        GdItemRarity.Epic => new(Color.FromArgb(0xff, 0x33, 0x8c, 0xce)),
        GdItemRarity.Legendary => new(Color.FromArgb(0xff, 0xa6, 0x38, 0xff)),
        _ => throw new NotImplementedException()
    };
}
