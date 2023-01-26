using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Songify_Slim.Util.Songify
{
    internal static class WebHelper
    {
        /// <summary>
        ///     This Class is a helper class to reduce repeatedly used code across multiple classes
        /// </summary>
        public static void UpdateWebQueue(string trackId, string artist, string title, string length, string requester,
            string played, string o)
        {
            string operation = "";

            // This switch tells the php to either add or delete one entry or clear the entire queue
            switch (o)
            {
                case "i":
                    operation = "Add";
                    break;
                case "u":
                    operation = "Delete";
                    break;
                case "c":
                    operation = "Clear";
                    break;
            }

            // Here a URL is being created to call the website and insert the values to the db

            string extras = Settings.Settings.Uuid +
                            "&trackid=" + WebUtility.UrlEncode(trackId) +
                            "&artist=" + WebUtility.UrlEncode(artist.Replace("\"", "\\\"")) +
                            "&title=" + WebUtility.UrlEncode(title.Replace("\"", "\\\"")) +
                            "&length=" + WebUtility.UrlEncode(length) +
                            "&requester=" + WebUtility.UrlEncode(requester) +
                            "&played=" + WebUtility.UrlEncode(played) +
                            "&o=" + WebUtility.UrlEncode(o) +
                            "&key=" + WebUtility.UrlEncode(Settings.Settings.AccessKey);

            string url = $"{GlobalObjects._baseUrl}/add_queue.php/?id=" + extras;
            WebUtility.UrlEncode(url);
            DoWebRequest(url, operation);
        }

        private static void DoWebRequest(string url, string operation = "")
        {
            try
            {
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Settings.WebUserAgent;

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if (myHttpWebResponse.StatusCode != HttpStatusCode.OK)
                    Logger.LogStr("WEB: " + operation + " Queue:" + myHttpWebResponse.StatusDescription);
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("(403)"))
                    Logger.LogStr($"WEB: Your key changed. Please contact us to resolve the issue.");

                Logger.LogExc(ex);

            }
        }

        public static void SendTelemetry()
        {
            string extras = $"?id={Settings.Settings.Uuid}" +
                            $"&tst={WebUtility.UrlEncode(((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString())}" +
                            $"&v={WebUtility.UrlEncode(Assembly.GetExecutingAssembly().GetName().Version.ToString())}" +
                            $"&key={WebUtility.UrlEncode(Settings.Settings.AccessKey)}" +
                            $"&tid={WebUtility.UrlEncode(Settings.Settings.TwitchUser == null ? "" : Settings.Settings.TwitchUser.Id)}" +
                            $"&tn={WebUtility.UrlEncode(Settings.Settings.TwitchUser == null ? "" : Settings.Settings.TwitchUser.DisplayName)}";
            string url = $"{GlobalObjects._baseUrl}/songifydata.php/" + extras;
            WebUtility.UrlEncode(url);
            DoWebRequest(url, "Telemetry");
        }

        public static void UploadSong(string currSong, string coverUrl = null)
        {
            // extras are UUID and Songinfo
            string extras = Settings.Settings.Uuid +
                            "&song=" + HttpUtility.UrlEncode(currSong.Trim().Replace("\"", ""), Encoding.UTF8) +
                            "&cover=" + HttpUtility.UrlEncode(coverUrl, Encoding.UTF8) +
                            "&key=" + WebUtility.UrlEncode(Settings.Settings.AccessKey);
            string url = $"{GlobalObjects._baseUrl}/song.php?id=" + extras;
            Console.WriteLine(url);
            DoWebRequest(url, "Upload Song");
        }

        public static void UploadHistory(string currSong, int unixTimestamp)
        {
            string extras = Settings.Settings.Uuid + 
                            "&tst=" + unixTimestamp + 
                            "&song=" + HttpUtility.UrlEncode(currSong, Encoding.UTF8)+
                            "&key=" + WebUtility.UrlEncode(Settings.Settings.AccessKey);
            string url = $"{GlobalObjects._baseUrl}/song_history.php/?id=" + extras;
            // Create a new 'HttpWebRequest' object to the mentioned URL.
            DoWebRequest(url, "Upload History");
        }
    }
}