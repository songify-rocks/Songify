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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Songify_Slim.Models.Spotify;
    using Songify_Slim.Util.General;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

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

        public async Task<bool?> PlaylistContainsTrackAsync(string playlistId, string trackId, int pageSize = 100)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentException("playlistId is required", nameof(playlistId));
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentException("trackId is required", nameof(trackId));

            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            int offset = 0;

            // Request minimal fields and support both "item" (new) and "track" (legacy)
            const string fields = "items(item(id),track(id)),next,total";

            while (true)
            {
                string url =
                    $"playlists/{Uri.EscapeDataString(playlistId)}/items" +
                    $"?limit={pageSize}&offset={offset}" +
                    $"&fields={Uri.EscapeDataString(fields)}";

                using HttpResponseMessage resp = await SendAsync(() => _http.GetAsync(url)).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        $"PlaylistContainsTrack failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {body}");
                    return null; // unknown/error
                }

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject root = JObject.Parse(json);

                JArray items = root["items"] as JArray;
                if (items == null || items.Count == 0)
                    return false;

                if (items.Select(entry => (string)entry["item"]?["id"] ??
                                          (string)entry["track"]?["id"]).Any(id => !string.IsNullOrEmpty(id) &&
                        string.Equals(id, trackId, StringComparison.Ordinal)))
                {
                    return true;
                }

                bool hasNext = root["next"] != null && root["next"].Type != JTokenType.Null;
                if (!hasNext)
                    return false;

                offset += pageSize;
            }
        }

        public async Task<SpotifyPlaylistCache> GetPlaylistMetaAsync(string playlistId)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentException("playlistId is required", nameof(playlistId));

            const string playlistFields =
                "id,name,owner(display_name,id),external_urls,snapshot_id,images(url)";

            string url =
                $"playlists/{Uri.EscapeDataString(playlistId)}?fields={Uri.EscapeDataString(playlistFields)}";

            using HttpResponseMessage resp = await SendAsync(() => _http.GetAsync(url)).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return null;

            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject meta = JObject.Parse(json);

            var playlist = new SpotifyPlaylistCache
            {
                Id = (string)meta["id"] ?? playlistId,
                Name = (string)meta["name"] ?? "",
                Owner =
                    (string)meta["owner"]?["display_name"] ??
                    (string)meta["owner"]?["id"] ??
                    "",
                Url = (string)meta["external_urls"]?["spotify"] ?? "",
                SnapshotId = (string)meta["snapshot_id"] ?? ""
            };

            if (meta["images"] is JArray imgs)
            {
                foreach (JToken img in imgs)
                {
                    string u = (string)img["url"];
                    if (!string.IsNullOrEmpty(u))
                        playlist.Images.Add(u);
                }
            }

            return playlist;
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

        public async Task<List<bool>> CheckSavedTracksAsync(IEnumerable<string> trackIds)
        {
            if (trackIds == null)
                throw new ArgumentNullException(nameof(trackIds));

            List<string> ids = trackIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (ids.Count == 0)
                return [];

            const int batchSize = 40;
            List<bool> result = [];

            for (int i = 0; i < ids.Count; i += batchSize)
            {
                List<string> batch = ids.Skip(i).Take(batchSize)
                    .Select(id => $"spotify:track:{id}")
                    .ToList();

                string joined = string.Join(",", batch);

                string url = $"me/library/contains?uris={Uri.EscapeDataString(joined)}";

                using HttpResponseMessage resp =
                    await SendAsync(() => _http.GetAsync(url)).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        $"CheckSavedTracks failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {body}");

                    return null;
                }

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                List<bool> partial = JsonConvert.DeserializeObject<List<bool>>(json);
                if (partial != null)
                    result.AddRange(partial);
            }

            return result;
        }

        public async Task<bool> SaveToLibraryAsync(IEnumerable<string> spotifyUris)
        {
            var body = new { uris = spotifyUris };
            StringContent content = new(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            using HttpResponseMessage resp = await SendAsync(() => _http.PutAsync("me/library", content)).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromLibraryAsync(IEnumerable<string> spotifyUris)
        {
            // Spotify uses DELETE with a JSON body here.
            var body = new { uris = spotifyUris };
            HttpRequestMessage req = new(HttpMethod.Delete, "me/library")
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

        public async Task<SpotifyPlaylistCache> GetPlaylistWithItemsAsync(string playlistId, int pageSize = 100)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentException("playlistId is required", nameof(playlistId));

            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            // 1) Fetch playlist metadata
            // Keep it small and stable.
            const string playlistFields =
                "id,name,owner(display_name,id),external_urls,snapshot_id,images(url)";

            string metaUrl =
                $"playlists/{Uri.EscapeDataString(playlistId)}?fields={Uri.EscapeDataString(playlistFields)}";

            using HttpResponseMessage metaResp = await SendAsync(() => _http.GetAsync(metaUrl)).ConfigureAwait(false);

            if (!metaResp.IsSuccessStatusCode)
            {
                string body = await metaResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.Log(LogLevel.Warning, LogSource.Spotify,
                   $"GetPlaylist meta failed: {(int)metaResp.StatusCode} {metaResp.ReasonPhrase} {body}");
                return null;
            }

            string metaJson = await metaResp.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject meta = JObject.Parse(metaJson);

            var playlist = new SpotifyPlaylistCache
            {
                Id = (string)meta["id"] ?? playlistId,
                Name = (string)meta["name"] ?? "",
                Owner =
                    (string)meta["owner"]?["display_name"] ??
                    (string)meta["owner"]?["id"] ??
                    "",
                Url = (string)meta["external_urls"]?["spotify"] ?? "",
                SnapshotId = (string)meta["snapshot_id"] ?? ""
            };

            if (meta["images"] is JArray playlistImages)
            {
                foreach (JToken img in playlistImages)
                {
                    string url = (string)img["url"];
                    if (!string.IsNullOrEmpty(url))
                        playlist.Images.Add(url);
                }
            }

            // 2) Fetch items (paged)
            const string itemFields =
                "items(added_at,is_local,item(id,uri,name,duration_ms,artists(name),album(images(url)))),next,total";

            int offset = 0;

            while (true)
            {
                string itemsUrl =
                    $"playlists/{Uri.EscapeDataString(playlistId)}/items" +
                    $"?limit={pageSize}&offset={offset}" +
                    $"&fields={Uri.EscapeDataString(itemFields)}";

                using HttpResponseMessage itemsResp = await SendAsync(() => _http.GetAsync(itemsUrl)).ConfigureAwait(false);

                if (!itemsResp.IsSuccessStatusCode)
                {
                    string body = await itemsResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        $"GetPlaylist items failed: {(int)itemsResp.StatusCode} {itemsResp.ReasonPhrase} {body}");
                    return null;
                }

                string itemsJson = await itemsResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject root = JObject.Parse(itemsJson);

                JArray items =
                    root["items"] as JArray ??
                    root["items"]?["items"] as JArray ??
                    root["items"]?["items"]?["item"] as JArray;

                if (items == null || items.Count == 0)
                {
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        $"GetPlaylist items returned no items. JSON: {itemsJson}");
                    break;
                }

                foreach (JToken entry in items)
                {
                    // New shape prefers "item", old shape had "track"
                    JToken track = entry["item"] ?? entry["track"];
                    if (track == null || track.Type == JTokenType.Null)
                        continue;

                    var pi = new SpotifyPlaylistItem
                    {
                        AddedAt = (string)entry["added_at"] ?? "",
                        IsLocal = (bool?)entry["is_local"] ?? false,

                        TrackId = (string)track["id"] ?? "",
                        Uri = (string)track["uri"] ?? "",
                        Title = (string)track["name"] ?? "",
                        DurationMs = (int?)track["duration_ms"] ?? 0
                    };

                    if (track["artists"] is JArray artists)
                    {
                        foreach (JToken a in artists)
                        {
                            string name = (string)a["name"];
                            if (!string.IsNullOrEmpty(name))
                                pi.Artists.Add(name);
                        }
                    }

                    if (track["album"]?["images"] is JArray images)
                    {
                        foreach (JToken img in images)
                        {
                            string url = (string)img["url"];
                            if (!string.IsNullOrEmpty(url))
                                pi.AlbumImages.Add(url);
                        }
                    }

                    if (string.IsNullOrEmpty(pi.Uri) && !string.IsNullOrEmpty(pi.TrackId))
                        pi.Uri = $"spotify:track:{pi.TrackId}";

                    playlist.Items.Add(pi);
                }

                bool hasNext =
                    root["next"] != null && root["next"].Type != JTokenType.Null;

                if (!hasNext)
                    break;

                offset += pageSize;
            }

            return playlist;
        }
    }
}