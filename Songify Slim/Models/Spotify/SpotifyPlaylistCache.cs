using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Spotify
{
    public sealed class SpotifyPlaylistCache
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Owner { get; set; } = "";
        public string Url { get; set; } = "";

        // Optional but useful for detecting changes
        public string SnapshotId { get; set; } = "";

        public List<string> Images { get; set; } = new();

        // Full list of items (tracks) in the playlist
        public List<SpotifyPlaylistItem> Items { get; set; } = new();
    }
}