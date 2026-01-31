using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Channels.GetAdSchedule;

namespace Songify_Slim.Models.Twitch
{
    public class PollItem
    {
        public string Id { get; set; }
        public bool IsActive { get; set; }
        public string RedemptionId { get; set; }
        public string RewardId { get; set; }
    }
}