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
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Client.Models;
using Application = System.Windows.Application;
using Newtonsoft.Json;
using Songify_Slim.Models.WebSocket;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Songify_Slim.Util.Songify.Twitch;
using SpotifyAPI.Web;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Youtube.YTMYHCH.YtmDesktopApi; // for Enums

namespace Songify_Slim.Util.General
{
    public class WebServer
    {
        public bool Run;
        private HttpListener _listener = new();
        private static readonly ConcurrentDictionary<Guid, WebSocket> ConnectedClients = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> ChannelClients = new();

        // ---- Command routing (no giant switch) ----
        private delegate Task<string> CommandHandler(WebSocketCommand command);

        private static readonly Dictionary<string, CommandHandler> CommandMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["youtube"] = HandleYoutubeAsync,

            ["queue_add"] = HandleQueueAddAsync,

            ["vol_set"] = HandleVolSetAsync,
            ["vol_up"] = c => HandleVolumeStepAsync(+5),
            ["vol_down"] = c => HandleVolumeStepAsync(-5),

            ["send_to_chat"] = HandleSendToChatAsync,

            ["block_artist"] = c => { BlockArtist(); return Task.FromResult("Artist blocked."); },
            ["block_all_artists"] = c => { BlockAllArtists(); return Task.FromResult("All artists blocked."); },
            ["block_song"] = async c => { BlockSong(); return await Task.FromResult("Song blocked."); },
            ["block_user"] = c =>
            {
                string user = BlockUser();
                return Task.FromResult(!string.IsNullOrWhiteSpace(user) ? $"User {user} blocked" : "No user to block.");
            },

            ["skip"] = HandleSkipAsync,
            ["next"] = HandleSkipAsync,

            ["play_pause"] = HandlePlayPauseAsync,
            ["pause"] = HandlePlayPauseAsync,
            ["play"] = HandlePlayPauseAsync,

            ["stop_sr_reward"] = HandleStopSrRewardAsync
        };

        // Abstraction for player-specific ops
        private interface IPlayerOps
        {
            Task<string> QueueAddAsync(QueueAddData data);
            Task<string> SetVolumeAsync(int value);
            Task<string> VolumeStepAsync(int change);
            Task<string> SkipAsync();
            Task<string> TogglePlayPauseAsync(string action); // "play", "pause", "play_pause"
            Task<string> SendToChatAsync();
        }

        private sealed class SpotifyOps : IPlayerOps
        {
            public async Task<string> QueueAddAsync(QueueAddData data)
            {
                string trackId = await TwitchHandler.GetTrackIdFromInput(data.Track);
                return await TwitchHandler.AddSongFromWebsocket(trackId, data.Requester ?? "");
            }

            public async Task<string> SetVolumeAsync(int value)
            {
                int volume = MathUtils.Clamp(value, 0, 100);
                await SpotifyApiHandler.SetVolume(volume);
                return $"Volume set to {volume}%";
            }

            public async Task<string> VolumeStepAsync(int change)
            {
                CurrentlyPlayingContext playback = await SpotifyApiHandler.GetPlayback();
                int current = playback?.Device?.VolumePercent ?? 0;
                int newVolume = MathUtils.Clamp(current + change, 0, 100);
                await SpotifyApiHandler.SetVolume(newVolume);
                return $"Volume set to {newVolume}%";
            }

            public async Task<string> SkipAsync()
            {
                await SpotifyApiHandler.SkipSong();
                return "Song skipped.";
            }

            public async Task<string> TogglePlayPauseAsync(string action)
            {
                var playbackContext = await SpotifyApiHandler.GetPlayback();
                bool isPlaying = playbackContext?.IsPlaying ?? false;

                if (string.Equals(action, "pause", StringComparison.OrdinalIgnoreCase) || (string.Equals(action, "play_pause", StringComparison.OrdinalIgnoreCase) && isPlaying))
                {
                    await SpotifyApiHandler.PlayPause(Enums.PlaybackAction.Pause);
                    return "Playback paused.";
                }

                await SpotifyApiHandler.PlayPause(Enums.PlaybackAction.Play);
                return "Playback resumed.";
            }

            public Task<string> SendToChatAsync()
            {
                TwitchHandler.SendCurrSong();
                return Task.FromResult("Current song sent to chat.");
            }
        }

        private sealed class YtmthchOps : IPlayerOps
        {
            public async Task<string> QueueAddAsync(QueueAddData data)
            {
                string trackId = await TwitchHandler.GetTrackIdFromInput(data.Track);
                return await TwitchHandler.AddSongFromWebsocket(trackId, data.Requester ?? "");
            }

            public async Task<string> SetVolumeAsync(int value)
            {
                int volume = MathUtils.Clamp(value, 0, 100);
                await YtmDesktopApi.SetVolumeAsync(volume);
                return $"Volume set to {volume}%";
            }

            public async Task<string> VolumeStepAsync(int change)
            {
                VolumeState vol = await YtmDesktopApi.GetVolume();
                int current = vol.State;
                int newVolume = MathUtils.Clamp(current + change, 0, 100);
                await YtmDesktopApi.SetVolumeAsync(newVolume);
                return $"Volume set to {newVolume}%";
            }

            public async Task<string> SkipAsync()
            {
                await YtmDesktopApi.NextAsync();
                return "Song skipped.";
            }

            public async Task<string> TogglePlayPauseAsync(string action)
            {
                SongResponse playback = await YtmDesktopApi.GetCurrentSongAsync();
                bool isPlaying = !playback?.IsPaused ?? false;
                if (string.Equals(action, "pause", StringComparison.OrdinalIgnoreCase) || (string.Equals(action, "play_pause", StringComparison.OrdinalIgnoreCase) && isPlaying))
                {
                    await YtmDesktopApi.PlayPauseAsync();
                    return "Playback paused.";
                }
                await YtmDesktopApi.PlayPauseAsync();
                return "Playback resumed.";
            }
           
            public Task<string> SendToChatAsync()
            {
                TwitchHandler.SendCurrSong();
                return Task.FromResult("Current song sent to chat.");
            }
        }

        private static readonly IPlayerOps Spotify = new SpotifyOps();
        private static readonly IPlayerOps Ytm = new YtmthchOps();

        private static IPlayerOps GetPlayerOps()
        {
            switch (Settings.Settings.Player)
            {
                case Enums.PlayerType.SpotifyWeb:
                    return Spotify;
                case Enums.PlayerType.Ytmthch:
                    return Ytm;
                default:
                    return Spotify; // default to Spotify behavior
            }
        }

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
                try
                {
                    while (socket.State == WebSocketState.Open)
                    {
                        var buffer = new ArraySegment<byte>(new byte[4096]);
                        WebSocketReceiveResult result;

                        try
                        {
                            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                        }
                        catch (WebSocketException ex)
                        {
                            Logger.LogExc(ex); // Log and break the loop
                            break;
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            break;
                        }

                        if (buffer.Array == null) continue;
                        if (socket.State != WebSocketState.Open) break;
                        string message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, result.Count);
                        string response = await ProcessMessage(message);

                        if (!string.IsNullOrEmpty(response))
                        {
                            byte[] responseBytes = Encoding.UTF8.GetBytes("Command executed: " + response);
                            await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex); // Fallback logging
                }
                finally
                {
                    if (socket.State != WebSocketState.Closed)
                    {
                        try
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", CancellationToken.None);
                        }
                        catch
                        {
                            // Ignore, it might already be closed
                        }
                    }

                    socket.Dispose();
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

        private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim> _sendLocks = new();

        private async Task SafeSendAsync(WebSocket socket, byte[] data, WebSocketMessageType type)
        {
            var sem = _sendLocks.GetOrAdd(socket, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                if (socket.State == WebSocketState.Open)
                    await socket.SendAsync(new ArraySegment<byte>(data), type, true, CancellationToken.None);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task BroadcastToChannelAsync(string path, string message)
        {
            if (!ChannelClients.TryGetValue(path, out var clients))
                return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (KeyValuePair<Guid, WebSocket> pair in clients.ToArray())
            {
                Guid id = pair.Key;
                WebSocket socket = pair.Value;

                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await SafeSendAsync(socket, messageBytes, WebSocketMessageType.Text);
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

        // ---- New ProcessMessage using the router ----
        private static async Task<string> ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "";

            WebSocketCommand command;
            try
            {
                command = JsonConvert.DeserializeObject<WebSocketCommand>(message);
            }
            catch (JsonException)
            {
                return "Invalid JSON.";
            }

            if (command == null || string.IsNullOrWhiteSpace(command.Action))
                return "Invalid command format.";

            if (CommandMap.TryGetValue(command.Action, out var handler))
            {
                return await handler(command);
            }

            return $"Unknown action: {command.Action}";
        }

        // ---- Command Handlers ----
        private static Task<string> HandleYoutubeAsync(WebSocketCommand command)
        {
            if (command.Data == null) return Task.FromResult("Missing data for youtube.");
            YoutubeData youtubeData = command.Data.ToObject<YoutubeData>();
            if (youtubeData == null) return Task.FromResult("Invalid data for youtube.");
            if (!Equals(GlobalObjects.YoutubeData, youtubeData))
                GlobalObjects.YoutubeData = youtubeData;
            return Task.FromResult(string.Empty);
        }

        private static async Task<string> HandleQueueAddAsync(WebSocketCommand command)
        {
            if (command.Data == null) return "Missing data for queue_add.";
            QueueAddData queueData = command.Data.ToObject<QueueAddData>();
            if (queueData == null) return "Invalid data for queue_add.";

            var ops = GetPlayerOps();
            return await ops.QueueAddAsync(queueData);
        }

        private static async Task<string> HandleVolSetAsync(WebSocketCommand command)
        {
            if (command.Data == null) return "Missing data for vol_set.";
            VolumeData volData = command.Data.ToObject<VolumeData>();
            if (volData == null) return "Invalid data for vol_set.";

            var ops = GetPlayerOps();
            return await ops.SetVolumeAsync(MathUtils.Clamp(volData.Value, 0, 100));
        }

        private static async Task<string> HandleVolumeStepAsync(int change)
        {
            var ops = GetPlayerOps();
            return await ops.VolumeStepAsync(change);
        }

        private static async Task<string> HandleSkipAsync(WebSocketCommand command)
        {
            var ops = GetPlayerOps();
            return await ops.SkipAsync();
        }

        private static async Task<string> HandlePlayPauseAsync(WebSocketCommand command)
        {
            var ops = GetPlayerOps();
            return await ops.TogglePlayPauseAsync(command.Action);
        }

        private static async Task<string> HandleSendToChatAsync(WebSocketCommand command)
        {
            var ops = GetPlayerOps();
            return await ops.SendToChatAsync();
        }

        private static async Task<string> HandleStopSrRewardAsync(WebSocketCommand command)
        {
            foreach (string rewardId in Settings.Settings.TwRewardId)
            {
                await TwitchHandler.TwitchApi.Helix.ChannelPoints.UpdateCustomRewardAsync(
                    Settings.Settings.TwitchUser.Id, rewardId, new UpdateCustomRewardRequest
                    {
                        IsPaused = true
                    }, Settings.Settings.TwitchAccessToken);
            }
            return "Song request rewards stopped.";
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