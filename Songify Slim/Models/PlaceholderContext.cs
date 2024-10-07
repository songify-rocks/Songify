using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class PlaceholderContext
    {
        public string User { get; set; }
        public string Artist { get; set; }
        public string SingleArtist { get; set; }
        public string Title { get; set; }
        public string MaxReq { get; set; }
        public string ErrorMsg { get; set; }
        public string MaxLength { get; set; }
        public string Votes { get; set; }
        public string Song { get; set; }
        public string Req { get; set; }
        public string Url { get; set; }
        public string PlaylistName { get; set; }
        public string PlaylistUrl { get; set; }
        public string Cd { get; set; }

        // Constructor that accepts a FullTrack object
        public PlaceholderContext(TrackInfo track = null)
        {
            if (track != null)
            {
                Artist = string.Join(", ", track.FullArtists.Select(artist => artist.Name).ToList());
                SingleArtist = track.FullArtists.FirstOrDefault()?.Name;
                Title = track.Title;
                Song = $"{track.Artists} - {track.Title}";
                Url = track.Url;
                PlaylistName = track.Playlist?.Name;
                PlaylistUrl = track.Playlist?.Url;
            }
        }
    }


}
