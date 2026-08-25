using System.Windows;
using System.Windows.Media;

namespace Songify_Slim.Util.General;

/// <summary>
/// Fluent layer/control fills are translucent over Mica/Acrylic. These helpers
/// force fully opaque surfaces that still track the active theme colors.
/// </summary>
internal static class ThemeBrushes
{
    public const string OpaqueSurfaceKey = "SongifyOpaqueSurfaceBrush";
    public const string OpaqueSecondaryKey = "SongifyOpaqueSecondaryBrush";

    public static void EnsureAppResources()
    {
        if (Application.Current?.Resources == null)
            return;

        Application.Current.Resources[OpaqueSurfaceKey] = CreateOpaqueSurfaceBrush();
        Application.Current.Resources[OpaqueSecondaryKey] = CreateOpaqueSecondaryBrush();
    }

    public static SolidColorBrush CreateOpaqueSurfaceBrush()
        => CreateOpaqueFrom(
            "ApplicationBackgroundBrush",
            "SolidBackgroundFillColorBaseBrush",
            "CardBackgroundFillColorDefaultBrush",
            fallback: Color.FromRgb(0x20, 0x20, 0x20));

    public static SolidColorBrush CreateOpaqueSecondaryBrush()
        => CreateOpaqueFrom(
            "ControlFillColorSecondaryBrush",
            "CardBackgroundFillColorSecondaryBrush",
            "SolidBackgroundFillColorSecondaryBrush",
            fallback: Color.FromRgb(0x2C, 0x2C, 0x2C));

    private static SolidColorBrush CreateOpaqueFrom(
        string primaryKey,
        string secondaryKey,
        string tertiaryKey,
        Color fallback)
    {
        Color color = fallback;
        if (TryGetSolid(primaryKey, out Color primary))
            color = primary;
        else if (TryGetSolid(secondaryKey, out Color secondary))
            color = secondary;
        else if (TryGetSolid(tertiaryKey, out Color tertiary))
            color = tertiary;

        SolidColorBrush brush = new(Color.FromArgb(255, color.R, color.G, color.B));
        if (brush.CanFreeze)
            brush.Freeze();
        return brush;
    }

    private static bool TryGetSolid(string key, out Color color)
    {
        color = default;
        if (Application.Current?.TryFindResource(key) is not SolidColorBrush brush)
            return false;

        color = brush.Color;
        return true;
    }
}
