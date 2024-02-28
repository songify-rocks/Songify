using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class CategoryPlaylist : BasicModel
  {
    [JsonProperty("playlists")]
    public Paging<SimplePlaylist> Playlists { get; set; }
  }
}