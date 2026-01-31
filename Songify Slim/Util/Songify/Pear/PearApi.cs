using Newtonsoft.Json;
using Songify_Slim.Models.Pear;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Youtube.YTMYHCH;
using Songify_Slim.Util.Youtube.YTMYHCH.YtmDesktopApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Songify.Pear
{
    internal static class PearApi
    {
        private static readonly HttpClient _httpClient;

        private const string LogPrefix = "PearApi";

        static PearApi()
        {
            _httpClient = new HttpClient
            {
                // Pear (formerly YTMusic Desktop) API base
                BaseAddress = new Uri("http://localhost:26538/api/v1/")
            };

            // Optional: tune this
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Get current song / player state.
        /// </summary>
        public static async Task<PearResponse> GetNowPlayingAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("song-info");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error(LogSource.Pear, $"HTTP Request failed with status code: {response.StatusCode}");
                    return null;
                }

                string result = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(result))
                {
                    return JsonConvert.DeserializeObject<PearResponse>(result);
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Pear, "An error occurred while getting now playing", e);
            }

            return null;
        }

        /// <summary>
        /// Get the current Pear queue.
        /// </summary>
        public static async Task<List<Song>> GetQueueAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("queue");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error(LogSource.Pear, $"HTTP Request failed with status code: {response.StatusCode}");
                    return null;
                }

                string result = await response.Content.ReadAsStringAsync();
                List<Song> songs = QueueParser.ExtractSongs(result);
                return songs;
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Pear, "An error occurred while getting the queue", e);
                return null;
            }
        }

        /// <summary>
        /// Search in Pear by query string.
        /// </summary>
        public static async Task<PearSearch> SearchAsync(string query)
        {
            var payload = new
            {
                query
            };

            string json = JsonConvert.SerializeObject(payload);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("search", content);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Error(LogSource.Pear, $"search failed with status code: {response.StatusCode}");
                return null;
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            PearSearch song = YTHCHSearchParser.ParseTopSongResult(responseJson);

            return song;
        }

        /// <summary>
        /// Add a song to the Pear queue.
        /// </summary>
        public static async Task<bool> EnqueueAsync(string videoId, InsertPosition position)
        {
            var payload = new
            {
                videoId,
                insertPosition = position
            };

            string json = JsonConvert.SerializeObject(payload);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("queue", content);
            if (response.IsSuccessStatusCode)
                return true;

            Logger.Error(LogSource.Pear, $"enqueue failed with status code: {response.StatusCode}");
            return false;
        }

        /// <summary>
        /// Move a queue item from one index to another.
        /// </summary>
        public static async Task<bool> MoveQueueItemAsync(int currentIndex, int desiredIndex)
        {
            var payload = new
            {
                toIndex = desiredIndex
            };

            string json = JsonConvert.SerializeObject(payload);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PatchAsync($"queue/{currentIndex}", content);
            if (response.IsSuccessStatusCode)
                return true;

            Logger.Error(LogSource.Pear, $"reorder failed with status code: {response.StatusCode}");
            return false;
        }

        /// <summary>
        /// Skip to the next track.
        /// </summary>
        public static async Task<bool> SkipAsync()
        {
            HttpResponseMessage response = await _httpClient.PostAsync("next", null);

            if (response.IsSuccessStatusCode)
                return true;

            Logger.Error(LogSource.Pear, $"next failed with status code: {response.StatusCode}");
            return false;
        }

        public static async Task<int> GetVolumeAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("volume");
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error(LogSource.Pear, $"HTTP Request failed with status code: {response.StatusCode}");
                    return -1;
                }

                string result = await response.Content.ReadAsStringAsync();

                Volume volumeResponse =
                    JsonConvert.DeserializeObject<Volume>(result);

                if (volumeResponse == null)
                    return -1;

                // If muted → treat as volume = 0 or return separate?
                return volumeResponse.IsMuted ? 0 : volumeResponse.State; // 0–100
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Pear, "Error getting volume", e);
                return -1;
            }
        }

        public static async Task<ApiOk> SetVolumeAsncy(int volume)
        {
            var payload = new
            {
                volume
            };

            string json = JsonConvert.SerializeObject(payload);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("volume", content);
            if (response.IsSuccessStatusCode)
                return new ApiOk
                {
                    Ok = true
                };
            Logger.Error(LogSource.Pear, $"set volume failed with status code: {response.StatusCode}");
            return new ApiOk
            {
                Ok = false
            };
        }

        public static async Task Pause()
        {
            await _httpClient.PostAsync("pause", null);
        }

        public static async Task Play()
        {
            await _httpClient.PostAsync("play", null);
        }

        public static async Task Next()
        {
            await _httpClient.PostAsync("next", null);
        }

        public static async Task<ApiOk> SeekTo(int position)
        {
            var payload = new
            {
                seconds = position
            };

            string json = JsonConvert.SerializeObject(payload);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("seek-to", content);
            if (response.IsSuccessStatusCode)
                return new ApiOk
                {
                    Ok = true
                };
            Logger.Error(LogSource.Pear, $"set volume failed with status code: {response.StatusCode}");
            return new ApiOk
            {
                Ok = false
            };
        }

        public static async Task Previous()
        {
            await _httpClient.PostAsync("previous", null);
        }
    }
}