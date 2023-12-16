using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GrimBuilder2.Helpers;
static class TextBlockGdStatHelper
{
    public static readonly DependencyProperty GdStatProperty = DependencyProperty.RegisterAttached("GdStat",
        typeof(GdStatModel), typeof(TextBlockGdStatHelper), new PropertyMetadata(null, (s, e) =>
            OnGdStatChanged((TextBlock)s, (GdStatModel)e.NewValue)));
    public static string? GetGdStat(TextBlock item) => (string?)item.GetValue(GdStatProperty);
    public static void SetGdStat(TextBlock item, string? value) => item.SetValue(GdStatProperty, value);

    static readonly StringBuilder tempSB = new();
    static readonly Brush gdToolTipStatValueBrush = (Brush)App.Current.Resources["GdToolTipStatValueBrush"];
    static void OnGdStatChanged(TextBlock s, GdStatModel newValue)
    {
        if (newValue is null)
            s.Text = null;
        else
        {
            s.Inlines.Clear();
            tempSB.Clear();

            for (int i = 0; i < newValue.DisplayFormatString.Length; ++i)
                if (newValue.DisplayFormatString[i] == '{'
                    && Regex.Match(newValue.DisplayFormatString[i..], @"^{(\d+)}") is { Success: true } m)
                {
                    if (tempSB.Length > 0)
                        s.Inlines.Add(new Run { Text = tempSB.ToString() });
                    tempSB.Clear();

                    s.Inlines.Add(new Run
                    {
                        Text = m.Groups[1].Value switch
                        {
                            "0" => newValue.Value.ToString(CultureInfo.InvariantCulture),
                            _ => throw new NotImplementedException()
                        },
                        Foreground = gdToolTipStatValueBrush
                    });
                    i += m.Length - 1;
                }
                else
                    tempSB.Append(newValue.DisplayFormatString[i]);

            if (tempSB.Length > 0)
                s.Inlines.Add(new Run { Text = tempSB.ToString() });
        }
    }
}
