using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Unosquare.Swan.Formatters;
using MahApps.Metro.IconPacks;
using TwitchLib.Api.Helix;
using System.Collections;
using System.Windows.Threading;
using Songify_Slim.Models.YTMD;
using Songify_Slim.Util.Spotify;
using Queue = Songify_Slim.Models.YTMD.Queue;
using QueueItem = Songify_Slim.Models.YTMD.QueueItem;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Subscriptions;
using System.Collections.Concurrent;
using Songify_Slim.Models.WebSocket;

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
        public static List<Chatter> chatters = [];
        public static List<Subscription> subscribers = [];
        public static List<Moderator> moderators = [];
        public static List<ChannelVIPsResponseModel> vips = [];
        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H") ? "HH:mm:ss" : "hh:mm:ss tt";
        public static WebServer WebServer = new();
        public static bool TwitchUserTokenExpired = false;
        public static bool TwitchBotTokenExpired = false;
        public static string AllowedPlaylistName;
        internal static string AllowedPlaylistUrl;
        public static PrivateProfile SpotifyProfile;
        public static bool ForceUpdate;
        private static readonly TaskQueue UpdateQueueWindowTasks = new();
        public static List<PlaylistTrack> LikedPlaylistTracks = [];
        public static Tuple<bool, string> Canvas;
        public static ObservableCollection<TwitchUser> TwitchUsers = [];
        public static bool IoClientConnected = false;
        private static readonly ConcurrentQueue<TaskCompletionSource<bool>> UpdateQueue = new();
        private static bool _isProcessingQueue;
        public static YoutubeData YoutubeData = null;


        public static string RootDirectory => string.IsNullOrEmpty(Settings.Settings.Directory)
            ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
            : Settings.Settings.Directory;

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

        public static bool IsObjectDefault<T>(T obj)
        {
            if (obj == null)
                return false;  // The object itself is not null based on your condition.

            // Get all properties of the object using reflection.
            PropertyInfo[] properties = typeof(T).GetProperties();

            // Check each property if it's equal to the default value of its type.
            return !(from property in properties
                     let propertyValue = property.GetValue(obj)
                     let defaultValue = property.PropertyType.IsValueType
                         ? Activator.CreateInstance(property.PropertyType)
                         : null
                     where !Equals(propertyValue, defaultValue)
                     select propertyValue).Any();
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
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
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
                        Logger.LogStr("CORE: UpdateQueueWindow threw an exception.");
                        Logger.LogExc(ex);
                        tcs.SetException(ex);
                    }
                }

                _isProcessingQueue = false;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        public static async Task UpdateQueueWindow()
        {
            Debug.Write(Settings.Settings.Player);
            int index;
            List<RequestObject> tempQueueList2;
            switch (Settings.Settings.Player)
            {
                case Enums.PlayerType.SpotifyWeb:
                    try
                    {
                        SimpleQueue queue = await SpotifyApiHandler.GetQueueInfo();
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
                                if (queue.Queue.Any(o => o.Id == requestObject.Trackid)) continue;

                                dynamic payload = new
                                {
                                    uuid = Settings.Settings.Uuid,
                                    key = Settings.Settings.AccessKey,
                                    queueid = requestObject.Queueid,
                                };

                                try
                                {
                                    await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch,
                                        Json.Serialize(payload));
                                    itemsToRemove.Add(requestObject);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogStr("API: Error updating value in web queue");
                                    Logger.LogExc(ex);
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
                                        Logger.LogStr("CORE: Error removing item from ReqList");
                                        Logger.LogExc(ex);
                                    }
                                });
                            }
                        }

                        bool isLikedSongsPlaylist = false;
                        Dictionary<string, bool> isInLikedSongs = [];

                        List<string> ids = queue.Queue.Select(track => track.Id).ToList();
                        if (CurrentSong != null && !ids.Contains(CurrentSong.SongId))
                            ids.Insert(0, CurrentSong.SongId); // Ensure current song is included

                        try
                        {
                            if (!string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                                Settings.Settings.SpotifyPlaylistId == "-1")
                            {
                                isLikedSongsPlaylist = true;
                                ListResponse<bool> x = await SpotifyApiHandler.Spotify.CheckSavedTracksAsync(ids);

                                for (int i = 0; i < ids.Count; i++)
                                {
                                    isInLikedSongs[ids[i]] = x.List[i];
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
                            Logger.LogStr("Spotify API: Error getting Liked Songs");
                        }

                        List<RequestObject> tempQueueList = [];
                        Dictionary<string, bool> replacementTracker = [];

                        foreach (FullTrack fullTrack in queue.Queue)
                        {
                            try
                            {
                                bool isInLikedPlaylist = isLikedSongsPlaylist
                                    ? isInLikedSongs.TryGetValue(fullTrack.Id, out bool boolValue) && boolValue
                                    : LikedPlaylistTracks.Any(o => o.Track.Id == fullTrack.Id);

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
                                        Uuid = Settings.Settings.Uuid,
                                        Trackid = fullTrack.Id,
                                        Artist = string.Join(", ", fullTrack.Artists.Select(o => o.Name)),
                                        Title = fullTrack.Name,
                                        Length = MsToMmSsConverter((int)fullTrack.DurationMs),
                                        Requester = "Spotify",
                                        Played = 0,
                                        Albumcover = null,
                                        IsLiked = isInLikedPlaylist,
                                        PlayerType = "Spotify"
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogStr("CORE: Error processing queue item");
                                Logger.LogExc(ex);
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
                                        ? isInLikedSongs.TryGetValue(CurrentSong.SongId, out var liked) && liked
                                        : LikedPlaylistTracks.Any(o => o.Track.Id == CurrentSong.SongId);

                                    QueueTracks.Insert(0, new RequestObject
                                    {
                                        Queueid = 0,
                                        Uuid = Settings.Settings.Uuid,
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
                                Logger.LogStr("CORE: Encountered an error while updating the UI");
                                Logger.LogExc(ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogStr("CORE: Error in QueueUpdate method");
                        Logger.LogExc(ex);
                    }

                    break;
                case Enums.PlayerType.YtmDesktop:
                    YtmdResponse response = await WebHelper.GetYtmData();
                    if (response == null)
                    {
                        return;
                    }

                    tempQueueList2 = [];
                    List<QueueItem> queueItems = [];

                    // Find the index of the current VideoId
                    index = response.Player.Queue.Items.IndexOf(
                        response.Player.Queue.Items.Find(i => i.VideoId == response.Video.Id));

                    if (index != -1 && index + 1 < response.Player.Queue.Items.Count)
                    {
                        // Skip items only if the index is valid and not the last item
                        queueItems = response.Player.Queue.Items.Skip(index + 1).ToList();
                    }

                    tempQueueList2.AddRange(queueItems.Select(queueItem => new RequestObject
                    {
                        Queueid = 0,
                        Uuid = Settings.Settings.Uuid,
                        Trackid = queueItem.VideoId,
                        Artist = queueItem.Author,
                        Title = queueItem.Title,
                        Length = queueItem.Duration,
                        Requester = "YouTube",
                        Played = 0,
                        Albumcover = queueItem.Thumbnails.Last().Url,
                        IsLiked = false,
                        PlayerType = "Youtube"

                    }));

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

                            // Add the current song to the top of the queue
                            QueueTracks.Insert(0, new RequestObject
                            {
                                Queueid = 0,
                                Uuid = Settings.Settings.Uuid,
                                Trackid = response.Video.Id,
                                Artist = response.Video.Author,
                                Title = response.Video.Title,
                                Length = SecondsToMmss(response.Video.DurationSeconds),
                                Requester = "YouTube",
                                Played = -1,
                                Albumcover = response.Video.Thumbnails.Last().Url,
                                IsLiked = false
                            });
                            windowQueue.UpdateQueueIcons();
                        }

                        return Task.CompletedTask;
                    });

                    break;
                //case Enums.PlayerType.SpotifyLegacy:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                case Enums.PlayerType.Ytmthch:
                    YTMYHCHQueue ytmthchQueue = await WebHelper.GetYtmthchQueue();
                    YTMYHCHResponse ytmthchResponse = await WebHelper.GetYtmthchData();

                    if (ytmthchQueue == null || ytmthchResponse == null)
                    {
                        return;
                    }

                    index = ytmthchQueue.Items.FindIndex(item =>
                        item.PlaylistPanelVideoWrapperRenderer?.PrimaryRenderer?.PlaylistPanelVideoRenderer?.VideoId == ytmthchResponse.VideoId
                    );

                    if(index > 0)
                        ytmthchQueue.Items.RemoveRange(0, index);

                    tempQueueList2 = [];
                    tempQueueList2 = [];

                    tempQueueList2.AddRange(
                        ytmthchQueue.Items.Select(item =>
                        {
                            PlaylistPanelVideoRenderer renderer = item.PlaylistPanelVideoWrapperRenderer?.PrimaryRenderer?.PlaylistPanelVideoRenderer;
                            if (renderer == null) return null;

                            return new RequestObject
                            {
                                Queueid = 0,
                                Uuid = Settings.Settings.Uuid,
                                Trackid = renderer.VideoId,
                                Artist = renderer.ShortBylineText?.Runs?.FirstOrDefault()?.Text ?? "",
                                Title = renderer.Title?.Runs?.FirstOrDefault()?.Text ?? "",
                                Length = renderer.LengthText?.Runs?.FirstOrDefault()?.Text ?? "",
                                Requester = "YouTube",
                                Played = renderer.VideoId == ytmthchResponse.VideoId ? -1 : 0,
                                Albumcover = renderer.Thumbnail?.Thumbnails?.LastOrDefault()?.Url ?? "",
                                PlayerType = "YouTube",
                                IsLiked = false
                            };
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

        private static string ComputeQueueHash(IEnumerable<FullTrack> queue)
        {
            return string.Join(",", queue.Select(t => t.Id)).GetHashCode().ToString();
        }

        public static string SecondsToMmss(int totalSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
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

            if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId))
            {
                return false;
            }

            if (LikedPlaylistTracks == null)
                await LoadLikedPlaylistTracks();

            if (LikedPlaylistTracks != null && LikedPlaylistTracks.Any(o => o.Track.Id == trackInfo.SongId))
                return true;

            IsInPlaylist = false;
            return false;
        }

        private static async Task LoadLikedPlaylistTracks()
        {
            if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId))
            {
                return;
            }

            bool firstFetch = true;
            LikedPlaylistTracks = [];
            Paging<PlaylistTrack> tracks = null;
            do
            {
                tracks = firstFetch
                    ? await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId)
                    : await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100,
                        tracks.Offset + tracks.Limit);
                LikedPlaylistTracks.AddRange(tracks.Items);
                firstFetch = false;
            } while (tracks.HasNextPage());
        }

        public static string GetReadablePlayer()
        {
            return Settings.Settings.Player switch
            {
                Enums.PlayerType.SpotifyWeb => "Spotify API",
                //Enums.PlayerType.SpotifyLegacy => "Spotify Legacy",
                Enums.PlayerType.FooBar2000 => "Foobar2000",
                Enums.PlayerType.Vlc => "VLC",
                Enums.PlayerType.BrowserCompanion => "Browser Extension",
                Enums.PlayerType.YtmDesktop => "YTM Desktop",
                _ => ""
            };
        }
    }
}