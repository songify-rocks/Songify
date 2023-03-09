using Newtonsoft.Json;
using SpotifyAPI.Web.Models;

namespace Songify_Slim.Models
{
    public class RequestObject
    {
        [JsonProperty("queueid")]
        public int queueid { get; set; }
        [JsonProperty("uuid")]
        public string uuid { get; set; }
        [JsonProperty("trackid")]
        public string trackid { get; set; }
        [JsonProperty("artist")]
        public string artist { get; set; }
        [JsonProperty("title")]
        public string title { get; set; }
        [JsonProperty("length")]
        public string length { get; set; }
        [JsonProperty("requester")]
        public string requester { get; set; }
        [JsonIgnore] // ignore this field during JSON deserialization
        public int played { get; set; }
        [JsonProperty("albumcover")]
        public string albumcover { get; set; }
    }
}