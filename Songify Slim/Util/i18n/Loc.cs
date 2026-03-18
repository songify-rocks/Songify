using System;
using System.Globalization;
using System.Windows.Markup;

namespace Songify_Slim;

/// <summary>
/// XAML markup extension for localized strings from Properties.Resources (RESX).
/// Use: xmlns:i18n="clr-namespace:Songify_Slim" then {DynamicResource YourResourceKey}.
/// In root namespace so XAML designer can resolve the type reliably.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class Loc : MarkupExtension
{
    public string Key { get; set; }

    public Loc()
    { }

    public Loc(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
            return string.Empty;

        // Use current UI culture so language setting is respected
        string value = Properties.Resources.ResourceManager.GetString(Key, CultureInfo.CurrentUICulture);

        return string.IsNullOrEmpty(value) ? $"!{Key}!" : value;
    }
}