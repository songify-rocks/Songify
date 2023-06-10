using Songify_Slim.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public const string ApiUrl = "https://api.songify.rocks/v2";
        public const string BaseUrl = "https://songify.overcode.tv";
        public static string ApiResponse;
        public static string AppVersion;
        public static FlowDocument ConsoleDocument = new FlowDocument();
        public static TrackInfo CurrentSong;
        public static bool DetachConsole = false;
        public static bool IsBeta = false;
        public static bool IsInPlaylist;
        public static ObservableCollection<RequestObject> ReqList = new ObservableCollection<RequestObject>();
        public static string Requester = "";
        public static int RewardGoalCount = 0;
        public static List<RequestObject> SkipList = new List<RequestObject>();
        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H") ? "HH:mm:ss" : "hh:mm:ss tt";
        public static WebServer WebServer = new WebServer();
        public static bool TwitchUserTokenExpired = false;
        public static bool TwitchBotTokenExpired = false;

        public static T FindChild<T>(DependencyObject parent, string childName)
            where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
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
                    var frameworkElement = child as FrameworkElement;
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

        public static string GetReadablePlayer()
        {
            switch (Settings.Settings.Player)
            {
                case 0:
                    return "Spotify API";
                case 1:
                    return "Spotify Legacy";
                case 2:
                    return "Deezer";
                case 3:
                    return "Foobar2000";
                case 4:
                    return "VLC";
                case 5:
                    return "YouTube";
            }

            return "";
        }
    }
}
