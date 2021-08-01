using Newtonsoft.Json;

namespace Songify.General
{
    public class JsonConverter : Converter
    {
        /// <summary>
        /// Convert JSON text to an object of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">Text in the JSON format</param>
        /// <returns>An object of type T</returns>
        public override T Deserialize<T>(string text) => JsonConvert.DeserializeObject<T>(text);

        /// <summary>
        /// Convert an object of type T to JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">An object of type T</param>
        /// <returns>A JSON string</returns>
        public override string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}
