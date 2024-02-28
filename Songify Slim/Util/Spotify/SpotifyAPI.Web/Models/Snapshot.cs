using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class Snapshot : BasicModel
  {
    [JsonProperty("snapshot_id")]
    public string SnapshotId { get; set; }
  }
}