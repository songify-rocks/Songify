using System.Collections.Generic;
using Songify.Classes;
using System.IO;

namespace Songify.Config
{
    public class ConfigManager
    {
        private Converter converter = new Converter();
        private PathManager pm = new PathManager();
        public List<PluginConfig> PluginConfigs;

        public ConfigManager()
        {
            LoadPluginConfig();
        }

        public void LoadPluginConfig()
        {
            string pluginConfigJSON = File.ReadAllText(pm.PluginConfigFilePath);
            PluginConfigs = converter.ConvertJSONToObject<List<PluginConfig>>(pluginConfigJSON);
        }

        public void SavePluginConfig()
        {
            string pluginConfigJSON = converter.ConvertObjectToJSON(PluginConfigs);
            File.WriteAllText(pm.PluginConfigFilePath, pluginConfigJSON);
        }

    }
}
