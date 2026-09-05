using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Songify_Slim.Util;

/// <summary>
/// Formats a bound value with a localized string resource.
/// ConverterParameter = resource key containing a {0} format string.
/// </summary>
public sealed class ResourceStringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = parameter as string ?? "";
        string fmt = Application.Current?.TryFindResource(key) as string ?? "{0}";
        try
        {
            return string.Format(culture, fmt, value ?? "");
        }
        catch (FormatException)
        {
            return value?.ToString() ?? "";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
