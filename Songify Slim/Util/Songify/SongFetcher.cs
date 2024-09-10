using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Unosquare.Swan.Formatters;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using TwitchLib.Api.Helix.Models.Soundtrack;
using System.Reflection;
using System.Xml.Linq;
using Utils = MahApps.Metro.Controls.Utils;

namespace Songify_Slim.Util.Songify
{
    /// <summary>
    ///     This class is for retrieving data of currently playing songs
    /// </summary>
    public class SongFetcher
    {
        private static int _id;
        private readonly List<string> _browsers = ["chrome", "msedge", "opera"];
        private static readonly List<string> AudioFileTypes =
        [
            ".3gp", ".aa", ".aac", ".aax", ".act", ".aiff", ".alac", ".amr", ".ape", ".au", ".awb", ".dss", ".dvf",
            ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".mmf", ".mp3", ".mpc", ".msv", ".nmf", ".ogg",
            ".oga", ".mogg", ".opus", ".ra", ".rm", ".raw", ".rf64", ".sln", ".tta", ".voc", ".vox", ".wav", ".wma",
            ".wv", ".webm", ".8svx", ".cda"
        ];
        private AutomationElement _parent;
        private static SongInfo _previousSonginfo;
        private static bool _trackChanged;
        private bool _updating = false;

        /// <summary>
        ///     A method to fetch the song that's currently playing on Spotify.
        ///     returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        public Task FetchDesktopPlayer(string player)
        {
            TrackInfo trackinfo = null;
            Process[] processes = Process.GetProcessesByName(player);
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
                                string[] windowTitleSplits = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                                try
                                {
                                    artist = windowTitleSplits[0].Trim();
                                    title = windowTitleSplits[1].Trim();
                                    trackinfo = new TrackInfo
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
                            }
                            break;
                        case "vlc":
                            //Splitting the win title which is always Artist - Title
                            if (string.IsNullOrEmpty(wintitle) || wintitle == "vlc")
                                return Task.CompletedTask;

                            if (!wintitle.Contains(" - VLC media player"))
                            {
                                trackinfo = Settings.Settings.CustomPauseTextEnabled
                                    ? new TrackInfo { Artists = Settings.Settings.CustomPauseText, Title = "" }
                                    : new TrackInfo { Artists = "", Title = "" };
                            }


                            wintitle = wintitle.Replace(" - VLC media player", "");

                            try
                            {
                                foreach (string item in AudioFileTypes.Where(item => wintitle.Contains(item)))
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

                            if (wintitle.Contains(" - "))
                            {
                                artist = wintitle.Split(Convert.ToChar("-"))[0].Trim();
                                title = wintitle.Split(Convert.ToChar("-"))[1].Trim();
                            }

                            trackinfo = new TrackInfo { Artists = artist, Title = title };

                            break;
                        case "foobar2000":
                            // Splitting the win title which is always Artist - Title
                            if (wintitle.StartsWith("foobar2000"))
                            {
                                if (Settings.Settings.CustomPauseTextEnabled)
                                    trackinfo = new TrackInfo { Artists = Settings.Settings.CustomPauseText, Title = "" };
                            }

                            wintitle = wintitle.Replace(" [foobar2000]", "");
                            try
                            {
                                foreach (string item in AudioFileTypes.Where(item => wintitle.Contains(item)))
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
                            int dashIndex = wintitle.IndexOf(" - ", StringComparison.Ordinal);
                            if (dashIndex != -1)
                            {
                                artist = wintitle.Substring(0, dashIndex).Trim();
                                dashIndex += 3;
                                title = wintitle.Substring(dashIndex, wintitle.Length - dashIndex).Trim();
                            }
                            trackinfo = new TrackInfo { Artists = artist, Title = title };
                            break;
                    }
                }

            if (trackinfo == null || trackinfo == GlobalObjects.CurrentSong)
            {
                return Task.CompletedTask;
            }

            GlobalObjects.CurrentSong = trackinfo;
            UpdateWebServerResponse(trackinfo);

            string output = Settings.Settings.OutputString;

            int start = output.IndexOf("{{", StringComparison.Ordinal);
            int end = output.LastIndexOf("}}", StringComparison.Ordinal) + 2;
            if (start >= 0) output = output.Remove(start, end - start);

            output = output.Format(
                artist => trackinfo.Artists ?? "",
                single_artist => trackinfo.Artists ?? "",
                title => trackinfo.Title ?? "",
                extra => "",
                uri => trackinfo.SongId ?? "",
                url => trackinfo.Url ?? ""
            ).Format();

            output = output.Trim();

            if (output.EndsWith("-"))
            {
                // Remove the trailing "-" if it exists
                output = output.Substring(0, output.Length - 1);
            }

            IOManager.WriteOutput($"{GlobalObjects.RootDirectory}/songify.txt", output.Trim());

            if (Settings.Settings.SplitOutput)
            {
                IOManager.WriteSplitOutput(trackinfo?.Artists, trackinfo?.Title, "");
            }

            if (Settings.Settings.Upload)
                try
                {
                    WebHelper.UploadSong(output.Trim().Replace(@"\n", " - ").Replace("  ", " "));
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    // if error occurs write text to the status asynchronous
                    Application.Current.MainWindow?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        (((MainWindow)Application.Current.MainWindow)!).LblStatus.Content = "Error uploading Song information";
                    }));
                }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                main?.img_cover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    main.SetTextPreview(output);
                }));
            });

            return Task.CompletedTask;
        }

        /// <summary>
        ///     A method to fetch the song that's currently playing on Youtube.
        ///     returns empty string if unsuccessful and custom pause text is not set.
        ///     Currently supported browsers: Google Chrome
        /// </summary>
        /// <param name="website"></param>
        /// <returns>Returns String with Youtube Video Title</returns>
        public Task FetchBrowser(string website)
        {
            string browser = "";
            TrackInfo songInfo = null;
            // chrome, opera, msedge
            foreach (string s in _browsers.Where(s => Process.GetProcessesByName(s).Length > 0))
            {
                browser = s;
                break;
            }

            Process[] procsBrowser = Process.GetProcessesByName(browser);

            foreach (Process procBrowser in procsBrowser)
            {
                // the chrome process must have a window
                if (procBrowser.MainWindowHandle == IntPtr.Zero) continue;

                AutomationElement elm =
                    _parent ?? AutomationElement.FromHandle(procBrowser.MainWindowHandle);

                string formattedString = "";
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
                                        //UpdateWebServerResponse();
                                        // Regex pattern to replace the notification in front of the tab (1) - (99+)
                                        //return FormattedString("YouTube", Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                        formattedString = FormattedString("YouTube", Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                        songInfo = new TrackInfo
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
                                    }

                                    break;

                                case "Deezer":
                                    if (elem.Current.Name.Contains("Deezer"))
                                    {
                                        _id = elem.Current.ControlType.Id;
                                        _parent = TreeWalker.RawViewWalker.GetParent(elem);
                                        //UpdateWebServerResponse();
                                        //return FormattedString("Deezer", elem.Current.Name);
                                        formattedString = FormattedString("Deezer", elem.Current.Name);
                                        songInfo = new TrackInfo
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
                                    formattedString = FormattedString("YouTube", Regex.Replace(element.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", ""));
                                    //try splitting the formatted string to Artist and Title
                                    songInfo = new TrackInfo
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
                                }

                                break;

                            case "Deezer":
                                if (element.Current.Name.Contains("Deezer"))
                                {
                                    _id = element.Current.ControlType.Id;
                                    _parent = TreeWalker.RawViewWalker.GetParent(element);
                                    formattedString = FormattedString("Deezer", element.Current.Name);
                                    //try splitting the formatted string to Artist and Title
                                    songInfo = GlobalObjects.CurrentSong = new TrackInfo
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
                                }
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }


            if (songInfo == null || songInfo == GlobalObjects.CurrentSong)
            {
                return Task.CompletedTask;
            }
            GlobalObjects.CurrentSong = songInfo;
            UpdateWebServerResponse(songInfo);

            string output = Settings.Settings.OutputString;

            int start = output.IndexOf("{{", StringComparison.Ordinal);
            int end = output.LastIndexOf("}}", StringComparison.Ordinal) + 2;
            if (start >= 0) output = output.Remove(start, end - start);


            output = output.Format(
                artist => songInfo.Artists ?? "",
                title => songInfo.Title ?? "",
                extra => "",
                uri => songInfo.SongId ?? "",
                url => songInfo.Url ?? ""
            ).Format();

            output = output.Trim();

            if (output.EndsWith("-"))
            {
                // Remove the trailing "-" if it exists
                output = output.Substring(0, output.Length - 1);
            }

            IOManager.WriteOutput($"{GlobalObjects.RootDirectory}/songify.txt", output);

            if (Settings.Settings.SplitOutput)
            {
                IOManager.WriteSplitOutput(songInfo?.Artists, songInfo?.Title, "");
            }

            return Task.CompletedTask;
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

        public async Task FetchSpotifyWeb()
        {
            // If the spotify object hast been created (successfully authed)
            //if (_updating)
            //    return;
            //_updating = true;
            if (SpotifyApiHandler.Spotify == null)
            {
                if (!string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken) &&
                    !string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken))
                {
                    await SpotifyApiHandler.DoAuthAsync();
                }
                return;
            }

            // gets the current playing song info
            TrackInfo songInfo = SpotifyApiHandler.GetSongInfo();

            if (songInfo == null)
            {
                _updating = false;
                return;
            }

            if (GlobalObjects.CurrentSong != null && songInfo.IsPlaying != GlobalObjects.CurrentSong.IsPlaying)
            {
                GlobalObjects.ForceUpdate = true;
            }

            try
            {
                if (GlobalObjects.CurrentSong == null || (GlobalObjects.CurrentSong.SongId != songInfo.SongId && songInfo.SongId != null))
                {
                    _trackChanged = true;

                    if (GlobalObjects.CurrentSong != null)
                        Logger.LogStr($"CORE: Previous Song {GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    if (songInfo.SongId != null)
                        Logger.LogStr($"CORE: Now Playing {songInfo.Artists} - {songInfo.Title}");

                    RequestObject previous = GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == GlobalObjects.CurrentSong.SongId);
                    RequestObject current = GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == songInfo.SongId);

                    GlobalObjects.CurrentSong = songInfo;

                    //if current track is on skiplist, skip it
                    if (GlobalObjects.SkipList.Find(o => o.Trackid == songInfo.SongId) != null)
                    {
                        await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                        {
                            GlobalObjects.SkipList.Remove(
                                GlobalObjects.SkipList.Find(o => o.Trackid == songInfo.SongId));
                            await SpotifyApiHandler.SkipSong();
                        });
                    }

                    //if current is not null, mark it as played in the database
                    if (current != null)
                    {
                        dynamic payload = new
                        {
                            uuid = Settings.Settings.Uuid,
                            key = Settings.Settings.AccessKey,
                            queueid = current.Queueid,
                        };
                        await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
                    }

                    //if Previous is not null then try to remove it from the internal queue (ReqList)
                    if (previous != null)
                    {
                        Logger.LogStr($"QUEUE: Trying to remove {previous.Artist} - {previous.Title}");
                        do
                        {
                            await Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                GlobalObjects.ReqList.Remove(previous);
                            });
                            Thread.Sleep(250);
                        } while (GlobalObjects.ReqList.Contains(previous));

                        Logger.LogStr($"QUEUE: Removed {previous.Artist} - {previous.Title} requested by {previous.Requester} from the queue.");

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


                if (!songInfo.IsPlaying && GlobalObjects.CurrentSong.IsPlaying != songInfo.IsPlaying)
                {
                    GlobalObjects.ForceUpdate = true;
                }

                GlobalObjects.CurrentSong.IsPlaying = songInfo.IsPlaying;


                if (_trackChanged || GlobalObjects.ForceUpdate)
                {
                    _trackChanged = false;
                    GlobalObjects.ForceUpdate = false;
                    if (songInfo.SongId != null && !string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId))
                    {
                        //GlobalObjects.IsInPlaylist = await CheckInLikedPlaylist(GlobalObjects.CurrentSong);
                        await WebHelper.QueueRequest(WebHelper.RequestMethod.Get);
                    }

                    GlobalObjects.QueueUpdateQueueWindow();

                    // Insert the Logic from mainwindow's WriteSong method here since it's easier to handel the song info here
                    await WriteSongInfo(songInfo);
                    await CheckInLikedPlaylist(songInfo);

                }
                UpdateWebServerResponse(songInfo);
            }
            catch (Exception e)
            {
                _updating = false;
                Logger.LogExc(e);
            }
            _updating = false;
        }

        private static Task WriteSongInfo(TrackInfo songInfo)
        {
            string songPath = GlobalObjects.RootDirectory + "/Songify.txt";
            string coverPath = GlobalObjects.RootDirectory + "/cover.png";
            string title = songInfo.Title;

            if (!songInfo.IsPlaying)
            {
                switch (Settings.Settings.PauseOption)
                {
                    case Enums.PauseOptions.Nothing:
                        return Task.CompletedTask;
                    case Enums.PauseOptions.PauseText:
                        // read the text file
                        if (!File.Exists(songPath)) File.Create(songPath).Close();
                        IOManager.WriteOutput(songPath, Settings.Settings.CustomPauseText);
                        if (Settings.Settings.DownloadCover && (Settings.Settings.PauseOption == Enums.PauseOptions.PauseText)) IOManager.DownloadCover(null, coverPath);
                        if (Settings.Settings.SplitOutput) IOManager.WriteSplitOutput(Settings.Settings.CustomPauseText, "", "");

                        if (Settings.Settings.Upload)
                            WebHelper.UploadSong(Settings.Settings.CustomPauseText);

                        break;
                    case Enums.PauseOptions.ClearAll:
                        if (Settings.Settings.DownloadCover && (Settings.Settings.PauseOption == Enums.PauseOptions.ClearAll)) IOManager.DownloadCover(null, coverPath);
                        IOManager.WriteOutput(songPath, "");
                        if (Settings.Settings.SplitOutput) IOManager.WriteSplitOutput("", "", "");
                        if (Settings.Settings.Upload)
                            WebHelper.UploadSong("");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = Application.Current.MainWindow as MainWindow;
                    main?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        switch (Settings.Settings.PauseOption)
                        {
                            case Enums.PauseOptions.PauseText:
                                main.SetTextPreview(Settings.Settings.CustomPauseText);
                                break;
                            case Enums.PauseOptions.ClearAll:
                                main.SetTextPreview("");
                                break;
                            case Enums.PauseOptions.Nothing:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }));
                });

                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(songInfo.Artists) && string.IsNullOrEmpty(songInfo.Title))
            {
                // We don't have any song info, so we can't write anything
                return Task.CompletedTask;
            }

            string albumUrl = songInfo.Albums != null && songInfo.Albums.Count != 0 ? songInfo.Albums[0].Url : "";
            string currentSongOutput = Settings.Settings.OutputString;
            string currentSongOutputTwitch = Settings.Settings.BotRespSong;
            // this only is used for Spotify because here the artist and title are split
            // replace parameters with actual info
            currentSongOutput = currentSongOutput.Format(
                single_artist => songInfo.FullArtists.FirstOrDefault().Name,
                artist => songInfo.Artists,
                title => songInfo.Title,
                extra => "",
                uri => songInfo.SongId,
                url => songInfo.Url
            ).Format();

            currentSongOutputTwitch = currentSongOutputTwitch.Format(
                single_artist => songInfo.FullArtists.FirstOrDefault().Name,
                artist => songInfo.Artists,
                title => songInfo.Title,
                extra => "",
                uri => songInfo.SongId,
                url => songInfo.Url
            ).Format();
            RequestObject rq = null;

            if (GlobalObjects.ReqList.Count > 0)
            {

                rq = GlobalObjects.ReqList.FirstOrDefault(x => x.Trackid == songInfo.SongId);
                if (rq != null)
                {
                    currentSongOutput = currentSongOutput.Replace("{{", "");
                    currentSongOutput = currentSongOutput.Replace("}}", "");
                    currentSongOutput = currentSongOutput.Replace("{req}", rq.Requester);

                    currentSongOutputTwitch = currentSongOutputTwitch.Replace("{{", "");
                    currentSongOutputTwitch = currentSongOutputTwitch.Replace("}}", "");
                    currentSongOutputTwitch = currentSongOutputTwitch.Replace("{req}", rq.Requester);
                    GlobalObjects.Requester = rq.Requester;

                }
                else
                {
                    int start = currentSongOutput.IndexOf("{{", StringComparison.Ordinal);
                    int end = currentSongOutput.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutput = currentSongOutput.Remove(start, end - start);
                    start = currentSongOutputTwitch.IndexOf("{{", StringComparison.Ordinal);
                    end = currentSongOutputTwitch.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutputTwitch = currentSongOutputTwitch.Remove(start, end - start);
                    GlobalObjects.Requester = "";
                }
            }
            else
            {
                try
                {
                    int start = currentSongOutput.IndexOf("{{", StringComparison.Ordinal);
                    int end = currentSongOutput.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutput = currentSongOutput.Remove(start, end - start);
                    start = currentSongOutputTwitch.IndexOf("{{", StringComparison.Ordinal);
                    end = currentSongOutputTwitch.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutputTwitch = currentSongOutputTwitch.Remove(start, end - start);
                    GlobalObjects.Requester = "";
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }

            // Cleanup the string (remove double spaces, trim and add trailing spaces for scroll)
            currentSongOutput = CleanFormatString(currentSongOutput);

            // read the text file
            if (!File.Exists(songPath))
            {
                try
                {
                    File.Create(songPath).Close();
                }
                catch (Exception e)
                {
                    Logger.LogExc(e);
                    return Task.CompletedTask;
                }
            }

            //if (new FileInfo(_songPath).Length == 0) File.WriteAllText(_songPath, currentSongOutput);
            string temp = File.ReadAllText(songPath);

            // if the text file is different to _currentSongOutput (fetched song) or update is forced
            if (temp.Trim() != currentSongOutput.Trim())
                // Clear the SkipVotes list in TwitchHandler Class
                TwitchHandler.ResetVotes();

            // write song to the text file
            try
            {
                IOManager.WriteOutput(songPath, currentSongOutput);
            }
            catch (Exception)
            {
                Logger.LogStr($"File {songPath} couldn't be accessed.");
            }


            if (Settings.Settings.SplitOutput) IOManager.WriteSplitOutput(songInfo.Artists, songInfo.Title, "", rq?.Requester);

            // if upload is enabled
            if (Settings.Settings.Upload)
                try
                {
                    WebHelper.UploadSong(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "), albumUrl);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    // if error occurs write text to the status asynchronous
                    Application.Current.MainWindow?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).LblStatus.Content = "Error uploading Song information";
                    }));
                }

            //Write History
            if (Settings.Settings.SaveHistory && !string.IsNullOrEmpty(currentSongOutput.Trim()) &&
                currentSongOutput.Trim() != Settings.Settings.CustomPauseText)
            {

                int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                //save the history file
                string historyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" +
                                     "history.shr";
                XDocument doc;
                if (!File.Exists(historyPath))
                {
                    doc = new XDocument(new XElement("History",
                        new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy"))));
                    doc.Save(historyPath);
                }

                doc = XDocument.Load(historyPath);
                if (!doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).Any())
                    doc.Descendants("History").FirstOrDefault()
                        ?.Add(new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy")));

                XElement elem = new("Song", currentSongOutput.Trim());
                elem.Add(new XAttribute("Time", unixTimestamp));
                XElement x = doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).FirstOrDefault();
                XNode lastNode = x.LastNode;
                if (lastNode != null)
                {
                    if (currentSongOutput.Trim() != ((XElement)lastNode).Value)
                        x?.Add(elem);
                }
                else
                {
                    x?.Add(elem);
                }
                doc.Save(historyPath);
            }

            //Upload History
            if (Settings.Settings.UploadHistory && !string.IsNullOrEmpty(currentSongOutput.Trim()) &&
                currentSongOutput.Trim() != Settings.Settings.CustomPauseText)
            {

                int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                // Upload Song
                try
                {
                    WebHelper.UploadHistory(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "), unixTimestamp);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    // Writing to the statusstrip label
                    Application.Current.MainWindow?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).LblStatus.Content = "Error uploading history";
                    }));
                }
            }

            // Update Song Queue, Track has been played. All parameters are optional except track id, playedd and o. o has to be the value "u"
            //if (rTrackId != null) WebHelper.UpdateWebQueue(rTrackId, "", "", "", "", "1", "u");

            // Send Message to Twitch if checked
            if (Settings.Settings.AnnounceInChat)
            {
                TwitchHandler.SendCurrSong();
            }

            //Save Album Cover
            if (Settings.Settings.DownloadCover) IOManager.DownloadCover(albumUrl, coverPath);


            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                main?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    main.SetTextPreview(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "));
                }));
            });
            return Task.CompletedTask;
        }

        private static string CleanFormatString(string currentSongOutput)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new("[ ]{2,}", options);
            currentSongOutput = regex.Replace(currentSongOutput, " ");
            currentSongOutput = currentSongOutput.Trim();

            return currentSongOutput;
        }

        private static void UpdateWebServerResponse(TrackInfo track)
        {
            string j = Json.Serialize(track ?? new TrackInfo());
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
                    ? await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId)
                    : await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100,
                        tracks.Offset + tracks.Limit);
                if (tracks.Items.Any(t => t.Track.Id == id))
                {
                    GlobalObjects.IsInPlaylist = true;
                    return true;
                }
                firstFetch = false;
            } while (tracks.HasNextPage());

            GlobalObjects.IsInPlaylist = false;
            return false;
        }
    }
}