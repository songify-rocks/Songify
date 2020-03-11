using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Songify_Slim
{
    public static class TwitchHandler
    {
        public static TwitchClient _client;
        public static bool onCooldown = false;
        public static Timer cooldownTimer = new Timer
        {
           Interval = TimeSpan.FromSeconds(Settings.TwSRCooldown).TotalMilliseconds,           
        };

        public static void BotConnect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Settings.TwAcc, Settings.TwOAuth);
            ClientOptions clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, Settings.TwChannel);

            _client.OnLog += _client_OnLog;
            _client.OnJoinedChannel += _client_OnJoinedChannel;
            _client.OnMessageReceived += _client_OnMessageReceived;
            _client.OnWhisperReceived += _client_OnWhisperReceived;
            _client.OnConnected += _client_OnConnected;
            _client.OnDisconnected += _client_OnDisconnected;
            _client.Connect();

            cooldownTimer.Elapsed += CooldownTimer_Elapsed;
        }

        private static void _client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        (window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                        (window as MainWindow).LblStatus.Content = "Disconnected from Twitch";
                    }
                }

            }));
        }

        private static void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            onCooldown = false;
            cooldownTimer.Stop();
        }

        private static void _client_OnConnected(object sender, OnConnectedArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.GetType() == typeof(MainWindow))
                                {
                                    (window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Green);
                                    (window as MainWindow).LblStatus.Content = "Connected to Twitch";
                                }
                            }

                        }));
        }

        private static void _client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
        }

        private static void _client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {

            if (Settings.MsgLoggingEnabled)
            {
                if (e.ChatMessage.CustomRewardId != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() == typeof(SettingsWindow))
                            {
                                (window as SettingsWindow).txtbx_RewardID.Text = e.ChatMessage.CustomRewardId;
                            }
                        }

                    }));
                }
            }

            if (Settings.TwSRReward && e.ChatMessage.CustomRewardId == Settings.TwRewardID)
            {
                if (e.ChatMessage.Message.StartsWith("spotify:track:"))
                {

                    SpotifyAPI.Web.Models.FullTrack track = APIHandler.GetTrack(e.ChatMessage.Message.Replace("spotify:track:", ""));
                    if (isInQueue(track.Id))
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " this song is already in the queue.");
                        return;
                    }

                    if (MaxQueueItems(e.ChatMessage.DisplayName))
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " maximum number of songs in queue reached (" + Settings.TwSRMaxReq + ").");
                        return;
                    }

                    SpotifyAPI.Web.Models.ErrorResponse error = APIHandler.AddToQ(e.ChatMessage.Message);
                    if (error.Error != null)
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " there was an error adding your Song to the queue. Error message: " + error.Error.Message);
                        return;
                    }

                    _client.SendMessage(e.ChatMessage.Channel, track.Artists[0].Name + " - " + track.Name + " requested by @" + e.ChatMessage.DisplayName + " has been added to the queue");

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() == typeof(MainWindow))
                            {
                                (window as MainWindow).ReqList.Add(new RequestObject
                                {
                                    Requester = e.ChatMessage.DisplayName,
                                    TrackID = e.ChatMessage.Message.Replace("spotify:track:", "")
                                });
                            }
                        }

                    }));
                }
                return;
            }

            if (Settings.TwSRCommand && e.ChatMessage.Message.StartsWith("!ssr"))
            {
                if (onCooldown)
                {
                    return;
                }

                string[] msgSplit = e.ChatMessage.Message.Split(' ');
                string trackID = msgSplit[1].Replace("spotify:track:", "");

                SpotifyAPI.Web.Models.FullTrack track = APIHandler.GetTrack(trackID);
                if (isInQueue(track.Id))
                {
                    _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " this song is already in the queue.");
                    return;
                }

                if (MaxQueueItems(e.ChatMessage.DisplayName))
                {
                    _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " maximum number of songs in queue reached (" + Settings.TwSRMaxReq + ").");
                    return;
                }

                SpotifyAPI.Web.Models.ErrorResponse error = APIHandler.AddToQ(msgSplit[1]);
                if (error.Error != null)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " there was an error adding your Song to the queue. Error message: " + error.Error.Message);
                    return;
                }

                _client.SendMessage(e.ChatMessage.Channel, track.Artists[0].Name + " - " + track.Name + " requested by @" + e.ChatMessage.DisplayName + " has been added to the queue");

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            (window as MainWindow).ReqList.Add(new RequestObject
                            {
                                Requester = e.ChatMessage.DisplayName,
                                TrackID = trackID
                            });
                        }
                    }
                }));
                onCooldown = true;
                cooldownTimer.Interval = TimeSpan.FromSeconds(Settings.TwSRCooldown).TotalMilliseconds;
                cooldownTimer.Start();
                return;
            }

            Console.WriteLine(e.ChatMessage.RawIrcMessage);
        }

        private static bool isInQueue(string id)
        {
            List<RequestObject> temp = new List<RequestObject>();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        temp = (window as MainWindow).ReqList.FindAll(x => x.TrackID == id);
                    }
                }
            }));

            if (temp.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static bool MaxQueueItems(string requester)
        {
            List<RequestObject> temp = new List<RequestObject>();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        temp = (window as MainWindow).ReqList.FindAll(x => x.Requester == requester);
                    }
                }
            }));

            if (temp.Count < Settings.TwSRMaxReq)
            {
                return false;
            }

            return true;
        }

        private static void _client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
        }

        private static void _client_OnLog(object sender, OnLogArgs e)
        {

        }
    }
}
