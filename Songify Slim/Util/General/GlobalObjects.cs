using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Songify_Slim.Util.Spotify;
using QueueItem = Songify_Slim.Models.Pear.QueueItem;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Subscriptions;
using System.Collections.Concurrent;
using Songify_Slim.Models.Pear;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Models.WebSocket;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Pear;
using Songify_Slim.Util.Songify.Twitch;
using SpotifyAPI.Web;
using Swan.Formatters;
using Song = Songify_Slim.Util.Youtube.YTMYHCH.Song;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public const string ApiUrl = "https://api.songify.rocks/v2";
        public static string BaseUrl = "https://songify.rocks";
        public static string AuthUrl = "https://songify.rocks";
        public const string AltAuthUrl = "https://songify.bloemacher.com";
        public static string ApiResponse;
        public static string AppVersion;
        public static FlowDocument ConsoleDocument = new();
        public static TrackInfo CurrentSong;
        public static bool DetachConsole = false;
        public static bool IsInPlaylist;
        public static ObservableCollection<RequestObject> ReqList = [];
        public static string Requester = "";
        public static int RewardGoalCount = 0;
        public static List<RequestObject> SkipList = [];
        public static ObservableCollection<RequestObject> QueueTracks { get; set; } = [];
        public static List<Chatter> Chatters = [];
        public static List<Subscription> Subscribers = [];
        public static List<Moderator> Moderators = [];
        public static List<ChannelVIPsResponseModel> Vips = [];

        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H")
            ? "HH:mm:ss"
            : "hh:mm:ss tt";

        public static WebServer WebServer = new();
        public static bool TwitchUserTokenExpired = false;
        public static bool TwitchBotTokenExpired = false;
        public static string AllowedPlaylistName;
        internal static string AllowedPlaylistUrl;
        public static PrivateUser SpotifyProfile;
        public static bool ForceUpdate;
        private static readonly TaskQueue UpdateQueueWindowTasks = new();
        public static List<PlaylistTrack<IPlayableItem>> LikedPlaylistTracks = [];
        public static Tuple<bool, string> Canvas;
        public static ObservableCollection<TwitchUser> TwitchUsers = [];
        public static bool IoClientConnected = false;
        private static readonly ConcurrentQueue<TaskCompletionSource<bool>> UpdateQueue = new();
        private static bool _isProcessingQueue;
        public static YoutubeData YoutubeData = null;
        public static List<string> ConnectedEventsubs = [];

        public static string RootDirectory => string.IsNullOrEmpty(Settings.Directory)
            ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
            : Settings.Directory;

        public static SimpleTwitchUser FullRequester { get; set; }

        public static T FindChild<T>(DependencyObject parent, string childName)
            where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                if (child is not T childType)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    // If the child's name is set for search
                    if (childType is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = childType;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = childType;
                    break;
                }
            }

            return foundChild;
        }

        public static string MsToMmSsConverter(int milliseconds)
        {
            // Convert milliseconds to seconds
            int totalSeconds = milliseconds / 1000;

            // Calculate minutes and seconds
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            // Format and return the time in "mm:ss" format
            return $"{minutes:D1}:{seconds:D2}";
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                switch (ithChild)
                {
                    case T t:
                        yield return t;
                        break;
                }

                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        // Helper method to find a child of a specific type in the visual tree.
        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        public static Task QueueUpdateQueueWindow()
        {
            TaskCompletionSource<bool> tcs = new();
            UpdateQueue.Enqueue(tcs);
            ProcessQueue();
            return tcs.Task;
        }

        private static async void ProcessQueue()
        {
            try
            {
                if (_isProcessingQueue)
                    return;

                _isProcessingQueue = true;

                while (UpdateQueue.TryDequeue(out TaskCompletionSource<bool> tcs))
                {
                    try
                    {
                        await UpdateQueueWindow();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogSource.Core, "An error occurred while updating the queue: " + ex.Message);
                        tcs.SetException(ex);
                    }
                }

                _isProcessingQueue = false;
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Core, "Error updating queue window.", e);
            }
        }

        public static async Task UpdateQueueWindow()
        {
            int index;
            List<RequestObject> tempQueueList2;
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    try
                    {
                        QueueResponse queue = await SpotifyApiHandler.GetQueueInfo();
                        if (queue?.Queue == null || queue.Queue.Count == 0)
                        {
                            return;
                        }

                        // Remove all songs from the web queue that are not in the current playback queue
                        if (ReqList?.Count > 0)
                        {
                            List<RequestObject> itemsToRemove = [];

                            foreach (RequestObject requestObject in ReqList)
                            {
                                if (queue.Queue.Any(o => ((FullTrack)o).Id == requestObject.Trackid)) continue;

                                dynamic payload = new
                                {
                                    uuid = Settings.Uuid,
                                    key = Settings.AccessKey,
                                    queueid = requestObject.Queueid,
                                };

                                try
                                {
                                    await SongifyApi.PatchQueueAsync(Json.Serialize(payload));
                                    itemsToRemove.Add(requestObject);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(LogSource.Api, "Error updating value in web queue", ex);
                                }
                            }

                            foreach (RequestObject item in itemsToRemove)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        if (item.Trackid == CurrentSong.SongId)
                                        {
                                            return;
                                        }

                                        ReqList.Remove(item);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error(LogSource.Core, "Error removing item from ReqList", ex);
                                    }
                                });
                            }
                        }

                        bool isLikedSongsPlaylist = false;
                        Dictionary<string, bool> isInLikedSongs = [];
                        List<string> ids = queue.Queue.Select(track => ((FullTrack)track).Id).ToList();
                        if (CurrentSong != null && !ids.Contains(CurrentSong.SongId))
                            ids.Insert(0, CurrentSong.SongId); // Ensure current song is included

                        try
                        {
                            if (!string.IsNullOrEmpty(Settings.SpotifyPlaylistId) ||
                                Settings.SpotifyPlaylistId == "-1")
                            {
                                isLikedSongsPlaylist = true;
                                List<bool> x = await SpotifyApiHandler.CheckLibrary(ids);
                                if (x != null)
                                    for (int i = 0; i < ids.Count; i++)
                                    {
                                        isInLikedSongs[ids[i]] = x[i];
                                    }
                            }
                            else
                            {
                                isLikedSongsPlaylist = false;
                                await LoadLikedPlaylistTracks();
                            }
                        }
                        catch (Exception)
                        {
                            Logger.Error(LogSource.Spotify, "Error getting Liked Songs");
                        }

                        List<RequestObject> tempQueueList = [];
                        Dictionary<string, bool> replacementTracker = [];

                        foreach (FullTrack fullTrack in queue.Queue)
                        {
                            try
                            {
                                bool isInLikedPlaylist = isLikedSongsPlaylist
                                    ? isInLikedSongs.TryGetValue(fullTrack.Id, out bool boolValue) && boolValue
                                    : LikedPlaylistTracks.Any(o => ((FullTrack)o.Track).Id == fullTrack.Id);

                                RequestObject reqObj = ReqList.FirstOrDefault(o =>
                                    o.Trackid == fullTrack.Id && !replacementTracker.ContainsKey(o.Trackid) &&
                                    fullTrack.Id != CurrentSong.SongId);

                                RequestObject skipObj = SkipList.FirstOrDefault(o => o.Trackid == fullTrack.Id);

                                if (reqObj != null)
                                {
                                    reqObj.IsLiked = isInLikedPlaylist;
                                    tempQueueList.Add(reqObj);
                                    replacementTracker[reqObj.Trackid] = true;
                                }
                                else if (skipObj != null)
                                {
                                    skipObj.Requester = "Skipping...";
                                    skipObj.IsLiked = isInLikedPlaylist;
                                    tempQueueList.Add(skipObj);
                                    replacementTracker[skipObj.Trackid] = true;
                                }
                                else
                                {
                                    tempQueueList.Add(new RequestObject
                                    {
                                        Queueid = 0,
                                        Uuid = Settings.Uuid,
                                        Trackid = fullTrack.Id,
                                        Artist = string.Join(", ", fullTrack.Artists.Select(o => o.Name)),
                                        Title = fullTrack.Name,
                                        Length = MsToMmSsConverter((int)fullTrack.DurationMs),
                                        Requester = "Spotify",
                                        Played = 0,
                                        Albumcover = fullTrack.Album.Images.First().Url,
                                        IsLiked = isInLikedPlaylist,
                                        PlayerType = "Spotify"
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(LogSource.Core, "Error processing queue item", ex);
                            }
                        }

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                QueueTracks.Clear();
                                foreach (RequestObject item in tempQueueList)
                                {
                                    QueueTracks.Add(item);
                                }

                                foreach (Window window in Application.Current.Windows)
                                {
                                    if (window is not WindowQueue windowQueue)
                                        continue;

                                    if (windowQueue.dgv_Queue.ItemsSource != QueueTracks)
                                    {
                                        windowQueue.dgv_Queue.ItemsSource = QueueTracks;
                                    }

                                    bool isInLikedPlaylist = isLikedSongsPlaylist
                                        ? isInLikedSongs.TryGetValue(CurrentSong.SongId, out bool liked) && liked
                                        : LikedPlaylistTracks.Any(o => ((FullTrack)o.Track).Id == CurrentSong.SongId);

                                    QueueTracks.Insert(0, new RequestObject
                                    {
                                        Queueid = 0,
                                        Uuid = Settings.Uuid,
                                        Trackid = CurrentSong.SongId,
                                        Artist = CurrentSong.Artists,
                                        Title = CurrentSong.Title,
                                        Length = MsToMmSsConverter((int)CurrentSong.DurationMs),
                                        Requester = string.IsNullOrEmpty(Requester) ? "Spotify" : Requester,
                                        Played = -1,
                                        Albumcover = null,
                                        IsLiked = isInLikedPlaylist
                                    });

                                    windowQueue.UpdateQueueIcons();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(LogSource.Core, "Encountered an error while updating the UI", ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogSource.Core, "CORE: Error in QueueUpdate method", ex);
                    }

                    break;

                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                case Enums.PlayerType.Pear:
                    List<Song> pearQueue = await PearApi.GetQueueAsync();
                    PearResponse pearResponse = await PearApi.GetNowPlayingAsync();

                    if (pearQueue == null || pearResponse == null)
                    {
                        return;
                    }

                    index = pearQueue.FindIndex(item => item.Id == pearResponse.VideoId
                    );

                    if (index > 0)
                        pearQueue.RemoveRange(0, index);

                    tempQueueList2 = [];

                    tempQueueList2.AddRange(
                        pearQueue.Select(item => new RequestObject
                        {
                            Queueid = 0,
                            Uuid = Settings.Uuid,
                            Trackid = item.Id,
                            Artist = item.Artist ?? "",
                            Title = item.Title ?? "",
                            Length = item.Length.ToString() ?? "",
                            Requester = ReqList.Any(r => r.Trackid == item.Id)
                                ? ReqList.FirstOrDefault(r => r.Trackid == item.Id)?.Requester
                                : "YouTube",
                            Played = item.Id == pearResponse.VideoId ? -1 : 0,
                            Albumcover = item.CoverUrl ?? "",
                            PlayerType = "YouTube",
                            IsLiked = false
                        }).Where(x => x != null)
                    );

                    QueueTracks = new ObservableCollection<RequestObject>(tempQueueList2);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Check if the queue window is open and update it accordingly
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(WindowQueue))
                                continue;

                            if (window is not WindowQueue windowQueue) continue;
                            // Set the DataGrid's ItemsSource to the ObservableCollection (only done once)
                            windowQueue.dgv_Queue.ItemsSource = QueueTracks;
                            windowQueue.UpdateQueueIcons();
                        }

                        return Task.CompletedTask;
                    });

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static async Task<bool> CheckInLikedPlaylist(TrackInfo trackInfo)
        {
            if (trackInfo.SongId == null)
                return false;
            string id = trackInfo.SongId;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (string.IsNullOrEmpty(Settings.SpotifyPlaylistId))
            {
                return false;
            }

            if (LikedPlaylistTracks == null)
                await LoadLikedPlaylistTracks();

            if (LikedPlaylistTracks != null &&
                LikedPlaylistTracks.Any(o => ((FullTrack)o.Track).Id == trackInfo.SongId))
                return true;

            IsInPlaylist = false;
            return false;
        }

        private static async Task LoadLikedPlaylistTracks()
        {
            if (string.IsNullOrEmpty(Settings.SpotifyPlaylistId))
            {
                return;
            }

            LikedPlaylistTracks = [];
            Paging<PlaylistTrack<IPlayableItem>> tracks;
            do
            {
                tracks = await SpotifyApiHandler.GetPlaylistTracks(Settings.SpotifyPlaylistId);
                if (tracks.Items != null) LikedPlaylistTracks.AddRange(tracks.Items);
            } while (tracks.Next != null);
        }

        public static string GetReadablePlayer()
        {
            return Settings.Player switch
            {
                Enums.PlayerType.Spotify => "Spotify API",
                //Enums.PlayerType.SpotifyLegacy => "Spotify Legacy",
                Enums.PlayerType.FooBar2000 => "Foobar2000",
                Enums.PlayerType.Vlc => "VLC",
                Enums.PlayerType.BrowserCompanion => "Browser Extension",
                //Enums.PlayerType.YtmDesktop => "YTM Desktop",
                _ => ""
            };
        }

        public static string GetRefundConditionLabel(Enums.RefundCondition condition)
        {
            Dictionary<Enums.RefundCondition, string> refundConditionLabels = new()
            {
                { Enums.RefundCondition.UserLevelTooLow, Properties.Resources.Sw_Integration_RefundUserLevelLow },
                { Enums.RefundCondition.UserBlocked, Properties.Resources.Sw_Integration_RefundUSerBlocked },
                { Enums.RefundCondition.SpotifyNotConnected, Properties.Resources.Sw_Integration_RefundSpotifyNotConnected },
                { Enums.RefundCondition.SongUnavailable, Properties.Resources.Sw_Integration_RefundSongNotAvailable },
                { Enums.RefundCondition.SongBlocked, Properties.Resources.Sw_Integration_RefundSongBlocked },
                { Enums.RefundCondition.ArtistBlocked, Properties.Resources.Sw_Integration_RefundArtistBlocked },
                { Enums.RefundCondition.SongTooLong, Properties.Resources.Sw_Integration_RefundSongTooLong },
                { Enums.RefundCondition.SongAlreadyInQueue, Properties.Resources.Sw_Integration_RefundSongAlreadyInQueue },
                { Enums.RefundCondition.QueueLimitReached, Properties.Resources.Sw_Integration_RefundQueueLimitReached },
                { Enums.RefundCondition.NoSongFound, Properties.Resources.Sw_Integration_RefundNoSongFound },
                { Enums.RefundCondition.SongAddedButError, Properties.Resources.Sw_Integration_RefundSongAdded },
                { Enums.RefundCondition.TrackIsEplicit, Properties.Resources.Sw_Integration_RefundTrackIsExplicit },
                { Enums.RefundCondition.OnSuccess, Properties.Resources.Sw_Integration_RefundAlways },
            };

            return refundConditionLabels.TryGetValue(condition, out string label)
                ? label
                : $"Unknown refund condition: {condition}";
        }
    }
}