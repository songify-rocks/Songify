using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Models.Pear;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Models.WebSocket;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Pear;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Youtube.Pear;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using System.Xml.Linq;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Songify_Slim.Models.Pear.WebSocket;
using Image = SpotifyAPI.Web.Image;
using JsonDocument = System.Text.Json.JsonDocument;

namespace Songify_Slim.Util.Songify
{
    /// <summary>
    ///     This class is for retrieving data of currently playing songs
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SongFetcher
    {
        private YoutubeData currentYoutubeData = new();
        private int fetchCounter = 0;

        private static readonly List<string> AudioFileTypes =
        [
            ".3gp", ".aa", ".aac", ".aax", ".act", ".aiff", ".alac", ".amr", ".ape", ".au", ".awb", ".dss", ".dvf",
            ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".mmf", ".mp3", ".mpc", ".msv", ".nmf", ".ogg",
            ".oga", ".mogg", ".opus", ".ra", ".rm", ".raw", ".rf64", ".sln", ".tta", ".voc", ".vox", ".wav", ".wma",
            ".wv", ".webm", ".8svx", ".cda"
        ];

        private static bool _trackChanged;
        private string _localTrackTitle;
        private static bool _isLocalTrack;
        private static Tuple<bool, string> _canvasResponse;
        private static readonly Regex DriveLetterRegex = new(@"^[A-Z]:", RegexOptions.IgnoreCase);
        private PlaylistInfo playbackPlaylist = null;

        private PearSong currentSong = null;
        private TrackInfo tI = null;

        /// <summary>
        ///     A method to fetch the song that's currently playing on Spotify.
        ///     returns null if unsuccessful and custom pause text is not set.
        /// </summary>
        /// <returns>Returns String-Array with Artist, Title, Extra</returns>
        public async Task FetchDesktopPlayer(string player)
        {
            TrackInfo trackinfo = null;
            Process[] processes = Process.GetProcessesByName(player);
            foreach (Process process in processes)
                if (process.ProcessName == player && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    // If the process name is "Spotify" and the window title is not empty
                    string wintitle = process.MainWindowTitle;
                    string artist, title;

                    switch (player)
                    {
                        case "Spotify":
                            // Checks if the title is Spotify Premium or Spotify Free in which case we don't want to fetch anything
                            if (wintitle != "Spotify" && wintitle != "Spotify Premium" && wintitle != "Spotify Free" &&
                                wintitle != "Drag")
                            {
                                // Splitting the win title which is always Artist - Title
                                string[] windowTitleSplits = wintitle.Split([" - "], StringSplitOptions.None);
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
                                    Logger.Error(LogSource.Core, "Error grabbing Spotify window information", ex);
                                }
                            }
                            else if (wintitle is "Spotify" or "Spotify Premium")
                            {
                                // we assume that the song is paused
                                await ExecutePauseActions();
                                return;
                            }
                            break;

                        case "vlc":
                            //Splitting the win title which is always Artist - Title
                            if (string.IsNullOrEmpty(wintitle) || wintitle == "vlc")
                                return;

                            if (!wintitle.Contains(" - VLC media player"))
                            {
                                trackinfo = Settings.CustomPauseTextEnabled
                                    ? new TrackInfo { Artists = Settings.CustomPauseText, Title = "" }
                                    : new TrackInfo { Artists = "", Title = "" };
                                break;
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
                                Logger.Error(LogSource.Core, "Error grabbing VLC window information", ex);
                            }
                            finally
                            {
                                artist = wintitle;
                                title = "";
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
                            if (wintitle.StartsWith("foobar2000") && Settings.CustomPauseTextEnabled)
                            {
                                trackinfo = new TrackInfo { Artists = Settings.CustomPauseText, Title = "" };
                                break; // Exit early, do NOT continue with the parsing
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
                                Logger.Error(LogSource.Core, "Error grabbing foobar2000 window information", ex);
                            }

                            int dashIndex = wintitle.IndexOf(" - ", StringComparison.Ordinal);
                            if (dashIndex != -1)
                            {
                                artist = wintitle.Substring(0, dashIndex).Trim();
                                dashIndex += 3;
                                title = wintitle.Substring(dashIndex, wintitle.Length - dashIndex).Trim();
                            }
                            else
                            {
                                artist = wintitle;
                                title = "";
                            }

                            trackinfo = new TrackInfo { Artists = artist, Title = title };
                            break;
                    }
                }

            if (trackinfo == null || trackinfo == GlobalObjects.CurrentSong)
            {
                return;
            }

            GlobalObjects.CurrentSong = trackinfo;
            await UpdateWebServerResponse(trackinfo);

            string output = Settings.OutputString;

            int start = output.IndexOf("{{", StringComparison.Ordinal);
            int end = output.LastIndexOf("}}", StringComparison.Ordinal) + 2;
            if (start >= 0) output = output.Remove(start, end - start);

            output = output.Format(
                artist => trackinfo.Artists ?? "",
                single_artist => trackinfo.FullArtists == null ? "" : trackinfo.FullArtists.FirstOrDefault().Name,
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

            //IoManager.WriteOutput($"{GlobalObjects.RootDirectory}/songify.txt", output.Trim());
            IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "songify.txt"), output.Trim());

            if (Settings.SplitOutput)
            {
                IoManager.WriteSplitOutput(trackinfo.Artists, trackinfo.Title, "");
            }
            try
            {
                await SongifyService.UploadSong(output.Trim().Replace(@"\n", " - ").Replace("  ", " "));
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Api, "Error uploading song information", ex);
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
        }

        private static async Task ExecutePauseActions()
        {
            switch (Settings.PauseOption)
            {
                case Enums.PauseOptions.Nothing:
                    return;

                case Enums.PauseOptions.PauseText:
                    // read the text file
                    if (!File.Exists(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"))) File.Create(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")).Close();
                    IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), Settings.CustomPauseText);
                    if (Settings.DownloadCover &&
                        (Settings.PauseOption == Enums.PauseOptions.PauseText))
                        await IoManager.DownloadCover(null, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
                    if (Settings.SplitOutput) IoManager.WriteSplitOutput(Settings.CustomPauseText, "", "");
                    await SongifyService.UploadSong(Settings.CustomPauseText);
                    GlobalObjects.CurrentSong = new TrackInfo();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow main = Application.Current.MainWindow as MainWindow;
                        main?.TxtblockLiveoutput.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            main.SetTextPreview(Settings.CustomPauseText);
                        }));
                    });
                    break;

                case Enums.PauseOptions.ClearAll:
                    if (Settings.DownloadCover && (Settings.PauseOption == Enums.PauseOptions.ClearAll)) await IoManager.DownloadCover(null, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
                    IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), "");
                    if (Settings.SplitOutput) IoManager.WriteSplitOutput("", "", "");
                    await SongifyService.UploadSong("");
                    GlobalObjects.CurrentSong = new TrackInfo();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow main = Application.Current.MainWindow as MainWindow;
                        main?.TxtblockLiveoutput.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            main.SetTextPreview("");
                        }));
                    });
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task FetchYoutubeData()
        {
            YoutubeData ytData = GlobalObjects.YoutubeData;
            if (ytData == null)
                return;

            if (ytData.Hash == currentYoutubeData.Hash)
                return;

            Logger.Info(LogSource.Core, $"Previous Song {currentYoutubeData.Artist} - {currentYoutubeData.Title}");

            Logger.Info(LogSource.Core, $"Now Playing {ytData.Artist} - {ytData.Title}");

            currentYoutubeData = ytData;

            TrackInfo songInfo = new()
            {
                Artists = ytData.Artist,
                Title = ytData.Title,
                Albums = !string.IsNullOrEmpty(ytData.Cover)
                    ?
                    [
                        new Image
                        {
                            Url = ytData.Cover,
                            Width = 0,
                            Height = 0
                        }
                    ]
                    : null,
                SongId = ytData.VideoId,
                DurationMs = 0,
                IsPlaying = true,
                Url = "",
                DurationPercentage = 0,
                DurationTotal = 0,
                Progress = 0,
                Playlist = null,
                FullArtists = [],
            };

            await WriteSongInfo(songInfo);
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
            if (SpotifyApiHandler.Client == null)
            {
                if (!string.IsNullOrEmpty(Settings.SpotifyAccessToken) &&
                    !string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                {
                    await SpotifyApiHandler.Auth();
                }
                return;
            }

            // gets the current playing song info
            TrackInfo songInfo = await SpotifyApiHandler.GetSongInfo();

            if (songInfo == null)
            {
                return;
            }
            songInfo.Playlist = playbackPlaylist;
            if (GlobalObjects.CurrentSong != null && songInfo.IsPlaying != GlobalObjects.CurrentSong.IsPlaying)
            {
                GlobalObjects.ForceUpdate = true;
            }

            try
            {
                if (GlobalObjects.CurrentSong == null || (GlobalObjects.CurrentSong.SongId != songInfo.SongId && songInfo.SongId != null) || (songInfo.SongId == null && !string.IsNullOrEmpty(songInfo.Title)))
                {
                    //for local files: store track title, match it and check if id == null
                    if (songInfo.SongId == null && _localTrackTitle == songInfo.Title)
                    {
                        return;
                    }

                    _isLocalTrack = songInfo.SongId == null;
                    _localTrackTitle = songInfo.Title;
                    _trackChanged = true;

                    if (!string.IsNullOrWhiteSpace(songInfo.Playlist?.Id))
                    {
                        songInfo.Playlist.Url = $"https://open.spotify.com/playlist/{songInfo.Playlist.Id}";
                    }

                    if (GlobalObjects.CurrentSong != null)
                        Logger.Info(LogSource.Core, $"Previous Song {GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    if (songInfo.SongId != null)
                        Logger.Info(LogSource.Core, $"Now Playing {songInfo.Artists} - {songInfo.Title}");

                    RequestObject previous = GlobalObjects.CurrentSong != null ? GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == GlobalObjects.CurrentSong.SongId) : null;
                    RequestObject current = GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == songInfo.SongId);

                    // Get Playlist info for current song if available
                    playbackPlaylist = await SpotifyApiHandler.GetPlaybackPlaylist();

                    GlobalObjects.CurrentSong = songInfo;
                    _canvasResponse = await CanvasService.GetCanvasAsync(songInfo.SongId);
                    GlobalObjects.Canvas = songInfo.SongId != null ? _canvasResponse : new Tuple<bool, string>(false, "");
                    //if current track is on skiplist, skip it
                    if (GlobalObjects.SkipList.Find(o => o.Trackid == songInfo.SongId) != null)
                    {
                        await Application.Current.Dispatcher.Invoke(async () =>
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
                            uuid = Settings.Uuid,
                            key = Settings.AccessKey,
                            queueid = current.Queueid,
                        };
                        await SongifyApi.PatchQueueAsync(Json.Serialize(payload));
                    }

                    //if Previous is not null then try to remove it from the internal queue (ReqList)
                    if (previous != null)
                    {
                        Logger.Info(LogSource.Core, $"Trying to remove {previous.Artist} - {previous.Title} from queue.");
                        do
                        {
                            await Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                GlobalObjects.ReqList.Remove(previous);
                            });
                            Thread.Sleep(250);
                        } while (GlobalObjects.ReqList.Contains(previous));

                        Logger.Info(LogSource.Core, $"Removed {previous.Artist} - {previous.Title} requested by {previous.Requester} from the queue.");

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
                    if (songInfo.SongId != null && !string.IsNullOrEmpty(Settings.SpotifyPlaylistId))
                    {
                        //GlobalObjects.IsInPlaylist = await CheckInLikedPlaylist(GlobalObjects.CurrentSong);
                        await QueueService.CleanupServerQueueAsync();
                    }

                    await GlobalObjects.QueueUpdateQueueWindow();

                    // Insert the Logic from mainwindow's WriteSong method here since it's easier to handel the song info here
                    await WriteSongInfo(songInfo, Enums.RequestPlayerType.Spotify);
                    await GlobalObjects.CheckInLikedPlaylist(songInfo);
                }

                await UpdateWebServerResponse(songInfo);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        public async Task FetchWindowsApi()
        {
            // Ensure all SMTC calls happen on UI STA
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await FetchWindowsApi());
                return;
            }

            await FetchWindowsApiCoreAsync(retried: false);
        }

        public static async Task<string> ThumbnailToDataUrlAsync(IRandomAccessStreamReference thumbRef)
        {
            if (thumbRef == null) return null;

            using IRandomAccessStreamWithContentType stream = await thumbRef.OpenReadAsync();
            byte[] bytes = new byte[stream.Size];
            using (DataReader reader = new(stream))
            {
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
            }

            // assume png/jpg bytes as provided by the session; png is common
            string base64 = Convert.ToBase64String(bytes);
            // If you want to be fancy, sniff first few bytes to choose image/png vs image/jpeg.
            return $"data:image/png;base64,{base64}";
        }

        private async Task FetchWindowsApiCoreAsync(bool retried)
        {
            try
            {
                GlobalSystemMediaTransportControlsSessionManager mgr = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                GlobalSystemMediaTransportControlsSession session = mgr.GetCurrentSession();
                if (session == null) { Console.WriteLine("No active media session."); return; }

                GlobalSystemMediaTransportControlsSessionMediaProperties props = await session.TryGetMediaPropertiesAsync();
                string title = props?.Title ?? "";
                string artistFlat = props?.Artist ?? "";
                string albumTitle = props?.AlbumTitle ?? "";
                string albumArtist = props?.AlbumArtist ?? "";
                int trackNo = props?.TrackNumber ?? 0;
                string[] genres = props?.Genres?.ToArray() ?? [];

                string thumbPath = await SaveThumbnailToTempAsync(props?.Thumbnail);

                GlobalSystemMediaTransportControlsSessionPlaybackInfo playback = session.GetPlaybackInfo();
                GlobalSystemMediaTransportControlsSessionTimelineProperties timeline = session.GetTimelineProperties();
                bool isPlaying =
                    playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                TimeSpan start = timeline.StartTime;
                TimeSpan end = timeline.EndTime;
                TimeSpan position = timeline.Position;

                int totalMs = ClampToInt((end - start).TotalMilliseconds);
                int progress = ClampToInt((position - start).TotalMilliseconds);
                if (totalMs < 0) totalMs = 0;
                if (progress < 0) progress = 0;
                if (progress > totalMs && totalMs > 0) progress = totalMs;

                int percent = (totalMs > 0) ? (int)Math.Round((double)progress * 100.0 / totalMs) : 0;
                percent = Math.Max(0, Math.Min(100, percent));

                List<SimpleArtist> fullArtists = SplitArtists(artistFlat);

                TrackInfo tr = new()
                {
                    Artists = artistFlat,
                    Title = title,
                    Albums = [new Image { Url = await ThumbnailToDataUrlAsync(props?.Thumbnail) }],
                    SongId = GenerateId(artistFlat, title),
                    DurationMs = progress,
                    IsPlaying = isPlaying,
                    Url = null,
                    DurationPercentage = percent,
                    DurationTotal = totalMs,
                    Progress = progress,
                    Playlist = null,
                    FullArtists = fullArtists
                };

                await UpdateWebServerResponse(tr);
                tr.Albums = [new Image() { Url = thumbPath }];
                if (GlobalObjects.CurrentSong == null ||
                    (GlobalObjects.CurrentSong.SongId != tr.SongId && tr.SongId != null) ||
                    (tr.SongId == null && !string.IsNullOrEmpty(tr.Title)))
                {
                    GlobalObjects.CurrentSong = tr;
                    await WriteSongInfo(tr);
                }
            }
            catch (COMException ex) when ((uint)ex.HResult == 0x80010108)
            {
                if (!retried)
                {
                    await FetchWindowsApiCoreAsync(retried: true);
                }
                else
                {
                    Logger.LogExc(ex);
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static string GenerateId(string artist, string title)
        {
            if (string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(title))
                return null;

            string combined = $"{artist?.Trim().ToLowerInvariant()}|{title?.Trim().ToLowerInvariant()}";

            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            // shorten to something readable (e.g., first 16 hex chars)
            return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
        }

        private static int ClampToInt(double ms)
        {
            return ms switch
            {
                <= int.MinValue => int.MinValue,
                >= int.MaxValue => int.MaxValue,
                _ => (int)Math.Round(ms)
            };
        }

        /// <summary>
        /// Save WinRT thumbnail stream to a temp PNG/JPEG file and return its absolute path (or null).
        /// Must be called on the same STA thread where the WinRT object was obtained.
        /// </summary>
        private static async Task<string> SaveThumbnailToTempAsync(IRandomAccessStreamReference thumbRef)
        {
            if (thumbRef == null) return null;

            string dir = Path.Combine(Path.GetTempPath(), "SongifySlim", "covers");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"cover_{Guid.NewGuid():N}.png");

            using IRandomAccessStreamWithContentType stream = await thumbRef.OpenReadAsync();
            byte[] bytes = new byte[stream.Size];
            using (DataReader reader = new(stream))
            {
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
            }
            File.WriteAllBytes(path, bytes);
            return path;
        }

        /// <summary>
        /// Heuristic splitter for artist strings like "Artist1, Artist2 & Artist3 feat. Guest"
        /// Produces List&lt;SimpleArtist&gt; with Name set; extend as needed.
        /// </summary>
        private static List<SimpleArtist> SplitArtists(string artists)
        {
            if (string.IsNullOrWhiteSpace(artists)) return [];

            string norm = Regex.Replace(artists, "feat\\.", "ft.", RegexOptions.IgnoreCase);

            List<string> parts = norm
                .Split([",", "&", " x ", " ft. ", " feat. "], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return parts.Select(p => new SimpleArtist { Name = p }).ToList();
        }

        private static async Task WriteSongInfo(TrackInfo songInfo, Enums.RequestPlayerType playerType = Enums.RequestPlayerType.Other)
        {
            if (!songInfo.IsPlaying)
            {
                switch (Settings.PauseOption)
                {
                    case Enums.PauseOptions.Nothing:
                        return;

                    case Enums.PauseOptions.PauseText:
                        // read the text file
                        if (!File.Exists(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"))) File.Create(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")).Close();
                        IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), Settings.CustomPauseText);
                        if (!Settings.KeepAlbumCover)
                        {
                            if (Settings.DownloadCover && (Settings.PauseOption == Enums.PauseOptions.PauseText)) await IoManager.DownloadCover(null, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
                            if (Settings.DownloadCanvas && Settings.PauseOption == Enums.PauseOptions.PauseText)
                            {
                                IoManager.DownloadCanvas(null, Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4"));
                                GlobalObjects.Canvas = null;
                            }
                        }
                        IoManager.WriteSplitOutput(Settings.CustomPauseText, "", "");

                        await SongifyService.UploadSong(Settings.CustomPauseText);

                        break;

                    case Enums.PauseOptions.ClearAll:
                        if (!Settings.KeepAlbumCover)
                        {
                            if (Settings.DownloadCover && Settings.PauseOption == Enums.PauseOptions.ClearAll) await IoManager.DownloadCover(null, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
                            if (Settings.DownloadCanvas && Settings.PauseOption == Enums.PauseOptions.ClearAll)
                            {
                                IoManager.DownloadCanvas(null, Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4"));
                                GlobalObjects.Canvas = null;
                            }
                        }
                        IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), "");
                        IoManager.WriteSplitOutput("", "", "");
                        await SongifyService.UploadSong("");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = Application.Current.MainWindow as MainWindow;
                    main?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        switch (Settings.PauseOption)
                        {
                            case Enums.PauseOptions.PauseText:
                                main.SetTextPreview(Settings.CustomPauseText);
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

                return;
            }

            if (string.IsNullOrEmpty(songInfo.Artists) && string.IsNullOrEmpty(songInfo.Title))
            {
                // We don't have any song info, so we can't write anything
                return;
            }

            string albumUrl = songInfo.Albums != null && songInfo.Albums.Count != 0 ? songInfo.Albums[0].Url : "";
            string currentSongOutput = Settings.OutputString;
            // this only is used for Spotify because here the artist and title are split
            // replace parameters with actual info
            string s_singleArtist = songInfo.FullArtists?.FirstOrDefault() != null
                ? songInfo.FullArtists.FirstOrDefault()?.Name
                : songInfo.Artists ?? "";

            string s_artist = songInfo.Artists ?? "";
            string s_title = songInfo.Title ?? "";
            string s_uri = songInfo.SongId ?? "";
            string s_url = songInfo.Url ?? "";

            currentSongOutput = currentSongOutput.Format(
                single_artist => s_singleArtist,
                artist => s_artist,
                title => s_title,
                extra => "",
                uri => s_uri,
                url => s_url
            ).Format();

            if (_isLocalTrack && string.IsNullOrEmpty(songInfo.Artists))
            {
                //find where the title starts and remove everything before it
                int index = currentSongOutput.IndexOf(songInfo.Title, StringComparison.Ordinal);
                if (index > 0)
                {
                    currentSongOutput = currentSongOutput.Substring(index);
                }
            }

            RequestObject rq = null;

            if (GlobalObjects.ReqList.Count > 0)
            {
                rq = GlobalObjects.ReqList.FirstOrDefault(x => x.Trackid == songInfo.SongId);
                if (rq != null)
                {
                    currentSongOutput = currentSongOutput.Replace("{{", "");
                    currentSongOutput = currentSongOutput.Replace("}}", "");
                    currentSongOutput = currentSongOutput.Replace("{req}", rq.Requester);
                    GlobalObjects.Requester = rq.Requester;
                    GlobalObjects.FullRequester = rq.FullRequester;
                }
                else
                {
                    int start = currentSongOutput.IndexOf("{{", StringComparison.Ordinal);
                    int end = currentSongOutput.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutput = currentSongOutput.Remove(start, end - start);
                    GlobalObjects.Requester = "";
                    GlobalObjects.FullRequester = null;
                }
            }
            else
            {
                try
                {
                    int start = currentSongOutput.IndexOf("{{", StringComparison.Ordinal);
                    int end = currentSongOutput.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) currentSongOutput = currentSongOutput.Remove(start, end - start);
                    GlobalObjects.FullRequester = null;
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
            if (!File.Exists(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")))
            {
                try
                {
                    File.Create(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")).Close();
                }
                catch (Exception e)
                {
                    Logger.LogExc(e);
                    return;
                }
            }

            //if (new FileInfo(_Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")).Length == 0) File.WriteAllText(_Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), currentSongOutput);
            string temp = File.ReadAllText(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"));

            // if the text file is different to _currentSongOutput (fetched song) or update is forced
            if (temp.Trim() != currentSongOutput.Trim())
                // Clear the SkipVotes list in TwitchHandler Class
                TwitchHandler.ResetVotes();

            // write song to the text file
            try
            {
                IoManager.WriteOutput(Path.Combine(GlobalObjects.RootDirectory, "Songify.txt"), currentSongOutput);
            }
            catch (Exception)
            {
                Logger.Error(LogSource.Core, $"File {Path.Combine(GlobalObjects.RootDirectory, "Songify.txt")} couldn't be accessed.");
            }

            IoManager.WriteSplitOutput(
                Settings.OutputString.Contains("{single_artist}")
                    ? songInfo.FullArtists?.FirstOrDefault()?.Name ?? ""
                    : songInfo.Artists ?? "",
                songInfo.Title ?? "",
                "",
                rq?.Requester ?? ""
            );

            await IoManager.DownloadImage(rq?.FullRequester?.ProfileImageUrl,
                GlobalObjects.RootDirectory + "/requester.png");

            IoManager.WriteOutput($"{GlobalObjects.RootDirectory}/url.txt", songInfo.Url);

            // if upload is enabled
            try
            {
                await SongifyService.UploadSong(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "), albumUrl, playerType, songInfo.Artists, songInfo.Title, GlobalObjects.Requester);
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
            string historySongOutput = $"{songInfo.Artists} - {songInfo.Title}";
            if (Settings.SaveHistory && !string.IsNullOrEmpty(historySongOutput) &&
                historySongOutput.Trim() != Settings.CustomPauseText)
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

                XElement elem = new("Song", historySongOutput);
                elem.Add(new XAttribute("Time", unixTimestamp));
                XElement x = doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).FirstOrDefault();
                XNode lastNode = x?.LastNode;
                if (lastNode != null)
                {
                    if (historySongOutput != ((XElement)lastNode).Value)
                        x.Add(elem);
                }
                else
                {
                    x?.Add(elem);
                }
                doc.Save(historyPath);
            }

            //Upload History
            if (Settings.UploadHistory && !string.IsNullOrEmpty(currentSongOutput.Trim()) &&
                currentSongOutput.Trim() != Settings.CustomPauseText)
            {
                int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                // Upload Song
                try
                {
                    await SongifyService.UploadHistory(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "), unixTimestamp);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    // Writing to the statusstrip label
                    Application.Current.MainWindow?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        (((MainWindow)Application.Current.MainWindow)!).LblStatus.Content = "Error uploading history";
                    }));
                }
            }

            // Update Song Queue, Track has been played. All parameters are optional except track id, playedd and o. o has to be the value "u"
            //if (rTrackId != null) WebHelper.UpdateWebQueue(rTrackId, "", "", "", "", "1", "u");

            // Send Message to Twitch if checked
            if (Settings.AnnounceInChat)
            {
                TwitchHandler.SendCurrSong();
            }

            //Save Album Cover
            // Check if there is a canvas available for the song id using https://api.songify.rocks/v2/canvas/{ID}, if there is us that instead
            if (Settings.DownloadCanvas && _canvasResponse is { Item1: true })
            {
                IoManager.DownloadCanvas(_canvasResponse.Item2, Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4"));
                await IoManager.DownloadCover(albumUrl, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
            }
            else if (Settings.DownloadCover)
            {
                await IoManager.DownloadCover(albumUrl, Path.Combine(GlobalObjects.RootDirectory, "cover.png"));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                main?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (currentSongOutput.Trim().StartsWith("-"))
                    {
                        currentSongOutput = currentSongOutput.Remove(0, 1).Trim();
                    }

                    main.SetTextPreview(currentSongOutput.Trim().Replace(@"\n", " - ").Replace("  ", " "));
                }));
            });
        }

        private static string CleanFormatString(string currentSongOutput)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new("[ ]{2,}", options);
            currentSongOutput = regex.Replace(currentSongOutput, " ");
            currentSongOutput = currentSongOutput.Trim();

            return currentSongOutput;
        }

        private static async Task UpdateWebServerResponse(TrackInfo track)
        {
            // Normalize album image URLs
            foreach (Image album in track.Albums)
            {
                if (string.IsNullOrEmpty(album.Url) || !DriveLetterRegex.IsMatch(album.Url)) continue;
                string normalized = album.Url.Replace("\\", "/");
                album.Url = "file:///" + normalized;
            }

            var userInfo = new
            {
                TwitchUser = new
                {
                    Settings.TwitchUser.Id,
                    Settings.TwitchUser.Login,
                    Settings.TwitchUser.BroadcasterType
                },
                SpotifyUser = new
                {
                    Settings.SpotifyProfile.Id,
                    Settings.SpotifyProfile.DisplayName,
                    Settings.SpotifyProfile.Product
                }
            };

            var songifyInfo = new
            {
                version = GlobalObjects.AppVersion,
                beta = App.IsBeta
            };

            Dictionary<string, object> trackDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                JsonConvert.SerializeObject(track)
            );

            // Serialize track -> base dictionary
            string j = JsonConvert.SerializeObject(track); // you can drop Json.Serialize and just use Newtonsoft
            Dictionary<string, object> dictionary = new()
            {
                // Add your "first dynamics"
                ["UserInfo"] = userInfo,
                ["SongifyInfo"] = songifyInfo
            };

            if (trackDict != null)
            {
                foreach (KeyValuePair<string, object> kv in trackDict)
                    dictionary[kv.Key] = kv.Value;
            }

            // Add the rest
            if (GlobalObjects.Canvas != null)
                dictionary["CanvasUrl"] = GlobalObjects.Canvas.Item1 ? GlobalObjects.Canvas.Item2 : "";
            else
                dictionary["CanvasUrl"] = "";

            dictionary["IsInLikedPlaylist"] = GlobalObjects.IsInPlaylist;
            dictionary["Requester"] = GlobalObjects.Requester;
            dictionary["RequesterProfilePic"] = GlobalObjects.FullRequester == null
                ? ""
                : GlobalObjects.FullRequester.ProfileImageUrl;
            dictionary["GoalTotal"] = Settings.RewardGoalAmount;
            dictionary["GoalCount"] = GlobalObjects.RewardGoalCount;
            dictionary["QueueCount"] = GlobalObjects.ReqList.Count;
            dictionary["RequestQueue"] = GlobalObjects.ReqList;
            dictionary["Queue"] = GlobalObjects.QueueTracks;

            string updatedJson = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
            GlobalObjects.ApiResponse = updatedJson;

            await GlobalObjects.WebServer.BroadcastToChannelAsync("/ws/data", updatedJson);
        }

        public async Task FetchPear()
        {
            fetchCounter += 1;

            PearResponse data = await PearApi.GetNowPlayingAsync();
            if (data == null) return;

            // 1) keep the previous id
            string prevId = GlobalObjects.CurrentSong?.SongId;

            TrackInfo t = new()
            {
                Artists = data.Artist,
                Title = data.Title,
                Albums =
                [
                    new Image { Url = data.ImageSrc, Width = 0, Height = 0 }
                ],
                SongId = data.VideoId,
                DurationMs = data.SongDuration * 1000,
                IsPlaying = !data.IsPaused,
                Url = data.Url,
                DurationPercentage = (int)(data.SongDuration == 0 ? 0 :
                    (double)data.ElapsedSeconds / data.SongDuration * 100),
                DurationTotal = data.SongDuration * 1000,
                Progress = data.ElapsedSeconds * 1000,
                Playlist = new PlaylistInfo
                {
                    Name = null,
                    Id = null,
                    Owner = null,
                    Url = data.PlaylistId,
                    Image = null
                },
                FullArtists =
                [
                    new SimpleArtist
                    {
                        ExternalUrls = new Dictionary<string, string>(),
                        Href = string.Empty,
                        Id = string.Empty,
                        Name = data.Artist,
                        Type = string.Empty,
                        Uri = string.Empty
                    }
                ]
            };

            await UpdateWebServerResponse(t);

            if (fetchCounter >= 5)
            {
                await TwitchHandler.EnsureOrderAsync();
                fetchCounter = 0;
            }

            // 2) If same song, nothing to do
            if (GlobalObjects.CurrentSong != null && GlobalObjects.CurrentSong.SongId == data.VideoId)
                return;

            // 3) Song changed -> previous finished: mark & remove
            string prevKey = GlobalObjects.CurrentSong == null
                ? null
                : NormalizeKey(GlobalObjects.CurrentSong.Artists, GlobalObjects.CurrentSong.Title);

            if (!string.IsNullOrEmpty(prevKey))
                MarkPlayedAndRemoveByKey(prevKey);

            // 4) Update current & UI
            GlobalObjects.CurrentSong = t;
            await WriteSongInfo(t, Enums.RequestPlayerType.Youtube);
            await GlobalObjects.QueueUpdateQueueWindow();
        }

        private static readonly object _sync = new();

        private static void MarkPlayedAndRemoveByKey(string finishedKey)
        {
            lock (_sync)
            {
                // mark first occurrence
                RequestObject first = GlobalObjects.ReqList.FirstOrDefault(r =>
                    r.Played == 0 &&
                    NormalizeKey(r.Artist, r.Title) == finishedKey);

                if (first != null) first.Played = 1;

                // remove all matching entries
                for (int i = GlobalObjects.ReqList.Count - 1; i >= 0; i--)
                {
                    RequestObject r = GlobalObjects.ReqList[i];
                    if (NormalizeKey(r.Artist, r.Title) == finishedKey)
                        GlobalObjects.ReqList.RemoveAt(i);
                }
            }
        }

        private static void MarkPlayedAndRemove(string finishedId)
        {
            lock (_sync)
            {
                // mark first occurrence as played (optional if you keep history somewhere)
                RequestObject first = GlobalObjects.ReqList.FirstOrDefault(r => r.Trackid == finishedId && r.Played == 0);
                first?.Played = 1;

                // actually remove all entries for that id
                for (int i = GlobalObjects.ReqList.Count - 1; i >= 0; i--)
                {
                    if (GlobalObjects.ReqList[i].Trackid == finishedId)
                        GlobalObjects.ReqList.RemoveAt(i);
                }
            }
        }

        private static string NormalizeKey(string artist, string title, int? durationSec = null)
        {
            static string norm(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = s.ToLowerInvariant().Trim();

                // remove common noise
                s = Regex.Replace(s, @"\s+", " ");
                s = Regex.Replace(s, @"\((.*?)\)", " ");          // (...) like (Official Video)
                s = Regex.Replace(s, @"\[(.*?)\]", " ");          // [...] like [Lyrics]
                s = Regex.Replace(s, @"\bfeat\.?\b|\bft\.?\b", " ");
                s = Regex.Replace(s, @"[^a-z0-9\s]", " ");        // punctuation
                s = Regex.Replace(s, @"\s+", " ").Trim();
                return s;
            }

            string a = norm(artist);
            string t = norm(title);

            // bucket duration to reduce false positives, but keep tolerant
            string d = durationSec.HasValue && durationSec.Value > 0
                ? (durationSec.Value / 5).ToString()             // 5-second buckets
                : "";

            return $"{a}|{t}|{d}";
        }

        public async Task FetchPearWebsocket()
        {
            PearWebSocketClient.SetMessageHandler(async msg =>
            {
                string type = JsonDocument.Parse(msg)
                               .RootElement
                               .GetProperty("type")
                               .GetString();

                switch (type)
                {
                    case "PLAYER_STATE_CHANGED":
                        {
                            PlayerStateChangedMessage data = System.Text.Json.JsonSerializer.Deserialize<PlayerStateChangedMessage>(msg)!;
                            break;
                        }

                    case "POSITION_CHANGED":
                        {
                            PositionChangedMessage data = System.Text.Json.JsonSerializer.Deserialize<PositionChangedMessage>(msg)!;
                            // Handle event here
                            if (tI != null)
                            {
                                tI.Progress = (int)data.Position;
                                tI.DurationPercentage = (int)((data.Position / tI.DurationTotal) * 100);
                                await UpdateWebServerResponse(tI);
                            }
                            break;
                        }

                    case "VIDEO_CHANGED":
                        {
                            VideoChangedMessage data = System.Text.Json.JsonSerializer.Deserialize<VideoChangedMessage>(msg)!;
                            currentSong = data.Song;
                            tI = new TrackInfo
                            {
                                Artists = currentSong.Artist,
                                Title = currentSong.Title,
                                Albums =
                                [
                                    new Image
                                    {
                                        Height = 720,
                                        Width = 1280,
                                        Url = currentSong.ImageSrc
                                    }
                                ],
                                SongId = currentSong.PlaylistId,
                                DurationMs = (int)TimeSpan.FromSeconds(currentSong.SongDuration).TotalMilliseconds,
                                IsPlaying = !currentSong.IsPaused,
                                Url = currentSong.Url,
                                DurationPercentage = (currentSong.ElapsedSeconds / currentSong.SongDuration) * 100,
                                DurationTotal = currentSong.SongDuration,
                                Progress = currentSong.ElapsedSeconds,
                                Playlist = null,
                                FullArtists = null
                            };
                            await WriteSongInfo(tI, Enums.RequestPlayerType.Youtube);
                            break;
                        }

                    default:
                        break;
                }
            });

            await PearWebSocketClient.ConnectAsync();
        }
    }
}