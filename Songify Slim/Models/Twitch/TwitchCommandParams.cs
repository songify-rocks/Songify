using System.Collections.Generic;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Models.Twitch
{
    public class TwitchCommandParams
    {
        public int Subtier { get; set; }
        public bool IsAllowed { get; set; }
        public List<int> UserLevels { get; set; }
        public TwitchUser ExistingUser { get; set; }
    }
}