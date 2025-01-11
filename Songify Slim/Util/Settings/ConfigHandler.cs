using Songify_Slim.Util.General;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
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

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is for writing, exporting and importing the config file
    ///     The config file is XML and has a single config tag with attributes
    /// </summary>
    internal class ConfigHandler
    {
        public static void WriteConfig(Enums.ConfigTypes configType, object o, string path = null, bool isBackup = false)
        {
            path ??= Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string yaml;
            string fileEnding = isBackup ? ".bak" : ".yaml";

            switch (configType)
            {
                case Enums.ConfigTypes.SpotifyCredentials:
                    path += "/SpotifyCredentials" + fileEnding;
                    yaml = serializer.Serialize(o as SpotifyCredentials ?? throw new InvalidOperationException());
                    break;
                case Enums.ConfigTypes.TwitchCredentials:
                    path += "/TwitchCredentials" + fileEnding;
                    yaml = serializer.Serialize(o as TwitchCredentials ?? throw new InvalidOperationException());
                    break;
                case Enums.ConfigTypes.BotConfig:
                    path += "/BotConfig" + fileEnding;
                    yaml = serializer.Serialize(o as BotConfig ?? throw new InvalidOperationException());
                    break;
                case Enums.ConfigTypes.AppConfig:
                    try
                    {
                        path += "/AppConfig" + fileEnding;
                        yaml = serializer.Serialize(o as AppConfig ?? throw new InvalidOperationException());
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine(e);
                        return;
                    }
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
            else if (File.Exists(bakPath))
            {
                return deserializer.Deserialize<T>(File.ReadAllText(bakPath));
            }
            else
            {
                // Return a new instance with default values already set
                return new T();
            }
        }

        public static void ReadConfig(string path = null)
        {
            path ??= Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            Configuration config = new();

            foreach (Enums.ConfigTypes configType in (Enums.ConfigTypes[])Enum.GetValues(typeof(Enums.ConfigTypes)))
            {
                switch (configType)
                {
                    case Enums.ConfigTypes.SpotifyCredentials:
                        config.SpotifyCredentials = LoadOrCreateConfig<SpotifyCredentials>(path, "SpotifyCredentials", deserializer);
                        break;
                    case Enums.ConfigTypes.TwitchCredentials:
                        config.TwitchCredentials = LoadOrCreateConfig<TwitchCredentials>(path, "TwitchCredentials", deserializer);
                        break;
                    case Enums.ConfigTypes.BotConfig:
                        config.BotConfig = LoadOrCreateConfig<BotConfig>(path, "BotConfig", deserializer);
                        break;
                    case Enums.ConfigTypes.AppConfig:
                        config.AppConfig = LoadOrCreateConfig<AppConfig>(path, "AppConfig", deserializer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Settings.Import(config);
        }

        public static string GenerateAccessKey()
        {
            string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_~.";
            string key = new(Enumerable.Repeat(allowedChars, 1)
                .SelectMany(s => s)
                .Take(128)
                .OrderBy(s => Guid.NewGuid())
                .ToArray());

            return key;
        }

        public static void WriteAllConfig(Configuration config, string path = null, bool isBackup = false)
        {
            WriteConfig(Enums.ConfigTypes.AppConfig, config.AppConfig, path, isBackup);
            WriteConfig(Enums.ConfigTypes.BotConfig, config.BotConfig, path, isBackup);
            WriteConfig(Enums.ConfigTypes.SpotifyCredentials, config.SpotifyCredentials, path, isBackup);
            WriteConfig(Enums.ConfigTypes.TwitchCredentials, config.TwitchCredentials, path, isBackup);
        }
    }

    public class Configuration
    {
        public AppConfig AppConfig { get; set; }
        public SpotifyCredentials SpotifyCredentials { get; set; }
        public TwitchCredentials TwitchCredentials { get; set; }
        public BotConfig BotConfig { get; set; }
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
        public bool ChatLiveStatus { get; set; }
        public bool OnlyWorkWhenLive { get; set; }
        public int BotCmdSkipVoteCount { get; set; } = 5;
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
        public bool DownloadCover { get; set; }
        public bool DownloadCanvas { get; set; }
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
        public Enums.PauseOptions PauseOption { get; set; } = Enums.PauseOptions.Nothing;
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
        public string TwRewardSkipId { get; set; } = "";
        public string Uuid { get; set; } = "";
        public bool KeepAlbumCover { get; set; } = false;

        public string WebUserAgent = "Songify Data Provider";
        public string YTMDToken;
    }
}