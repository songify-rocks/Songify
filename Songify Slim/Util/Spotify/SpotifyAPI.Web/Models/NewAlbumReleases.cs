using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class NewAlbumReleases : BasicModel
  {
    [JsonProperty("albums")]
    public Paging<SimpleAlbum> Albums { get; set; }
  }
}