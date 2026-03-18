using Songify_Slim.Models.Spotify;
using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Songify_Slim.Util.Configuration;

internal class YamlTypeConverters
{
    public class SingleStringToListConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(List<string>);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume(out Scalar scalar))
            {
                // If the node is a scalar (single string), return it as a single-item list
                return new List<string> { scalar.Value };
            }
            else if (parser.TryConsume(out SequenceStart _))
            {
                // If the node is a sequence, deserialize it as a list of strings
                List<string> list = [];
                while (!parser.TryConsume(out SequenceEnd _))
                {
                    string item = parser.Consume<Scalar>().Value;
                    list.Add(item);
                }
                return list;
            }
            throw new YamlException("Expected a scalar or sequence node.");
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            // not implemented
        }
    }

    public sealed class PlaylistSnapshotYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(PlaylistSnapshot);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            // Old format: spotifyPlaylistId: "61RwrFpWlO7Wyc8HzMDnlc"
            if (parser.TryConsume(out Scalar scalar))
            {
                string value = scalar.Value?.Trim();

                return string.IsNullOrEmpty(value)
                    ? new PlaylistSnapshot()
                    : new PlaylistSnapshot { PlaylistId = value };
            }

            parser.Consume<MappingStart>();

            PlaylistSnapshot result = new();

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                string key = parser.Consume<Scalar>().Value;

                // In your structure both are scalars
                string val = parser.TryConsume(out Scalar v) ? v.Value : null;

                if (string.Equals(key, "playlistId", StringComparison.OrdinalIgnoreCase))
                    result.PlaylistId = val;
                else if (string.Equals(key, "snapshot", StringComparison.OrdinalIgnoreCase))
                    result.Snapshot = val;
                else
                {
                    // If an unknown key has a complex value, consume it safely
                    if (val == null)
                        _ = rootDeserializer(typeof(object));
                }
            }

            return result;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            serializer(value);
        }
    }
}