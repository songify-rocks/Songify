using Songify_Slim.Models;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;

namespace Songify_Slim.Util.Songify
{
    internal static class WebHelper
    {
        /// <summary>
        ///     This Class is a helper class to reduce repeatedly used code across multiple classes
        /// </summary>

        private static readonly ApiClient ApiClient = new ApiClient(GlobalObjects.ApiUrl);

        private enum RequestType
        {
            Queue,
            UploadSong,
            UploadHistory,
            Telemetry
        }

        internal enum RequestMethod
        {
            Get,
            Post,
            Patch,
            Clear
        }

        public static async void QueueRequest(RequestMethod method, string payload = null)
        {
            try
            {
                string result;
                switch (method)
                {
                    case RequestMethod.Get:
                        result = await ApiClient.Get("queue", Settings.Settings.Uuid);
                        if (string.IsNullOrEmpty(result))
                            return;
                        try
                        {
                            List<Models.QueueItem> queue = Json.Deserialize<List<Models.QueueItem>>(result);
                            queue.ForEach(q =>
                            {
                                if (GlobalObjects.ReqList.Count != 0 &&
                                    GlobalObjects.ReqList.Any(o => o.Queueid == q.Queueid)) return;
                                var pL = new
                                {
                                    uuid = Settings.Settings.Uuid,
                                    key = Settings.Settings.AccessKey,
                                    queueid = q.Queueid
                                };
                                QueueRequest(RequestMethod.Patch, Json.Serialize(pL));
                            });
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
                            return;
                        }
                        try
                        {
                            RequestObject response = Json.Deserialize<RequestObject>(result);
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GlobalObjects.ReqList.Add(response);
                            }));
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
                        await ApiClient.Clear("queue", payload);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(method), method, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async void TelemetryRequest(RequestMethod method, string payload)
        {
            if (method == RequestMethod.Post)
                await ApiClient.Post("telemetry", payload);
        }

        public static async void SongRequest(RequestMethod method, string payload)
        {
            if (method == RequestMethod.Post)
            {
                var response = await ApiClient.Post("song", payload);
                Debug.WriteLine(response);
            }
        }

        private static void DoWebRequest(string url, RequestType requestType, string operation = "")
        {
            const int maxTries = 5;
            const int waitTimeSeconds = 1;

            for (int currentTry = 1; currentTry <= maxTries; currentTry++)
            {
                try
                {
                    // Create a new 'HttpWebRequest' object to the mentioned URL.
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                    myHttpWebRequest.UserAgent = Settings.Settings.WebUserAgent;

                    // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                    using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
                    {
                        LogWebResponseStatus(requestType, operation, myHttpWebResponse.StatusDescription);

                        if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                        {
                            break; // Exit the for loop
                        }

                        if (myHttpWebResponse.StatusCode == HttpStatusCode.BadRequest)
                        {
                            if (currentTry >= maxTries)
                            {
                                // Maximum tries reached
                                break; // Exit the for loop
                            }
                            LogRetryAttempt(requestType, operation, currentTry, maxTries);

                            // Wait for some time before retrying
                            int waitTimeMillis = (int)Math.Pow(2, currentTry) * waitTimeSeconds * 1000;
                            Thread.Sleep(waitTimeMillis);
                        }
                        else if (myHttpWebResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            Logger.LogStr("API: PLEASE CONTACT US ON THE DISCORD -> https://discord.com/invite/H8nd4T4");
                            break; // Exit the for loop
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Logger.LogStr("API: PLEASE CONTACT US ON THE DISCORD -> https://discord.com/invite/H8nd4T4");
                        break; // Exit the for loop
                    }
                    else if (currentTry >= maxTries)
                    {
                        Logger.LogExc(ex);
                        break; // Exit the for loop
                    }
                    LogRetryAttempt(requestType, operation, currentTry, maxTries);

                    // Wait for some time before retrying
                    int waitTimeMillis = (int)Math.Pow(2, currentTry) * waitTimeSeconds * 1000;
                    Thread.Sleep(waitTimeMillis);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    break; // Exit the for loop
                }
            }
        }

        private static void LogWebResponseStatus(RequestType requestType, string operation, string statusDescription)
        {
            string message = $"API: {requestType}{(string.IsNullOrWhiteSpace(operation) ? ":" : " " + operation + ":")} Status: {statusDescription}";
            Logger.LogStr(message);
        }

        private static void LogRetryAttempt(RequestType requestType, string operation, int currentTry, int maxTries)
        {
            Logger.LogStr($"API: {requestType}{(!string.IsNullOrWhiteSpace(operation) ? " " + operation : "")}: Try {currentTry} of {maxTries}");
        }

        public static void UploadSong(string currSong, string coverUrl = null)
        {

            dynamic paylod = new
            {
                uuid = Settings.Settings.Uuid,
                key = Settings.Settings.AccessKey,
                song = currSong,
                cover = coverUrl
            };
            SongRequest(RequestMethod.Post, Json.Serialize(paylod));

            //// extras are UUID and Songinfo
            //string extras = Settings.Settings.Uuid +
            //                "&song=" + HttpUtility.UrlEncode(currSong.Trim().Replace("\"", ""), Encoding.UTF8) +
            //                "&cover=" + HttpUtility.UrlEncode(coverUrl, Encoding.UTF8) +
            //                "&key=" + WebUtility.UrlEncode(Settings.Settings.AccessKey);
            //string url = $"{GlobalObjects.BaseUrl}/song.php?id=" + extras;
            //DoWebRequest(url, RequestType.UploadSong);
        }

        public static void UploadHistory(string currSong, int unixTimestamp)
        {
            string song = GlobalObjects.CurrentSong == null ? currSong : $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}";

            string extras = Settings.Settings.Uuid +
                            "&tst=" + unixTimestamp +
                            "&song=" + HttpUtility.UrlEncode(song, Encoding.UTF8) +
                            "&key=" + WebUtility.UrlEncode(Settings.Settings.AccessKey);
            string url = $"{GlobalObjects.ApiUrl}/history.php/?id=" + extras;
            // Create a new 'HttpWebRequest' object to the mentioned URL.
            DoWebRequest(url, RequestType.UploadHistory);
        }

        public static async Task<string> GetBetaPatchNotes(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
        }
    }
}