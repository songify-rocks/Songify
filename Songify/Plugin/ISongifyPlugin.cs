using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify.Plugin
{
    public interface ISongifyPlugin
    {
        public PluginTypes PluginType { get; }
        public string Author { get; }
        public string Version { get; }

        /// <summary>
        /// <para>
        /// If this plugin is of type SongFetcher, this will be called every time this plugin is selected as the song information source.
        /// </para>
        /// <para>
        /// If this plugin is of type FeatureExtension, this will be called once the plugin has been loaded.
        /// </para>
        /// </summary>
        public void Initialize();
        
        /// <summary>
        /// This method will be called when the settings for this plugin have been modified.
        /// </summary>
        public void RestartFetchProcess();
    }
}
