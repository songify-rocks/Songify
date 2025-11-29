using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Settings
{
    public class PlayerTypeYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
            => type == typeof(Enums.PlayerType); // <- your enum type here

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            Scalar scalar = parser.Consume<Scalar>();
            string value = scalar.Value;

            // Map old value to new one
            if (string.Equals(value, "Ytmthch", StringComparison.OrdinalIgnoreCase))
                return Enums.PlayerType.Pear;
            if (string.Equals(value, "SpotifyWeb", StringComparison.OrdinalIgnoreCase))
                return Enums.PlayerType.Spotify;

            // Fallback: normal enum parsing
            return Enum.Parse(typeof(Enums.PlayerType), value, ignoreCase: true);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            // How you want it written back to YAML
            emitter.Emit(new Scalar(value.ToString()));
        }
    }
}