using System;
using System.Net;
using System.Web;

namespace Songify_Slim
{
    static class WebHelper
    {
        /// <summary>
        /// This Class is a helper class to reduce repeatedly used code across multiple classes
        /// </summary>

        public static void UpdateWebQueue(string trackID, string artist, string title, string length, string requester, string played, string o)
        {
            string operation = "";

            // This switch tells the php to either add or delte one entry or clear the entire queue
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
            try
            {
                string extras = Settings.Uuid +
                "&trackid=" + WebUtility.UrlEncode(trackID) +
                "&artist=" + WebUtility.UrlEncode(artist.Replace("\"","\\\"")) +
                "&title=" + WebUtility.UrlEncode(title.Replace("\"", "\\\"")) +
                "&length=" + WebUtility.UrlEncode(length) +
                "&requester=" + WebUtility.UrlEncode(requester) +
                "&played=" + WebUtility.UrlEncode(played) +
                "&o=" + WebUtility.UrlEncode(o);
                string url = "http://songify.rocks/add_queue.php/?id=" + extras;
                string escapeurl = WebUtility.UrlEncode(url);


                Console.WriteLine(url);
                Console.WriteLine(escapeurl);

                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Logger.LogStr(operation + " Queue:" + myHttpWebResponse.StatusDescription);
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

    }
}
