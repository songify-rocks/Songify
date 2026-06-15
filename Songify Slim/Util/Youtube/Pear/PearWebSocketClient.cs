using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Youtube.Pear
{
    internal static class PearWebSocketClient
    {
        private static readonly Uri PearUri = new("ws://127.0.0.1:26538/api/v1/ws");

        private static ClientWebSocket _socket;
        private static CancellationTokenSource _cts;
        private static readonly object _lock = new();
        private static bool _isConnecting;

        private static Func<string, Task> _messageHandler;

        public static event Action ConnectionStateChanged;

        public static string Endpoint => PearUri.ToString();

        /// <summary>
        /// Controls whether the periodic Pear fetch path is allowed to auto-connect the WebSocket.
        /// </summary>
        /// <remarks>
        /// This is runtime-only UI state (not persisted in settings).
        /// <list type="bullet">
        /// <item><description>Set to <c>true</c> when the user selects Pear as the active player source.</description></item>
        /// <item><description>Set to <c>false</c> when switching away from Pear.</description></item>
        /// <item><description>Temporarily set to <c>false</c> when the user clicks the Pear status indicator to disconnect while Pear remains selected.</description></item>
        /// </list>
        /// When <c>false</c>, <c>SongFetcher.FetchPear()</c> exits before calling <c>ConnectAsync()</c>,
        /// preventing immediate re-connect loops after a manual disconnect.
        /// </remarks>
        public static bool AutoConnectEnabled { get; set; }

        public static bool IsConnected =>
            _socket is { State: WebSocketState.Open };

        public static bool IsConnecting
        {
            get
            {
                lock (_lock)
                {
                    return _isConnecting;
                }
            }
        }

        /// <summary>
        /// Set (replace) the async message handler. This avoids multiple subscriptions.
        /// Pass null to clear.
        /// </summary>
        public static void SetMessageHandler(Func<string, Task> handler)
        {
            lock (_lock)
            {
                _messageHandler = handler;
            }
        }

        /// <summary>
        /// Connect to the Pear WebSocket if not already connected.
        /// Ensures only one connection attempt at a time.
        /// </summary>
        public static async Task ConnectAsync()
        {
            ClientWebSocket socket;
            CancellationTokenSource cts;

            lock (_lock)
            {
                if (_socket is { State: WebSocketState.Open })
                {
                    // Already connected
                    return;
                }

                if (_isConnecting)
                {
                    // Another thread is already connecting; just bail out
                    return;
                }

                _isConnecting = true;
                NotifyConnectionStateChanged();

                socket = new ClientWebSocket();
                cts = new CancellationTokenSource();

                _socket = socket;
                _cts = cts;
            }

            try
            {
                await socket.ConnectAsync(PearUri, cts.Token).ConfigureAwait(false);
                NotifyConnectionStateChanged();

                // Start background receive loop for THIS socket/cts pair
                _ = Task.Run(() => ReceiveLoop(socket, cts));
            }
            catch
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                lock (_lock)
                {
                    _isConnecting = false;
                }

                NotifyConnectionStateChanged();
            }
        }

        /// <summary>
        /// Disconnect from Pear and stop the receive loop.
        /// </summary>
        public static async Task DisconnectAsync()
        {
            ClientWebSocket socket;
            CancellationTokenSource cts;

            lock (_lock)
            {
                socket = _socket;
                cts = _cts;
                _socket = null;
                _cts = null;
            }

            if (cts is { IsCancellationRequested: false })
            {
                cts.Cancel();
            }

            if (socket != null)
            {
                try
                {
                    if (socket.State == WebSocketState.Open ||
                        socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client disconnect",
                            CancellationToken.None
                        ).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // ignore close errors
                }
                finally
                {
                    socket.Dispose();
                }
            }

            NotifyConnectionStateChanged();
        }

        private static async Task ReceiveLoop(ClientWebSocket socket, CancellationTokenSource cts)
        {
            try
            {
                ArraySegment<byte> buffer = new(new byte[8192]);

                while (socket.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    using MemoryStream ms = new();
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, cts.Token)
                            .ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            try
                            {
                                await socket.CloseAsync(
                                    WebSocketCloseStatus.NormalClosure,
                                    "Server closed",
                                    CancellationToken.None
                                ).ConfigureAwait(false);
                            }
                            catch
                            {
                                // ignore
                            }
                            return;
                        }

                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    string message = Encoding.UTF8.GetString(ms.ToArray());

                    Func<string, Task> handler;
                    lock (_lock)
                    {
                        handler = _messageHandler;
                    }

                    if (handler != null)
                    {
                        try
                        {
                            await handler(message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(LogSource.Pear, "Pear WebSocket message handler failed", ex);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Debug(LogSource.Pear, "Pear WebSocket receive loop cancelled.");
            }
            catch (ObjectDisposedException)
            {
                Logger.Debug(LogSource.Pear, "Pear WebSocket receive loop ended (socket disposed).");
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Pear, "Pear WebSocket receive loop error", ex);
            }
            finally
            {
                NotifyConnectionStateChanged();
            }
        }

        private static void NotifyConnectionStateChanged()
        {
            try
            {
                ConnectionStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Pear, "Pear ConnectionStateChanged subscriber failed", ex);
            }
        }
    }
}