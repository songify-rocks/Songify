using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Songify_Slim.Util;

/// <summary>
/// Converts a string URL to an ImageSource (BitmapImage) for binding to Image.Source.
/// Returns null when the string is null, empty, or not a valid URI.
/// </summary>
public class UrlToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => FromUrl(value as string);

    /// <summary>Converts a URL string to ImageSource for use without a binding converter (e.g. in ViewModels/Models).</summary>
    public static System.Windows.Media.ImageSource FromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || !uri.IsAbsoluteUri)
            return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}