using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using System.Web;
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

            Logger.LogStr("Disconnected from Twitch");
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
            Logger.LogStr("Connected to Twitch");
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
                    string TrackID = e.ChatMessage.Message.Replace("spotify:track:", "");

                    AddSong(TrackID, e);
                }
                else
                {
                    SpotifyAPI.Web.Models.SearchItem searchItem = APIHandler.FindTrack(e.ChatMessage.Message);
                    SpotifyAPI.Web.Models.FullTrack fullTrack = searchItem.Tracks.Items[0];

                    AddSong(fullTrack.Id, e);
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

                AddSong(trackID, e);

                onCooldown = true;
                cooldownTimer.Interval = TimeSpan.FromSeconds(Settings.TwSRCooldown).TotalMilliseconds;
                cooldownTimer.Start();
                return;
            }

            Console.WriteLine(e.ChatMessage.RawIrcMessage);
        }

        private static void AddSong(string trackID, OnMessageReceivedArgs e)
        {
            SpotifyAPI.Web.Models.FullTrack track = APIHandler.GetTrack(trackID);

            if (track.DurationMs >= TimeSpan.FromMinutes(10).TotalMilliseconds)
            {
                _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " the song you requested exceeded the maximum song length (10 minutes)");
                return;
            }

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

            string SpotifyURI = "spotify:track:" + trackID;

            SpotifyAPI.Web.Models.ErrorResponse error = APIHandler.AddToQ(SpotifyURI);
            if (error.Error != null)
            {
                Logger.LogStr(error.Error.Message + "\n" + error.Error.Status);
                _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " there was an error adding your Song to the queue. Error message: " + error.Error.Message);
                return;
            }

            _client.SendMessage(e.ChatMessage.Channel, track.Artists[0].Name + " - " + track.Name + " requested by @" + e.ChatMessage.DisplayName + " has been added to the queue");

            UploadToQueue(track, e.ChatMessage.DisplayName);

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
        }

        private static void UploadToQueue(FullTrack track, string displayName)
        {
            try
            {
                string artists = "";

                for (int i = 0; i < track.Artists.Count; i++)
                {
                    if (i != track.Artists.Count - 1)
                        artists += track.Artists[i].Name + ", ";
                    else
                        artists += track.Artists[i].Name;
                }
                string minutes, seconds;

                TimeSpan t = TimeSpan.FromMilliseconds(track.DurationMs);
                minutes = t.Minutes.ToString();

                if (t.Seconds < 10)
                {
                    seconds = "0" + t.Seconds;
                }
                else
                {
                    seconds = t.Seconds.ToString();
                }

                string length = minutes + seconds;


                string extras = Settings.Uuid +
                    "&trackid=" + HttpUtility.UrlEncode(track.Id) +
                    "&artist=" + HttpUtility.UrlEncode(artists) +
                    "&title=" + HttpUtility.UrlEncode(track.Name) +
                    "&length=" + HttpUtility.UrlEncode(length) +
                    "&requester=" + displayName +
                    "&played=" + "0" +
                    "&o=" + "i";

                string url = "http://songify.bloemacher.com/add_queue.php/?id=" + extras;


                Console.WriteLine(url);
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            (window as MainWindow).LblStatus.Content = "Error Uploading Queue";
                        }
                    }
                }));
                Logger.LogExc(ex);
            }
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
