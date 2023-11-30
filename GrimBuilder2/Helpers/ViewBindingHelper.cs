using GrimBuilder2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GrimBuilder2.Helpers;
static class ViewBindingHelper
{
    public static bool ToBoolean(object? value)
    {
        if (value is null) return false;
        if (value is bool b) return b;
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        if (value is int i) return i != 0;
        if (value is double d) return d != 0;

        return true;
    }

    public static bool ToNotBoolean(object? value) => !ToBoolean(value);

    public static Visibility ToVisibility(object? value) => ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility ToNotVisibility(object? value) => ToNotBoolean(value) ? Visibility.Visible : Visibility.Collapsed;

    public static double NegativeCoordinateWorkaroundOffset => -10000;
    public static double GetNegativeCoordinateWorkaroundValue(int val) =>
        val - NegativeCoordinateWorkaroundOffset;

    public static string? GetFullClassName(int classIndex1, int classIndex2)
    {
        var commonViewModel = App.GetService<CommonViewModel>();
        return commonViewModel.GetClassCombinationName(
            commonViewModel.Classes?.FirstOrDefault(c => c.Index == classIndex1),
            commonViewModel.Classes?.FirstOrDefault(c => c.Index == classIndex2));
    }
}

sealed class NotBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        ViewBindingHelper.ToNotBoolean(value);

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}