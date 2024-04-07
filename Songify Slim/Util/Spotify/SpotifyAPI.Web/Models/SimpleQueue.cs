using System.Collections.Generic;
using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
    public class SimpleQueue : BasicModel
    {
        [JsonProperty("currently_playing")]
        public FullTrack CurrenTrack { get; set; }

        [JsonProperty("queue")]
        public List<FullTrack> Queue { get; set; }
    }
}
