using System.Linq;

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
        public string Reason { get; set; }

        // Constructor that accepts a FullTrack object
        public PlaceholderContext(TrackInfo track = null)
        {
            if (track == null) return;
            Artist = track.FullArtists != null ? string.Join(", ", track.FullArtists.Select(artist => artist.Name).ToList()) : track.Artists;
            if (track.FullArtists != null)
            {
                SingleArtist = track.FullArtists.FirstOrDefault()?.Name;
            }
            else if (track.Artists.Contains(", ") && track.FullArtists == null)
            {
                SingleArtist = track.Artists.Split().First().Trim();
            }
            else
            {
                SingleArtist = track.Artists;
            }
            Title = track.Title;
            Song = $"{track.Artists} - {track.Title}";
            Url = track.Url;
            PlaylistName = track.Playlist?.Name;
            PlaylistUrl = track.Playlist?.Url;
        }
    }


}
