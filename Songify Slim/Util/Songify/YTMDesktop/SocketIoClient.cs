using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ControlzEx.Standard;
using Newtonsoft.Json;
using SocketIOClient;
using Songify_Slim.Models.YTMD;
using Songify_Slim.Views;
using Unosquare.Swan.Formatters;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Songify.YTMDesktop
{
    public class SocketIoClient(string url, string token)
    {
        private readonly SocketIOClient.SocketIO _client = new(url, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket, // WebSocket only
            Auth = new { token },// Pass the token in the auth object
            ConnectionTimeout = new TimeSpan(0, 0, 0, 5)
        });

        private YTMDResponse prevResponse = new YTMDResponse();
        private DateTime _lastUpdateTime = DateTime.MinValue; // To track the last processed time
        private readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(0.5); // Throttle interval


        // Initialize the Socket.IO client with options
        // WebSocket only
        // Pass the token in the auth object

        /// <summary>
        /// Connects to the Socket.IO server and sets up event listeners.
        /// </summary>
        public async Task ConnectAsync()
        {
            // Handle connection success
            _client.OnConnected += async (sender, e) =>
            {
                Debug.WriteLine("Connected to the server.");
                Logger.LogStr("YTMD: Connected to the server.");
            };

            // Listen for the 'state-update' event
            _client.On("state-update", async response =>
            {
                if (Settings.Settings.Player != 6)
                    return;



                _lastUpdateTime = DateTime.Now; // Update the timestamp

                // Process the response
                try
                {
                    string res = response.ToString();
                    //Debug.WriteLine(res);
                    List<YTMDResponse> yTmdResponseList = JsonConvert.DeserializeObject<List<YTMDResponse>>(res);
                    YTMDResponse yTmdResponse = yTmdResponseList.First();
                    // Check if enough time has passed since the last update
                    if (prevResponse.Player != null && yTmdResponse.Player.TrackState == prevResponse.Player.TrackState)
                        if (DateTime.Now - _lastUpdateTime < _throttleInterval)
                            return;
                    // Update the UI using the dispatcher
                    prevResponse = yTmdResponse;
                    await Application.Current.Dispatcher.Invoke(async () => await ((MainWindow)Application.Current.MainWindow)?.Sf.FetchYTM(yTmdResponse)!);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing state-update: {ex.Message}");
                }
            });

            _client.OnDisconnected += async (sender, e) =>
            {
                Debug.WriteLine("Disconnected from the server.");
                Logger.LogStr("YTMD: Disconnected from websocket server");
            };

            _client.OnError += async (sender, e) =>
            {
                Debug.WriteLine("Connection error: " + e);
                Logger.LogStr($"YTMD: {e}");
            };

            try
            {
                // Attempt to connect to the server
                await _client.ConnectAsync();
                Debug.WriteLine("Connection established!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnects from the Socket.IO server.
        /// </summary>
        public async Task DisconnectAsync()
        {
            await _client.DisconnectAsync();
            Debug.WriteLine("Disconnected from the server.");
        }

        /// <summary>
        /// Emits an event to the server.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="data">The data to send with the event.</param>
        public async Task EmitAsync(string eventName, object data)
        {
            await _client.EmitAsync(eventName, data);
            Debug.WriteLine($"Event '{eventName}' emitted with data: {data}");
        }
    }
}
