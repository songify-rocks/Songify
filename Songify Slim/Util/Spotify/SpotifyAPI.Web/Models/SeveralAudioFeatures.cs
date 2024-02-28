using System.Collections.Generic;
using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class SeveralAudioFeatures : BasicModel
  {
    [JsonProperty("audio_features")]
    public List<AudioFeatures> AudioFeatures { get; set; }
  }
}