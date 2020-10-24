using Songify.Models;
using System.Collections.Generic;

namespace Songify.Interfaces
{
    public interface ISongifyPlugin
    {
        string Name { get; }
        string Identifier { get; }
        List<PluginSettings> Settings {get; set; }
        SongInfo Fetch();
        void Initialize();
    }
}
