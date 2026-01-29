using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Songify_Slim.Models.Twitch
{
    public class TwitchPollSettings
    {
        public string Title { get; set; } = "Skip current song?";
        public List<string> Choices { get; set; } = ["Yes", "No"];
        public int Duration { get; set; } = 1; // Duration in Minuets, fixed values are 1, 2,3, 5, 10
        public bool AdditionalVotesEnabled { get; set; } = false;
        public int ChannelPointsPerVote { get; set; } = 100;
        public string WinningChoice { get; set; } = "Yes";
    }
}