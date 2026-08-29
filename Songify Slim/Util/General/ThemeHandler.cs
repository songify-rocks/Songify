using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

internal static class ThemeHandler
{
    private static string _appliedTheme;
    private static string _appliedBackdrop;
    private static string _appliedAccent;
    private static int _accentStampGeneration;

    /// <summary>
    /// Brushes ApplicationAccentColorManager writes onto Application.Resources.
    /// Settings tab text binds these, so they update without a theme reload.
    /// </summary>
    private static readonly string[] AppAccentKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorPrimary",
        "SystemAccentColorSecondary",
        "SystemAccentColorTertiary",
        "SystemAccentBrush",
        "SystemAccentColorBrush",
        "SystemAccentColorPrimaryBrush",
        "SystemAccentColorSecondaryBrush",
        "SystemAccentColorTertiaryBrush",
        "SystemFillColorAttentionBrush",
        "AccentTextFillColorPrimaryBrush",
        "AccentTextFillColorSecondaryBrush",
        "AccentTextFillColorTertiaryBrush",
        "AccentFillColorSelectedTextBackgroundBrush",
        "AccentFillColorDefault",
        "AccentFillColorDefaultBrush",
        "AccentFillColorSecondary",
        "AccentFillColorSecondaryBrush",
        "AccentFillColorTertiary",
        "AccentFillColorTertiaryBrush",
        "TextOnAccentFillColorPrimary",
        "TextOnAccentFillColorSecondary",
        "TextOnAccentFillColorDisabled",
        "TextOnAccentFillColorSelectedText",
        "AccentTextFillColorDisabled",
        "TooltipAccentBrush"
    ];

    /// <summary>
    /// WPF-UI Dark/Light.xaml brushes whose Color is a DynamicResource to an accent Color.
    /// Those Color bindings do not update at runtime, so the brush objects must be replaced.
    /// </summary>
    private static readonly (string BrushKey, string ColorKey)[] ThemeAccentBrushes =
    [
        ("ToggleSwitchFillOn", "SystemAccentColorPrimary"),
        ("ToggleSwitchFillOnPointerOver", "AccentFillColorSecondary"),
        ("ToggleSwitchFillOnPressed", "AccentFillColorTertiary"),
        ("ToggleSwitchStrokeOn", "SystemAccentColorPrimary"),
        ("ToggleSwitchStrokeOnPointerOver", "AccentFillColorSecondary"),
        ("ToggleSwitchStrokeOnPressed", "AccentFillColorTertiary"),
        ("ToggleSwitchKnobFillOn", "TextOnAccentFillColorPrimary"),
        ("ToggleSwitchKnobFillOnPointerOver", "TextOnAccentFillColorPrimary"),
        ("ToggleSwitchKnobFillOnPressed", "TextOnAccentFillColorPrimary"),
        ("AccentButtonBackground", "AccentFillColorDefault"),
        ("AccentButtonBackgroundPointerOver", "AccentFillColorSecondary"),
        ("AccentButtonBackgroundPressed", "AccentFillColorTertiary"),
        ("AccentButtonForeground", "TextOnAccentFillColorPrimary"),
        ("AccentButtonForegroundPointerOver", "TextOnAccentFillColorPrimary"),
        ("AccentButtonForegroundPressed", "TextOnAccentFillColorSecondary"),
        ("CheckBoxCheckBackgroundFillChecked", "SystemAccentColorPrimary"),
        ("CheckBoxCheckBackgroundFillCheckedPointerOver", "AccentFillColorSecondary"),
        ("CheckBoxCheckBackgroundFillCheckedPressed", "AccentFillColorTertiary"),
        ("CheckBoxCheckGlyphForeground", "TextOnAccentFillColorPrimary"),
        ("RadioButtonOuterEllipseCheckedStroke", "SystemAccentColorPrimary"),
        ("RadioButtonOuterEllipseCheckedStrokePointerOver", "AccentFillColorTertiary"),
        ("RadioButtonCheckGlyphFill", "TextOnAccentFillColorPrimary"),
        ("SliderThumbBackground", "SystemAccentColorPrimary"),
        ("SliderThumbBackgroundPointerOver", "AccentFillColorSecondary"),
        ("TextControlFocusedBorderBrush", "SystemAccentColorPrimary"),
        ("NavigationViewSelectionIndicatorForeground", "SystemAccentColorPrimary"),
        ("ProgressBarForeground", "SystemAccentColorPrimary"),
        ("ProgressRingForegroundThemeBrush", "SystemAccentColorPrimary"),
        ("ComboBoxBorderBrushFocused", "SystemAccentColorSecondary"),
        ("ComboBoxItemPillFillBrush", "SystemAccentColorPrimary"),
        ("ListViewItemPillFillBrush", "SystemAccentColorPrimary"),
        ("ListBoxItemSelectedBackgroundThemeBrush", "SystemAccentColorPrimary"),
        ("HyperlinkButtonForeground", "SystemAccentColorTertiary"),
        ("HyperlinkButtonForegroundPointerOver", "SystemAccentColorTertiary"),
        ("HyperlinkButtonForegroundPressed", "SystemAccentColorSecondary"),
        ("ToggleButtonBackgroundChecked", "SystemAccentColorPrimary"),
        ("ToggleButtonBackgroundCheckedPressed", "SystemAccentColorTertiary"),
        ("ToggleButtonForegroundChecked", "TextOnAccentFillColorPrimary"),
        ("ToggleButtonForegroundCheckedPointerOver", "SystemAccentColorSecondary"),
        ("CalendarViewSelectedBackground", "SystemAccentColorPrimary"),
        ("CalendarViewSelectedBackgroundPointerOver", "AccentFillColorSecondary"),
        ("CalendarViewSelectedBorderBrush", "SystemAccentColorPrimary"),
        ("CalendarViewTodayBackground", "SystemAccentColorPrimary"),
        ("BadgeBackground", "SystemAccentColorPrimary"),
        ("InfoBarInformationalSeverityIconBackground", "SystemAccentColorPrimary"),
        ("RatingControlSelectedForeground", "SystemAccentColorPrimary"),
        ("ThumbRateForeground", "SystemAccentColorPrimary"),
        ("TreeViewItemSelectionIndicatorForeground", "SystemAccentColorPrimary")
    ];

    public static void ApplyTheme(bool force = false)
    {
        if (string.IsNullOrEmpty(Settings.Theme))
            Settings.Theme = "Dark";
        if (string.IsNullOrEmpty(Settings.WindowBackdrop))
            Settings.WindowBackdrop = "Mica";

        bool dark = !Settings.Theme.Contains("Light", StringComparison.OrdinalIgnoreCase);
        ApplicationTheme theme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        WindowBackdropType backdrop = ParseBackdrop(Settings.WindowBackdrop);
        string accent = Settings.AccentColor ?? "";

        bool themeChanged = !string.Equals(_appliedTheme, Settings.Theme, StringComparison.OrdinalIgnoreCase);
        bool backdropChanged = !string.Equals(_appliedBackdrop, Settings.WindowBackdrop, StringComparison.OrdinalIgnoreCase);
        bool accentChanged = !string.Equals(_appliedAccent, accent, StringComparison.OrdinalIgnoreCase);

        if (!force && !themeChanged && !backdropChanged && !accentChanged)
        {
            ApplyBackdropToWindows(backdrop);
            ThemeBrushes.EnsureAppResources();
            return;
        }

        bool reloadTheme = themeChanged || backdropChanged;

        SettingsUi.BeginExternalUiMutation();
        try
        {
            bool useSystemAccent = string.IsNullOrWhiteSpace(accent);
            if (reloadTheme)
                ApplicationThemeManager.Apply(theme, backdrop, updateAccent: useSystemAccent);

            ApplyBackdropToWindows(backdrop);
            ApplyAccent(accent, theme, useSystemAccent);

            ThemeBrushes.EnsureAppResources();
            _appliedTheme = Settings.Theme;
            _appliedBackdrop = Settings.WindowBackdrop;
            _appliedAccent = accent;
        }
        finally
        {
            SettingsUi.EndExternalUiMutation();
        }
    }

    private static void ApplyAccent(string accent, ApplicationTheme theme, bool useSystemAccent)
    {
        if (useSystemAccent)
            ApplicationAccentColorManager.ApplySystemAccent();
        else if (TryParseHex(accent, out Color color))
            ApplicationAccentColorManager.Apply(color, theme);

        PublishAccentResources();
        StampAccentAfterLayout(accent, theme, useSystemAccent);
    }

    private static void StampAccentAfterLayout(string accent, ApplicationTheme theme, bool useSystemAccent)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        int generation = ++_accentStampGeneration;
        dispatcher.BeginInvoke(() =>
        {
            if (generation != _accentStampGeneration)
                return;
            if (useSystemAccent)
                ApplicationAccentColorManager.ApplySystemAccent();
            else if (TryParseHex(accent, out Color color))
                ApplicationAccentColorManager.Apply(color, theme);
            PublishAccentResources();
            ThemeBrushes.EnsureAppResources();
        }, DispatcherPriority.Loaded);
    }

    private static void PublishAccentResources()
    {
        ResourceDictionary appResources = Application.Current?.Resources;
        if (appResources == null)
            return;

        Dictionary<string, Color> colors = ReadAccentColors(appResources);
        Dictionary<string, object> replacements = [];

        foreach (string key in AppAccentKeys)
        {
            object copy = CopyResource(appResources[key]);
            if (copy != null)
                replacements[key] = copy;
        }

        foreach ((string brushKey, string colorKey) in ThemeAccentBrushes)
        {
            if (!colors.TryGetValue(colorKey, out Color color))
                continue;
            replacements[brushKey] = NewBrush(color);
        }

        if (colors.TryGetValue("SystemAccentColorPrimary", out Color primary))
            replacements["TooltipAccentBrush"] = NewBrush(primary);

        StampAllDictionaries(appResources, replacements);
    }

    private static Dictionary<string, Color> ReadAccentColors(ResourceDictionary appResources)
    {
        Dictionary<string, Color> colors = new(StringComparer.Ordinal);
        string[] keys =
        [
            "SystemAccentColor",
            "SystemAccentColorPrimary",
            "SystemAccentColorSecondary",
            "SystemAccentColorTertiary",
            "AccentFillColorDefault",
            "AccentFillColorSecondary",
            "AccentFillColorTertiary",
            "TextOnAccentFillColorPrimary",
            "TextOnAccentFillColorSecondary",
            "TextOnAccentFillColorDisabled",
            "TextOnAccentFillColorSelectedText"
        ];

        foreach (string key in keys)
        {
            if (TryGetColor(appResources, key, out Color color))
                colors[key] = color;
        }

        return colors;
    }

    private static SolidColorBrush NewBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        if (brush.CanFreeze)
            brush.Freeze();
        return brush;
    }

    private static object CopyResource(object value) => value switch
    {
        SolidColorBrush brush => NewBrush(brush.Color),
        Color color => color,
        _ => null
    };

    private static bool TryGetColor(ResourceDictionary resources, string key, out Color color)
    {
        color = default;
        object value = resources[key];
        switch (value)
        {
            case Color c:
                color = c;
                return true;
            case SolidColorBrush brush:
                color = brush.Color;
                return true;
            default:
                return false;
        }
    }

    private static void StampAllDictionaries(ResourceDictionary appResources, Dictionary<string, object> replacements)
    {
        foreach (KeyValuePair<string, object> pair in replacements)
            appResources[pair.Key] = CloneStampValue(pair.Value);

        foreach (ResourceDictionary dictionary in EnumerateMerged(appResources))
        {
            foreach (KeyValuePair<string, object> pair in replacements)
            {
                if (dictionary.Contains(pair.Key))
                    dictionary[pair.Key] = CloneStampValue(pair.Value);
            }
        }
    }

    private static object CloneStampValue(object value) => value switch
    {
        SolidColorBrush brush => NewBrush(brush.Color),
        Color color => color,
        _ => value
    };

    private static IEnumerable<ResourceDictionary> EnumerateMerged(ResourceDictionary root)
    {
        foreach (ResourceDictionary merged in root.MergedDictionaries)
        {
            yield return merged;
            foreach (ResourceDictionary nested in EnumerateMerged(merged))
                yield return nested;
        }
    }

    public static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        string value = hex.Trim().TrimStart('#');
        if (value.Length == 3)
            value = $"{value[0]}{value[0]}{value[1]}{value[1]}{value[2]}{value[2]}";
        if (value.Length != 6)
            return false;
        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
            return false;

        color = Color.FromRgb(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));
        return true;
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
