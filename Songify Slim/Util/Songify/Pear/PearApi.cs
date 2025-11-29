using Newtonsoft.Json;
using Songify_Slim.Models.Pear;
using Songify_Slim.Util.Youtube.YTMYHCH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.General;
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
                    Logger.LogStr($"{LogPrefix}: HTTP Request failed with status code: {response.StatusCode}");
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
                Logger.LogExc(e);
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
                    Logger.LogStr($"{LogPrefix}: HTTP Request failed with status code: {response.StatusCode}");
                    return null;
                }

                string result = await response.Content.ReadAsStringAsync();
                List<Song> songs = QueueParser.ExtractSongs(result);
                return songs;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
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
                Logger.LogStr($"{LogPrefix}: search failed with status code: {response.StatusCode}");
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

            Logger.LogStr($"{LogPrefix}: enqueue failed with status code: {response.StatusCode}");
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

            Logger.LogStr($"{LogPrefix}: reorder failed with status code: {response.StatusCode}");
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

            Logger.LogStr($"{LogPrefix}: next failed with status code: {response.StatusCode}");
            return false;
        }
    }
}