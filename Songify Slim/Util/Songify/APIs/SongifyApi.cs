using Newtonsoft.Json;
using Songify_Slim.Models.Pear;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using SpotifyAPI.Web;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Songify_Slim.Util.Songify.APIs
{
    internal static class SongifyApi
    {
        private static readonly ApiClient ApiClient = new(GlobalObjects.ApiUrl);
        //private static readonly ApiClient ApiClient = new("unreachable.host");

        public static Task<string> GetQueueRawAsync() => ApiClient.Get("queue", Settings.Uuid);

        public static Task<string> PostQueueAsync(string body) => ApiClient.Post("queue", body);

        public static Task PatchQueueAsync(string body) => ApiClient.Patch("queue", body);

        public static Task PatchQueueItemAsync(int queueId)
        {
            var payload = new
            {
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                queueid = queueId
            };

            return ApiClient.Patch("queue", Json.Serialize(payload));
        }

        public static Task ClearQueueAsync(string body) => ApiClient.Clear("queue_delete", body);

        public static Task PostTelemetryAsync(string body) => ApiClient.Post("telemetry", body);

        public static Task PostSongAsync(string body) => ApiClient.Post("song", body);

        public static Task PostHistoryAsync(string body) => ApiClient.Post("history", body);

        public static Task<string> GetMotdAsync() => ApiClient.Get("motd", "");

        public static Task<string> GetCanvasRawAsync(string id) => ApiClient.GetCanvas(id);

        public static async Task<PearSearch> GetYoutubeData(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
                throw new ArgumentException("videoId is required", nameof(videoId));
            string result = await ApiClient.PostYtEndpoint(videoId);
            return JsonConvert.DeserializeObject<PearSearch>(result);
        }
    }
}