using MahApps.Metro.IconPacks;
using Songify_Slim.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace Songify_Slim.Util.General
{
    public class WebServer
    {
        public bool Run;
        private readonly HttpListener _listener = new HttpListener();

        public void StartWebServer(int port)
        {
            if (port < 1025 || port > 65535)
            {
                Logger.LogStr($"Webserver: Invalid port number {port}.");
                return;
            }

            if (!PortIsFree(port))
            {
                Logger.LogStr($"Webserver: The Port {port} is blocked. Can't start webserver.");
                return;
            }

            // Setup listener
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            if (IsRunningAsAdministrator())
            {
                string localIp = GetLocalIpAddress();
                if (!string.IsNullOrWhiteSpace(localIp))
                {
                    _listener.Prefixes.Add($"http://{localIp}:{port}/");
                }
            }

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                Logger.LogStr($"WebServer: Failed to start on port {port}. Exception: {ex.Message}");
                return;
            }

            Run = true;
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow == null) return;
                    // Assuming these UI elements and methods exist in your MainWindow
                    ((MainWindow)Application.Current.MainWindow).IconWebServer.Foreground = Brushes.GreenYellow;
                    ((MainWindow)Application.Current.MainWindow).IconWebServer.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                });

                Logger.LogStr($"WebServer: Started on port {port}");
                Logger.LogStr($"WebSocket: Started on ws://127.0.0.1:{port}");

                while (Run)
                {
                    try
                    {
                        HttpListenerContext context = _listener.GetContext();
                        // Here you could inspect the request to determine if it's a WebSocket request
                        // and call a different method to handle it.
                        if (IsWebSocketRequest(context.Request))
                        {
                            ProcessWebSocketRequest(context);
                        }
                        else
                        {
                            ProcessHttpRequest(context);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogExc(e);
                    }
                }
            });
        }

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                // Check for an Authorization header (or any other header as needed)
                var authHeader = context.Request.Headers["Authorization"];

                // Implement your authentication logic here.
                // For example, verify a token extracted from the authHeader.
                //bool isAuthenticated = ValidateAuthToken(authHeader); // You need to implement ValidateAuthToken

                //if (!isAuthenticated)
                //{
                //    // If authentication fails, respond with an appropriate status code and close the connection.
                //    Debug.WriteLine("Unauthorized");
                //    context.Response.StatusCode = 401; // Unauthorized
                //    context.Response.StatusDescription = "Unauthorized";
                //    context.Response.Close();
                //    return;
                //}

                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                var webSocket = webSocketContext.WebSocket;

                while (webSocket.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                        // Decode the received message
                        string message = System.Text.Encoding.UTF8.GetString(buffer.Array, buffer.Offset, result.Count);

                        // Process the command
                        ProcessCommand(message);

                    } while (!result.EndOfMessage);
                }
            }
            catch (Exception e)
            {
                // Handle exception
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                webSocketContext?.WebSocket?.Dispose();
            }
        }

        private static bool ValidateAuthToken(string authHeader)
        {
            return authHeader == "123456";
        }

        private async void ProcessCommand(string message)
        {
            Logger.LogStr($"WEBSOCKET: Command '{message}' received");
            // Here, we're assuming commands are simple text commands. Adjust parsing logic as necessary.
            switch (message.ToLower())
            {
                case "block_artist":
                    BlockArtist();
                    break;
                case "block_all_artists":
                    BlockAllArtists();
                    break;
                case "block_song":
                    BlockSong();
                    break;
                case "block_user":
                    BlockUser();
                    break;
                case "skip":
                case "next":
                    await ApiHandler.SkipSong();
                    break;
                case "pause":
                    await ApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
                    break;
                case "play":
                    await ApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, "");
                    break;
                case "stop_sr_reward":
                    foreach (string s in Settings.Settings.TwRewardId)
                    {
                        await TwitchHandler.TwitchApi.Helix.ChannelPoints.UpdateCustomRewardAsync(
                            Settings.Settings.TwitchUser.Id, s, new UpdateCustomRewardRequest
                            {
                                IsPaused = true
                            }, Settings.Settings.TwitchAccessToken);
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown command: {message}");
                    break;
            }
        }

        private static void BlockUser()
        {
            if (GlobalObjects.ReqList.All(o => o.Trackid != GlobalObjects.CurrentSong.SongId)) return;
            {
                RequestObject req =
                    GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == GlobalObjects.CurrentSong.SongId);
                if (req == null) return;
                Settings.Settings.UserBlacklist.Add(req.Requester);
                Settings.Settings.UserBlacklist = Settings.Settings.UserBlacklist;
            }
        }

        private static async void BlockSong()
        {
            FullTrack result = ApiHandler.GetTrack(GlobalObjects.CurrentSong.SongId);
            if (result != null)
                Settings.Settings.SongBlacklist.Add(new TrackItem
                {
                    Artists = string.Join(", ", result.Artists.Select(o => o.Name).ToList()),
                    TrackName = result.Name,
                    TrackId = result.Id,
                    TrackUri = result.Uri,
                    ReadableName = string.Join(", ", result.Artists.Select(o => o.Name).ToList()) + " - " + result.Name
                });
            Settings.Settings.SongBlacklist = Settings.Settings.SongBlacklist;
            await ApiHandler.SkipSong();
            // If Window_Blacklist is open, call LoadBlacklists();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(Window_Blacklist))
                        ((Window_Blacklist)window).LoadBlacklists();
            });
        }

        private static void BlockAllArtists()
        {
            foreach (SimpleArtist currentSongFullArtist in GlobalObjects.CurrentSong.FullArtists)
            {
                Settings.Settings.ArtistBlacklist.Add(currentSongFullArtist.Name);
            }
            Settings.Settings.ArtistBlacklist = Settings.Settings.ArtistBlacklist;
        }

        private static void BlockArtist()
        {
            Settings.Settings.ArtistBlacklist.Add(GlobalObjects.CurrentSong.FullArtists[0].Name);
            Settings.Settings.ArtistBlacklist = Settings.Settings.ArtistBlacklist;
        }

        private static bool IsWebSocketRequest(HttpListenerRequest request)
        {
            return request.IsWebSocketRequest;
        }


        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where ip.AddressFamily == AddressFamily.InterNetwork select ip.ToString()).FirstOrDefault();
        }

        public static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void StopWebServer()
        {

            Run = false;
            Logger.LogStr("WebServer: Started stopped");
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow == null) return;
                ((MainWindow)Application.Current.MainWindow).IconWebServer.Foreground = Brushes.Gray;
                ((MainWindow)Application.Current.MainWindow).IconWebServer.Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill;
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

        private void ProcessHttpRequest(HttpListenerContext context)
        {
            if (string.IsNullOrWhiteSpace(GlobalObjects.ApiResponse))
                return;
            // Convert the response string to a byte array.
            byte[] responseBytes = Encoding.UTF8.GetBytes(GlobalObjects.ApiResponse);

            // Get the response output stream and write the response to it.
            HttpListenerResponse response = context.Response;
            response.ContentLength64 = responseBytes.Length;
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            using (Stream output = response.OutputStream)
            {
                output.Write(responseBytes, 0, responseBytes.Length);
            }
        }
    }
}
