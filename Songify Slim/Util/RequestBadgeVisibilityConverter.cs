using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Songify_Slim.Util;

/// <summary>
/// Returns Visible when Requester is a real request (non-empty, not "Skipping...", "Spotify", or "YouTube"), otherwise Collapsed.
/// Used to show the "Request" badge on queue cards only for actual song requests (e.g. from Twitch).
/// </summary>
public class RequestBadgeVisibilityConverter : IValueConverter
{
    private const StringComparison Ignore = StringComparison.OrdinalIgnoreCase;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string requester || string.IsNullOrWhiteSpace(requester))
            return Visibility.Collapsed;
        if (string.Equals(requester, "Skipping...", Ignore)
            || string.Equals(requester, "Spotify", Ignore)
            || string.Equals(requester, "YouTube", Ignore))
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}