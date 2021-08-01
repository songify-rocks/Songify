using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify.Plugin;

namespace Songify
{
    class MeinTollesTestPlugin : ISongifyPlugin
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void RestartFetchProcess()
        {
            throw new NotImplementedException();
        }

        [PluginSetting(5)]
        public int brudiSetting;

        public string Author => "Der krasse Brudi";

        public string Version => "0.1.0";

        public PluginTypes PluginType => PluginTypes.SongFetcher;
    }
}
