using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using Newtonsoft.Json;
using Songify_Slim.Models;

namespace Songify_Slim.Util.Songify
{
    /// <summary>
    ///     This class is for retrieving data of currently playing songs
    /// </summary>
    internal class SongFetcher
    {
        private static int _id;
        private readonly List<string> _browsers = new List<string> { "chrome", "msedge", "opera" };
        private AutomationElement _parent;
        private static string[] _songinfo;

        /// <summary>
        ///     A method to fetch the song that's currently playing on Spotify.
        ///     returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        internal async Task<SongInfo> FetchDesktopPlayer(string player)
        {
            var processes = Process.GetProcessesByName(player);
            foreach (Process process in processes)
                if (process.ProcessName == player && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    // If the process name is "Spotify" and the window title is not empty
                    string wintitle = process.MainWindowTitle;
                    string artist = "", title = "", extra = "";

                    switch (player)
                    {
                        case "Spotify":
                            // Checks if the title is Spotify Premium or Spotify Free in which case we don't want to fetch anything
                            if (wintitle != "Spotify" && wintitle != "Spotify Premium" && wintitle != "Spotify Free" &&
                                wintitle != "Drag")
                            {
                                // Splitting the wintitle which is always Artist - Title
                                _songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                                try
                                {
                                    artist = _songinfo[0].Trim();
                                    title = _songinfo[1].Trim();
                                    // Extra content like "- Offical Anthem" or "- XYZ Remix" and so on
                                    if (_songinfo.Length > 2)
                                        extra = "(" + string.Join("", _songinfo, 2, _songinfo.Length - 2).Trim() + ")";
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogExc(ex);
                                }

                                return new SongInfo { Artist = artist, Title = title };
                            }
                            // the wintitle gets changed as soon as spotify is paused, therefore I'm checking 
                            //if custom pause text is enabled and if so spit out custom text

                            if (Settings.Settings.CustomPauseTextEnabled)
                                return new SongInfo { Artist = "", Title = "", Extra = "" }; // (Settings.GetCustomPauseText(), "", "");
                            break;

                        case "vlc":
                            //Splitting the wintitle which is always Artist - Title

                            if (!wintitle.Contains(" - VLC media player"))
                                return Settings.Settings.CustomPauseTextEnabled
                                    ? new SongInfo { Artist = Settings.Settings.CustomPauseText, Title = "", Extra = "" }
                                    : new SongInfo { Artist = "", Title = "", Extra = "" };

                            wintitle = wintitle.Replace(" - VLC media player", "");

                            _songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);

                            try
                            {
                                if (wintitle.LastIndexOf('.') > 0)
                                {
                                    wintitle = wintitle.Substring(0, wintitle.LastIndexOf('.'));
                                }
                                artist = wintitle;
                                title = "";
                                extra = "";
                            }
                            catch (Exception ex)
                            {
                                Logger.LogExc(ex);
                            }

                            return new SongInfo { Artist = artist, Title = title, Extra = extra };

                        case "foobar2000":
                            // Splitting the wintitle which is always Artist - Title
                            if (wintitle.StartsWith("foobar2000"))
                            {
                                if (Settings.Settings.CustomPauseTextEnabled)
                                    return new SongInfo
                                    {
                                        Artist = Settings.Settings.CustomPauseText,
                                        Title = "",
                                        Extra = ""
                                    };
                                return new SongInfo
                                {
                                    Artist = "",
                                    Title = "",
                                    Extra = ""
                                };
                            }

                            wintitle = wintitle.Replace(" [foobar2000]", "");
                            try
                            {
                                wintitle = wintitle.Substring(0, wintitle.LastIndexOf('.'));

                                artist = wintitle;
                                title = "";
                                extra = "";
                            }
                            catch (Exception ex)
                            {
                                Logger.LogExc(ex);
                            }

                            return new SongInfo { Artist = artist, Title = title, Extra = extra };
                    }
                }

            return null;
        }


        /// <summary>
        ///     A method to fetch the song that's currently playing on Youtube.
        ///     returns empty string if unsuccessful and custom pause text is not set.
        ///     Currently supported browsers: Google Chrome
        /// </summary>
        /// <param name="website"></param>
        /// <returns>Returns String with Youtube Video Title</returns>
        public string FetchBrowser(string website)
        {
            string browser = "";

            // chrome, opera, msedge
            foreach (string s in _browsers.Where(s => Process.GetProcessesByName(s).Length > 0))
            {
                browser = s;
                break;
            }

            var procsBrowser = Process.GetProcessesByName(browser);

            foreach (Process procBrowser in procsBrowser)
            {
                // the chrome process must have a window
                if (procBrowser.MainWindowHandle == IntPtr.Zero) continue;

                AutomationElement elm =
                    _parent == null ? AutomationElement.FromHandle(procBrowser.MainWindowHandle) : _parent;

                if (_id == 0)
                    // find the automation element
                    try
                    {
                        AutomationElementCollection elementCollection = elm.FindAll(TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
                        foreach (AutomationElement elem in elementCollection)
                            // if the Tabitem Name contains Youtube
                            switch (website)
                            {
                                case "YouTube":
                                    if (elem.Current.Name.Contains("YouTube"))
                                    {
                                        _id = elem.Current.ControlType.Id;
                                        _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                        // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                                        return FormattedString("YouTube",
                                            Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                    }

                                    break;

                                case "Deezer":
                                    if (elem.Current.Name.Contains("Deezer"))
                                    {
                                        _id = elem.Current.ControlType.Id;
                                        _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                        return FormattedString("Deezer", elem.Current.Name);
                                    }

                                    break;
                            }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                        // Chrome has probably changed something, and above walking needs to be modified. :(
                        // put an assertion here or something to make sure you don't miss it
                    }
                else
                    try
                    {
                        AutomationElement element =
                            elm.FindFirst(TreeScope.Descendants,
                                new PropertyCondition(AutomationElement.ControlTypeProperty,
                                    ControlType.LookupById(_id)));

                        // if the Tabitem Name contains Youtube
                        switch (website)
                        {
                            case "YouTube":
                                if (element == null)
                                    break;
                                if (element.Current.Name.Contains("YouTube"))
                                {
                                    _id = element.Current.ControlType.Id;
                                    _parent = TreeWalker.RawViewWalker.GetParent(element);
                                    // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                                    return FormattedString("YouTube",
                                        Regex.Replace(element.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                }

                                break;

                            case "Deezer":
                                if (element.Current.Name.Contains("Deezer"))
                                {
                                    _id = element.Current.ControlType.Id;
                                    _parent = TreeWalker.RawViewWalker.GetParent(element);
                                    return FormattedString("Deezer", element.Current.Name);
                                }

                                break;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }

            return "";
        }

        private static string FormattedString(string player, string temp)
        {
            string s = temp;
            int index;
            switch (player)
            {
                case "YouTube":
                    index = s.LastIndexOf("- YouTube", StringComparison.Ordinal);
                    // Remove everything after the last "-" int the string 
                    // which is "- Youtube" and info that music is playing on this tab
                    if (index > 0)
                        s = s.Substring(0, index);
                    s = s.Trim();
                    break;
                case "Deezer":
                    //string temp = Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", "");
                    index = s.LastIndexOf("- Deezer", StringComparison.Ordinal);
                    // Remove everything after the last "-" int the string 
                    // which is "- Youtube" and info that music is playing on this tab
                    if (index > 0)
                        s = s.Substring(0, index);
                    s = s.Trim();
                    break;
            }

            return s;
        }

        /// <summary>
        ///     A method to fetch the song that is currently playing via NightBot Song Request.
        ///     Returns null if unsuccessful and custom pause text is not set.
        ///     Returns Error Message if NightBot ID is not set
        /// </summary>
        /// <returns>Returns String with currently playing NB Song Request</returns>

        //public string FetchNightBot()
        //{
        //    // Checking if the user has set the setting for Nightbot
        //    if (string.IsNullOrEmpty(Settings.Settings.NbUserId)) return "No NightBot ID set.";
        //    // Getting JSON from the nightbot API
        //    string jsn;
        //    using (WebClient wc = new WebClient
        //    {
        //        Encoding = Encoding.UTF8
        //    })
        //    {
        //        string url = "https://api.nightbot.tv/1/song_requests/queue/?channel="+Settings.Settings.NbUserId;
        //        jsn = wc.DownloadString("https://api.nightbot.tv/1/song_requests/queue/?channel=" +
        //                                Settings.Settings.NbUserId);
        //    }

        //    // Deserialize JSON and get the current song 
        //    NBObj json = JsonConvert.DeserializeObject<NBObj>(jsn);
        //    return json._currentsong == null ? null : (string)json._currentsong.track.title;
        //}

        public TrackInfo FetchSpotifyWeb()
        {
            // If the spotify object hast been created (successfully authed)
            if (ApiHandler.Spotify == null)
            {
                Logger.LogStr("SPOTIFY API: Spotify API Object is NULL");
                return null;
            }

            // gets the current playing songinfo
            TrackInfo songInfo = ApiHandler.GetSongInfo();
            // if no song is playing and custompausetext is enabled
            return songInfo ?? new TrackInfo { isPlaying = false };
            // return a new stringarray containing artist, title and so on
        }
    }
}