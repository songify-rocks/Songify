namespace Songify.Plugin
{
    /// <summary>
    /// An attribute to tell songify about a setting that should be available to the user.
    /// Properties marked with this attribute will be rendered in the plugin settings window.
    /// Only public properties will be rendered.
    /// </summary>
    public class PluginSetting : System.Attribute
    {
        public object Default { get; private set; }

        public PluginSetting(object defaultValue)
        {
            Default = defaultValue;
        }
    }
}
