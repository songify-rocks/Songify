using Newtonsoft.Json.Linq;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Models.Queue;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.Songify
{
    internal static class QueueService
    {
        public static async Task AddRequestAsync(string payload)
        {
            string result = await SongifyApi.PostQueueAsync(payload).ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
            {
                AddRequestLocally(payload);
                await GlobalObjects.QueueUpdateQueueWindow();
                return;
            }

            try
            {
                RequestObject response = Json.Deserialize<RequestObject>(result);
                response.FullRequester ??= ExtractFullRequester(payload);

                await Application.Current.Dispatcher.InvokeAsync(() => { GlobalObjects.ReqList.Add(response); });

                await GlobalObjects.QueueUpdateQueueWindow();
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error while uploading queue", e);
            }
        }

        private static void AddRequestLocally(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;

            JObject x = JObject.Parse(payload);
            GlobalObjects.ReqList.Add(new RequestObject
            {
                Queueid = GlobalObjects.ReqList.Count + 1,
                Uuid = Settings.Uuid,
                Trackid = (string)x["queueItem"]?["Trackid"],
                Artist = (string)x["queueItem"]?["Artist"],
                Title = (string)x["queueItem"]?["Title"],
                Length = (string)x["queueItem"]?["Length"],
                Requester = (string)x["queueItem"]?["Requester"],
                Played = 0,
                Albumcover = (string)x["queueItem"]?["Albumcover"],
                FullRequester = x["queueItem"]?["FullRequester"]?.ToObject<SimpleTwitchUser>()
            });

            // Recalculate Queue IDs
            for (int i = 0; i < GlobalObjects.ReqList.Count; i++)
            {
                GlobalObjects.ReqList[i].Queueid = i + 1;
            }
        }

        private static SimpleTwitchUser ExtractFullRequester(string payload)
        {
            try
            {
                if (!string.IsNullOrEmpty(payload))
                {
                    JObject x = JObject.Parse(payload);
                    return x["queueItem"]?["FullRequester"]?.ToObject<SimpleTwitchUser>();
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error extracting full requester information.", e);
            }

            return null;
        }

        public static async Task CleanupServerQueueAsync()
        {
            string result = await SongifyApi.GetQueueRawAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
                return;

            try
            {
                List<QueueItem> serverQueue =
                    Json.Deserialize<List<QueueItem>>(result);

                // Find every item in SQL that isn't in ReqList
                List<QueueItem> toRemove = serverQueue
                    .Where(q => GlobalObjects.ReqList.All(o => o.Queueid != q.Queueid))
                    .ToList();

                if (!toRemove.Any())
                    return;

                IEnumerable<Task> tasks = toRemove.Select(item =>
                    SongifyApi.PatchQueueItemAsync(item.Queueid)
                );

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error cleaning up server queue.", e);
            }
        }
    }
}