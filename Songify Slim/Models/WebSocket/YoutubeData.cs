using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Songify_Slim.Models.WebSocket
{
    public class YoutubeData
    {
        [JsonProperty("videoId")]
        public string VideoId { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("channel")]
        public string Channel { get; set; }
    }
}
