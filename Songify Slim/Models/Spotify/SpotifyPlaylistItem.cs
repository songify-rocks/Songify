using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Spotify
{
    public sealed class SpotifyPlaylistItem
    {
        // Track identity
        public string TrackId { get; set; } = "";

        public string Uri { get; set; } = ""; // spotify:track:{id}

        // Display fields
        public string Title { get; set; } = "";

        public List<string> Artists { get; set; } = new();
        public int DurationMs { get; set; }

        // Album art (URLs)
        public List<string> AlbumImages { get; set; } = new();

        // Playlist-entry fields (optional)
        public string AddedAt { get; set; } = "";

        public bool IsLocal { get; set; }
    }
}