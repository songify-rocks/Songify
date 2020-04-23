using System.Collections.Generic;
using SpotifyAPI.Web.Models;

namespace Songify_Slim.Models
{
    public class TrackInfo
    {
        public string Artists { get; set; }
        public string Title { get; set; }
        public List<Image> albums { get; set; }
        public string SongID { get; set; }
        public int DurationMS { get; set; }
        public bool isPlaying { get; set; }
    }
}
