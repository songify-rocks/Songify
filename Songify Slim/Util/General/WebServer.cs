using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Util.Songify;

namespace Songify_Slim.Util.General
{
    public class WebServer
    {
        private readonly int port;
        public string progress;

        public WebServer(int port)
        {
            this.port = port;
        }

        public void StartWebServer()
        {
            HttpListener listener = new HttpListener();

            // Listen on the specified port.
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    // Wait for a request.
                    HttpListenerContext context = listener.GetContext();
                    ProcessRequest(context);
                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            // Generate the HTML response.
            //string responseString = "<html><body><h1>Dynamic Values</h1>";
            //responseString += "<p>Current time: " + DateTime.Now.ToString() + "</p>";
            //responseString += "</body></html>";

            string responseString = SongFetcher.progress;

            // Convert the response string to a byte array.
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

            // Get the response output stream and write the response to it.
            HttpListenerResponse response = context.Response;
            response.ContentLength64 = responseBytes.Length;
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");
            using (Stream output = response.OutputStream)
            {
                output.Write(responseBytes, 0, responseBytes.Length);
            }
        }
    }
}
