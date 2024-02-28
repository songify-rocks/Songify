using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class FeaturedPlaylists : BasicModel
  {
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("playlists")]
    public Paging<SimplePlaylist> Playlists { get; set; }
  }
}