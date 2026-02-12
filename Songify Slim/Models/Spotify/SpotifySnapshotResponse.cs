using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Songify_Slim.Models.Spotify
{
    public sealed class SpotifySnapshotResponse
    {
        [JsonProperty("snapshot_id")]
        public string SnapshotId { get; set; }
    }
}