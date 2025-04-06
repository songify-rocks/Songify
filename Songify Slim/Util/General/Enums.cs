using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General;

public static class Enums
{

    public enum PlayerType
    {
        [Description("Spotify API")]
        SpotifyWeb,

        [Description("Spotify (Legacy)")]
        SpotifyLegacy,

        [Description("Deezer")]
        Deezer,

        [Description("foobar2000")]
        FooBar2000,

        [Description("VLC Media Player")]
        Vlc,

        [Description("Browser Companion")]
        Youtube,

        [Description("YTM Desktop")]
        YtmDesktop
    }


    public enum SongRequestSource
    {
        Reward,
        Command,
        Websocket
    }

    public enum CommandType
    {
        [Description("Song Request")]
        SongRequest,
        [Description("Song")]
        Song,
        [Description("Next")]
        Next,
        [Description("Skip")]
        Skip,
        [Description("Voteskip")]
        Voteskip,
        [Description("Remove")]
        Remove,
        [Description("Position")]
        Position,
        [Description("Queue")]
        Queue,
        [Description("Songlike")]
        Songlike,
        [Description("Volume")]
        Volume,
        [Description("Play")]
        Play,
        [Description("Pause")]
        Pause,
        [Description("Commands")]
        Commands
    }

    public enum ConfigTypes
    {
        SpotifyCredentials,
        TwitchCredentials,
        BotConfig,
        AppConfig,
        TwitchCommands
    }

    public enum RequestPlayerType
    {
        Spotify,
        Youtube,
        Other
    }

    public enum TwitchAccount
    {
        Main,
        Bot
    }

    //create a list with Twitch UserTypes and assign int values to them
    public enum TwitchUserLevels
    {
        Viewer,
        Follower,
        Subscriber,
        SubscriberT2,
        SubscriberT3,
        Vip,
        Moderator,
        Broadcaster
    }

    public enum AnnouncementColor
    {
        Blue,
        Green,
        Orange,
        Purple,
        Primary
    }

    public enum PauseOptions
    {
        Nothing = 0,
        PauseText = 1,
        ClearAll = 2
    }
}