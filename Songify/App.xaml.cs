using System.Collections.Generic;
using System.Linq;
using Songify.Classes;
using Songify.Config;
using System.Windows;
using System.Windows.Threading;

namespace Songify
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        readonly Log logger = new Log();
        private ConfigManager cm = new ConfigManager();
        private PluginManager pm;
        public App()
        {
            logger.Start();
            pm = new PluginManager();
            pm.Plugins.ForEach(p =>
            {
                IEnumerable<PluginConfig> plugins = cm.PluginConfigs.Where(cfg => cfg.PluginName == p.Name);
                if (plugins.Count() == 0)
                {
                    PluginConfig config = new PluginConfig()
                    {
                        Enabled = false,
                        PluginIdentifier = p.Identifier,
                        PluginName = p.Name,
                        Settings = p.Settings
                    };
                    cm.PluginConfigs.Add(config);
                }
            });

            
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            logger.Add(e.Exception.Message, Models.MessageType.Error);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            logger.Stop();
        }
    }
}
