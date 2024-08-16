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
using System.Windows.Documents;
using System.Windows.Media;
using Unosquare.Swan.Formatters;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public const string ApiUrl = "https://api.songify.rocks/v2";
        public const string BaseUrl = "https://songify.overcode.tv";
        public static string ApiResponse;
        public static string AppVersion;
        public static FlowDocument ConsoleDocument = new();
        public static TrackInfo CurrentSong;
        public static bool DetachConsole = false;
        public static bool IsBeta = false;
        public static bool IsInPlaylist;
        public static ObservableCollection<RequestObject> ReqList = new();
        public static string Requester = "";
        public static int RewardGoalCount = 0;
        public static List<RequestObject> SkipList = new();
        public static List<RequestObject> QueueTracks = new();
        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H") ? "HH:mm:ss" : "hh:mm:ss tt";
        public static WebServer WebServer = new();
        public static bool TwitchUserTokenExpired = false;
        public static bool TwitchBotTokenExpired = false;
        public static string AllowedPlaylistName;
        internal static string AllowedPlaylistUrl;
        public static PrivateProfile SpotifyProfile;
        public static bool ForceUpdate;
        private static readonly TaskQueue updateQueueWindowTasks = new();
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
            updateQueueWindowTasks.Enqueue(UpdateQueueWindow);
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        QueueTracks.Clear();
                        // Dictionary to keep track of replacements
                        Dictionary<string, bool> replacementTracker = new Dictionary<string, bool>();
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(WindowQueue))
                                continue;

                            if (window is not WindowQueue windowQueue)
                                continue;

                            windowQueue.dgv_Queue.ItemsSource = null;
                            windowQueue.dgv_Queue.Items.Clear();

                            foreach (FullTrack fullTrack in queue.Queue)
                            {
                                try
                                {
                                    // Determine if we have a matching request object that hasn't been used for replacement yet
                                    RequestObject reqObj = ReqList.FirstOrDefault(o => o.Trackid == fullTrack.Id && !replacementTracker.ContainsKey(o.Trackid) && fullTrack.Id != CurrentSong.SongId);
                                    RequestObject skipObj = SkipList.FirstOrDefault(o => o.Trackid == fullTrack.Id);

                                    if (reqObj != null)
                                    {
                                        // If we found a request object, and it hasn't been used for replacement, add it and mark as used
                                        windowQueue.dgv_Queue.Items.Add(reqObj);
                                        QueueTracks.Add(reqObj);
                                        replacementTracker[reqObj.Trackid] = true; // Mark this track ID as having been replaced
                                    }
                                    else if (skipObj != null)
                                    {
                                        skipObj.Requester = "Skipping...";
                                        windowQueue.dgv_Queue.Items.Add(skipObj);
                                        QueueTracks.Add(skipObj);
                                        replacementTracker[skipObj.Trackid] = true; // Mark this track ID as having been replaced
                                    }
                                    else
                                    {
                                        var newRequestObject = new RequestObject
                                        {
                                            Queueid = 0,
                                            Uuid = Settings.Settings.Uuid,
                                            Trackid = fullTrack.Id,
                                            Artist = string.Join(", ", fullTrack.Artists.Select(o => o.Name).ToList()),
                                            Title = fullTrack.Name,
                                            Length = MsToMmSsConverter((int)fullTrack.DurationMs),
                                            Requester = "Spotify",
                                            Played = 0,
                                            Albumcover = null
                                        };

                                        QueueTracks.Add(newRequestObject);
                                        windowQueue.dgv_Queue.Items.Add(newRequestObject);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log or handle error during queue processing
                                    Logger.LogStr("CORE: Error processing queue item");
                                    Logger.LogExc(ex);
                                }
                            }

                            // Add the current song to the top of the queue
                            windowQueue.dgv_Queue.Items.Insert(0, new RequestObject
                            {
                                Queueid = 0,
                                Uuid = Settings.Settings.Uuid,
                                Trackid = CurrentSong.SongId,
                                Artist = CurrentSong.Artists,
                                Title = CurrentSong.Title,
                                Length = MsToMmSsConverter((int)CurrentSong.DurationMs),
                                Requester = Requester == "" ? "Spotify" : Requester,
                                Played = -1,
                                Albumcover = null
                            });

                            windowQueue.dgv_Queue.Items.Refresh();
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
                _ => ""
            };
        }
    }
}
