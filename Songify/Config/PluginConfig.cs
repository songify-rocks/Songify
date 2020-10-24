using System.Collections.Generic;
using Songify.Models;

namespace Songify.Config
{
    public class PluginConfig
    {
        public string PluginName { get; set; }
        public string PluginIdentifier { get; set; }
        public bool Enabled { get; set; }
        public List<PluginSettings> Settings { get; set; }
    }
}
