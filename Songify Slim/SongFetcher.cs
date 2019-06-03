using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace Songify_Slim
{

    /// <summary>
    /// This class is for retrieving data of currently playing songs
    /// </summary>
    class SongFetcher
    {

        private AutomationElement parent = null;

        /// <summary>
        /// A method to fetch the song that's currently playing on Spotify.
        /// returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        public string[] FetchSpotify()
        {
            var processes = Process.GetProcessesByName("Spotify");
            foreach (var process in processes)
            {
                if (process.ProcessName == "Spotify" && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    // If the process name is "Spotify" and the window title is not empty
                    string wintitle = process.MainWindowTitle;
                    string artist = "", title = "", extra = "";
                    // Checks if the title is Spotify Premium or Spotify Free in which case we don't want to fetch anything
                    if (wintitle != "Spotify" && wintitle != "Spotify Premium" && wintitle != "Spotify Free" && wintitle != "Drag")
                    {
                        // Splitting the wintitle which is always Artist - Title
                        string[] songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                        try
                        {
                            artist = songinfo[0].Trim();
                            title = songinfo[1].Trim();
                            // Extra content like "- Offical Anthem" or "- XYZ Remix" and so on
                            if (songinfo.Length > 2)
                                extra = "(" + String.Join("", songinfo, 2, songinfo.Length - 2).Trim() + ")";
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }

                        return new string[] { artist, title, extra };
                    }
                    // the wintitle gets changed as soon as spotify is paused, therefore I'm checking 
                    //if custom pause text is enabled and if so spit out custom text
                    else
                    {
                        if (Settings.CustomPauseTextEnabled)
                        {
                            return new string[] { Settings.CustomPauseText, title, extra }; // (Settings.GetCustomPauseText(), "", "");
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// A method to fetch the song that's currently playing on Youtube.
        /// returns empty string if unsuccessful and custom pause text is not set.
        /// Currently supported browsers: Google Chrome
        /// </summary>
        /// <param name="Browser"></param>
        /// <returns>Returns String with Youtube Video Title</returns>
        public string FetchYoutube(string browser)
        {
            Process[] procsChrome = Process.GetProcessesByName("chrome");
            foreach (Process chrome in procsChrome)
            {
                // the chrome process must have a window
                if (chrome.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                var elm = parent == null ? AutomationElement.FromHandle(chrome.MainWindowHandle) : parent;

                // find the automation element
                try
                {
                    AutomationElementCollection elementCollection = elm.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
                    foreach (AutomationElement elem in elementCollection)
                    {
                        // if the Tabitem Name contains Youtube
                        if (elem.Current.Name.Contains("YouTube"))
                        {
                            parent = TreeWalker.RawViewWalker.GetParent(elem);
                            Console.WriteLine(elem.Current.Name);
                            // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                            string temp = Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", "");
                            int index = temp.LastIndexOf("- YouTube", StringComparison.Ordinal);
                            // Remove everything after the last "-" int the string 
                            // which is "- Youtube" and info that music is playing on this tab
                            if (index > 0)
                                temp = temp.Substring(0, index);
                            temp = temp.Trim();
                            Console.WriteLine(temp);
                            
                            // Making sure that temp is not empty
                            // this makes sure that the output is not empty
                            if (!String.IsNullOrWhiteSpace(temp))
                            {
                                return temp;
                            } else if (Settings.CustomPauseTextEnabled)
                            {
                                return Settings.CustomPauseText;
                            } else
                            {
                                return "";
                            }
                        }
                        else
                        {
                            return Settings.CustomPauseTextEnabled ? Settings.CustomPauseText : "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    // Chrome has probably changed something, and above walking needs to be modified. :(
                    // put an assertion here or something to make sure you don't miss it
                }
            }
            return "";
        }


        /// <summary>
        /// A method to fetch the song that is currently playing via NightBot Song Request.
        /// Returns null if unsuccessful and custom pause text is not set.
        /// Returns Error Message if NightBot ID is not set
        /// </summary>
        /// <returns>Returns String with currently playing NB Song Request</returns>
        public string FetchNightBot()
        {
            // Checking if the user has set the setting for Nightbot
            if (!String.IsNullOrEmpty(Settings.NBUserID))
            {
                // Getting JSON from the nightbot API
                string jsn = "";
                using (System.Net.WebClient wc = new WebClient()
                {
                    Encoding = Encoding.UTF8
                })
                {
                    jsn = wc.DownloadString("https://api.nightbot.tv/1/song_requests/queue/?channel=" +
                                            Settings.NBUserID);
                }

                // Deserialize JSON and get the current song 
                var serializer = new JsonSerializer();
                NBObj json = JsonConvert.DeserializeObject<NBObj>(jsn);
                if (json._currentsong == null)
                    return null;
                return json._currentsong.track.title;
            }

            return "No NightBot ID set.";
        }
    }
}