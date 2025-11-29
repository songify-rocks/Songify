using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Youtube.YTMYHCH
{
    namespace YtmDesktopApi
    {
        public static class YtmDesktopApi
        {
            private static readonly HttpClient Http = new();
            private const string BaseUrl = "http://127.0.0.1:26538";

            // ---- READ ----
            public static Task<SongResponse> GetCurrentSongAsync() =>
                GetAsync<SongResponse>("/api/v1/song");

            public static Task<QueueResponse> GetQueueAsync() =>
                GetAsync<QueueResponse>("/api/v1/queue");

            public static Task<SearchResponse> SearchAsync(string query, int limit = 10) =>
                GetAsync<SearchResponse>($"/api/v1/search?q={Uri.EscapeDataString(query)}&limit={limit}");

            public static Task<VolumeState> GetVolume() =>
                GetAsync<VolumeState>("/api/v1/volume");

            // ---- CONTROLS ----
            public static Task<ApiOk> PlayAsync() =>
                PostAsync<ApiOk>("/api/v1/play");

            public static Task<ApiOk> PauseAsync() =>
                PostAsync<ApiOk>("/api/v1/pause");

            public static Task<ApiOk> PlayPauseAsync() =>
                PostAsync<ApiOk>("/api/v1/toggle-play");

            public static Task<ApiOk> NextAsync() =>
                PostAsync<ApiOk>("/api/v1/next");

            public static Task<ApiOk> PreviousAsync() =>
                PostAsync<ApiOk>("/api/v1/previous");

            public static Task<ApiOk> SeekToAsync(int seconds) =>
                PostJsonAsync<ApiOk>("/api/v1/seek-to", new { seconds });

            public static Task<ApiOk> SetShuffleAsync(bool enabled) =>
                PostJsonAsync<ApiOk>("/api/v1/shuffle", new { enabled });

            public static Task<ApiOk> SetRepeatModeAsync(RepeatMode mode) =>
                PostJsonAsync<ApiOk>("/api/v1/repeat-mode", new { mode = mode.ToString().ToLowerInvariant() });

            public static Task<ApiOk> SetVolumeAsync(int volume) =>
                PostJsonAsync<ApiOk>("/api/v1/volume", new { volume });

            // ---- HELPERS ----
            private static async Task<T> GetAsync<T>(string path)
            {
                HttpResponseMessage resp = await Http.GetAsync(BaseUrl + path).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(json);
            }

            private static async Task<T> PostAsync<T>(string path)
            {
                HttpResponseMessage resp = await Http.PostAsync(BaseUrl + path, new StringContent("")).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(json) ? default : JsonConvert.DeserializeObject<T>(json);
            }

            private static async Task<T> PostJsonAsync<T>(string path, object payload)
            {
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new(json, Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await Http.PostAsync(BaseUrl + path, content).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                string respJson = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(respJson);
            }

            public static async Task<bool> AddToQueueAsync(string reqTrackid, Enums.InsertPosition insertAfterCurrentVideo)
            {
                var payload = new
                {
                    videoId = reqTrackid,
                    insertPosition = insertAfterCurrentVideo
                };
                HttpResponseMessage resp = await Http.PostAsync(BaseUrl + "/api/v1/queue", new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")).ConfigureAwait(false);
                return resp.IsSuccessStatusCode;
            }
        }

        // ---- Models ----
        public enum RepeatMode
        { Off, One, All }

        public sealed class ApiOk
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
        }

        public sealed class SongResponse
        {
            [JsonProperty("videoId")] public string VideoId { get; set; }
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("author")] public string Author { get; set; }
            [JsonProperty("album")] public string Album { get; set; }
            [JsonProperty("durationSeconds")] public int? DurationSeconds { get; set; }
            [JsonProperty("elapsedSeconds")] public int? ElapsedSeconds { get; set; }
            [JsonProperty("isPaused")] public bool? IsPaused { get; set; }
            [JsonProperty("thumbnailUrl")] public string ThumbnailUrl { get; set; }
        }

        public sealed class QueueResponse
        {
            [JsonProperty("currentIndex")] public int? CurrentIndex { get; set; }
            [JsonProperty("items")] public List<QueueItem> Items { get; set; }
        }

        public sealed class QueueItem
        {
            [JsonProperty("videoId")] public string VideoId { get; set; }
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("author")] public string Author { get; set; }
            [JsonProperty("durationSeconds")] public int? DurationSeconds { get; set; }
        }

        public sealed class SearchResponse
        {
            [JsonProperty("items")] public List<SearchItem> Items { get; set; }
        }

        public sealed class SearchItem
        {
            [JsonProperty("videoId")] public string VideoId { get; set; }
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("author")] public string Author { get; set; }
            [JsonProperty("durationSeconds")] public int? DurationSeconds { get; set; }
            [JsonProperty("url")] public string Url { get; set; }
        }

        public sealed class VolumeState
        {
            [JsonProperty("state")]
            public int State { get; set; }
        }
    }
}