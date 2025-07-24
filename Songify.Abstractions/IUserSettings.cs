using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify.Abstractions
{
    public interface IUserSettings
    {
        string TwitchUserId { get;  }
        string TwitchUsername { get; }
        string SongifyApiToken { get; }
    }
}
