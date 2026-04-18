using Newtonsoft.Json;

namespace Songify_Slim.Models.WebSocket
{
    /// <summary>Optional body for <c>sr_enable</c> / <c>sr_disable</c> (and <c>sr_open</c> / <c>sr_close</c>).</summary>
    public class SrScopeData
    {
        /// <summary>One of <c>both</c> (default), <c>reward</c>, or <c>command</c>.</summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}
