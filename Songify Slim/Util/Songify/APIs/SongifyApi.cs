using Songify_Slim.Util.General;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.Songify.APIs
{
    internal static class SongifyApi
    {
        private static readonly ApiClient ApiClient = new(GlobalObjects.ApiUrl);

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
    }
}