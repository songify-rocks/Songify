namespace Songify.Models
{
    public class PluginSettings
    {
        public string Name { get; set; }
        /// <summary>
        /// For this to render in the settings tab, it has to either be a
        /// - number
        /// - string
        /// - boolean
        /// Settings of other types won't be rendered.
        /// </summary>
        public object Value { get; set; }
    }
}
