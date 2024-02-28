using System.Collections.Generic;
using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class SeveralTracks : BasicModel
  {
    [JsonProperty("tracks")]
    public List<FullTrack> Tracks { get; set; }
  }
}