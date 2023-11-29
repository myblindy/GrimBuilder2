using GrimBuilder2.Core.Models;
using GrimBuilder2.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GrimBuilder2.Views.ToolTips;

public sealed partial class ItemToolTip : Grid
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(GdItem), typeof(ItemToolTip), new(null, OnItemPropertyChanged));

    public GdItem? Item
    {
        get => (GdItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    private static void OnItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tt = (ItemToolTip)d;
        if (e.NewValue is not GdItem { } item)
            tt.DescriptionTextBlock.Text = null;
        else
            tt.DescriptionTextBlock.SetMarkup(item.Description);
    }

    public ItemToolTip()
    {
        InitializeComponent();
    }
}
