using Songify_Slim.Util.General;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Convert;
using Application = System.Windows.Application;
using static Songify_Slim.Util.Settings.YamlTypeConverters;
using Markdig.Wpf;
using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is for writing, exporting and importing the config file
    ///     The config file is XML and has a single config tag with attributes
    /// </summary>
    internal class ConfigHandler
    {
        public static List<TwitchCommand> DefaultCommands { get; set; } =
       [
           new()
            {
                CommandType = CommandType.SongRequest,
                Trigger = "ssr",
                Response = "{artist} - {title} requested by @{user} has been added to the queue.",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

           new()
            {
                CommandType = CommandType.Next,
                Trigger = "next",
                Response = "@{user} {song}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Primary,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Play,
                Trigger = "play",
                Response = "Playback resumed.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Pause,
                Trigger = "pause",
                Response = "Playback stopped.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Position,
                Trigger = "pos",
                Response = "@{user} {songs}{pos} {song}{/songs}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Queue,
                Trigger = "queue",
                Response = "{queue}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Remove,
                Trigger = "remove",
                Response = "{user} your previous request ({song}) will be skipped.",
                IsEnabled = false,
                AllowedUserLevels = [0,1,2,3,4,5,6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Skip,
                Trigger = "skip",
                Response = "@{user} skipped the current song.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Voteskip,
                Trigger = "voteskip",
                Response = "@{user} voted to skip the current song. ({votes})",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>
                {
                    {"SkipCount", 5}
                }
            },

            new()
            {
                CommandType = CommandType.Song,
                Trigger = "song",
                Response = "@{user} {title} by {single_artist} {{requested by @{req}}}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Songlike,
                Trigger = "songlike",
                Response = "The Song {song} has been added to the playlist {playlist}.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Volume,
                Trigger = "vol",
                Response = "Spotify volume at {vol}%",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>
                {
                    {"VolumeSetResponse", "Spotify volume set to {vol}%"}
                }
            },

            new()
            {
                CommandType = CommandType.Commands,
                Trigger = "cmds",
                Response = "Active Songify commands: {commands}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            }
       ];

        public static void WriteConfig(ConfigTypes configType, object o, string path = null, bool isBackup = false)
        {
            path ??= Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string yaml;
            string fileEnding = isBackup ? ".bak" : ".yaml";

            switch (configType)
            {
                case ConfigTypes.SpotifyCredentials:
                    path += "/SpotifyCredentials" + fileEnding;
                    yaml = serializer.Serialize(o as SpotifyCredentials ?? throw new InvalidOperationException());
                    break;

                case ConfigTypes.TwitchCredentials:
                    path += "/TwitchCredentials" + fileEnding;
                    yaml = serializer.Serialize(o as TwitchCredentials ?? throw new InvalidOperationException());
                    break;

                case ConfigTypes.BotConfig:
                    path += "/BotConfig" + fileEnding;
                    yaml = serializer.Serialize(o as BotConfig ?? throw new InvalidOperationException());
                    break;

                case ConfigTypes.AppConfig:

                    path += "/AppConfig" + fileEnding;
                    yaml = serializer.Serialize(o as AppConfig ?? throw new InvalidOperationException());

                    break;

                case ConfigTypes.TwitchCommands:
                    path += "/TwitchCommands" + fileEnding;
                    Debug.WriteLine(o);
                    yaml = serializer.Serialize(o as TwitchCommands ?? throw new InvalidOperationException());
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(configType), configType, null);
            }
            File.WriteAllText(path, yaml);
        }

        private static T LoadOrCreateConfig<T>(string path, string fileName, IDeserializer deserializer) where T : new()
        {
            string yamlPath = $@"{path}\{fileName}.yaml";
            string bakPath = $@"{path}\{fileName}.bak";

            if (File.Exists(yamlPath))
            {
                return deserializer.Deserialize<T>(File.ReadAllText(yamlPath));
            }

            return File.Exists(bakPath) ? deserializer.Deserialize<T>(File.ReadAllText(bakPath)) :
                // Return a new instance with default values already set
                new T();
        }

        public static void ReadConfig(string path = null)
        {
            path ??= Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new SingleStringToListConverter())
                .IgnoreUnmatchedProperties()
                .Build();

            Configuration config = new();

            foreach (ConfigTypes configType in (ConfigTypes[])Enum.GetValues(typeof(ConfigTypes)))
            {
                switch (configType)
                {
                    case ConfigTypes.SpotifyCredentials:
                        config.SpotifyCredentials = LoadOrCreateConfig<SpotifyCredentials>(path, "SpotifyCredentials", deserializer);
                        break;

                    case ConfigTypes.TwitchCredentials:
                        config.TwitchCredentials = LoadOrCreateConfig<TwitchCredentials>(path, "TwitchCredentials", deserializer);
                        break;

                    case ConfigTypes.BotConfig:
                        config.BotConfig = LoadOrCreateConfig<BotConfig>(path, "BotConfig", deserializer);
                        break;

                    case ConfigTypes.AppConfig:
                        config.AppConfig = LoadOrCreateConfig<AppConfig>(path, "AppConfig", deserializer);
                        break;

                    case ConfigTypes.TwitchCommands:
                        config.TwitchCommands = LoadOrCreateConfig<TwitchCommands>(path, "TwitchCommands", deserializer);

                        if (config.TwitchCommands.Commands.Count == 0)
                        {
                            config.TwitchCommands.Commands = DefaultCommands;
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Settings.Import(config);
        }

        public static string GenerateAccessKey()
        {
            const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_~.";
            string key = new([.. Enumerable.Repeat(allowedChars, 1)
                .SelectMany(s => s)
                .Take(128)
                .OrderBy(_ => Guid.NewGuid())]);

            return key;
        }

        public static void WriteAllConfig(Configuration config, string path = null, bool isBackup = false)
        {
            WriteConfig(ConfigTypes.AppConfig, config.AppConfig, path, isBackup);
            WriteConfig(ConfigTypes.BotConfig, config.BotConfig, path, isBackup);
            WriteConfig(ConfigTypes.SpotifyCredentials, config.SpotifyCredentials, path, isBackup);
            WriteConfig(ConfigTypes.TwitchCredentials, config.TwitchCredentials, path, isBackup);
            WriteConfig(ConfigTypes.TwitchCommands, config.TwitchCommands, path, isBackup);
        }
    }

    public class Configuration
    {
        public AppConfig AppConfig { get; set; }
        public SpotifyCredentials SpotifyCredentials { get; set; }
        public TwitchCredentials TwitchCredentials { get; set; }
        public BotConfig BotConfig { get; set; }
        public TwitchCommands TwitchCommands { get; set; }
    }

    public class SpotifyCredentials
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public PrivateProfile Profile { get; set; } = new();
        public List<SimplePlaylist> PlaylistCache { get; set; } = [];
    }

    public class TwitchCredentials
    {
        public string AccessToken { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public string ChannelId { get; set; } = "";
        public string BotAccountName { get; set; } = "";
        public string BotOAuthToken { get; set; } = "";
        public User TwitchUser { get; set; }
        public string TwitchBotToken { get; set; } = "";
        public User BotUser { get; set; }
        public string TwitchUserColor { get; set; }
        public DateTime AccessTokenExpiryDate { get; set; }
        public DateTime BotTokenExpiryDate { get; set; }

    }

    public class BotConfig
    {
        public bool BotCmdNext { get; set; }
        public bool BotCmdPlayPause { get; set; }
        public bool BotCmdPos { get; set; }
        public bool BotCmdQueue { get; set; }
        public bool BotCmdRemove { get; set; }
        public bool BotCmdSkip { get; set; }
        public bool BotCmdSkipVote { get; set; }
        public bool BotCmdSong { get; set; }
        public bool BotCmdSonglike { get; set; }
        public bool BotCmdVol { get; set; }
        public bool BotCmdVolIgnoreMod { get; set; }
        public bool BotCmdCommands { get; set; }
        public bool ChatLiveStatus { get; set; }
        public bool OnlyWorkWhenLive { get; set; }
        public int BotCmdSkipVoteCount { get; set; } = 5;
        public string BotCmdPlayPauseTrigger { get; set; } = "!play, !pause";
        public string BotCmdSkipVoteTrigger { get; set; } = "!voteskip";
        public string BotCmdVolTrigger { get; set; } = "!vol";
        public string BotCmdCommandsTrigger { get; set; } = "!songcommands";
        public string BotCmdNextTrigger { get; set; } = "next";
        public string BotCmdPosTrigger { get; set; } = "pos";
        public string BotCmdQueueTrigger { get; set; } = "queue";
        public string BotCmdRemoveTrigger { get; set; } = "remove";
        public string BotCmdSkipTrigger { get; set; } = "skip";
        public string BotCmdSonglikeTrigger { get; set; } = "songlike";
        public string BotCmdSongTrigger { get; set; } = "song";
        public string BotCmdSsrTrigger { get; set; } = "ssr";
        public string BotCmdVoteskipTrigger { get; set; } = "voteskip";
        public string BotRespBlacklist { get; set; } = "@{user} the Artist: {artist} has been blacklisted by the broadcaster.";
        public string BotRespCooldown { get; set; } = "The command is on cooldown. Try again in {cd} seconds.";
        public string BotRespError { get; set; } = "@{user} there was an error adding your Song to the queue. Error message: {errormsg}";
        public string BotRespExplicitSong { get; set; } = "This Song containts explicit content and is not allowed.";
        public string BotRespIsInQueue { get; set; } = "@{user} this song is already in the queue.";
        public string BotRespLength { get; set; } = "@{user} the song you requested exceeded the maximum song length ({maxlength}).";
        public string BotRespMaxReq { get; set; } = "@{user} maximum number of songs in queue reached ({maxreq}).";
        public string BotRespModSkip { get; set; } = "@{user} skipped the current song.";
        public string BotRespNext { get; set; } = "@{user} {song}";
        public string BotRespNoSong { get; set; } = "@{user} please specify a song to add to the queue.";
        public string BotRespNoTrackFound { get; set; } = "No track found.";
        public string BotRespPlaylist { get; set; } = "This song was not found in the allowed playlist.({playlist_name} {playlist_url})";
        public string BotRespPos { get; set; } = "@{user} {songs}{pos} {song}{/songs}";
        public string BotRespRefund { get; set; } = "Your points have been refunded.";
        public string BotRespRemove { get; set; } = "{user} your previous request ({song}) will be skipped.";
        public string BotRespSong { get; set; } = "@{user} {song}";
        public string BotRespSongLike { get; set; } = "The Song {song} has been added to the playlist.";
        public string BotRespSuccess { get; set; } = "{artist} - {title} requested by @{user} has been added to the queue.";
        public string BotRespUnavailable { get; set; } = "The Song {song} is not available in the streamers country.";
        public string BotRespVoteSkip { get; set; } = "@{user} voted to skip the current song. ({votes})";
        public string BotRespUserCooldown { get; set; } = "@{user} you have to wait {cd} before you can request a song again.";

        public string BotRespUserLevelTooLowCommand { get; set; } =
            "Sorry, only {userlevel} or higher can request songs using the command.";

        public string BotRespUserLevelTooLowReward { get; set; } = "Sorry, only {userlevel} or higher can request songs using the reward.";
    }

    public class TwitchCommands
    {
        public List<TwitchCommand> Commands { get; set; } = ConfigHandler.DefaultCommands;
    }

    public class BotCommandInfo
    {
        public string CommandName { get; set; }
        public string Trigger { get; set; }
    }

    public static class BotConfigExtensions
    {
        /// <summary>
        /// Returns a list of bot command names (the bool property name) that are enabled (true)
        /// along with their corresponding trigger (if one exists).
        /// </summary>
        public static IEnumerable<BotCommandInfo> GetAllBotCommands(BotConfig config, bool onlyEnabled = false)
        {
            Type type = typeof(BotConfig);

            // Get all public instance properties that are booleans and start with "BotCmd"
            IEnumerable<PropertyInfo> boolProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(bool) && p.Name.StartsWith("BotCmd"));

            foreach (PropertyInfo boolProp in boolProperties)
            {
                bool enabled = (bool)boolProp.GetValue(config);
                if (onlyEnabled)
                    if (!enabled) continue;
                // Construct the expected trigger property name (e.g., BotCmdNext -> BotCmdNextTrigger)
                string triggerPropName = boolProp.Name + "Trigger";
                PropertyInfo triggerProp = type.GetProperty(triggerPropName);
                string triggerValue = triggerProp != null ? (string)triggerProp.GetValue(config) : null;
                if (triggerValue != null && triggerValue.Contains("!"))
                {
                    triggerValue = triggerValue.Replace("!", "");
                }

                yield return new BotCommandInfo
                {
                    CommandName = boolProp.Name,
                    Trigger = triggerValue
                };
            }
        }
    }

    public class AppConfig
    {
        public bool AddSrToPlaylist { get; set; }
        public bool AnnounceInChat { get; set; }
        public bool AppendSpaces { get; set; }
        public bool AppendSpacesSplitFiles { get; set; }
        public bool AutoClearQueue { get; set; }
        public bool Autostart { get; set; }
        public bool AutoStartWebServer { get; set; }
        public bool BetaUpdates { get; set; }
        public bool BlockAllExplicitSongs { get; set; }
        public bool BotOnlyWorkWhenLive { get; set; }
        public bool CustomPauseTextEnabled { get; set; }
        public bool DonationReminder { get; set; }
        public bool DownloadCanvas { get; set; }
        public bool DownloadCover { get; set; }
        public bool KeepAlbumCover { get; set; } = false;
        public bool LimitSrToPlaylist { get; set; }
        public bool MsgLoggingEnabled { get; set; }
        public bool OpenQueueOnStartup { get; set; }
        public bool RewardGoalEnabled { get; set; }
        public bool SaveHistory { get; set; }
        public bool SplitOutput { get; set; }
        public bool SpotifyControlVisible { get; set; }
        public bool Systray { get; set; }
        public bool Telemetry { get; set; }
        public bool TwAutoConnect { get; set; } = true;
        public bool TwSrCommand { get; set; }
        public bool TwSrReward { get; set; }
        public bool TwSrUnlimitedSr { get; set; }
        public bool UpdateRequired { get; set; } = true;
        public bool Upload { get; set; }
        public bool UploadHistory { get; set; }
        public bool UseDefaultBrowser { get; set; }
        public bool UseOwnApp { get; set; }
        public PauseOptions PauseOption { get; set; } = PauseOptions.Nothing;
        public int ChromeFetchRate { get; set; } = 1;
        public int FontSize { get; set; } = 22;
        public int FontsizeQueue { get; set; } = 12;
        public int LastShownMotdId { get; set; }
        public int MaxSongLength { get; set; } = 10;
        public int Player { get; internal set; }
        public int PosX { get; set; } = 100;
        public int PosY { get; set; } = 100;
        public int RewardGoalAmount { get; set; }
        public int SpaceCount { get; set; } = 10;
        public int TwitchFetchPort { get; set; } = 4004;
        public int TwitchRedirectPort { get; set; } = 4003;
        public int TwSrCooldown { get; set; } = 5;
        public int TwSrMaxReq { get; set; } = 3;
        public int TwSrMaxReqBroadcaster { get; set; } = 3;
        public int TwSrMaxReqEveryone { get; set; } = 3;
        public int TwSrMaxReqFollower { get; set; } = 3;
        public int TwSrMaxReqModerator { get; set; } = 3;
        public int TwSrMaxReqSubscriber { get; set; } = 3;
        public int TwSrMaxReqSubscriberT2 { get; set; } = 3;
        public int TwSrMaxReqSubscriberT3 { get; set; } = 3;
        public int TwSrMaxReqVip { get; set; } = 3;
        public int TwSrPerUserCooldown { get; set; } = 0;
        public int TwSrUserLevel { get; set; } = 1;
        public int WebServerPort { get; set; } = 65530;
        public int[] RefundConditons { get; set; } = [];
        public List<int> QueueWindowColumns { get; set; } = [0, 1, 2, 3, 4];
        public List<int> ReadNotificationIds { get; set; } = [];
        public List<int> UserLevelsCommand { get; set; } = [0, 1, 2, 3];
        public List<int> UserLevelsReward { get; set; } = [0, 1, 2, 3];
        public List<string> ArtistBlacklist { get; set; } = [];
        public List<string> TwRewardId { get; set; } = [];
        public List<string> TwRewardSkipId { get; set; } = [];
        public List<string> UserBlacklist { get; set; } = [];
        public List<TrackItem> SongBlacklist { get; set; } = [];
        public string AccessKey { get; set; } = ConfigHandler.GenerateAccessKey();
        public string BaseUrl { get; set; } = "https://songify.rocks";
        public string Color { get; set; } = "Blue";
        public string CustomPauseText { get; set; } = "";
        public string Directory { get; set; } = "";
        public string Language { get; set; } = "en";
        public string OutputString { get; set; } = "{artist} - {title} {extra}";
        public string OutputString2 { get; set; } = "{artist} - {title} {extra}";
        public string RequesterPrefix { get; set; } = "Requested by ";
        public string RewardGoalSong { get; set; } = "";
        public string SpotifyPlaylistId { get; set; } = "";
        public string SpotifySongLimitPlaylist { get; set; } = "";
        public string Theme { get; set; } = "Light";
        public string TwRewardGoalRewardId { get; set; } = "";
        public string Uuid { get; set; } = "";
        public bool ShowUserLevelBadges { get; set; } = true;
        public List<int> UnlimitedSrUserlevelsReward { get; set; } = [];
        public List<int> UnlimitedSrUserlevelsCommand { get; set; } = [];

        public string WebUserAgent = "Songify Data Provider";
        public string YtmdToken;
    }
}