using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

internal static class ThemeHandler
{
    public static void ApplyTheme()
    {
        if (string.IsNullOrEmpty(Settings.Theme))
            Settings.Theme = "Dark";
        if (string.IsNullOrEmpty(Settings.WindowBackdrop))
            Settings.WindowBackdrop = "Mica";

        bool dark = !Settings.Theme.Contains("Light", StringComparison.OrdinalIgnoreCase);
        ApplicationTheme theme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        WindowBackdropType backdrop = ParseBackdrop(Settings.WindowBackdrop);

        ApplicationThemeManager.Apply(theme, backdrop, updateAccent: true);
        ApplyBackdropToWindows(backdrop);
    }

    public static WindowBackdropType ParseBackdrop(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out WindowBackdropType backdrop))
            return backdrop;
        return WindowBackdropType.Mica;
    }

    public static void ApplyBackdropToWindows(WindowBackdropType backdrop)
    {
        if (Application.Current?.Windows == null)
            return;

        foreach (Window window in Application.Current.Windows)
        {
            if (window is FluentWindow fluent)
                fluent.WindowBackdropType = backdrop;
        }
    }
}
