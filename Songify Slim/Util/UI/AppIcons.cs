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

    public static FrameworkElement Fluent(SymbolRegular symbol, double size = 14, Brush? foreground = null)
    {
        var icon = new SymbolIcon
        {
            Symbol = symbol,
            FontSize = size,
            Width = size,
            Height = size,
            Filled = false,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (foreground != null)
            icon.Foreground = foreground;
        else
            icon.SetResourceReference(Control.ForegroundProperty, "TextFillColorSecondaryBrush");

        return icon;
    }

    /// <summary>Scalable brand mark from <c>Resources/Icons/BrandIcons.xaml</c>.</summary>
    public static FrameworkElement Brand(string geometryKey, double size = 14, Brush? fill = null)
    {
        var path = new Path
        {
            Data = Application.Current?.TryFindResource(geometryKey) as Geometry,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true
        };

        if (fill != null)
            path.Fill = fill;
        else
            path.SetResourceReference(Shape.FillProperty, "TextFillColorSecondaryBrush");

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
}
