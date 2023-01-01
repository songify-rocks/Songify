using Songify_Slim.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public static WebServer WebServer = new WebServer();
        public static List<RequestObject> ReqList = new List<RequestObject>();
        public static List<RequestObject> SkipList = new List<RequestObject>();
        public static string APIResponse;
        public static FlowDocument ConsoleDocument = new FlowDocument();
        public static string TimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H")
            ? "HH:mm:ss"
            : "hh:mm:ss tt";

        public static bool DetachConsole = false;
    }
}
