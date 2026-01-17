using Microsoft.Build.Utilities;
using Songify_Slim.Models.Blocklist;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Songify_Slim.Util.Configuration
{
    public sealed class SongBlacklistConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(List<BlockedSong>);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            List<BlockedSong> list = [];

            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                // Read entry as dictionary to detect legacy vs new
                Dictionary<string, object> dict = nestedObjectDeserializer(typeof(Dictionary<string, object>)) as Dictionary<string, object>;
                if (dict == null)
                    continue;

                bool isLegacy = dict.Keys.Any(k =>
                    string.Equals(k, "trackId", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "trackName", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "artists", StringComparison.OrdinalIgnoreCase));

                if (isLegacy)
                {
                    LegacyTrackItem legacy = new()
                    {
                        Artists = GetString(dict, "artists"),
                        TrackName = GetString(dict, "trackName"),
                        TrackId = GetString(dict, "trackId"),
                        TrackUri = GetString(dict, "trackUri"),
                        ReadableName = GetString(dict, "readableName"),
                    };

                    list.Add(new BlockedSong
                    {
                        Id = legacy.TrackId?.Trim(),
                        Artist = legacy.Artists?.Trim(),
                        Title = legacy.TrackName?.Trim(),
                        // Key/Display are computed -> do NOT set them
                    });

                    continue;
                }

                // New format mapping (ignore key/display if present in yaml)
                string id = GetString(dict, "id");
                string artist = GetString(dict, "artist");
                string title = GetString(dict, "title");

                // Only add if there's something meaningful
                if (string.IsNullOrWhiteSpace(id) &&
                    string.IsNullOrWhiteSpace(title))
                    continue;

                list.Add(new BlockedSong
                {
                    Id = id?.Trim(),
                    Artist = artist?.Trim(),
                    Title = title?.Trim(),
                });
            }

            return list;
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            // Writes the new format. If Key/Display are [YamlIgnore], they won't be written.
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

            if (value is IEnumerable<BlockedSong> items)
            {
                foreach (BlockedSong item in items)
                    nestedObjectSerializer(item);
            }

            emitter.Emit(new SequenceEnd());
        }

        private sealed class LegacyTrackItem
        {
            public string Artists { get; set; }
            public string TrackName { get; set; }
            public string TrackId { get; set; }
            public string TrackUri { get; set; }
            public string ReadableName { get; set; }
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            foreach (KeyValuePair<string, object> kv in dict)
            {
                if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (kv.Value is string s) return s;
                return kv.Value?.ToString() ?? "";
            }
            return "";
        }
    }
}