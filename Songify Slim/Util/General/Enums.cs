using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    public static class Enums
    {
        public enum ConfigTypes
        {
            SpotifyCredentials,
            TwitchCredentials,
            BotConfig,
            AppConfig
        }

        public enum TwitchAccount
        {
            Main,
            Bot
        }

        //create a list with Twitch UserTypes and assign int values to them
        public enum TwitchUserLevels
        {
            Everyone = 0,
            Subscriber = 1,
            Vip = 2,
            Moderator = 3,
            Broadcaster = 4
        }

        public enum PauseOptions
        {
            Nothing = 0,
            PauseText = 1,
            ClearAll = 2
        }

        public enum YoutubePlayerState
        {
            Unstarted = -1,
            Ended = 0,
            Playing = 1,
            Paused = 2,
            Buffering = 3,
            VideoCued = 5
        }

    }
}
