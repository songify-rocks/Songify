using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Songify_Slim.Util.General
{
    /// <summary>
    /// Documentation and JSON export for WebSocket command messages (client to server).
    /// Must stay in sync with <see cref="WebServer"/> command map keys (validated in DEBUG).
    /// </summary>
    public static class WebSocketCommandCatalog
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        /// <summary>Serialized payload for <c>GET /ws-commands.json</c>.</summary>
        public static string ToJson() =>
            JsonConvert.SerializeObject(new { commands = Commands }, JsonSettings);

        /// <summary>
        /// Every <paramref name="commandMapKeys"/> entry must appear as <see cref="WebSocketDocCommand.Action"/>
        /// or in <see cref="WebSocketDocCommand.Aliases"/>.
        /// </summary>
        public static void AssertCoversCommandMap(IEnumerable<string> commandMapKeys)
        {
#if DEBUG
            HashSet<string> documented = BuildDocumentedKeySet();
            foreach (string key in commandMapKeys)
            {
                if (!documented.Contains(key))
                {
                    Debug.Fail($"WebSocket CommandMap key '{key}' is missing from WebSocketCommandCatalog.");
                }
            }
#endif
        }

        internal static HashSet<string> BuildDocumentedKeySet(StringComparer comparer = null)
        {
            comparer ??= StringComparer.OrdinalIgnoreCase;
            HashSet<string> set = new HashSet<string>(comparer);
            foreach (WebSocketDocCommand cmd in Commands)
            {
                if (!string.IsNullOrEmpty(cmd.Action))
                    set.Add(cmd.Action);
                if (cmd.Aliases == null) continue;
                foreach (string alias in cmd.Aliases)
                {
                    if (!string.IsNullOrEmpty(alias))
                        set.Add(alias);
                }
            }

            return set;
        }

        public static IReadOnlyList<WebSocketDocCommand> Commands { get; } = new WebSocketDocCommand[]
        {
            new WebSocketDocCommand
            {
                Group = "overlay",
                Action = "youtube",
                Aliases = Array.Empty<string>(),
                Description = "Updates overlay YouTube metadata (videoId, title, artist, etc.).",
                DataRequired = true,
                ExampleJson =
                    "{\"action\":\"youtube\",\"data\":{\"videoId\":\"dQw4w9WgXcQ\",\"title\":\"Example\",\"artist\":\"Artist\",\"channel\":\"Channel\",\"cover\":\"\",\"hash\":\"\"}}"
            },
            new WebSocketDocCommand
            {
                Group = "playback",
                Action = "queue_add",
                Aliases = Array.Empty<string>(),
                Description = "Adds a track to the queue (Spotify URI/URL or search; Pear uses same field).",
                DataRequired = true,
                ExampleJson =
                    "{\"action\":\"queue_add\",\"data\":{\"track\":\"https://open.spotify.com/track/4PTG3Z6ehGkBFwjybzWkR8\",\"requester\":\"Viewer42\"}}"
            },
            new WebSocketDocCommand
            {
                Group = "playback",
                Action = "play_playlist",
                Aliases = Array.Empty<string>(),
                Description = "Starts playback from a Spotify playlist URI/ID with optional shuffle.",
                DataRequired = true,
                ExampleJson = "{\"action\":\"play_playlist\",\"data\":{\"playlist\":\"spotify:playlist:37i9dQZF1DXcBWIGoYBM5M\",\"shuffle\":false}}"
            },
            new WebSocketDocCommand
            {
                Group = "playback",
                Action = "skip",
                Aliases = new[] { "next" },
                Description = "Skips to the next track.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"skip\"}"
            },
            new WebSocketDocCommand
            {
                Group = "playback",
                Action = "play_pause",
                Aliases = new[] { "pause", "play" },
                Description = "Toggles or forces play/pause depending on action name and current playback.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"play_pause\"}"
            },
            new WebSocketDocCommand
            {
                Group = "playback",
                Action = "send_to_chat",
                Aliases = Array.Empty<string>(),
                Description = "Sends the current song info to Twitch chat.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"send_to_chat\"}"
            },
            new WebSocketDocCommand
            {
                Group = "twitch",
                Action = "stop_sr_reward",
                Aliases = Array.Empty<string>(),
                Description = "Pauses all configured Twitch channel-point song request rewards.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"stop_sr_reward\"}"
            },
            new WebSocketDocCommand
            {
                Group = "twitch",
                Action = "sr_enable",
                Aliases = new[] { "sr_open" },
                Description = "Enables song requests; optional data.scope: both, reward, or command.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"sr_enable\",\"data\":{\"scope\":\"both\"}}"
            },
            new WebSocketDocCommand
            {
                Group = "twitch",
                Action = "sr_disable",
                Aliases = new[] { "sr_close" },
                Description = "Disables song requests; optional data.scope: both, reward, or command.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"sr_disable\",\"data\":{\"scope\":\"both\"}}"
            },
            new WebSocketDocCommand
            {
                Group = "volume",
                Action = "vol_set",
                Aliases = Array.Empty<string>(),
                Description = "Sets player volume (0–100).",
                DataRequired = true,
                ExampleJson = "{\"action\":\"vol_set\",\"data\":{\"value\":80}}"
            },
            new WebSocketDocCommand
            {
                Group = "volume",
                Action = "vol_up",
                Aliases = Array.Empty<string>(),
                Description = "Increases volume by 5.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"vol_up\"}"
            },
            new WebSocketDocCommand
            {
                Group = "volume",
                Action = "vol_down",
                Aliases = Array.Empty<string>(),
                Description = "Decreases volume by 5.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"vol_down\"}"
            },
            new WebSocketDocCommand
            {
                Group = "blocklist",
                Action = "block_artist",
                Aliases = Array.Empty<string>(),
                Description = "Blocks the current track's artist.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"block_artist\"}"
            },
            new WebSocketDocCommand
            {
                Group = "blocklist",
                Action = "block_all_artists",
                Aliases = Array.Empty<string>(),
                Description = "Blocks all artists on the current track.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"block_all_artists\"}"
            },
            new WebSocketDocCommand
            {
                Group = "blocklist",
                Action = "block_song",
                Aliases = Array.Empty<string>(),
                Description = "Blocks the current song.",
                DataRequired = false,
                ExampleJson = "{\"action\":\"block_song\"}"
            },
            new WebSocketDocCommand
            {
                Group = "blocklist",
                Action = "block_user",
                Aliases = Array.Empty<string>(),
                Description = "Blocks the requester of the current song (if from queue).",
                DataRequired = false,
                ExampleJson = "{\"action\":\"block_user\"}"
            }
        };
    }

    public sealed class WebSocketDocCommand
    {
        public string Group { get; set; }
        public string Action { get; set; }
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public string ExampleJson { get; set; }
        public bool DataRequired { get; set; }
    }
}
