using System.Collections.Generic;
using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class SeveralArtists : BasicModel
  {
    [JsonProperty("artists")]
    public List<FullArtist> Artists { get; set; }
  }
}