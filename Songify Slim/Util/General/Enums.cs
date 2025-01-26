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
            Everyone,
            Follower,
            Subscriber,
            SubscriberT2,
            SubscriberT3,
            Vip,
            Moderator,
            Broadcaster
        }

        public enum PauseOptions
        {
            Nothing = 0,
            PauseText = 1,
            ClearAll = 2
        }
    }
}
