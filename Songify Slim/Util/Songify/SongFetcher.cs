using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Unosquare.Swan.Formatters;

namespace Songify_Slim.Util.Songify
{
    /// <summary>
    ///     This class is for retrieving data of currently playing songs
    /// </summary>
    public class SongFetcher
    {
        private static int _id;
        private readonly List<string> _browsers = new List<string> { "chrome", "msedge", "opera" };
        private readonly List<string> _audioFileyTypes = new List<string> { ".3gp", ".aa", ".aac", ".aax", ".act", ".aiff", ".alac", ".amr", ".ape", ".au", ".awb", ".dss", ".dvf", ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".mmf", ".mp3", ".mpc", ".msv", ".nmf", ".ogg", ".oga", ".mogg", ".opus", ".ra", ".rm", ".raw", ".rf64", ".sln", ".tta", ".voc", ".vox", ".wav", ".wma", ".wv", ".webm", ".8svx", ".cda" };
        private AutomationElement _parent;
        private static string[] _songinfo;
        private static SongInfo _previousSonginfo;
        private static TrackInfo songInfo;

        /// <summary>
        ///     A method to fetch the song that's currently playing on Spotify.
        ///     returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        internal Task<SongInfo> FetchDesktopPlayer(string player)
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
                                // Splitting the win title which is always Artist - Title
                                _songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                                try
                                {
                                    artist = _songinfo[0].Trim();
                                    title = _songinfo[1].Trim();
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogExc(ex);
                                }

                                return Task.FromResult(new SongInfo { Artist = artist, Title = title });
                            }
                            // the win title gets changed as soon as spotify is paused, therefore I'm checking 
                            //if custom pause text is enabled and if so spit out custom text

                            if (Settings.Settings.CustomPauseTextEnabled)
                                return Task.FromResult(new SongInfo { Artist = "", Title = "", Extra = "" }); // (Settings.GetCustomPauseText(), "", "");
                            break;

                        case "vlc":
                            //Splitting the win title which is always Artist - Title
                            if (string.IsNullOrEmpty(wintitle) || wintitle == "vlc")
                                return Task.FromResult(_previousSonginfo);

                            if (!wintitle.Contains(" - VLC media player"))
                                return Task.FromResult(Settings.Settings.CustomPauseTextEnabled
                                    ? new SongInfo { Artist = Settings.Settings.CustomPauseText, Title = "", Extra = "" }
                                    : new SongInfo { Artist = "", Title = "", Extra = "" });

                            wintitle = wintitle.Replace(" - VLC media player", "");

                            try
                            {
                                foreach (string item in _audioFileyTypes.Where(item => wintitle.Contains(item)))
                                {
                                    wintitle = wintitle.Replace(item, "");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogExc(ex);
                            }
                            finally
                            {
                                artist = wintitle;
                                title = "";
                                extra = "";
                            }

                            _songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);



                            _previousSonginfo = new SongInfo { Artist = artist, Title = title, Extra = extra };
                            return Task.FromResult(_previousSonginfo);

                        case "foobar2000":
                            // Splitting the win title which is always Artist - Title
                            if (wintitle.StartsWith("foobar2000"))
                            {
                                if (Settings.Settings.CustomPauseTextEnabled)
                                    return Task.FromResult(new SongInfo
                                    {
                                        Artist = Settings.Settings.CustomPauseText,
                                        Title = "",
                                        Extra = ""
                                    });
                                return Task.FromResult(new SongInfo
                                {
                                    Artist = "",
                                    Title = "",
                                    Extra = ""
                                });
                            }

                            wintitle = wintitle.Replace(" [foobar2000]", "");
                            try
                            {
                                foreach (string item in _audioFileyTypes.Where(item => wintitle.Contains(item)))
                                {
                                    wintitle = wintitle.Replace(item, "");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogExc(ex);
                            }
                            finally
                            {
                                artist = wintitle;
                                title = "";
                                extra = "";
                            }
                            _previousSonginfo = new SongInfo { Artist = artist, Title = title, Extra = extra };
                            return Task.FromResult(_previousSonginfo);
                    }
                }

            return Task.FromResult<SongInfo>(null);
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
                            // if the tab item Name contains Youtube
                            switch (website)
                            {
                                case "YouTube":
                                    if (elem.Current.Name.Contains("YouTube"))
                                    {
                                        _id = elem.Current.ControlType.Id;
                                        _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                        // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                                        return FormattedString("YouTube", Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
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

                        // if the tab item Name contains Youtube
                        switch (website)
                        {
                            case "YouTube":
                                if (element == null)
                                    break;
                                if (element.Current.Name.Contains("YouTube"))
                                {
                                    //_id = element.Current.ControlType.Id;
                                    //_parent = TreeWalker.RawViewWalker.GetParent(element);
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
                return null;
            }

            // gets the current playing songinfo
            songInfo = ApiHandler.GetSongInfo();
            try
            {
                GlobalObjects.CurrentSong = songInfo;
                string j = Json.Serialize(songInfo);
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(j);
                IDictionary<string, object> dictionary = obj.ToObject<IDictionary<string, object>>();
                dictionary["Requester"] = GlobalObjects.Requester;
                dictionary["GoalTotal"] = Settings.Settings.RewardGoalAmount;
                dictionary["GoalCount"] = GlobalObjects.RewardGoalCount;
                string updatedJson = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
                //Console.WriteLine(updatedJson);

                GlobalObjects.APIResponse = updatedJson;

                //WriteProgressFile($"{path}/progress.txt", j);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            //Console.WriteLine($"{songInfo.Progress} / {songInfo.DurationTotal} ({songInfo.DurationPercentage}%)");
            // if no song is playing and custompausetext is enabled
            return songInfo ?? new TrackInfo { isPlaying = false };
            // return a new stringarray containing artist, title and so on
        }
    }
}