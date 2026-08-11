using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Songify_Slim.Util.UI;

/// <summary>Factory for Fluent SymbolIcons and embedded brand Path geometries.</summary>
public static class AppIcons
{
    public const string Twitch = "IconTwitchGeometry";
    public const string Discord = "IconDiscordGeometry";

    public static FrameworkElement Fluent(SymbolRegular symbol, double size = 14, Brush? foreground = null) =>
        new SymbolIcon
        {
            Symbol = symbol,
            FontSize = size,
            Width = size,
            Height = size,
            Filled = false,
            Foreground = foreground ?? TryBrush("TextFillColorSecondaryBrush") ?? Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

    /// <summary>Scalable brand mark from <c>Resources/Icons/BrandIcons.xaml</c>.</summary>
    public static FrameworkElement Brand(string geometryKey, double size = 14, Brush? fill = null)
    {
        var path = new Path
        {
            Data = Application.Current?.TryFindResource(geometryKey) as Geometry,
            Fill = fill ?? TryBrush("TextFillColorSecondaryBrush") ?? Brushes.Gray,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true
        };

        return new Viewbox
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            Child = path,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true
        };
    }

    public static FrameworkElement? TryBrand(string geometryKey, double size = 14, Brush? fill = null)
    {
        if (Application.Current?.TryFindResource(geometryKey) is not Geometry)
            return null;
        return Brand(geometryKey, size, fill);
    }

    private static Brush? TryBrush(string key) =>
        Application.Current?.TryFindResource(key) as Brush;
}
