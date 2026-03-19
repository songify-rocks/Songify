using System;
using System.Windows.Markup;

namespace Songify_Slim.Util.i18n
{
    /// <summary>
    /// Resolves a resource key to a string from the RESX ResourceManager.
    /// For XAML, prefer {DynamicResource key} so strings update when the app language changes.
    /// This extension is still available for code or legacy use.
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

            string value = Properties.Resources.ResourceManager.GetString(Key);

            return string.IsNullOrEmpty(value) ? $"!{Key}!" : value;
        }
    }
}