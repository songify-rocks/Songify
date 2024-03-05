using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
