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
using System.Linq;

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
            if (string.IsNullOrEmpty(Settings.TwAcc) || string.IsNullOrEmpty(Settings.TwOAuth) || string.IsNullOrEmpty(Settings.TwChannel))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                            (window as MainWindow).LblStatus.Content = "Please fill in Twitch credentials.";
                        }
                    }
                }));
                return;
            }

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
                    if (window.GetType() != typeof(MainWindow))
                        continue;

                    //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                    (window as MainWindow).LblStatus.Content = "Disconnected from Twitch";
                    (window as MainWindow).mi_TwitchConnect.IsEnabled = true;
                    (window as MainWindow).mi_TwitchDisconnect.IsEnabled = false;

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
                                if (window.GetType() != typeof(MainWindow))
                                    continue;

                                //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Green);
                                (window as MainWindow).LblStatus.Content = "Connected to Twitch";
                                (window as MainWindow).mi_TwitchConnect.IsEnabled = false;
                                (window as MainWindow).mi_TwitchDisconnect.IsEnabled = true;
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
                    Settings.TwRewardID = e.ChatMessage.CustomRewardId;

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
                if (APIHandler.spotify == null)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                if (e.ChatMessage.Message.StartsWith("spotify:track:"))
                {
                    string TrackID = e.ChatMessage.Message.Replace("spotify:track:", "");

                    AddSong(TrackID, e);
                }
                else
                {
                    SearchItem searchItem = APIHandler.FindTrack(e.ChatMessage.Message);
                    if (searchItem.Tracks.Items.Count > 0)
                    {
                        FullTrack fullTrack = searchItem.Tracks.Items[0];
                        AddSong(fullTrack.Id, e);
                    }
                    else
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " there was an error adding your Song to the queue. Couldn't find a song matching your request.");
                    }
                }
                return;
            }

            if (Settings.TwSRCommand && e.ChatMessage.Message.StartsWith("!ssr"))
            {
                if (onCooldown)
                {
                    return;
                }

                if (APIHandler.spotify == null)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                string[] msgSplit = e.ChatMessage.Message.Split(' ');

                // Prevent crash on command without args
                if (msgSplit.Length <= 1)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " please specify a song to add to the queue.");
                    StartCooldown();
                    return;
                }
                if (msgSplit[1].StartsWith("spotify:track:"))
                {
                    string trackID = msgSplit[1].Replace("spotify:track:", "");
                    AddSong(trackID, e);
                }
                else
                {
                    string searchString = e.ChatMessage.Message.Replace("!ssr ", "");
                    SearchItem searchItem = APIHandler.FindTrack(searchString);
                    if (searchItem.Tracks.Items.Count > 0)
                    {
                        FullTrack fullTrack = searchItem.Tracks.Items[0];
                        AddSong(fullTrack.Id, e);
                    }
                    else
                    {
                        _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " there was an error adding your Song to the queue. Couldn't find a song matching your request.");
                    }
                }

                StartCooldown();
                return;
            }

            Console.WriteLine(e.ChatMessage.RawIrcMessage);
        }

        private static void StartCooldown()
        {
            onCooldown = true;
            cooldownTimer.Interval = TimeSpan.FromSeconds(Settings.TwSRCooldown).TotalMilliseconds;
            cooldownTimer.Start();
        }

        private static void AddSong(string trackID, OnMessageReceivedArgs e)
        {
            string[] Blacklist = Settings.ArtistBlacklist.Split(new[] { "|||" }, StringSplitOptions.None);

            FullTrack track = APIHandler.GetTrack(trackID);

            foreach (string s in Blacklist)
            {
                if (Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1)
                {
                    _client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.DisplayName + " the Artist: " + s + " has been blacklisted by the broadcaster.");
                    return;
                }
            }

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
                Window mw = null, qw = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                        mw = window;
                    if (window.GetType() == typeof(Window_Queue))
                        qw = window;
                }
                if (mw != null)
                    (mw as MainWindow).ReqList.Add(new RequestObject
                    {
                        Requester = e.ChatMessage.DisplayName,
                        TrackID = track.Id,
                        Title = track.Name,
                        Artists = track.Artists[0].Name,
                        Length = FormattedTime(track.DurationMs)
                    });

                if (qw != null)
                {
                    //(qw as Window_Queue).dgv_Queue.ItemsSource.
                    (qw as Window_Queue).dgv_Queue.Items.Refresh();
                }
            }));
        }

        public static string FormattedTime(int duration)
        {
            string minutes, seconds;

            TimeSpan t = TimeSpan.FromMilliseconds(duration);
            minutes = t.Minutes.ToString();

            if (t.Seconds < 10)
            {
                seconds = "0" + t.Seconds;
            }
            else
            {
                seconds = t.Seconds.ToString();
            }

            return minutes + ":" + seconds;
        }

        private static void UploadToQueue(FullTrack track, string displayName)
        {
            string artists = "";
            int counter = 0;
            foreach (SimpleArtist artist in track.Artists)
            {
                if (counter <= 3)
                {
                    artists += artist.Name + ", ";
                    counter++;
                }
                else
                {
                    continue;
                }
            }
            artists = artists.Remove(artists.Length - 2, 2);

            string length = FormattedTime(track.DurationMs);

            WebHelper.UpdateWebQueue(track.Id, artists, track.Name, length, displayName, "0", "i");
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
