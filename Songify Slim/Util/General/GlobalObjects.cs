using Songify_Slim.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public const string _baseUrl = "https://songify.overcode.tv";
        public static WebServer WebServer = new WebServer();
        public static List<RequestObject> ReqList = new List<RequestObject>();
        public static List<RequestObject> SkipList = new List<RequestObject>();
        public static string APIResponse;
        public static FlowDocument ConsoleDocument = new FlowDocument();
        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H")
            ? "HH:mm:ss"
            : "hh:mm:ss tt";

        public static bool DetachConsole = false;
        public static TrackInfo CurrentSong;
        
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }
    }


}
