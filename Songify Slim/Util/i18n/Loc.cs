using System;
using System.Windows.Markup;

namespace Songify_Slim.Util.i18n
{
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

            // Use your resx ResourceManager (adjust namespace/class)
            string value = Properties.Resources.ResourceManager.GetString(Key);

            return string.IsNullOrEmpty(value) ? $"!{Key}!" : value;
        }
    }
}