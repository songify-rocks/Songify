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
using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Client.Models;
using Application = System.Windows.Application;
using Newtonsoft.Json;
using Songify_Slim.Models.WebSocket;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Songify_Slim.Util.General
{
    public class WebServer
    {
        public bool Run;
        private HttpListener _listener = new();
        private static readonly ConcurrentDictionary<Guid, WebSocket> ConnectedClients = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> ChannelClients = new();


        public void StartWebServer(int port)
        {
            _listener = new HttpListener();
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

            // Setup listener for localhost only
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            // Optionally, support IPv6 localhost
            _listener.Prefixes.Add($"http://[::1]:{port}/");

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
            Guid clientId = Guid.NewGuid();
            string path = context.Request.Url.AbsolutePath;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(null);
                WebSocket socket = webSocketContext.WebSocket;

                // Add client to the appropriate channel
                ConcurrentDictionary<Guid, WebSocket> clients = ChannelClients.GetOrAdd(path, _ => new ConcurrentDictionary<Guid, WebSocket>());
                clients.TryAdd(clientId, socket);

                while (socket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new(new byte[4096]);
                    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (buffer.Array == null) continue;

                    string message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, result.Count);
                    string response = await ProcessMessage(message);

                    if (!string.IsNullOrEmpty(response))
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes("Command executed: " + response);
                        await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            finally
            {
                if (ChannelClients.TryGetValue(path, out var clients))
                {
                    clients.TryRemove(clientId, out _);
                }

                webSocketContext?.WebSocket?.Dispose();
            }
        }

        public async Task BroadcastToChannelAsync(string path, string message)
        {
            if (!ChannelClients.TryGetValue(path, out var clients))
                return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(messageBytes);

            foreach (KeyValuePair<Guid, WebSocket> pair in clients.ToArray())
            {
                Guid id = pair.Key;
                WebSocket socket = pair.Value;

                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        clients.TryRemove(id, out _);
                        socket.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    clients.TryRemove(id, out _);
                    socket.Dispose();
                }
            }
        }


        private static async Task<string> ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "";

            //Logger.LogStr($"WEBSOCKET: message '{message}' received");

            WebSocketCommand command;
            try
            {
                Debug.WriteLine(message);
                command = JsonConvert.DeserializeObject<WebSocketCommand>(message);
            }
            catch (JsonException)
            {
                return "Invalid JSON.";
            }

            if (command == null || string.IsNullOrWhiteSpace(command.Action))
                return "Invalid command format.";

            switch (command.Action)
            {
                case "youtube":
                    if (command.Data == null)
                        return "Missing data for youtube.";
                    YoutubeData youtubeData = command.Data.ToObject<YoutubeData>();
                    if (youtubeData == null)
                        return "Invalid data for youtube.";
                    if (!Equals(GlobalObjects.YoutubeData, youtubeData))
                        GlobalObjects.YoutubeData = youtubeData;
                    return "";

                case "queue_add":
                    if (command.Data == null)
                        return "Missing data for queue_add.";

                    QueueAddData queueData = command.Data.ToObject<QueueAddData>();

                    string trackId = await TwitchHandler.GetTrackIdFromInput(queueData.Track);
                    return await TwitchHandler.AddSongFromWebsocket(trackId, queueData.Requester ?? "");

                case "vol_set":
                    if (command.Data == null)
                        return "Missing data for vol_set.";

                    VolumeData volData = command.Data.ToObject<VolumeData>();

                    int volume = MathUtils.Clamp(volData.Value, 0, 100);
                    await SpotifyApiHandler.Spotify.SetVolumeAsync(volume);
                    return $"Volume set to {volume}%";

                case "send_to_chat":
                    TwitchHandler.SendCurrSong();
                    return "Current song sent to chat.";

                case "block_artist":
                    BlockArtist();
                    return "Artist blocked.";

                case "block_all_artists":
                    BlockAllArtists();
                    return "All artists blocked.";

                case "block_song":
                    BlockSong();
                    return "Song blocked.";

                case "block_user":
                    string user = BlockUser();
                    return !string.IsNullOrWhiteSpace(user) ? $"User {user} blocked" : "No user to block.";

                case "skip":
                case "next":
                    await SpotifyApiHandler.SkipSong();
                    return "Song skipped.";

                case "play_pause":
                case "pause":
                case "play":
                    var playbackContext = await SpotifyApiHandler.Spotify.GetPlaybackAsync();
                    if (playbackContext.IsPlaying)
                    {
                        await SpotifyApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
                        return "Playback paused.";
                    }
                    await SpotifyApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, "");
                    return "Playback resumed.";

                case "stop_sr_reward":
                    foreach (string rewardId in Settings.Settings.TwRewardId)
                    {
                        await TwitchHandler.TwitchApi.Helix.ChannelPoints.UpdateCustomRewardAsync(
                            Settings.Settings.TwitchUser.Id, rewardId, new UpdateCustomRewardRequest
                            {
                                IsPaused = true
                            }, Settings.Settings.TwitchAccessToken);
                    }
                    return "Song request rewards stopped.";

                case "vol_up":
                case "vol_down":
                    var device = (await SpotifyApiHandler.Spotify.GetDevicesAsync()).Devices
                        .FirstOrDefault(d => d.Id == Settings.Settings.SpotifyDeviceId);

                    if (device == null)
                        return "No device found.";

                    int change = command.Action == "vol_up" ? 5 : -5;
                    int newVolume = MathUtils.Clamp(device.VolumePercent + change, 0, 100);
                    await SpotifyApiHandler.Spotify.SetVolumeAsync(newVolume, device.Id);
                    return $"Volume set to {newVolume}%";

                default:
                    return $"Unknown action: {command.Action}";
            }
        }

        private static string BlockUser()
        {
            if (GlobalObjects.ReqList.All(o => o.Trackid != GlobalObjects.CurrentSong.SongId)) return "";
            {
                RequestObject req =
                    GlobalObjects.ReqList.FirstOrDefault(o => o.Trackid == GlobalObjects.CurrentSong.SongId);
                if (req == null) return "";
                Settings.Settings.UserBlacklist.Add(req.Requester);
                Settings.Settings.UserBlacklist = Settings.Settings.UserBlacklist;
                return req.Requester;
            }
        }

        private static async void BlockSong()
        {
            FullTrack result = await SpotifyApiHandler.GetTrack(GlobalObjects.CurrentSong.SongId);
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
            await SpotifyApiHandler.SkipSong();
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

        private static string GetLocalIpAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return (from ip in host.AddressList where ip.AddressFamily == AddressFamily.InterNetwork select ip.ToString()).FirstOrDefault();
        }

        private static bool IsRunningAsAdministrator()
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

        private static bool PortIsFree(int port)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            // Check active TCP connections
            bool isTcpPortUsed = properties.GetActiveTcpConnections().Any(c => c.LocalEndPoint.Port == port);

            // Check TCP listeners
            bool isTcpListenerUsed = properties.GetActiveTcpListeners().Any(l => l.Port == port);

            // Check UDP listeners
            bool isUdpListenerUsed = properties.GetActiveUdpListeners().Any(l => l.Port == port);

            // If any of these checks return true, the port is in use
            return !(isTcpPortUsed || isTcpListenerUsed || isUdpListenerUsed);
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
            using Stream output = response.OutputStream;
            output.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}