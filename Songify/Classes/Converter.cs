using Newtonsoft.Json;

namespace Songify.Classes
{
    /// <summary>
    /// Serializing and deserializing Objects to JSON
    /// </summary>
    class Converter
    {
        public string ConvertObjectToJSON<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public T ConvertJSONToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
