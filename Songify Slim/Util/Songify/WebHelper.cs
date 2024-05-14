﻿using Songify_Slim.Models;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Songify_Slim.Views;
using System.Windows;

namespace Songify_Slim.Util.Songify
{
    internal static class WebHelper
    {
        /// <summary>
        ///     This Class is a helper class to reduce repeatedly used code across multiple classes
        /// </summary>

        private static readonly ApiClient ApiClient = new(GlobalObjects.ApiUrl);
        
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
                            List<Task> tasks = new();
                            foreach (Models.QueueItem q in queue)
                            {
                                if (GlobalObjects.ReqList.Count != 0 &&
                                    GlobalObjects.ReqList.Any(o => o.Queueid == q.Queueid)) continue;
                                var pL = new
                                {
                                    uuid = Settings.Settings.Uuid,
                                    key = Settings.Settings.AccessKey,
                                    queueid = q.Queueid
                                };
                                tasks.Add(QueueRequest(RequestMethod.Patch, Json.Serialize(pL)));
                            }
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
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }
        }

        private static async void UpdateQueueWindow()
        {
            SimpleQueue queue = await SpotifyApiHandler.GetQueueInfo();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(WindowQueue))
                        continue;
                    //(qw as Window_Queue).dgv_Queue.ItemsSource.
                    ((WindowQueue)window).dgv_Queue.ItemsSource = null;
                    ((WindowQueue)window).dgv_Queue.Items.Clear();
                    foreach (FullTrack fullTrack in queue.Queue)
                    {
                        if (GlobalObjects.ReqList.Any(o => o.Trackid == fullTrack.Id))
                        {
                            RequestObject reqObj = GlobalObjects.ReqList.First(o => o.Trackid == fullTrack.Id);
                            (window as WindowQueue)?.dgv_Queue.Items.Add(reqObj);
                        }
                        else
                        {
                            (window as WindowQueue)?.dgv_Queue.Items.Add(new RequestObject
                            {
                                Queueid = 0,
                                Uuid = Settings.Settings.Uuid,
                                Trackid = fullTrack.Id,
                                Artist = string.Join(", ", fullTrack.Artists.Select(o => o.Name).ToList()),
                                Title = fullTrack.Name,
                                Length = GlobalObjects.MsToMmSsConverter((int)fullTrack.DurationMs),
                                Requester = "Spotify",
                                Played = 0,
                                Albumcover = null
                            });
                        }
                    }
                    (window as WindowQueue)?.dgv_Queue.Items.Refresh();
                }
            });
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
    }
}