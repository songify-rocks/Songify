using System.Collections.Generic;
using Songify.Interfaces;
using Songify.Config;
using System.Linq;
using System.Windows;

namespace Songify.Classes
{
    /// <summary>
    /// Load Songify Plugins
    /// </summary>
    public class PluginManager
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

        public void ExecuteEnabledPlugins()
        {
            Plugins.ForEach(p =>
            {
                if (((App)Application.Current).ConfigManager.PluginConfigs.First(cfg => p.Identifier == cfg.PluginIdentifier).Enabled)
                {
                    p.Fetch();
                }
            });
        }

        public void InitializeEnabledPlugins()
        {
            Plugins.ForEach(p =>
            {
                if (((App)Application.Current).ConfigManager.PluginConfigs.First(cfg => p.Identifier == cfg.PluginIdentifier).Enabled)
                {
                    p.Initialize();
                }
            });
        }
    }
}
