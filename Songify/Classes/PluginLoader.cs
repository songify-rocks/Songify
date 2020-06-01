using System.Collections.Generic;
using Songify.Interfaces;

namespace Songify.Classes
{
    /// <summary>
    /// Load Songify Plugins
    /// </summary>
    class PluginLoader
    {
        /// <summary>
        /// Get list of all available plugins
        /// </summary>
        /// <returns></returns>
        public List<IPlugin> GetPlugins()
        {
            List<IPlugin> plugins = InterfaceLoader.GetAll<IPlugin>();
            return plugins;
        }
    }
}
