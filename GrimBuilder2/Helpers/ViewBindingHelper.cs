using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Helpers;
static class ViewBindingHelper
{
    public static Visibility ToVisibility(object? value)
    {
        if (value is null) return Visibility.Collapsed;
        if (value is bool b) return b ? Visibility.Visible : Visibility.Collapsed;
        if (value is string s) return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
        if (value is int i) return i == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (value is double d) return d == 0 ? Visibility.Collapsed : Visibility.Visible;

        return Visibility.Visible;
    }
}
