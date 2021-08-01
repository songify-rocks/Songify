using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Songify.General
{
    public class YamlConverter : Converter
    {
        /// <summary>
        /// Converts a YAML formatted string to an object of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">A YAML formatted string</param>
        /// <returns>An object of type T</returns>
        public override T Deserialize<T>(string text)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<T>(text);
        }

        /// <summary>
        /// Converts an object of type T to a YAML formatted string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">An object of type T</param>
        /// <returns>A YAML formatted string</returns>
        public override string Serialize<T>(T obj)
        {
            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(obj);
        }
    }
}
