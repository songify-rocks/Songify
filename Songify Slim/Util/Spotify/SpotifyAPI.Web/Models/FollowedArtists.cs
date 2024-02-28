using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class FollowedArtists : BasicModel
  {
    [JsonProperty("artists")]
    public CursorPaging<FullArtist> Artists { get; set; }
  }
}