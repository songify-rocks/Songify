using Songify.Models;
using System.Collections.Generic;

namespace Songify.Interfaces
{
    public interface ISongifyPlugin
    {
        SongInfo Fetch();
        List<PluginSettings> Initialize();
    }
}
