using System.Collections.Generic;
using Songify.Interfaces;
using Songify.Config;

namespace Songify.Classes
{
    /// <summary>
    /// Load Songify Plugins
    /// </summary>
    class PluginManager
    {

        public List<ISongifyPlugin> Plugins { get; set; }
        public List<PluginConfig> Config { get; set; }

        public PluginManager()
        {
            Converter converter = new Converter();
            Plugins = LoadPlugins();
            
        }

        /// <summary>
        /// Get list of all available plugins
        /// </summary>
        /// <returns></returns>
        private List<ISongifyPlugin> LoadPlugins()
        {
            List<ISongifyPlugin> plugins = InterfaceLoader.GetAll<ISongifyPlugin>();
            return plugins;
        }
    }
}
