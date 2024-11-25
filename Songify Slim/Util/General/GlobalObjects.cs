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

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public const string ApiUrl = "https://api.songify.rocks/v2";
        public static string BaseUrl = Settings.Settings.BaseUrl;
        public static string AuthUrl = Settings.Settings.BaseUrl;
        public const string AltAuthUrl = "https://songify.bloemacher.com";
        public static string ApiResponse;
        public static string AppVersion;
        public static FlowDocument ConsoleDocument = new();
        public static TrackInfo CurrentSong;
        public static bool DetachConsole = false;
        public static bool IsInPlaylist;
        public static ObservableCollection<RequestObject> ReqList = new();
        public static string Requester = "";
        public static int RewardGoalCount = 0;
        public static List<RequestObject> SkipList = new();
        public static ObservableCollection<RequestObject> QueueTracks { get; set; } = new ObservableCollection<RequestObject>();

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


        public static string RootDirectory => string.IsNullOrEmpty(Settings.Settings.Directory)
            ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
            : Settings.Settings.Directory;


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
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    FrameworkElement frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
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

        public static void QueueUpdateQueueWindow()
        {
            UpdateQueueWindowTasks.Enqueue(UpdateQueueWindow);
        }

        public static async Task UpdateQueueWindow()
        {
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
                    List<RequestObject> itemsToRemove = new List<RequestObject>();

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
                            await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
                            itemsToRemove.Add(requestObject);
                        }
                        catch (Exception ex)
                        {
                            // Log or handle error for individual request failure
                            Logger.LogStr("API: Error updating value in web queue");
                            Logger.LogExc(ex);
                        }
                    }

                    foreach (RequestObject item in itemsToRemove)
                    {
                        await Application.Current.Dispatcher.BeginInvoke(() =>
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
                                // Log or handle error during list update
                                Logger.LogStr("CORE: Error removing item from ReqList");
                                Logger.LogExc(ex);
                            }
                        });
                    }
                }


                bool isLikedSongsPlaylist = false;

                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        // Clear the tracks in the queue (ObservableCollection will notify the UI)
                        Dictionary<string, bool> isInLikedSongs = new();
                        try
                        {
                            if (!string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                                Settings.Settings.SpotifyPlaylistId == "-1")
                            {
                                isLikedSongsPlaylist = true;
                                List<string> ids = queue.Queue.Select(track => track.Id).ToList();
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
                        // Dictionary to keep track of replacements
                        Dictionary<string, bool> replacementTracker = new();

                        // Process the queue
                        foreach (FullTrack fullTrack in queue.Queue)
                        {
                            try
                            {
                                bool isInLikedPlaylist;
                                if (isLikedSongsPlaylist)
                                    isInLikedPlaylist = isInLikedSongs.TryGetValue(fullTrack.Id, out bool boolValue) && boolValue;
                                else
                                    isInLikedPlaylist = LikedPlaylistTracks.Any(o => o.Track.Id == fullTrack.Id);

                                // Determine if we have a matching request object that hasn't been used for replacement yet
                                RequestObject reqObj = ReqList.FirstOrDefault(o => o.Trackid == fullTrack.Id && !replacementTracker.ContainsKey(o.Trackid) && fullTrack.Id != CurrentSong.SongId);
                                RequestObject skipObj = SkipList.FirstOrDefault(o => o.Trackid == fullTrack.Id);

                                if (reqObj != null)
                                {
                                    reqObj.IsLiked = isInLikedPlaylist;
                                    tempQueueList.Add(reqObj);
                                    replacementTracker[reqObj.Trackid] = true; // Mark this track ID as replaced
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
                                    RequestObject newRequestObject = new()
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
                                        IsLiked = isInLikedPlaylist
                                    };

                                    tempQueueList.Add(newRequestObject);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log or handle error during queue processing
                                Logger.LogStr("CORE: Error processing queue item");
                                Logger.LogExc(ex);
                            }
                        }

                        QueueTracks = new ObservableCollection<RequestObject>(tempQueueList);

                        // Check if the queue window is open and update it accordingly
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(WindowQueue))
                                continue;

                            if (window is WindowQueue windowQueue)
                            {
                                // Set the DataGrid's ItemsSource to the ObservableCollection (only done once)
                                windowQueue.dgv_Queue.ItemsSource = QueueTracks;
                                bool isInLikedPlaylist;

                                if (isLikedSongsPlaylist)
                                {
                                    if(CurrentSong == null)
                                        return;
                                    ListResponse<bool> x = await SpotifyApiHandler.Spotify.CheckSavedTracksAsync([CurrentSong.SongId]);
                                    isInLikedPlaylist = x.List.Count > 0 && x.List[0];
                                }
                                else
                                    isInLikedPlaylist = LikedPlaylistTracks.Any(o => o.Track.Id == CurrentSong.SongId);

                                // Add the current song to the top of the queue
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
                    }
                    catch (Exception ex)
                    {
                        // Log or handle error during UI update
                        Logger.LogStr("CORE: Encountered an error while updating the UI");
                        Logger.LogExc(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                // Log or handle error for the entire method failure
                Logger.LogStr("CORE: Error in QueueUpdate method");
                Logger.LogExc(ex);
            }

        }


        public static async Task<bool> CheckInLikedPlaylist(TrackInfo trackInfo)
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
                0 => "Spotify API",
                1 => "Spotify Legacy",
                2 => "Deezer",
                3 => "Foobar2000",
                4 => "VLC",
                5 => "YouTube",
                6 => "YTM Desktop",
                _ => ""
            };
        }
    }
}
