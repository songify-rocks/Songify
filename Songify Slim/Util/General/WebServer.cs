using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using Songify_Slim.Views;

namespace Songify_Slim.Util.General
{
    public class WebServer
    {
        public bool run;
        private readonly HttpListener _listener = new HttpListener();

        public void StartWebServer(int port)
        {
            if(port < 1025 || port > 65535)
                return;
            if (!PortIsFree(port)) return;
            // Listen on the specified port.
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            run = true;
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow != null)
                    {
                        ((MainWindow)Application.Current.MainWindow).IconWebServer.Foreground = Brushes.GreenYellow;
                        ((MainWindow)Application.Current.MainWindow).IconWebServer.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                    }
                });
                Logger.LogStr($"WebServer: Started on port {port}");

                while (run)
                {
                    try
                    {
                        // Wait for a request.
                        HttpListenerContext context = _listener.GetContext();
                        ProcessRequest(context);
                    }
                    catch (Exception)
                    {

                    }
                }

            });
        }

        public void StopWebServer()
        {

            run = false;
            Logger.LogStr("WebServer: Started stopped");
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow != null)
                {
                    ((MainWindow)Application.Current.MainWindow).IconWebServer.Foreground = Brushes.Gray;
                    ((MainWindow)Application.Current.MainWindow).IconWebServer.Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill;
                }


            });
            _listener.Stop();
        }

        public static bool PortIsFree(int port)
        {
            // Get the IP global properties for the local network
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Get a list of active TCP connections
            var connections = properties.GetActiveTcpConnections();

            // Check if the specified port is blocked
            bool isBlocked = connections.All(connection => connection.LocalEndPoint.Port != port);

            //Debug.WriteLine($"PortFree: {isBlocked}");
            return isBlocked;
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            // Generate the HTML response.
            //string responseString = "<html><body><h1>Dynamic Values</h1>";
            //responseString += "<p>Current time: " + DateTime.Now.ToString() + "</p>";
            //responseString += "</body></html>";

            if (string.IsNullOrWhiteSpace(GlobalObjects.APIResponse))
                return;
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
