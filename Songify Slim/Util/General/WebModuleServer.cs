
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Songify_Slim.Util.General
{
    public class WebModuleServer
    {
        private static readonly IDictionary<string, string> _mimeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {".apng", "image/apng"},
                {".avif", "image/avif"},
                {".avifs", "image/avif-sequence"},
                {".bmp", "image/bmp"},
                {".css", "text/css" },
                {".eot", "application/vnd.ms-fontobject"},
                {".gif", "image/gif"},
                {".html", "text/html" },
                {".jpeg", "image/jpeg" },
                {".jpg", "image/jpeg" },
                {".js", "text/javascript" },
                {".json", "application/json" },
                {".otf", "application/font-sfnt"},
                {".png", "image/png" },
                {".svg", "image/svg+xml" },
                {".ttf", "application/font-sfnt"},
                {".webm", "image/webm" },
                {".woff", "application/font-woff"},
                {".woff2", "application/font-woff2"},
            };

        public static bool ProcessModule(HttpListenerContext context)
        {
            bool hasProcessed = false;
            if (context.Request.Url.Segments.Length > 1)
            {
                switch (context.Request.Url.Segments[1])
                {
                    case "widget":
                    case "Widget":
                    case "widget/":
                    case "Widget/":
                        hasProcessed = ProcessWidget(context);
                        break;
                }
            }

            return hasProcessed;
        }

        private static bool ProcessWidget(HttpListenerContext context)
        {
            string[] fileSegments = context.Request.Url.Segments;
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            path += "/Widget/";

            if (fileSegments.Length > 2)
            {
                for (int i = 2; i < fileSegments.Length; i++)
                {
                    path += fileSegments[i];
                }
            }
            else
            {
                path += "index.html";
            }

            if (!File.Exists(path)) return false;

            string mimeType = GetMime(path);
            byte[] responseBytes = File.ReadAllBytes(path);

            if (mimeType == "text/html")
            {
                string responseString = Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
                responseString = responseString.Replace("<!-- settings -->", $"<script>const apiUrl = \"http://localhost:{context.Request.Url.Port}\";</script>");
                responseBytes = Encoding.UTF8.GetBytes(responseString);
            }

            HttpListenerResponse response = context.Response;
            response.ContentLength64 = responseBytes.Length;
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET");
            response.ContentType = mimeType;
            if (mimeType.StartsWith("text/"))
            {
                response.ContentType += "; charset=utf-8";
                response.ContentEncoding = Encoding.UTF8;
            }

            using (Stream output = response.OutputStream)
            {
                output.Write(responseBytes, 0, responseBytes.Length);
            }

            return true;
        }

        private static string GetMime(string path)
        {
            string mimeType = MimeMapping.GetMimeMapping(path);

            string extension = Path.GetExtension(path);

            return _mimeMappings.TryGetValue(extension, out string result) ? result : mimeType;
        }
    }
}