using System.Collections.Generic;
using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class Recommendations : BasicModel
  {
    [JsonProperty("seeds")]
    public List<RecommendationSeed> Seeds { get; set; }

    [JsonProperty("tracks")]
    public List<SimpleTrack> Tracks { get; set; }
  }
}