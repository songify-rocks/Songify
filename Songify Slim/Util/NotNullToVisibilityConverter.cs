using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Songify_Slim.Util;

/// <summary>
/// Returns Visible when the value is not null, otherwise Collapsed.
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}