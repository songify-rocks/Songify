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
                    catch (Exception e)
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

        public static void ReadConfig()
        {
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Configuration config = new();

            foreach (Enums.ConfigTypes configType in (Enums.ConfigTypes[])Enum.GetValues(typeof(Enums.ConfigTypes)))
            {
                switch (configType)
                {
                    case Enums.ConfigTypes.SpotifyCredentials:
                        if (File.Exists($@"{path}\SpotifyCredentials.yaml"))
                        {
                            SpotifyCredentials p = deserializer.Deserialize<SpotifyCredentials>(File.ReadAllText($@"{path}\SpotifyCredentials.yaml"));
                            config.SpotifyCredentials = p;
                        }
                        else
                        {
                            config.SpotifyCredentials = new SpotifyCredentials
                            {
                                AccessToken = "",
                                RefreshToken = "",
                                DeviceId = "",
                                ClientId = "",
                                ClientSecret = "",
                                Profile = new PrivateProfile(),
                                PlaylistCache = new List<SimplePlaylist>()
                            };
                        }
                        break;
                    case Enums.ConfigTypes.TwitchCredentials:
                        if (File.Exists($@"{path}\TwitchCredentials.yaml"))
                        {
                            TwitchCredentials p = deserializer.Deserialize<TwitchCredentials>(File.ReadAllText($@"{path}\TwitchCredentials.yaml"));
                            config.TwitchCredentials = p;
                        }
                        else
                        {
                            config.TwitchCredentials = new TwitchCredentials
                            {
                                AccessToken = "",
                                ChannelName = "",
                                ChannelId = "",
                                BotAccountName = "",
                                BotOAuthToken = "",
                                TwitchUser = null,
                                TwitchBotToken = "",
                                BotUser = null,

                            };
                        }
                        break;
                    case Enums.ConfigTypes.BotConfig:
                        if (File.Exists($@"{path}\BotConfig.yaml"))
                        {
                            BotConfig p = deserializer.Deserialize<BotConfig>(File.ReadAllText($@"{path}\BotConfig.yaml"));
                            config.BotConfig = p;

                        }
                        else
                        {
                            config.BotConfig = new BotConfig
                            {
                                BotCmdNext = false,
                                BotCmdNextTrigger = "next",
                                BotCmdPos = false,
                                BotCmdPosTrigger = "pos",
                                BotCmdRemove = false,
                                BotCmdRemoveTrigger = "remove",
                                BotCmdSonglike = false,
                                BotCmdSonglikeTrigger = "songlike",
                                BotRespSongLike = "The Song {song} has been added to the playlist.",
                                BotCmdPlayPause = false,
                                BotCmdSkip = false,
                                BotCmdSkipTrigger = "skip",
                                BotCmdSkipVote = false,
                                BotCmdSkipVoteCount = 5,
                                BotCmdSong = false,
                                BotCmdSongTrigger = "song",
                                BotCmdSsrTrigger = "ssr",
                                BotCmdVoteskipTrigger = "voteskip",
                                BotRespBlacklist =
                                    "@{user} the Artist: {artist} has been blacklisted by the broadcaster.",
                                BotRespError =
                                    "@{user} there was an error adding your Song to the queue. Error message: {errormsg}",
                                BotRespIsInQueue = "@{user} this song is already in the queue.",
                                BotRespLength =
                                    "@{user} the song you requested exceeded the maximum song length ({maxlength}).",
                                BotRespMaxReq = "@{user} maximum number of songs in queue reached ({maxreq}).",
                                BotRespModSkip = "@{user} skipped the current song.",
                                BotRespNext = "@{user} {song}",
                                BotRespNoSong = "@{user} please specify a song to add to the queue.",
                                BotRespPos = "@{user} {songs}{pos} {song}{/songs}",
                                BotRespRefund = "Your points have been refunded.",
                                BotRespSong = "@{user} {song}",
                                BotRespSuccess = "{artist} - {title} requested by @{user} has been added to the queue.",
                                BotRespVoteSkip = "@{user} voted to skip the current song. ({votes})",
                                ChatLiveStatus = false,
                                OnlyWorkWhenLive = false,
                                BotRespPlaylist = "This song was not found in the allowed playlist.({playlist_name} {playlist_url})",
                                BotRespRemove = "{user} your previous request ({song}) will be skipped.",
                                BotRespUnavailable = "The Song {song} is not available in the streamers country.",
                                BotRespExplicitSong = "This Song containts explicit content and is not allowed.",
                                BotRespCooldown = "The command is on cooldown. Try again in {cd} seconds.",
                                BotRespNoTrackFound = "No track found.",
                                BotCmdVol = false,
                                BotCmdVolIgnoreMod = false
                            };
                        }
                        break;
                    case Enums.ConfigTypes.AppConfig:
                        if (File.Exists($@"{path}\AppConfig.yaml"))
                        {
                            deserializer = new DeserializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .WithTypeConverter(new YamlTypeConverters.SingleStringToListConverter())
                                .Build();
                            AppConfig p = deserializer.Deserialize<AppConfig>(File.ReadAllText($@"{path}\AppConfig.yaml"));
                            config.AppConfig = p;
                            config.AppConfig.AccessKey = string.IsNullOrWhiteSpace(config.AppConfig.AccessKey) ? GenerateAccessKey() : config.AppConfig.AccessKey;
                        }
                        else
                        {
                            config.AppConfig = new AppConfig
                            {
                                AnnounceInChat = false,
                                AppendSpaces = false,
                                AutoClearQueue = false,
                                Autostart = false,
                                CustomPauseTextEnabled = false,
                                DownloadCover = false,
                                MsgLoggingEnabled = false,
                                OpenQueueOnStartup = false,
                                SaveHistory = false,
                                SplitOutput = false,
                                Systray = false,
                                Telemetry = false,
                                TwAutoConnect = false,
                                TwSrCommand = false,
                                TwSrReward = false,
                                Upload = false,
                                UploadHistory = false,
                                UseOwnApp = false,
                                MaxSongLength = 10,
                                PosX = 100,
                                PosY = 100,
                                SpaceCount = 10,
                                TwSrCooldown = 5,
                                TwSrMaxReq = 1,
                                TwSrMaxReqBroadcaster = 1,
                                TwSrMaxReqEveryone = 1,
                                TwSrMaxReqModerator = 1,
                                TwSrMaxReqSubscriber = 1,
                                TwSrMaxReqVip = 1,
                                TwSrUserLevel = 1,
                                TwRewardId = new(),
                                RefundConditons = new int[]
                                {
                                },
                                ArtistBlacklist = new List<string>(),
                                Color = "Blue",
                                CustomPauseText = "",
                                Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()
                                    ?.Location),
                                Language = "en",
                                OutputString = "{artist} - {title} {extra} {{requested by @{req}}}",
                                OutputString2 = "{artist} - {title} {extra} {{requested by @{req}}} -> {url}",
                                Theme = "",
                                UserBlacklist = new List<string>(),
                                Uuid = "",
                                WebServerPort = 1025,
                                AutoStartWebServer = false,
                                BetaUpdates = false,
                                ChromeFetchRate = 1,
                                Player = 0,
                                WebUserAgent = "Songify Data Provider",
                                UpdateRequired = false,
                                BotOnlyWorkWhenLive = false,
                                TwSrUnlimitedSr = false,
                                TwRewardSkipId = "",
                                AccessKey = GenerateAccessKey(),
                                TwitchFetchPort = 4004,
                                TwitchRedirectPort = 4003,
                                TwRewardGoalRewardId = "",
                                RewardGoalEnabled = false,
                                RewardGoalSong = "",
                                RewardGoalAmount = 0,
                                SongBlacklist = new List<TrackItem>(),
                                SpotifyPlaylistId = "",
                                UserLevelsReward = null,
                                UserLevelsCommand = null,
                                AddSrToPlaylist = false,
                                QueueWindowColumns = new List<int>(),
                                SpotifySongLimitPlaylist = null,
                                LimitSrToPlaylist = false,
                                BlockAllExplicitSongs = false,
                                RequesterPrefix = "Requested by ",
                                UseDefaultBrowser = false,
                                DonationReminder = false,
                                PauseOption = Enums.PauseOptions.Nothing,
                                AppendSpacesSplitFiles = false,
                                FontSize = 22,
                                FontsizeQueue = 12,
                            };
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
            string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_~.";
            string key = new(Enumerable.Repeat(allowedChars, 1)
                .SelectMany(s => s)
                .Take(128)
                .OrderBy(s => Guid.NewGuid())
                .ToArray());

            return key;
        }

        public static void ReadXml(string path)
        {
            try
            {
                if (new FileInfo(path).Length == 0)
                {
                    //WriteXml(path);
                    return;
                }

                List<string> fileList = new() { "SpotifyCredentials.yaml", "TwitchCredentials.yaml", "BotConfig.yaml", "AppConfig.yaml" };
                if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                                @"\SpotifyCredentials.yaml"))
                {
                    fileList.Remove("SpotifyCredentials.yaml");
                }
                if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                                @"\TwitchCredentials.yaml"))
                {
                    fileList.Remove("TwitchCredentials.yaml");
                }
                if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                                @"\BotConfig.yaml"))
                {
                    fileList.Remove("BotConfig.yaml");
                }
                if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                                @"\AppConfig.yaml"))
                {
                    fileList.Remove("AppConfig.yaml");
                }


                foreach (string s in fileList.Where(s => File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + $@"\{s}")))
                {
                    fileList.Remove(s);
                }

                if (fileList.Count == 0)
                {
                    ReadConfig();
                    return;
                }

                Config config = new();
                // reading the XML file, attributes get saved in Settings
                XmlDocument doc = new();
                doc.Load(path);
                if (doc.DocumentElement == null) return;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.Name != "Config") continue;
                    //Create a new Config object and set the attributes

                    if (node.Attributes == null) continue;
                    config.AccessToken = node.Attributes["accesstoken"] != null ? node.Attributes["accesstoken"].Value : "";
                    config.AnnounceInChat = node.Attributes["announceinchat"] != null && ToBoolean(node.Attributes["announceinchat"].Value);
                    config.AppendSpaces = node.Attributes["spacesenabled"] != null && ToBoolean(node.Attributes["spacesenabled"].Value);
                    config.ArtistBlacklist = node.Attributes["artistblacklist"] != null ? node.Attributes["artistblacklist"].Value.Split(new[] { "|||" }, StringSplitOptions.None).ToList() : new List<string>();
                    config.AutoClearQueue = node.Attributes["autoclearqueue"] != null && ToBoolean(node.Attributes["autoclearqueue"].Value);
                    config.Autostart = node.Attributes["autostart"] != null && ToBoolean(node.Attributes["autostart"].Value);
                    config.BotCmdNext = node.Attributes["botcmdnext"] != null && ToBoolean(node.Attributes["botcmdnext"].Value);
                    config.BotCmdPos = node.Attributes["botcmdpos"] != null && ToBoolean(node.Attributes["botcmdpos"].Value);
                    config.BotCmdSkip = node.Attributes["botcmdskip"] != null && ToBoolean(node.Attributes["botcmdskip"].Value);
                    config.BotCmdSkipVote = node.Attributes["botcmdskipvote"] != null && ToBoolean(node.Attributes["botcmdskipvote"].Value);
                    config.BotCmdSkipVoteCount = node.Attributes["botcmdskipvotecount"] != null ? ToInt32(node.Attributes["botcmdskipvotecount"].Value) : 5;
                    config.BotCmdSong = node.Attributes["botcmdsong"] != null && ToBoolean(node.Attributes["botcmdsong"].Value);
                    config.BotRespBlacklist = node.Attributes["botrespblacklist"] != null ? node.Attributes["botrespblacklist"].Value : "@{user} the Artist: {artist} has been blacklisted by the broadcaster.";
                    config.BotRespError = node.Attributes["botresperror"] != null ? node.Attributes["botresperror"].Value : "@{user} there was an error adding your Song to the queue. Error message: {errormsg}";
                    config.BotRespIsInQueue = node.Attributes["botrespisinqueue"] != null ? node.Attributes["botrespisinqueue"].Value : "@{user} this song is already in the queue.";
                    config.BotRespLength = node.Attributes["botresplength"] != null ? node.Attributes["botresplength"].Value : "@{user} the song you requested exceeded the maximum song length ({maxlength}).";
                    config.BotRespMaxReq = node.Attributes["botrespmaxreq"] != null ? node.Attributes["botrespmaxreq"].Value : "@{user} maximum number of songs in queue reached ({maxreq}).";
                    config.BotRespModSkip = node.Attributes["botrespmodskip"] != null ? node.Attributes["botrespmodskip"].Value : "@{user} skipped the current song.";
                    config.BotRespNoSong = node.Attributes["botrespnosong"] != null ? node.Attributes["botrespnosong"].Value : "@{user} please specify a song to add to the queue.";
                    config.BotRespSuccess = node.Attributes["botrespsuccess"] != null ? node.Attributes["botrespsuccess"].Value : "{artist} - {title} requested by @{user} has been added to the queue.";
                    config.BotRespVoteSkip = node.Attributes["botrespvoteskip"] != null ? node.Attributes["botrespvoteskip"].Value : "@{user} voted to skip the current song. ({votes})";
                    config.ClientId = node.Attributes["clientid"] != null ? node.Attributes["clientid"].Value : "";
                    config.ClientSecret = node.Attributes["clientsecret"] != null ? node.Attributes["clientsecret"].Value : "";
                    config.Color = node.Attributes["color"] != null ? node.Attributes["color"].Value : "Blue";
                    config.CustomPauseText = node.Attributes["customPauseText"] != null ? node.Attributes["customPauseText"].Value : "";
                    config.CustomPauseTextEnabled = node.Attributes["customPause"] != null && ToBoolean(node.Attributes["customPause"].Value);
                    config.Directory = node.Attributes["directory"] != null ? node.Attributes["directory"].Value : Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                    config.DownloadCover = node.Attributes["downloadcover"] != null && ToBoolean(node.Attributes["downloadcover"].Value);
                    config.Language = node.Attributes["lang"] != null ? node.Attributes["lang"].Value : "en";
                    config.MaxSongLength = node.Attributes["maxsonglength"] != null ? ToInt32(node.Attributes["maxsonglength"].Value) : 10;
                    config.MsgLoggingEnabled = node.Attributes["msglogging"] != null && ToBoolean(node.Attributes["msglogging"].Value);
                    config.OpenQueueOnStartup = node.Attributes["openqueueonstartup"] != null && ToBoolean(node.Attributes["openqueueonstartup"].Value);
                    config.OutputString = node.Attributes["outputString"] != null ? node.Attributes["outputString"].Value : "{artist} - {title} {extra}";
                    config.OutputString2 = node.Attributes["outputString2"] != null ? node.Attributes["outputString2"].Value : "{artist} - {title} {extra}";
                    config.PosX = node.Attributes["posx"] != null ? ToInt32(node.Attributes["posx"].Value) : 100;
                    config.PosY = node.Attributes["posy"] != null ? ToInt32(node.Attributes["posy"].Value) : 100;
                    config.RefreshToken = node.Attributes["refreshtoken"] != null ? node.Attributes["refreshtoken"].Value : "";
                    config.SaveHistory = node.Attributes["savehistory"] != null && ToBoolean(node.Attributes["savehistory"].Value);
                    config.SpaceCount = node.Attributes["Spacecount"] != null ? ToInt32(node.Attributes["Spacecount"].Value) : 10;
                    config.SplitOutput = node.Attributes["splitoutput"] != null && ToBoolean(node.Attributes["splitoutput"].Value);
                    config.SpotifyDeviceId = node.Attributes["spotifydeviceid"] != null ? node.Attributes["spotifydeviceid"].Value : "";
                    config.Systray = node.Attributes["systray"] != null && ToBoolean(node.Attributes["systray"].Value);
                    config.Telemetry = node.Attributes["telemetry"] != null && ToBoolean(node.Attributes["telemetry"].Value);
                    config.Theme = node.Attributes["theme"] != null ? node.Attributes["theme"].Value : "Dark";
                    config.TwAcc = node.Attributes["twacc"] != null ? node.Attributes["twacc"].Value : "";
                    config.TwAutoConnect = node.Attributes["twautoconnect"] != null && ToBoolean(node.Attributes["twautoconnect"].Value);
                    config.TwChannel = node.Attributes["twchannel"] != null ? node.Attributes["twchannel"].Value : "";
                    config.TwOAuth = node.Attributes["twoauth"] != null ? node.Attributes["twoauth"].Value : "";
                    config.TwRewardId = node.Attributes["twrewardid"] != null ? node.Attributes["twrewardid"].Value : "";
                    config.TwSrCommand = node.Attributes["twsrcommand"] != null && ToBoolean(node.Attributes["twsrcommand"].Value);
                    config.TwSrCooldown = node.Attributes["twsrcooldown"] != null ? ToInt32(node.Attributes["twsrcooldown"].Value) : 5;
                    config.TwSrMaxReq = node.Attributes["twsrmaxreq"] != null ? ToInt32(node.Attributes["twsrmaxreq"].Value) : 1;
                    config.TwSrMaxReqBroadcaster = node.Attributes["twsrmaxreqbroadcaster"] != null ? ToInt32(node.Attributes["twsrmaxreqbroadcaster"].Value) : 1;
                    config.TwSrMaxReqEveryone = node.Attributes["twsrmaxreqeveryone"] != null ? ToInt32(node.Attributes["twsrmaxreqeveryone"].Value) : 1;
                    config.TwSrMaxReqModerator = node.Attributes["twsrmaxreqmoderator"] != null ? ToInt32(node.Attributes["twsrmaxreqmoderator"].Value) : 1;
                    config.TwSrMaxReqSubscriber = node.Attributes["twsrmaxreqsubscriber"] != null ? ToInt32(node.Attributes["twsrmaxreqsubscriber"].Value) : 1;
                    config.TwSrMaxReqVip = node.Attributes["twsrmaxreqvip"] != null ? ToInt32(node.Attributes["twsrmaxreqvip"].Value) : 1;
                    config.TwSrReward = node.Attributes["twsrreward"] != null && ToBoolean(node.Attributes["twsrreward"].Value);
                    config.TwSrUserLevel = node.Attributes["twsruserlevel"] != null ? ToInt32(node.Attributes["twsruserlevel"].Value) : 1;
                    config.Upload = node.Attributes["uploadSonginfo"] != null && ToBoolean(node.Attributes["uploadSonginfo"].Value);
                    config.UploadHistory = node.Attributes["uploadhistory"] != null && ToBoolean(node.Attributes["uploadhistory"].Value);
                    config.UseOwnApp = node.Attributes["useownapp"] != null && ToBoolean(node.Attributes["useownapp"].Value);
                    config.UserBlacklist = node.Attributes["userblacklist"] != null ? node.Attributes["userblacklist"].Value.Split(new[] { "|||" }, StringSplitOptions.None).ToList() : new List<string>();
                    config.Uuid = node.Attributes["uuid"] != null ? node.Attributes["uuid"].Value : "";
                }

                ConvertConfig(config);

            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static void LoadConfig(string path = "")
        {
            if (path != "")
            {
                ReadXml(path);
            }
            else
            {
                // OpenfileDialog with settings initialdirectory is the path were the exe is located
                OpenFileDialog openFileDialog = new()
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Filter = @"XML files (*.xml)|*.xml|All files (*.*)|*.*"
                };

                // Opening the dialog and when the user hits "OK" the following code gets executed
                if (openFileDialog.ShowDialog() == DialogResult.OK) ReadXml(openFileDialog.FileName);

                // This will iterate through all windows of the software, if the window is typeof
                // Settingswindow (from there this class is called) it calls the method SetControls
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                        if (window.GetType() == typeof(Window_Settings))
                            ((Window_Settings)window).SetControls();
                });
            }
        }

        public static void ConvertConfig(Config cfg)
        {
            SpotifyCredentials sp = new()
            {
                AccessToken = cfg.AccessToken,
                RefreshToken = cfg.RefreshToken,
                DeviceId = cfg.SpotifyDeviceId,
                ClientId = cfg.ClientId,
                ClientSecret = cfg.ClientSecret,
            };
            WriteConfig(Enums.ConfigTypes.SpotifyCredentials, sp);

            TwitchCredentials twitchCredentials = new()
            {
                AccessToken = "",
                ChannelName = cfg.TwChannel,
                ChannelId = "",
                BotAccountName = cfg.TwAcc,
                BotOAuthToken = cfg.TwOAuth
            };
            WriteConfig(Enums.ConfigTypes.TwitchCredentials, twitchCredentials);

            BotConfig botConfig = new()
            {
                BotCmdNext = cfg.BotCmdNext,
                BotCmdPos = cfg.BotCmdPos,
                BotCmdSkip = cfg.BotCmdSkip,
                BotCmdSkipVote = cfg.BotCmdSkipVote,
                BotCmdSong = cfg.BotCmdSong,
                BotCmdSkipVoteCount = cfg.BotCmdSkipVoteCount,
                BotRespBlacklist = cfg.BotRespBlacklist,
                BotRespError = cfg.BotRespError,
                BotRespIsInQueue = cfg.BotRespIsInQueue,
                BotRespLength = cfg.BotRespLength,
                BotRespMaxReq = cfg.BotRespMaxReq,
                BotRespModSkip = cfg.BotRespModSkip,
                BotRespNoSong = cfg.BotRespNoSong,
                BotRespSuccess = cfg.BotRespSuccess,
                BotRespVoteSkip = cfg.BotRespVoteSkip,

            };
            WriteConfig(Enums.ConfigTypes.BotConfig, botConfig);

            AppConfig appConfig = new()
            {
                AnnounceInChat = cfg.AnnounceInChat,
                AppendSpaces = cfg.AppendSpaces,
                AutoClearQueue = cfg.AutoClearQueue,
                Autostart = cfg.Autostart,
                CustomPauseTextEnabled = cfg.CustomPauseTextEnabled,
                DownloadCover = cfg.DownloadCover,
                MsgLoggingEnabled = cfg.MsgLoggingEnabled,
                OpenQueueOnStartup = cfg.OpenQueueOnStartup,
                SaveHistory = cfg.SaveHistory,
                SplitOutput = cfg.SplitOutput,
                Systray = cfg.Systray,
                Telemetry = cfg.Telemetry,
                TwAutoConnect = cfg.TwAutoConnect,
                TwSrCommand = cfg.TwSrCommand,
                TwSrReward = cfg.TwSrReward,
                Upload = cfg.Upload,
                UploadHistory = cfg.UploadHistory,
                UseOwnApp = cfg.UseOwnApp,
                MaxSongLength = cfg.MaxSongLength,
                PosX = cfg.PosX,
                PosY = cfg.PosY,
                SpaceCount = cfg.SpaceCount,
                TwSrCooldown = cfg.TwSrCooldown,
                TwSrMaxReq = cfg.TwSrMaxReq,
                TwSrMaxReqBroadcaster = cfg.TwSrMaxReqBroadcaster,
                TwSrMaxReqEveryone = cfg.TwSrMaxReqEveryone,
                TwSrMaxReqModerator = cfg.TwSrMaxReqModerator,
                TwSrMaxReqSubscriber = cfg.TwSrMaxReqSubscriber,
                TwSrMaxReqVip = cfg.TwSrMaxReqVip,
                TwSrUserLevel = cfg.TwSrUserLevel,
                ArtistBlacklist = cfg.ArtistBlacklist,
                Color = cfg.Color,
                CustomPauseText = cfg.CustomPauseText,
                Directory = cfg.Directory,
                Language = cfg.Language,
                OutputString = cfg.OutputString,
                OutputString2 = cfg.OutputString2,
                Theme = cfg.Theme,
                UserBlacklist = cfg.UserBlacklist,
                Uuid = cfg.Uuid,
            };
            WriteConfig(Enums.ConfigTypes.AppConfig, appConfig);
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
        public bool BotCmdPos { get; set; }
        public bool BotCmdSkip { get; set; }
        public bool BotCmdSkipVote { get; set; }
        public bool BotCmdSong { get; set; }
        public int BotCmdSkipVoteCount { get; set; } = 5;
        public string BotRespBlacklist { get; set; } = "@{user} the Artist: {artist} has been blacklisted by the broadcaster.";
        public string BotRespError { get; set; } = "@{user} there was an error adding your Song to the queue. Error message: {errormsg}";
        public string BotRespIsInQueue { get; set; } = "@{user} this song is already in the queue.";
        public string BotRespLength { get; set; } = "@{user} the song you requested exceeded the maximum song length ({maxlength}).";
        public string BotRespMaxReq { get; set; } = "@{user} maximum number of songs in queue reached ({maxreq}).";
        public string BotRespModSkip { get; set; } = "@{user} skipped the current song.";
        public string BotRespNoSong { get; set; } = "@{user} please specify a song to add to the queue.";
        public string BotRespSuccess { get; set; } = "{artist} - {title} requested by @{user} has been added to the queue.";
        public string BotRespVoteSkip { get; set; } = "@{user} voted to skip the current song. ({votes})";
        public string BotRespPos { get; set; } = "@{user} {songs}{pos} {song}{/songs}";
        public string BotRespNext { get; set; } = "@{user} {song}";
        public bool OnlyWorkWhenLive { get; set; }
        public string BotCmdPosTrigger { get; set; } = "pos";
        public string BotCmdSongTrigger { get; set; } = "song";
        public string BotCmdNextTrigger { get; set; } = "next";
        public string BotCmdSkipTrigger { get; set; } = "skip";
        public string BotCmdVoteskipTrigger { get; set; } = "voteskip";
        public string BotCmdSsrTrigger { get; set; } = "ssr";
        public bool ChatLiveStatus { get; set; }
        public string BotRespSong { get; set; } = "@{user} {song}";
        public string BotRespRefund { get; set; } = "Your points have been refunded.";
        public bool BotCmdRemove { get; set; }
        public string BotCmdRemoveTrigger { get; set; } = "remove";
        public bool BotCmdSonglike { get; set; }
        public string BotCmdSonglikeTrigger { get; set; } = "songlike";
        public string BotRespSongLike { get; set; } = "The Song {song} has been added to the playlist.";
        public bool BotCmdPlayPause { get; set; }
        public string BotRespPlaylist { get; set; } = "This song was not found in the allowed playlist.({playlist_name} {playlist_url})";
        public string BotRespRemove { get; set; } = "{user} your previous request ({song}) will be skipped.";
        public string BotRespUnavailable { get; set; } = "The Song {song} is not available in the streamers country.";
        public string BotRespExplicitSong { get; set; } = "This Song containts explicit content and is not allowed.";
        public string BotRespCooldown { get; set; } = "The command is on cooldown. Try again in {cd} seconds.";
        public string BotRespNoTrackFound { get; set; } = "No track found.";
        public bool BotCmdVol { get; set; }
        public bool BotCmdVolIgnoreMod { get; set; } = false;
    }

    public class AppConfig
    {
        public bool AnnounceInChat { get; set; }
        public bool AppendSpaces { get; set; }
        public bool AutoClearQueue { get; set; }
        public bool Autostart { get; set; }
        public bool CustomPauseTextEnabled { get; set; }
        public bool DownloadCover { get; set; }
        public bool MsgLoggingEnabled { get; set; }
        public bool OpenQueueOnStartup { get; set; }
        public bool SaveHistory { get; set; }
        public bool SplitOutput { get; set; }
        public bool Systray { get; set; }
        public bool Telemetry { get; set; }
        public bool TwAutoConnect { get; set; }
        public bool TwSrCommand { get; set; }
        public bool TwSrReward { get; set; }
        public bool Upload { get; set; }
        public bool UploadHistory { get; set; }
        public bool UseOwnApp { get; set; }
        public int MaxSongLength { get; set; } = 10;
        public int PosX { get; set; } = 100;
        public int PosY { get; set; } = 100;
        public int SpaceCount { get; set; } = 10;
        public int TwSrCooldown { get; set; } = 5;
        public int TwSrMaxReq { get; set; } = 3;
        public int TwSrMaxReqBroadcaster { get; set; } = 3;
        public int TwSrMaxReqEveryone { get; set; } = 3;
        public int TwSrMaxReqModerator { get; set; } = 3;
        public int TwSrMaxReqSubscriber { get; set; } = 3;
        public int TwSrMaxReqVip { get; set; } = 3;
        public int TwSrUserLevel { get; set; } = 1;
        public List<string> TwRewardId { get; set; } = new();
        public int[] RefundConditons { get; set; } = Array.Empty<int>();
        public List<string> ArtistBlacklist { get; set; } = new();
        public string Color { get; set; } = "Blue";
        public string CustomPauseText { get; set; } = "";
        public string Directory { get; set; } = "";
        public string Language { get; set; } = "en";
        public string OutputString { get; set; } = "{artist} - {title} {extra}";
        public string OutputString2 { get; set; } = "{artist} - {title} {extra}";
        public string Theme { get; set; } = "Light";
        public List<string> UserBlacklist { get; set; } = new();
        public string Uuid { get; set; } = "";
        public int WebServerPort { get; set; } = 65530;
        public bool AutoStartWebServer { get; set; }
        public bool BetaUpdates { get; set; }
        public int ChromeFetchRate { get; set; } = 1;
        public int Player { get; internal set; }

        public string WebUserAgent = "Songify Data Provider";
        public bool UpdateRequired { get; set; } = true;
        public bool BotOnlyWorkWhenLive { get; set; }
        public bool TwSrUnlimitedSr { get; set; } = false;
        public string TwRewardSkipId { get; set; } = "";
        public string AccessKey { get; set; } = ConfigHandler.GenerateAccessKey();
        public int TwitchFetchPort { get; set; } = 4004;
        public int TwitchRedirectPort { get; set; } = 4003;
        public string TwRewardGoalRewardId { get; set; } = "";
        public bool RewardGoalEnabled { get; set; }
        public string RewardGoalSong { get; set; } = "";
        public int RewardGoalAmount { get; set; }
        public List<TrackItem> SongBlacklist { get; set; } = new();
        public string SpotifyPlaylistId { get; set; } = "";
        public List<int> UserLevelsReward { get; set; } = new();
        public List<int> UserLevelsCommand { get; set; } = new();
        public bool AddSrToPlaylist { get; set; } = false;
        public List<int> QueueWindowColumns { get; set; } = [0, 1, 2, 3, 4];
        public string SpotifySongLimitPlaylist { get; set; } = "";
        public bool LimitSrToPlaylist { get; set; } = false;
        public bool BlockAllExplicitSongs { get; set; } = false;
        public string RequesterPrefix { get; set; } = "Requested by ";
        public bool UseDefaultBrowser { get; set; } = false;
        public bool DonationReminder { get; set; } = false;
        public Enums.PauseOptions PauseOption { get; set; } = Enums.PauseOptions.Nothing;
        public bool AppendSpacesSplitFiles { get; set; } = false;
        public int FontSize { get; set; } = 22;
        public int FontsizeQueue { get; set; }
    }

    public class Config
    {
        // Create fields for each setting in the config file
        public bool AnnounceInChat { get; set; }
        public bool AppendSpaces { get; set; }
        public bool AutoClearQueue { get; set; }
        public bool Autostart { get; set; }
        public bool BotCmdNext { get; set; }
        public bool BotCmdPos { get; set; }
        public bool BotCmdSkip { get; set; }
        public bool BotCmdSkipVote { get; set; }
        public bool BotCmdSong { get; set; }
        public bool CustomPauseTextEnabled { get; set; }
        public bool DownloadCover { get; set; }
        public bool MsgLoggingEnabled { get; set; }
        public bool OpenQueueOnStartup { get; set; }
        public bool SaveHistory { get; set; }
        public bool SplitOutput { get; set; }
        public bool Systray { get; set; }
        public bool Telemetry { get; set; }
        public bool TwAutoConnect { get; set; }
        public bool TwSrCommand { get; set; }
        public bool TwSrReward { get; set; }
        public bool Upload { get; set; }
        public bool UploadHistory { get; set; }
        public bool UseOwnApp { get; set; }
        public int BotCmdSkipVoteCount { get; set; }
        public int MaxSongLength { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int SpaceCount { get; set; }
        public int TwSrCooldown { get; set; }
        public int TwSrMaxReq { get; set; }
        public int TwSrMaxReqBroadcaster { get; set; }
        public int TwSrMaxReqEveryone { get; set; }
        public int TwSrMaxReqModerator { get; set; }
        public int TwSrMaxReqSubscriber { get; set; }
        public int TwSrMaxReqVip { get; set; }
        public int TwSrUserLevel { get; set; }
        public string AccessToken { get; set; }
        public List<string> ArtistBlacklist { get; set; }
        public string BotRespBlacklist { get; set; }
        public string BotRespError { get; set; }
        public string BotRespIsInQueue { get; set; }
        public string BotRespLength { get; set; }
        public string BotRespMaxReq { get; set; }
        public string BotRespModSkip { get; set; }
        public string BotRespNoSong { get; set; }
        public string BotRespSuccess { get; set; }
        public string BotRespVoteSkip { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Color { get; set; }
        public string CustomPauseText { get; set; }
        public string Directory { get; set; }
        public string Language { get; set; }
        public string OutputString { get; set; }
        public string OutputString2 { get; set; }
        public string RefreshToken { get; set; }
        public string SpotifyDeviceId { get; set; }
        public string Theme { get; set; }
        public string TwAcc { get; set; }
        public string TwChannel { get; set; }
        public string TwOAuth { get; set; }
        public string TwRewardId { get; set; }
        public List<string> UserBlacklist { get; set; }
        public string Uuid { get; set; }
    }
}