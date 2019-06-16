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
        private string[] songinfo;
        private AutomationElement _parent;

        /// <summary>
        /// A method to fetch the song that's currently playing on Spotify.
        /// returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        public string[] FetchDesktopPlayer(string player)
        {
            var processes = Process.GetProcessesByName(player);
            foreach (var process in processes)
            {
                if (process.ProcessName == player && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    // If the process name is "Spotify" and the window title is not empty
                    string wintitle = process.MainWindowTitle;
                    string artist = "", title = "", extra = "";

                    switch (player)
                    {
                        case "Spotify":
                            // Checks if the title is Spotify Premium or Spotify Free in which case we don't want to fetch anything
                            if (wintitle != "Spotify" && wintitle != "Spotify Premium" && wintitle != "Spotify Free" && wintitle != "Drag")
                            {
                                // Splitting the wintitle which is always Artist - Title
                                songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
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

                                return new[] { artist, title, extra };
                            }
                            // the wintitle gets changed as soon as spotify is paused, therefore I'm checking 
                            //if custom pause text is enabled and if so spit out custom text

                            if (Settings.CustomPauseTextEnabled)
                            {
                                return new[] { Settings.CustomPauseText, "", "" }; // (Settings.GetCustomPauseText(), "", "");
                            }
                            break;

                        case "vlc":
                            // Splitting the wintitle which is always Artist - Title
                            if (!wintitle.Contains(" - VLC media player"))
                            {
                                if (Settings.CustomPauseTextEnabled)
                                {
                                    return new[] { Settings.CustomPauseText, "", "" }; // (Settings.GetCustomPauseText(), "", "");
                                }

                                return new[] { "", "", "" };

                            }

                            wintitle = wintitle.Replace(" - VLC media player", "");
                            songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
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
                            return new[] { artist, title, extra };

                        case "foobar2000":
                            // Splitting the wintitle which is always Artist - Title
                            if (wintitle.StartsWith("foobar2000"))
                            {
                                if (Settings.CustomPauseTextEnabled)
                                {
                                    return new[] { Settings.CustomPauseText, "", "" }; // (Settings.GetCustomPauseText(), "", "");
                                }

                                return new[] { "", "", "" };
                            }

                            wintitle = wintitle.Replace(" [foobar2000]", "");
                            songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
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
                            return new[] { artist, title, extra };
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
        /// <param name="browser"></param>
        /// <returns>Returns String with Youtube Video Title</returns>
        public string FetchBrowser(string website, string browser = "chrome")
        {
            Process[] procsChrome = Process.GetProcessesByName(browser);
            foreach (Process chrome in procsChrome)
            {
                // the chrome process must have a window
                if (chrome.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                var elm = _parent == null ? AutomationElement.FromHandle(chrome.MainWindowHandle) : _parent;

                // find the automation element
                try
                {
                    AutomationElementCollection elementCollection = elm.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
                    foreach (AutomationElement elem in elementCollection)
                    {
                        // if the Tabitem Name contains Youtube
                        switch (website)
                        {
                            case "YouTube":
                                if (elem.Current.Name.Contains("YouTube"))
                                {
                                    _parent = TreeWalker.RawViewWalker.GetParent(elem);
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
                                    }
                                }
                                break;

                            case "Deezer":
                                if (elem.Current.Name.Contains("Deezer"))
                                {
                                    _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                    Console.WriteLine(elem.Current.Name);
                                    // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                                    string temp = elem.Current.Name;
                                    //string temp = Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", "");
                                    int index = temp.LastIndexOf("- Deezer", StringComparison.Ordinal);
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
                                    }
                                }
                                break;
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
            if (!String.IsNullOrEmpty(Settings.NbUserId))
            {
                // Getting JSON from the nightbot API
                string jsn;
                using (WebClient wc = new WebClient()
                {
                    Encoding = Encoding.UTF8
                })
                {
                    jsn = wc.DownloadString("https://api.nightbot.tv/1/song_requests/queue/?channel=" +
                                            Settings.NbUserId);
                }

                // Deserialize JSON and get the current song 
                var json = JsonConvert.DeserializeObject<NBObj>(jsn);
                return json._currentsong == null ? null : (string)json._currentsong.track.title;
            }

            return "No NightBot ID set.";
        }
    }
}