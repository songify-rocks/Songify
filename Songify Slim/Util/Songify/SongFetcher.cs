using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
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
        private static TrackInfo _songInfo;
        private static bool _trackChanged;

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
                    string artist = "", title = "", extra;

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
                                    _songInfo = GlobalObjects.CurrentSong = new TrackInfo
                                    {
                                        Artists = artist,
                                        Title = title,
                                        Albums = null,
                                        SongId = null,
                                        DurationMs = 0,
                                        IsPlaying = false,
                                        Url = null,
                                        DurationPercentage = 0,
                                        DurationTotal = 0,
                                        Progress = 0,
                                        Playlist = null
                                    };
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogExc(ex);
                                }
                                UpdateWebServerResponse();
                                return Task.FromResult(new SongInfo { Artist = artist, Title = title });
                            }
                            // the win title gets changed as soon as spotify is paused, therefore I'm checking
                            //if custom pause text is enabled and if so spit out custom text

                            if (Settings.Settings.CustomPauseTextEnabled)
                                return Task.FromResult(new SongInfo { Artist = "", Title = "", Extra = "" });
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
                            UpdateWebServerResponse();

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
                            UpdateWebServerResponse();

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
                                        UpdateWebServerResponse();
                                        // Regex pattern to replace the notification in front of the tab (1) - (99+)
                                        return FormattedString("YouTube", Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                    }

                                    break;

                                case "Deezer":
                                    if (elem.Current.Name.Contains("Deezer"))
                                    {
                                        _id = elem.Current.ControlType.Id;
                                        _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                        UpdateWebServerResponse();
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
                                    string formattedString = FormattedString("YouTube", Regex.Replace(element.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                    //try splitting the formatted string to Artist and Title
                                    _songInfo = GlobalObjects.CurrentSong = new TrackInfo
                                    {
                                        Artists = formattedString.Contains("-") ? formattedString.Split('-')[0].Trim() : formattedString,
                                        Title = formattedString.Contains("-") ? formattedString.Split('-')[1].Trim() : formattedString,
                                        Albums = null,
                                        SongId = null,
                                        DurationMs = 0,
                                        IsPlaying = false,
                                        Url = null,
                                        DurationPercentage = 0,
                                        DurationTotal = 0,
                                        Progress = 0,
                                        Playlist = null
                                    };
                                    UpdateWebServerResponse();
                                    return formattedString;
                                }

                                break;

                            case "Deezer":
                                if (element.Current.Name.Contains("Deezer"))
                                {
                                    _id = element.Current.ControlType.Id;
                                    _parent = TreeWalker.RawViewWalker.GetParent(element);
                                    string formattedString = FormattedString("Deezer", element.Current.Name);
                                    //try splitting the formatted string to Artist and Title
                                    _songInfo = GlobalObjects.CurrentSong = new TrackInfo
                                    {
                                        Artists = formattedString.Contains("-") ? formattedString.Split('-')[0].Trim() : formattedString,
                                        Title = formattedString.Contains("-") ? formattedString.Split('-')[1].Trim() : formattedString,
                                        Albums = null,
                                        SongId = null,
                                        DurationMs = 0,
                                        IsPlaying = false,
                                        Url = null,
                                        DurationPercentage = 0,
                                        DurationTotal = 0,
                                        Progress = 0,
                                        Playlist = null
                                    };
                                    UpdateWebServerResponse();
                                    return formattedString;
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

        public async Task<TrackInfo> FetchSpotifyWeb()
        {
            // If the spotify object hast been created (successfully authed)
            if (ApiHandler.Spotify == null)
            {
                if (!string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken) &&
                    !string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken))
                {
                    ApiHandler.DoAuthAsync();
                }
                return null;
            }
            // gets the current playing songinfo*
            _songInfo = ApiHandler.GetSongInfo();
            try
            {
                if (GlobalObjects.CurrentSong == null || (GlobalObjects.CurrentSong.SongId != _songInfo.SongId && _songInfo.SongId != null))
                {
                    _trackChanged = true;
                    if (GlobalObjects.CurrentSong != null)
                        Logger.LogStr($"CORE: Previous Song {GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    if (_songInfo.SongId != null)
                        Logger.LogStr($"CORE: Now Playing {_songInfo.Artists} - {_songInfo.Title}");
                    RequestObject rq = GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == GlobalObjects.CurrentSong.SongId);
                    if (rq != null)
                    {
                        Logger.LogStr($"QUEUE: Trying to remove {rq.Artist} - {rq.Title}");
                        do
                        {
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GlobalObjects.ReqList.Remove(rq);
                            }));
                        } while (GlobalObjects.ReqList.Contains(rq));

                        Logger.LogStr($"CORE: Removed {rq.Artist} - {rq.Title} requested by {rq.Requester} from the queue.");

                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            foreach (Window window in Application.Current.Windows)
                                            {
                                                if (window.GetType() != typeof(WindowQueue))
                                                    continue;
                                                //(qw as Window_Queue).dgv_Queue.ItemsSource.
                                                (window as WindowQueue)?.dgv_Queue.Items.Refresh();
                                            }
                                        });
                    }
                }
                if (_songInfo.SongId != null)
                    GlobalObjects.CurrentSong = _songInfo;
                if (GlobalObjects.ReqList.Count > 0 && GlobalObjects.CurrentSong?.SongId == GlobalObjects.ReqList.First().Trackid && _trackChanged)
                {
                    //WebHelper.UpdateWebQueue(songInfo.SongID, "", "", "", "", "1", "u");
                    dynamic payload = new
                    {
                        uuid = Settings.Settings.Uuid,
                        key = Settings.Settings.AccessKey,
                        queueid = GlobalObjects.ReqList.First().Queueid,
                    };
                    WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
                }
                // Check if the current song is alredy in the liked playlist
                if (_trackChanged)
                {
                    _trackChanged = false;
                    if (_songInfo.SongId != null && !string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId))
                        GlobalObjects.IsInPlaylist = await CheckInLikedPlaylist(GlobalObjects.CurrentSong);
                    WebHelper.QueueRequest(WebHelper.RequestMethod.Get);
                }

                UpdateWebServerResponse();
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            //Console.WriteLine($"{songInfo.Progress} / {songInfo.DurationTotal} ({songInfo.DurationPercentage}%)");
            // if no song is playing and custompausetext is enabled
            return _songInfo ?? new TrackInfo { IsPlaying = false };
            // return a new stringarray containing artist, title and so on
        }

        private static void UpdateWebServerResponse()
        {
            if (_songInfo == null)
            {
                _songInfo = GlobalObjects.CurrentSong ?? new TrackInfo();
            }
            string j = Json.Serialize(_songInfo);
            dynamic obj = JsonConvert.DeserializeObject<dynamic>(j);
            IDictionary<string, object> dictionary = obj.ToObject<IDictionary<string, object>>();
            dictionary["IsInLikedPlaylist"] = GlobalObjects.IsInPlaylist;
            dictionary["Requester"] = GlobalObjects.Requester;
            dictionary["GoalTotal"] = Settings.Settings.RewardGoalAmount;
            dictionary["GoalCount"] = GlobalObjects.RewardGoalCount;
            dictionary["QueueCount"] = GlobalObjects.ReqList.Count;
            dictionary["Queue"] = GlobalObjects.ReqList;
            string updatedJson = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
            GlobalObjects.ApiResponse = updatedJson;
        }

        private static async Task<bool> CheckInLikedPlaylist(TrackInfo trackInfo)
        {
            Debug.WriteLine("Check Playlist");
            if (trackInfo.SongId == null)
                return false;
            string id = trackInfo.SongId;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId))
            {
                return false;
            }

            bool firstFetch = true;
            Paging<PlaylistTrack> tracks = null;
            do
            {
                tracks = firstFetch
                    ? await ApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId)
                    : await ApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100,
                        tracks.Offset + tracks.Limit);
                if (tracks.Items.Any(t => t.Track.Id == id))
                {
                    return true;
                }
                firstFetch = false;
            } while (tracks.HasNextPage());
            return false;
        }
    }
}