using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Spotify
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class SpotifyApi
    {
        private readonly HttpClient _http;
        private readonly Func<string> _getAccessToken;
        private readonly Func<Task<bool>> _refreshToken;

        public SpotifyApi(Func<string> getAccessToken, Func<Task<bool>> refreshToken, HttpMessageHandler handler = null)
        {
            _getAccessToken = getAccessToken ?? throw new ArgumentNullException(nameof(getAccessToken));
            _refreshToken = refreshToken; // can be null if you do not want auto-refresh

            _http = handler == null ? new HttpClient() : new HttpClient(handler);
            _http.BaseAddress = new Uri("https://api.spotify.com/v1/");
            _http.Timeout = TimeSpan.FromSeconds(20);
        }

        private void ApplyAuthHeader()
        {
            string token = _getAccessToken();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
        {
            ApplyAuthHeader();
            HttpResponseMessage resp = await send().ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.Unauthorized && _refreshToken != null)
            {
                resp.Dispose();

                bool refreshed = await _refreshToken().ConfigureAwait(false);
                if (!refreshed) return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                ApplyAuthHeader();
                resp = await send().ConfigureAwait(false);
            }

            return resp;
        }

        public async Task<bool> SaveToLibraryAsync(IEnumerable<string> spotifyUris)
        {
            var body = new { uris = spotifyUris };
            StringContent content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            using HttpResponseMessage resp = await SendAsync(() => _http.PutAsync("me/library", content)).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromLibraryAsync(IEnumerable<string> spotifyUris)
        {
            // Spotify uses DELETE with a JSON body here.
            var body = new { uris = spotifyUris };
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Delete, "me/library")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };

            using HttpResponseMessage resp = await SendAsync(() => _http.SendAsync(req)).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        // Playlist items helper that is robust against the "tracks -> items" shape change.
        // It returns track ids found in the playlist items paging.
        public async Task<List<string>> GetPlaylistTrackIdsAsync(string playlistId, int limit = 100)
        {
            List<string> ids = [];

            // Request fields defensively, but do not assume exact response shape.
            // If Spotify returns metadata-only for some playlists, items may be missing or empty.
            string url =
                $"playlists/{Uri.EscapeDataString(playlistId)}?fields=id,name,owner(display_name,id),external_urls,items(items(item(track(id))))&limit={limit}";

            using HttpResponseMessage resp = await SendAsync(() => _http.GetAsync(url)).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return ids;

            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject root = JObject.Parse(json);

            // New shape: items.items[].item.track.id
            // Some responses may not include items at all.
            JToken items = root["items"]?["items"];
            if (items is not { Type: JTokenType.Array }) return ids;

            ids.AddRange(items.Select(entry => (string)entry?["item"]?["track"]?["id"]).Where(id => !string.IsNullOrEmpty(id)));

            return ids;
        }

        // Playlist paging: fetch playlist items page-by-page (recommended for large playlists).
        // This one hits the items endpoint directly and tries both old and new shapes.
        public async Task<List<string>> GetPlaylistTrackIdsPagedAsync(string playlistId, int pageSize = 100)
        {
            List<string> ids = [];
            int offset = 0;

            while (true)
            {
                string url =
                    $"playlists/{Uri.EscapeDataString(playlistId)}/items?limit={pageSize}&offset={offset}";

                using HttpResponseMessage resp = await SendAsync(() => _http.GetAsync(url)).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) break;

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject root = JObject.Parse(json);

                // Try common shapes:
                // New: items[].item.track.id or items[].track.id
                // Old: items[].track.id
                if (root["items"] is not JArray arr || arr.Count == 0) break;

                int before = ids.Count;

                ids.AddRange(arr.Select(entry => (string)entry?["item"]?["track"]?["id"] ?? (string)entry?["track"]?["id"]).Where(id => !string.IsNullOrEmpty(id)));

                // If we did not add anything, either it is metadata-only or the shape is different.
                if (ids.Count == before) break;

                // Next page?
                bool hasNext = root["next"] != null && root["next"].Type != JTokenType.Null;
                if (!hasNext) break;

                offset += pageSize;
            }

            return ids;
        }
    }
}