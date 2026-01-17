using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Channels.GetAdSchedule;
using YamlDotNet.Serialization;

namespace Songify_Slim.Models.Blocklist
{
    public interface IBlacklistItem
    {
        [YamlIgnore]
        string Key { get; }

        [YamlIgnore]
        string Display { get; }
    }
}