using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Songify_Slim.Models
{
    /// <summary>
    /// Represents a generic WebSocket command structure.
    /// </summary>
    /// <remarks>
    /// Sample WebSocket commands:
    ///
    /// Add to Queue:
    /// {
    ///     "action": "queue_add",
    ///     "data": {
    ///         "track": "https://open.spotify.com/track/4PTG3Z6ehGkBFwjybzWkR8",
    ///         "requester": "Viewer42"
    ///     }
    /// }
    ///
    /// Volume Set:
    /// {
    ///     "action": "vol_set",
    ///     "data": {
    ///         "value": 80
    ///     }
    /// }
    ///
    /// Simple Actions:
    /// {
    ///     "action": "skip"
    /// }
    /// </remarks>
    public class WebSocketCommand
    {

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }
    }

}
