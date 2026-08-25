using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

internal static class ThemeHandler
{
    private static string _appliedTheme;
    private static string _appliedBackdrop;

    public static void ApplyTheme(bool force = false)
    {
        if (string.IsNullOrEmpty(Settings.Theme))
            Settings.Theme = "Dark";
        if (string.IsNullOrEmpty(Settings.WindowBackdrop))
            Settings.WindowBackdrop = "Mica";

        bool dark = !Settings.Theme.Contains("Light", StringComparison.OrdinalIgnoreCase);
        ApplicationTheme theme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        WindowBackdropType backdrop = ParseBackdrop(Settings.WindowBackdrop);

        bool unchanged = !force
                         && string.Equals(_appliedTheme, Settings.Theme, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(_appliedBackdrop, Settings.WindowBackdrop, StringComparison.OrdinalIgnoreCase);

        if (unchanged)
        {
            // Still sync backdrop on any new FluentWindow that just opened.
            ApplyBackdropToWindows(backdrop);
            ThemeBrushes.EnsureAppResources();
            return;
        }

        // Theme/style refresh can reset PasswordBox values and fire PasswordChanged with "".
        // Suspend settings secret fields so that cannot wipe SongifyApiKey / other secrets to disk.
        SettingsUi.BeginExternalUiMutation();
        try
        {
            ApplicationThemeManager.Apply(theme, backdrop, updateAccent: true);
            ApplyBackdropToWindows(backdrop);
            ThemeBrushes.EnsureAppResources();
            _appliedTheme = Settings.Theme;
            _appliedBackdrop = Settings.WindowBackdrop;
        }
        finally
        {
            SettingsUi.EndExternalUiMutation();
        }
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
