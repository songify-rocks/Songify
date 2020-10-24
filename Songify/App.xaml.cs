using System.Collections.Generic;
using System.Linq;
using Songify.Classes;
using Songify.Config;
using Songify.Interfaces;
using System.Windows;
using System.Windows.Threading;

namespace Songify
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public readonly Log Logger = new Log();
        public ConfigManager ConfigManager;
        public PluginManager PluginManager;

        public App()
        {
            ConfigManager = new ConfigManager();
            PluginManager = new PluginManager();

            Logger.Start();
            for (int i = 0; i < PluginManager.Plugins.Count; i++)
            {
                PluginConfig pConfig = ConfigManager.PluginConfigs.FirstOrDefault(cfg => cfg.PluginName == PluginManager.Plugins[i].Name);
                // New Plugin Detected, use default settings
                if (pConfig == null)
                {
                    ISongifyPlugin p = PluginManager.Plugins[i];
                    PluginConfig config = new PluginConfig()
                    {
                        Enabled = false,
                        PluginIdentifier = p.Identifier,
                        PluginName = p.Name,
                        Settings = p.Settings
                    };
                    ConfigManager.PluginConfigs.Add(config);
                    continue;
                }

                // Existing plugin detected, load config from config file
                PluginManager.Plugins[i].Settings = pConfig.Settings;
            }

            ConfigManager.SavePluginConfig();            
        }

        // Add a message to the log file every time an unhandled exception occurs
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Logger.Add(e.Exception.Message, Models.MessageType.Error);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Logger.Stop();
        }
    }
}
