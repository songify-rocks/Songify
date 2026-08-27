using Newtonsoft.Json;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Views.WPFUI;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Songify_Slim.Util.Configuration.YamlTypeConverters;
using static Songify_Slim.Util.General.Enums;
using File = System.IO.File;
using FileMode = System.IO.FileMode;

namespace Songify_Slim.Util.Configuration
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
                CommandType = CommandType.ToggleSr,
                Trigger = "togglesr",
                Aliases = null,
                Response = "Song requests are now {state}",
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
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6],
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
                    { "SkipCount", 5 }
                }
            },

            new()
            {
                CommandType = CommandType.Song,
                Trigger = "song",
                Response = "@{user} {single_artist} - {title} {{requested by @{req}}}",
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
                    { "VolumeSetResponse", "Spotify volume set to {vol}%" }
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
            },

            new()
            {
                CommandType = CommandType.BanSong,
                Trigger = "bansong",
                Response = "The song {song} has been added to the blocklist.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.SkipPoll,
                Trigger = "skippoll",
                Response = "@{user} started a skip poll.",
                IsEnabled = false,
                AllowedUserLevels = [6],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            },

            new()
            {
                CommandType = CommandType.Playlist,
                Trigger = "playlist",
                Response = "@{user} {playlist_name} {playlist_url}",
                IsEnabled = false,
                AllowedUserLevels = [0, 1, 2, 3, 4, 5, 6,],
                IsAnnouncement = false,
                AnnouncementColor = AnnouncementColor.Blue,
                CustomProperties = new Dictionary<string, object>()
            }
        ];

        public static void WriteConfig(ConfigTypes configType, object o, string path = null, bool isBackup = false)
        {
            path ??= AppPaths.GetAppDirectory();
            Directory.CreateDirectory(path);

            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string fileEnding = isBackup ? ".bak" : ".yaml";
            string fileName = configType switch
            {
                ConfigTypes.SpotifyCredentials => "SpotifyCredentials",
                ConfigTypes.TwitchCredentials => "TwitchCredentials",
                ConfigTypes.BotConfig => "BotConfig",
                ConfigTypes.AppConfig => "AppConfig",
                ConfigTypes.TwitchCommands => "TwitchCommands",
                ConfigTypes.BlockedSpotifyArtists => "BlockedSpotifyArtists",
                _ => throw new ArgumentOutOfRangeException(nameof(configType), configType, null)
            };

            object configObject = configType switch
            {
                ConfigTypes.SpotifyCredentials => o as SpotifyCredentials ?? throw new InvalidOperationException(),
                ConfigTypes.TwitchCredentials => o as TwitchCredentials ?? throw new InvalidOperationException(),
                ConfigTypes.BotConfig => o as BotConfig ?? throw new InvalidOperationException(),
                ConfigTypes.AppConfig => o as AppConfig ?? throw new InvalidOperationException(),
                ConfigTypes.TwitchCommands => o as TwitchCommands ?? throw new InvalidOperationException(),
                ConfigTypes.BlockedSpotifyArtists => o as BlockedSpotifyArtists ?? throw new InvalidOperationException(),
                _ => throw new ArgumentOutOfRangeException(nameof(configType), configType, null)
            };

            string yaml = serializer.Serialize(configObject);

            string fullPath = Path.Combine(path, fileName + fileEnding);
            string tempPath = Path.Combine(path, Path.GetRandomFileName()); // same dir/volume

            try
            {
                // 1) Write temp file and flush to disk
                using (FileStream fs = new(
                           tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                           FileOptions.WriteThrough | FileOptions.SequentialScan))
                using (StreamWriter writer = new(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                {
                    writer.Write(yaml);
                    writer.Flush();
                    fs.Flush(flushToDisk: true);
                }

                // 2) Ensure destination isn’t read-only
                if (File.Exists(fullPath))
                {
                    FileAttributes attrs = File.GetAttributes(fullPath);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(fullPath, attrs & ~FileAttributes.ReadOnly);
                }

                // 3) Replace/move with small retry loop (handles transient locks/AV)
                const int maxAttempts = 6;
                int delayMs = 50;

                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            File.Replace(tempPath, fullPath, destinationBackupFileName: null,
                                ignoreMetadataErrors: true);
                        }
                        else
                        {
                            File.Move(tempPath, fullPath);
                        }

                        break; // success
                    }
                    catch (IOException) when (attempt < maxAttempts)
                    {
                        Thread.Sleep(delayMs);
                        delayMs *= 2;
                    }
                    catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                    {
                        Thread.Sleep(delayMs);
                        delayMs *= 2;
                    }
                }
            }
            catch (Exception ex)
            {
                // Clean up temp if anything failed
                try
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
                catch
                {
                    /* ignore */
                }

                Logger.Error(LogSource.Core, "Error writing config files.", ex);
            }
        }

        internal static void MigrateReleaseChannel(AppConfig appConfig)
        {
            if (appConfig.ReleaseChannel != null)
            {
                appConfig.BetaUpdates = appConfig.ReleaseChannel == ReleaseChannel.Beta;
                return;
            }

            appConfig.ReleaseChannel = appConfig.BetaUpdates ? ReleaseChannel.Beta : ReleaseChannel.Stable;
        }

        private static T LoadOrCreateConfig<T>(string path, string fileName, IDeserializer deserializer) where T : new()
        {
            string yamlPath = Path.Combine(path, fileName + ".yaml");
            string bakPath = Path.Combine(path, fileName + ".bak");

            static T TryRead<T>(string p, IDeserializer d)
            {
                if (!File.Exists(p)) return default;
                using FileStream fs = new(p, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using StreamReader sr = new(fs);
                string text = sr.ReadToEnd();
                return d.Deserialize<T>(text);
            }

            try
            {
                T fromYaml = TryRead<T>(yamlPath, deserializer);
                if (fromYaml != null) return fromYaml;
            }
            catch
            {
                /* corrupted yaml? fall through to bak */
            }

            try
            {
                T fromBak = TryRead<T>(bakPath, deserializer);
                if (fromBak != null) return fromBak;
            }
            catch
            {
                /* corrupted bak too */
            }

            return new T(); // defaults
        }

        public static void ReadConfig(string path = null)
        {
            path ??= AppPaths.GetAppDirectory();
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                //.WithTypeConverter(new SingleStringToListConverter())
                .WithTypeConverter(new PlaylistSnapshotYamlConverter())
                .WithTypeConverter(new PlayerTypeYamlConverter())
                .WithTypeConverter(new ListStringOrObjectConverter<BlockedArtist>(s => new BlockedArtist { Name = s }))
                .WithTypeConverter(new ListStringOrObjectConverter<BlockedUser>(s => new BlockedUser { Username = s }))
                .WithTypeConverter(new SongBlacklistConverter())
                .IgnoreUnmatchedProperties()
                .Build();

            Configuration config = new();

            foreach (ConfigTypes configType in (ConfigTypes[])Enum.GetValues(typeof(ConfigTypes)))
            {
                switch (configType)
                {
                    case ConfigTypes.SpotifyCredentials:
                        config.SpotifyCredentials =
                            LoadOrCreateConfig<SpotifyCredentials>(path, "SpotifyCredentials", deserializer);
                        break;

                    case ConfigTypes.TwitchCredentials:
                        config.TwitchCredentials =
                            LoadOrCreateConfig<TwitchCredentials>(path, "TwitchCredentials", deserializer);
                        break;

                    case ConfigTypes.BotConfig:
                        config.BotConfig = LoadOrCreateConfig<BotConfig>(path, "BotConfig", deserializer);
                        break;

                    case ConfigTypes.AppConfig:
                        config.AppConfig = LoadOrCreateConfig<AppConfig>(path, "AppConfig", deserializer);
                        config.AppConfig.DownloadCover = true;
                        MigrateReleaseChannel(config.AppConfig);
                        WriteConfig(ConfigTypes.AppConfig, config.AppConfig, path, false);
                        break;

                    case ConfigTypes.BlockedSpotifyArtists:
                        config.BlockedSpotifyArtists = LoadOrCreateConfig<BlockedSpotifyArtists>(path, "BlockedSpotifyArtists", deserializer);
                        WriteConfig(ConfigTypes.BlockedSpotifyArtists, config.BlockedSpotifyArtists, path, false);
                        break;

                    case ConfigTypes.TwitchCommands:
                        config.TwitchCommands =
                            LoadOrCreateConfig<TwitchCommands>(path, "TwitchCommands", deserializer);

                        if (config.TwitchCommands.Commands.Count == 0)
                        {
                            config.TwitchCommands.Commands = DefaultCommands;
                        }

                        // Check for any missing command types and add them from defaults
                        foreach (CommandType cmdType in Enum.GetValues(typeof(CommandType)))
                        {
                            if (config.TwitchCommands.Commands.All(c => c.CommandType != cmdType))
                            {
                                // Add the default command for this type
                                TwitchCommand defaultCmd = DefaultCommands.First(c => c.CommandType == cmdType);
                                config.TwitchCommands.Commands.Add(defaultCmd);
                            }
                            else
                            {
                                // Command exists but ensure CustomProperties contains expected keys for the command type
                                TwitchCommand existingCommand =
                                    config.TwitchCommands.Commands.First(c => c.CommandType == cmdType);
                                TwitchCommand defaultCommand = DefaultCommands.First(c => c.CommandType == cmdType);

                                // Ensure command has CustomProperties dictionary
                                existingCommand.CustomProperties ??= new Dictionary<string, object>();

                                // For specific command types with expected custom properties, ensure they exist
                                if (cmdType == CommandType.Voteskip)
                                {
                                    // Only add SkipCount if it doesn't exist in the existing command
                                    if (!existingCommand.CustomProperties.ContainsKey("SkipCount"))
                                    {
                                        existingCommand.CustomProperties["SkipCount"] =
                                            defaultCommand.CustomProperties.TryGetValue("SkipCount",
                                                out object customProperty)
                                                ? customProperty
                                                : 5;
                                    }
                                }
                                else if (cmdType == CommandType.Volume)
                                {
                                    // Only add VolumeSetResponse if it doesn't exist in the existing command
                                    if (!existingCommand.CustomProperties.ContainsKey("VolumeSetResponse"))
                                    {
                                        existingCommand.CustomProperties["VolumeSetResponse"] =
                                            defaultCommand.CustomProperties.TryGetValue("VolumeSetResponse",
                                                out object customProperty)
                                                ? customProperty
                                                : "Volume set to {vol}%.";
                                    }
                                }
                            }
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Settings.Import(config);
            // Keep only compact ID/name indexes in RAM; full artist objects stay on disk until edited.
            ArtistBlocklistStore.InitializeFromConfigAndUnload();
        }

        /// <summary>Load blocked artists from YAML without keeping them on <see cref="Settings.CurrentConfig"/>.</summary>
        public static BlockedSpotifyArtists LoadBlockedSpotifyArtists(string path = null)
        {
            path ??= AppPaths.GetAppDirectory();
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new ListStringOrObjectConverter<BlockedArtist>(s => new BlockedArtist { Name = s }))
                .IgnoreUnmatchedProperties()
                .Build();

            return LoadOrCreateConfig<BlockedSpotifyArtists>(path, "BlockedSpotifyArtists", deserializer);
        }

        public static void WriteAllConfig(Configuration config, string path = null, bool isBackup = false)
        {
            (ConfigTypes type, object obj)[] configsToWrite =
            [
                (ConfigTypes.AppConfig, config.AppConfig),
                (ConfigTypes.BotConfig, config.BotConfig),
                (ConfigTypes.SpotifyCredentials, config.SpotifyCredentials),
                (ConfigTypes.TwitchCredentials, config.TwitchCredentials),
                (ConfigTypes.TwitchCommands, config.TwitchCommands),
                (ConfigTypes.BlockedSpotifyArtists, config.BlockedSpotifyArtists)
            ];

            foreach ((ConfigTypes type, object obj) in configsToWrite)
            {
                // When the full artist list was unloaded from memory, CurrentConfig holds an empty list.
                // Disk is the source of truth — do not overwrite BlockedSpotifyArtists.yaml with [].
                if (type == ConfigTypes.BlockedSpotifyArtists &&
                    ReferenceEquals(config, Settings.CurrentConfig) &&
                    ArtistBlocklistStore.ShouldSkipConfigWrite())
                {
                    continue;
                }

                WriteConfig(type, obj, path, isBackup);
            }
        }

        public static async Task<Tuple<bool, HttpStatusCode>> CloudSaveSettings(string userId,
            Configuration config)
        {
            try
            {
                // Deep clone entire Configuration object
                Configuration clonedConfig = DeepCloneYaml(config);

                // Guard: never mutate the live config if YAML clone somehow shared AppConfig.
                if (ReferenceEquals(clonedConfig.AppConfig, config.AppConfig))
                {
                    Logger.Error(LogSource.Core,
                        "Cloud save aborted: deep clone shared AppConfig with live settings.");
                    return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.InternalServerError);
                }

                // Blocked artists may be unloaded from CurrentConfig — attach a fresh disk snapshot for cloud backup.
                clonedConfig.BlockedSpotifyArtists = new BlockedSpotifyArtists
                {
                    Artists = ArtistBlocklistStore.LoadCopy()
                };

                // Exclude tokens and sensitive data (clone only — live Settings must stay intact)
                clonedConfig.AppConfig.YoutubeApiKey = null;
                clonedConfig.AppConfig.SongifyApiKey = null;
                clonedConfig.AppConfig.WebServerPassword = null;

                // Strip sensitive information
                clonedConfig.SpotifyCredentials = null;
                clonedConfig.TwitchCredentials = null;

                // Serialize to YAML and then base64
                ISerializer serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                string yaml = serializer.Serialize(clonedConfig);
                string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(yaml));

                // Prepare HTTP body
                var body = new
                {
                    userId = userId,
                    settings = base64
                };

                using HttpResponseMessage response = await SongifyApi.PostUserSettingsAsync(
                    JsonConvert.SerializeObject(body));
                SongifyPremiumService.ApplyFromCloudStatus(response.StatusCode);
                switch (response.StatusCode)
                {
                    // Handle response codes 200 OK: User has premium access and operation succeeded
                    // 401 Unauthorized: Invalid token
                    // 403 Forbidden: User not found, no email, or no premium status
                    // 500 Internal Server Error: Database error
                    case HttpStatusCode.Unauthorized:
                        Logger.Warning(LogSource.Api,
                            "Cloud save failed: Unauthorized access. Invalid API token or user ID.");
                        return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode,
                            HttpStatusCode.Unauthorized);

                    case HttpStatusCode.Forbidden:
                        Logger.Warning(LogSource.Api,
                            "Cloud save failed: Forbidden access. User not found or no premium status.");
                        return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode, HttpStatusCode.Forbidden);

                    case HttpStatusCode.InternalServerError:
                        Logger.Warning(LogSource.Api,
                            "Cloud save failed: Internal server error. Please try again later.");
                        return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode,
                            HttpStatusCode.InternalServerError);
                }

                return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode, response.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error during cloud save.", ex);
                return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.ServiceUnavailable);
            }
        }

        private static T DeepCloneYaml<T>(T obj)
        {
            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            string yaml = serializer.Serialize(obj);
            return deserializer.Deserialize<T>(yaml);
        }

        /// <summary>
        /// Migrates legacy <see cref="AppConfig.ArtistBlacklist"/> into
        /// <see cref="BlockedSpotifyArtists"/> for fair restore diffs (does not write disk).
        /// </summary>
        private static void NormalizeBlockedArtistsForCompare(Configuration config)
        {
            if (config == null)
                return;

            config.BlockedSpotifyArtists ??= new BlockedSpotifyArtists();
            config.BlockedSpotifyArtists.Artists ??= [];

            List<BlockedArtist> legacy = config.AppConfig?.ArtistBlacklist;
            if (legacy is not { Count: > 0 })
            {
                if (config.AppConfig != null)
                    config.AppConfig.ArtistBlacklist = [];
                return;
            }

            HashSet<string> existingKeys = config.BlockedSpotifyArtists.Artists
                .Select(x => x.Key)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToHashSet();

            config.BlockedSpotifyArtists.Artists.AddRange(
                legacy.Where(x => !string.IsNullOrEmpty(x.Key) && existingKeys.Add(x.Key)));

            config.AppConfig.ArtistBlacklist = [];
        }

        public static async Task<Tuple<bool, HttpStatusCode>> CloudRestoreSettings(string userId)
        {
            try
            {
                string base64;
                HttpStatusCode statusCode;
                bool success;
                using (HttpResponseMessage response = await SongifyApi.GetUserSettingsAsync(userId))
                {
                    SongifyPremiumService.ApplyFromCloudStatus(response.StatusCode);
                    switch (response.StatusCode)
                    {
                        // Handle response codes 200 OK: User has premium access and operation succeeded
                        // 401 Unauthorized: Invalid token
                        // 403 Forbidden: User not found, no email, or no premium status
                        // 500 Internal Server Error: Database error
                        case HttpStatusCode.Unauthorized:
                            Logger.Warning(LogSource.Api,
                                "Cloud restore failed: Unauthorized access. Invalid API token or user ID.");
                            return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode,
                                HttpStatusCode.Unauthorized);

                        case HttpStatusCode.Forbidden:
                            Logger.Warning(LogSource.Api,
                                "Cloud restore failed: Forbidden access. User not found or no premium status.");
                            return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode, HttpStatusCode.Forbidden);

                        case HttpStatusCode.InternalServerError:
                            Logger.Warning(LogSource.Api,
                                "Cloud restore failed: Internal server error. Please try again later.");
                            return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode,
                                HttpStatusCode.InternalServerError);
                    }

                    // If the API returns the raw base64 blob directly:
                    base64 = await response.Content.ReadAsStringAsync();
                    statusCode = response.StatusCode;
                    success = response.IsSuccessStatusCode;
                }

                base64 = JsonConvert.DeserializeObject<string>(base64);
                if (string.IsNullOrWhiteSpace(base64))
                    return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.NoContent);
                //base64 = base64.Replace("\"", "");
                // Decode base64 and parse YAML
                string yaml = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

                IDeserializer deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                Configuration restoredConfig = deserializer.Deserialize<Configuration>(yaml);

                // Local blocked artists are usually unloaded from CurrentConfig (disk is source of truth).
                // Build a comparison snapshot the same way cloud save attaches artists for upload.
                Configuration localForCompare = DeepCloneYaml(Settings.CurrentConfig);
                localForCompare.BlockedSpotifyArtists = new BlockedSpotifyArtists
                {
                    Artists = ArtistBlocklistStore.LoadCopy()
                };
                if (localForCompare.AppConfig != null)
                    localForCompare.AppConfig.ArtistBlacklist = [];

                // Older cloud saves may still keep artists on AppConfig — normalize before diffing.
                NormalizeBlockedArtistsForCompare(restoredConfig);

                Window sW = new();
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is ShellWindow)
                        sW = win;
                }

                // Preview the import
                Window_CloudImportPreview preview = new(localForCompare, restoredConfig)
                {
                    Owner = sW,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (preview.DiffCount == 0)
                {
                    return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.NotModified);
                }

                preview.ShowDialog();

                if (!preview.IsConfirmed)
                {
                    return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.NotAcceptable);
                }

                await Settings.ImportCloudSave(restoredConfig);
                return new Tuple<bool, HttpStatusCode>(success, statusCode);
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error restoring cloud save.", ex);
                return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.ServiceUnavailable);
            }
        }

        private class CloudSettingsResponse
        {
            [JsonProperty("settings")] public string Settings { get; set; }

            [JsonProperty("updatedAt")] public DateTime? UpdatedAt { get; set; }
        }
    }

    public class Configuration
    {
        public AppConfig AppConfig { get; set; }
        public SpotifyCredentials SpotifyCredentials { get; set; }
        public TwitchCredentials TwitchCredentials { get; set; }
        public BotConfig BotConfig { get; set; }
        public TwitchCommands TwitchCommands { get; set; }
        public BlockedSpotifyArtists BlockedSpotifyArtists { get; set; }
    }

    public class SpotifyCredentials
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public PrivateUser Profile { get; set; } = new();
        public List<SpotifyPlaylistCache> PlaylistCache { get; set; } = new();
        public string RedirectUri { get; set; } = "localhost";
        public long SpotifyTokenExpiresAt { get; set; }
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
        public TwitchChatAccount TwitchChatAccount { get; set; }
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

        public string BotRespBlacklist { get; set; } =
            "@{user} the Artist: {artist} has been blocked by the broadcaster.";

        public string BotRespBlacklistSong { get; set; } =
            "@{user} the song: {song} has been blocked by the broadcaster.";

        public string BotRespCooldown { get; set; } = "The command is on cooldown. Try again in {cd} seconds.";

        public string BotRespError { get; set; } =
            "@{user} there was an error adding your Song to the queue. Error message: {errormsg}";

        public string BotRespExplicitSong { get; set; } = "This Song containts explicit content and is not allowed.";
        public string BotRespIsInQueue { get; set; } = "@{user} this song is already in the queue.";

        public string BotRespLength { get; set; } =
            "@{user} the song you requested exceeded the maximum song length ({maxlength}).";

        public string BotRespMaxReq { get; set; } = "@{user} maximum number of songs in queue reached ({maxreq}).";
        public string BotRespModSkip { get; set; } = "@{user} skipped the current song.";
        public string BotRespNext { get; set; } = "@{user} {song}";
        public string BotRespNoSong { get; set; } = "@{user} please specify a song to add to the queue.";
        public string BotRespNoTrackFound { get; set; } = "No track found.";

        public string BotRespPlaylist { get; set; } =
            "This song was not found in the allowed playlist.({playlist_name} {playlist_url})";

        public string BotRespPos { get; set; } = "@{user} {songs}{pos} {song}{/songs}";
        public string BotRespRefund { get; set; } = "Your points have been refunded.";
        public string BotRespRemove { get; set; } = "{user} your previous request ({song}) will be skipped.";
        public string BotRespSong { get; set; } = "@{user} {song}";
        public string BotRespSongLike { get; set; } = "The Song {song} has been added to the playlist.";

        public string BotRespSuccess { get; set; } =
            "{artist} - {title} requested by @{user} has been added to the queue.";

        public string BotRespUnavailable { get; set; } = "The Song {song} is not available in the streamers country.";
        public string BotRespVoteSkip { get; set; } = "@{user} voted to skip the current song. ({votes})";

        public string BotRespUserCooldown { get; set; } =
            "@{user} you have to wait {cd} before you can request a song again.";

        public string BotRespCommandDisabled { get; set; } = "@{user} the command {cmd} is not enabled.";

        public string BotRespPlayerOwnershipDenied { get; set; } =
            "@{user} song requests are currently handled by {active_player}.";

        public string BotRespUserLevelTooLowCommand { get; set; } =
            "Sorry, only {userlevel} or higher can request songs using the command.";

        public string BotRespUserLevelTooLowReward { get; set; } =
            "Sorry, only {userlevel} or higher can request songs using the reward.";
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
                    if (!enabled)
                        continue;
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
        /// <summary>Legacy bool from before <see cref="ReleaseChannel"/>. Kept so old YAML still deserializes.</summary>
        public bool BetaUpdates { get; set; }
        /// <summary>Null in configs written before release channels existed.</summary>
        public ReleaseChannel? ReleaseChannel { get; set; }
        public bool BlockAllExplicitSongs { get; set; }
        public bool BotOnlyWorkWhenLive { get; set; }
        public bool CustomPauseTextEnabled { get; set; }
        public bool DonationReminder { get; set; }
        public bool DownloadCanvas { get; set; }
        public bool DownloadCover { get; set; } = true;
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
        public bool UseDefaultBrowser { get; set; }
        public bool UseOwnApp { get; set; }
        public PauseOptions PauseOption { get; set; } = PauseOptions.Nothing;
        public int ChromeFetchRate { get; set; } = 1;
        public int FontSize { get; set; } = 22;
        public int FontsizeQueue { get; set; } = 12;
        public int LastShownMotdId { get; set; }
        public int MaxSongLength { get; set; } = 10;
        public PlayerType Player { get; internal set; }
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

        /// <summary>
        /// When true, WebSocket control commands require <see cref="WebServerPassword"/>
        /// (via auth action, per-command password field, or ?password= on the WS URL).
        /// Off by default — localhost is treated as trusted unless the user opts in.
        /// </summary>
        public bool WebServerPasswordEnabled { get; set; }

        /// <summary>Shared secret for optional WebSocket command authentication.</summary>
        public string WebServerPassword { get; set; } = "";

        public List<RefundCondition> RefundConditons { get; set; } = [];
        public List<int> QueueWindowColumns { get; set; } = [0, 1, 2, 3, 4];
        public List<int> ReadNotificationIds { get; set; } = [];
        public List<int> UserLevelsCommand { get; set; } = [0, 1, 2, 3];
        public List<int> UserLevelsReward { get; set; } = [0, 1, 2, 3];
        public List<int> UserLevelsExplicitSongs { get; set; } = [];
        public List<BlockedArtist> ArtistBlacklist { get; set; } = [];
        public List<string> TwRewardId { get; set; } = [];
        public List<string> TwRewardSkipId { get; set; } = [];
        public List<BlockedUser> UserBlacklist { get; set; } = [];
        public List<BlockedSong> SongBlacklist { get; set; } = [];
        public string BaseUrl { get; set; } = "https://songify.rocks";
        public string Color { get; set; } = "Blue";
        public string CustomPauseText { get; set; } = "";
        public string Directory { get; set; } = "";
        public string Language { get; set; } = "en";
        public string OutputString { get; set; } = "{artist} - {title} {extra}";
        public string OutputString2 { get; set; } = "{artist} - {title} {extra}";
        public string RequesterPrefix { get; set; } = "Requested by ";
        public string RewardGoalSong { get; set; } = "";
        public PlaylistSnapshot SpotifyPlaylistId { get; set; } = new();
        public string SpotifySongLimitPlaylist { get; set; } = "";
        public string Theme { get; set; } = "Dark";

        /// <summary>WPF-UI window backdrop: None, Auto, Mica, Acrylic, Tabbed.</summary>
        public string WindowBackdrop { get; set; } = "Mica";

        /// <summary>
        /// Extra UI zoom on top of Windows DPI (1.0 = 100%, 2.0 = 200%).
        /// Helps 1440p / 4K when the 900×500 layout is too small to read.
        /// </summary>
        public double UiScale { get; set; } = 1.0;

        public string TwRewardGoalRewardId { get; set; } = "";
        public string Uuid { get; set; } = "";
        public bool ShowUserLevelBadges { get; set; } = true;
        public List<int> UnlimitedSrUserlevelsReward { get; set; } = [];
        public List<int> UnlimitedSrUserlevelsCommand { get; set; } = [];
        public bool HideSpotifyPremiumWarning { get; set; }
        public bool LongBadgeNames { get; set; }
        public bool AddSrtoPlaylistOnly { get; set; } = false;
        public string SongifyApiKey { get; set; } = "";
        public bool SkipOnlyNonSrSongs { get; set; } = false;
        public bool SrForBits { get; set; } = false;
        public int SpotifyFetchRate { get; set; } = 2;

        /// <summary>
        /// When true, Spotify now-playing fetch runs even when not live on Twitch and TestMode is off.
        /// </summary>
        /// <remarks>
        /// Default gating exists to limit steady Spotify Web API traffic when the stream is offline, reducing
        /// unnecessary calls and the risk of rate limiting while Songify runs in the background.
        /// </remarks>
        public bool BypassSpotifyFetchGate { get; set; }

        /// <summary>
        /// When false, Windows toast popups for Spotify errors/rate limits are suppressed.
        /// The in-app persistent issue banner is unaffected.
        /// </summary>
        public bool ShowSpotifyToasts { get; set; } = true;

        /// <summary>When true, Songify hourly downloads <see cref="ArtistBlocklistSyncUrl"/> and merges artists into the blocklist.</summary>
        public bool ArtistBlocklistSyncEnabled { get; set; }

        /// <summary>Raw CSV URL used for hourly artist blocklist sync.</summary>
        public string ArtistBlocklistSyncUrl { get; set; } = "";

        /// <summary>CSV header name mapped to artist display name. Empty = auto-detect.</summary>
        public string ArtistBlocklistSyncNameColumn { get; set; } = "";

        /// <summary>CSV header name mapped to Spotify artist id. Empty or "(None)" = no id column.</summary>
        public string ArtistBlocklistSyncIdColumn { get; set; } = "";

        /// <summary>UTC ISO-8601 timestamp of the last successful sync.</summary>
        public string ArtistBlocklistSyncLastUtc { get; set; } = "";

        public bool DebugLogging { get; set; } = false;

        /// <summary>
        /// How many daily log files to keep per type (normal and DEBUG-). Older files are deleted.
        /// 0 = keep all (no pruning).
        /// </summary>
        public int LogFileRetentionCount { get; set; } = 5;

        public string YoutubeApiKey { get; set; }
        public TwitchPollSettings TwitchPollSettings { get; set; } = new();
        public List<string> TwRewardSkipPoll { get; set; } = [];
        public bool SharedChatEnabled { get; set; } = false;
        public string SrForBitsKeyWord { get; set; }
        public SpotifyPersistentIssue SpotifyPersistentIssue { get; set; }
        public List<SpotifyPersistentIssue> SpotifyPersistentIssues { get; set; } = new();

        /// <summary>
        /// Empty = use OS current SMTC session; otherwise match SourceAppUserModelId from the Windows media session.
        /// </summary>
        public string WindowsMediaSessionAumid { get; set; } = "";

        public string WebUserAgent = "Songify Data Provider";
        public string YtmdToken;
        public int MinimumBitsForSR = 1;

        /// <summary>True after the first-launch setup wizard was finished or skipped.</summary>
        public bool SetupCompleted { get; set; }

        /// <summary>True when the Overview "Getting started" card was dismissed.</summary>
        public bool SetupChecklistDismissed { get; set; }

        /// <summary>Last completed guided-setup flow version. 0 = never shown.</summary>
        public int SetupWizardVersion { get; set; }

        public int MinimumMessagesBetweenAnnounces { get; set; } = 0;
    }

    public class BlockedSpotifyArtists
    {
        public List<BlockedArtist> Artists { get; set; } = [];
    }
}