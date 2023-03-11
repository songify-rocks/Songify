using System.Collections.Generic;
using SpotifyAPI.Web.Models;

namespace Songify_Slim.Models
{
    public class TrackInfo
    {
        public string Artists { get; set; }
        public string Title { get; set; }
        public List<Image> Albums { get; set; }
        public string SongId { get; set; }
        public int DurationMs { get; set; }
        public bool IsPlaying { get; set; }
        public string Url { get; set; }
        public int DurationPercentage { get; set; }
        public int DurationTotal { get; set; }
        public int Progress { get; set; }
    }
}