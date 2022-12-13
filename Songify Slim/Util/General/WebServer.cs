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
        public string progress;
        public bool run;

        public void StartWebServer(int port)
        {
            if (!PortIsFree(port)) return;
            HttpListener listener = new HttpListener();
            // Listen on the specified port.
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            run = true;
            Task.Run(() =>
            {
                while (run)
                {
                    // Wait for a request.
                    HttpListenerContext context = listener.GetContext();
                    ProcessRequest(context);
                }
            });
        }

        private bool PortIsFree(int port)
        {
            // Get the IP global properties for the local network
            var properties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

            // Get a list of active TCP connections
            var connections = properties.GetActiveTcpConnections();

            // Check if the specified port is blocked
            bool isBlocked = connections.All(connection => connection.LocalEndPoint.Port != port && connection.RemoteEndPoint.Port != port);
            
            return isBlocked;
        }

        public void StopWebServer()
        {
            run = false;
        }
        private void ProcessRequest(HttpListenerContext context)
        {
            // Generate the HTML response.
            //string responseString = "<html><body><h1>Dynamic Values</h1>";
            //responseString += "<p>Current time: " + DateTime.Now.ToString() + "</p>";
            //responseString += "</body></html>";
            

            // Convert the response string to a byte array.
            byte[] responseBytes = Encoding.UTF8.GetBytes(GlobalObjects.APIResponse);

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
