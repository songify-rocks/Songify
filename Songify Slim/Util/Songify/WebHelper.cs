using Songify_Slim.Models;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Songify_Slim.Views;
using System.Windows;
using Newtonsoft.Json.Linq;
using Songify_Slim.Models.YTMD;

namespace Songify_Slim.Util.Songify
{
    internal static class WebHelper
    {
        /// <summary>
        ///     This Class is a helper class to reduce repeatedly used code across multiple classes
        /// </summary>

        private static readonly ApiClient ApiClient = new(GlobalObjects.ApiUrl);
        private static readonly YTMDApiClient ApiClientYTM = new("http://localhost:9863/api/v1");

        internal enum RequestMethod
        {
            Get,
            Post,
            Patch,
            Clear
        }

        public static async Task QueueRequest(RequestMethod method, string payload = null)
        {
            try
            {
                string result;
                switch (method)
                {
                    case RequestMethod.Get:
                        result = await ApiClient.Get("queue", Settings.Settings.Uuid).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(result))
                            return;

                        try
                        {
                            List<Models.QueueItem> queue = Json.Deserialize<List<Models.QueueItem>>(result);
                            List<Task> tasks = [];
                            tasks.AddRange(from q in queue
                                           where GlobalObjects.ReqList.Count == 0 || GlobalObjects.ReqList.All(o => o.Queueid != q.Queueid)
                                           select new { uuid = Settings.Settings.Uuid, key = Settings.Settings.AccessKey, queueid = q.Queueid }
                                into pL
                                           select QueueRequest(RequestMethod.Patch, Json.Serialize(pL)));
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }
                        break;
                    case RequestMethod.Post:
                        result = await ApiClient.Post("queue", payload);
                        if (string.IsNullOrEmpty(result))
                        {
                            JObject x = JObject.Parse(payload);
                            GlobalObjects.ReqList.Add(new RequestObject
                            {
                                Queueid = GlobalObjects.ReqList.Count + 1,
                                Uuid = Settings.Settings.Uuid,
                                Trackid = (string)x["queueItem"]["Trackid"],
                                Artist = (string)x["queueItem"]["Artist"],
                                Title = (string)x["queueItem"]["Title"],
                                Length = (string)x["queueItem"]["Length"],
                                Requester = (string)x["queueItem"]["Requester"],
                                Played = 0,
                                Albumcover = (string)x["queueItem"]["Albumcover"]
                            });
                            //Update indexes of the queue
                            for (int i = 0; i < GlobalObjects.ReqList.Count; i++)
                            {
                                GlobalObjects.ReqList[i].Queueid = i + 1;
                            }

                            UpdateQueueWindow();

                            return;
                        }
                        try
                        {
                            RequestObject response = Json.Deserialize<RequestObject>(result);
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GlobalObjects.ReqList.Add(response);
                            }));
                            UpdateQueueWindow();
                        }

                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }

                        break;
                    case RequestMethod.Patch:
                        await ApiClient.Patch("queue", payload);
                        break;
                    case RequestMethod.Clear:
                        await ApiClient.Clear("queue_delete", payload);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(method), method, null);
                }
            }
            catch (Exception)
            {
                //Console.WriteLine(e);
            }
        }

        private static void UpdateQueueWindow()
        {
            GlobalObjects.QueueUpdateQueueWindow();
        }

        public static async Task TelemetryRequest(RequestMethod method, string payload)
        {
            if (method == RequestMethod.Post)
                await ApiClient.Post("telemetry", payload);
        }

        public static async void SongRequest(RequestMethod method, string payload)
        {
            if (method == RequestMethod.Post)
            {
                string response = await ApiClient.Post("song", payload);
                //Debug.WriteLine(response);
            }
        }

        public static async void HistoryRequest(RequestMethod method, string payload)
        {
            switch (method)
            {
                case RequestMethod.Get:
                    break;
                case RequestMethod.Post:
                    string response = await ApiClient.Post("history", payload);
                    break;
                case RequestMethod.Patch:
                    break;
                case RequestMethod.Clear:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }

        public static void UploadSong(string currSong, string coverUrl = null)
        {
            dynamic payload = new
            {
                uuid = Settings.Settings.Uuid,
                key = Settings.Settings.AccessKey,
                song = currSong,
                cover = coverUrl,
                song_id = GlobalObjects.CurrentSong == null ? null : GlobalObjects.CurrentSong.SongId
            };
            SongRequest(RequestMethod.Post, Json.Serialize(payload));
        }

        public static void UploadHistory(string currSong, int unixTimestamp)
        {
            string song = GlobalObjects.CurrentSong == null ? currSong : $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}";

            dynamic payload = new
            {
                id = Settings.Settings.Uuid,
                tst = unixTimestamp,
                song = song,
                key = Settings.Settings.AccessKey
            };
            HistoryRequest(RequestMethod.Post, Json.Serialize(payload));
        }

        public static async Task<string> GetBetaPatchNotes(string url)
        {
            using (HttpClient httpClient = new())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                return content;
            }
        }

        public static async Task<List<PSA>> GetPSA()
        {
            string result = await ApiClient.Get("motd", "");
            if (string.IsNullOrEmpty(result))
            {
                return null;
            }
            try
            {
                List<PSA> psas = JsonConvert.DeserializeObject<List<PSA>>(result);
                return psas.Count == 0 ? null : psas;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }

            return null;
        }

        public static async Task<Tuple<bool, string>> GetCanvasAsync(string songInfoSongId)
        {
            if (string.IsNullOrEmpty(songInfoSongId))
            {
                return new Tuple<bool, string>(false, "");
            }
            string result = await ApiClient.GetCanvas(songInfoSongId);
            result = result.Replace("\"", "");
            return result != "No canvas found" ? new Tuple<bool, string>(true, result) : new Tuple<bool, string>(false, "");
        }

        internal static async Task<YTMDResponse> GetYTMData()
        {
            if (string.IsNullOrEmpty(Settings.Settings.YTMDToken))
                return null;

            string result = await ApiClientYTM.Get("state");
            return string.IsNullOrEmpty(result) ? null : JsonConvert.DeserializeObject<YTMDResponse>(result);
        }
    }
}