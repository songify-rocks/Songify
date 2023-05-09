using SpotifyAPI.Web.Models;
using System.Collections.Generic;

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
        public PlaylistInfo Playlist { get; set; }
    }

    public class PlaylistInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Owner { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }
}