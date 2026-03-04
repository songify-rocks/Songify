using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Spotify
{
    public static class SpotifyEmbedNextData
    {
        private static readonly HttpClient Http = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        // Extracts the JSON content of the __NEXT_DATA__ script tag
        private static readonly Regex NextDataRegex = new(
            @"<script[^>]+id=""__NEXT_DATA__""[^>]*>\s*(?<json>\{.*?\})\s*</script>",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static async Task<(string Name, string Owner)?> TryGetPlaylistNameAndOwnerAsync(
            string playlistId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentException("playlistId cannot be empty.", nameof(playlistId));

            string url = $"https://open.spotify.com/embed/playlist/{playlistId}";

            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "Songify/1.0");

            using HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
                return null;

            string html = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            Match m = NextDataRegex.Match(html);
            if (!m.Success)
                return null;

            string json = m.Groups["json"].Value;

            // Parse as JObject to avoid creating a huge object graph
            JObject root = JsonConvert.DeserializeObject<JObject>(json);
            if (root == null)
                return null;

            // Token path is case-sensitive in JToken.SelectToken; use the exact casing seen in the JSON:
            // props.pageProps.state.data.entity.name
            // props.pageProps.state.data.entity.subtitle
            string name = root.SelectToken("props.pageProps.state.data.entity.name")?.Value<string>();
            string owner = root.SelectToken("props.pageProps.state.data.entity.subtitle")?.Value<string>();

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(owner))
                return null;

            return (name?.Trim() ?? "unknown", owner?.Trim() ?? "unknown");
        }
    }
}