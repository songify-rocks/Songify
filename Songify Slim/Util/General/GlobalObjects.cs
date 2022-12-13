using Songify_Slim.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    public static class GlobalObjects
    {
        public static WebServer WebServer = new WebServer();
        public static List<RequestObject> ReqList = new List<RequestObject>();
        public static List<RequestObject> SkipList = new List<RequestObject>();
        public static string APIResponse;
    }
}
