using Songify.Models;

namespace Songify.Interfaces
{
    public interface IPlugin
    {
        SongInfo Fetch();
        void Initialize();
    }
}
