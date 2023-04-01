using Newtonsoft.Json;

namespace Songify_Slim.Models
{
    public class RequestObject
    {
        [JsonProperty("queueid")]
        public int Queueid { get; set; }
        [JsonProperty("uuid")]
        public string Uuid { get; set; }
        [JsonProperty("trackid")]
        public string Trackid { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("length")]
        public string Length { get; set; }
        [JsonProperty("requester")]
        public string Requester { get; set; }
        [JsonIgnore] // ignore this field during JSON deserialization
        public int Played { get; set; }
        [JsonProperty("albumcover")]
        public string Albumcover { get; set; }
    }
}