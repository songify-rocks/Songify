using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using Songify_Slim.Models;
using SpotifyAPI.Web.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding to twitch.tv
    public static class TwitchHandler
    {
        public static TwitchClient Client;
        private static bool _onCooldown;

        private static readonly Timer CooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds
        };

        public static void BotConnect()
        {
            try
            {
                // Checks if twitch credentials are present
                if (string.IsNullOrEmpty(Settings.Settings.TwAcc) || string.IsNullOrEmpty(Settings.Settings.TwOAuth) ||
                    string.IsNullOrEmpty(Settings.Settings.TwChannel))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                            if (window.GetType() == typeof(MainWindow))
                                //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                                ((MainWindow)window).LblStatus.Content = "Please fill in Twitch credentials.";
                    });
                    return;
                }

                // creates new connection based on the credentials in settings
                ConnectionCredentials credentials =
                    new ConnectionCredentials(Settings.Settings.TwAcc, Settings.Settings.TwOAuth);
                ClientOptions clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                WebSocketClient customClient = new WebSocketClient(clientOptions);
                Client = new TwitchClient(customClient);
                Client.Initialize(credentials, Settings.Settings.TwChannel);

                Client.OnMessageReceived += _client_OnMessageReceived;
                Client.OnConnected += _client_OnConnected;
                Client.OnDisconnected += _client_OnDisconnected;

                Client.Connect();

                // subscirbes to the cooldowntimer elapsed event for the command cooldown
                CooldownTimer.Elapsed += CooldownTimer_Elapsed;
            }
            catch (Exception)
            {
                Logger.LogStr("TWITCH: Couldn't connect to Twitch, maybe credentials are wrong?");
            }
        }

        private static void _client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            // Disconnected
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(MainWindow))
                        continue;
                    //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                    ((MainWindow)window).LblStatus.Content = "Disconnected from Twitch";
                    ((MainWindow)window).mi_TwitchConnect.IsEnabled = true;
                    ((MainWindow)window).mi_TwitchDisconnect.IsEnabled = false;
                }
            });

            Logger.LogStr("TWITCH: Disconnected from Twitch");
        }

        private static void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Resets the cooldown for the !ssr command
            _onCooldown = false;
            CooldownTimer.Stop();
        }

        private static void _client_OnConnected(object sender, OnConnectedArgs e)
        {
            // Connected
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(MainWindow))
                        continue;

                    //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Green);
                    ((MainWindow)window).LblStatus.Content = "Connected to Twitch";
                    ((MainWindow)window).mi_TwitchConnect.IsEnabled = false;
                    ((MainWindow)window).mi_TwitchDisconnect.IsEnabled = true;
                }
            });
            Logger.LogStr("TWITCH: Connected to Twitch");
        }

        private static void _client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (Settings.Settings.MsgLoggingEnabled)
                // If message logging is enabled and the reward was triggered, save it to the settings (if settings window is open, write it to the textbox)
                if (e.ChatMessage.CustomRewardId != null)
                {
                    Settings.Settings.TwRewardId = e.ChatMessage.CustomRewardId;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                            if (window.GetType() == typeof(Window_Settings))
                            {
                                ((Window_Settings)window).txtbx_RewardID.Text = e.ChatMessage.CustomRewardId;
                                ((Window_Settings)window).Chbx_MessageLogging.IsChecked = false;
                            }
                    });
                }

            // if the reward is the same with the desired reward for the requests 
            if (Settings.Settings.TwSrReward && e.ChatMessage.CustomRewardId == Settings.Settings.TwRewardId)
            {
                if (IsUserBlocked(e.ChatMessage.DisplayName))
                {
                    Client.SendWhisper(e.ChatMessage.Username, "You are blocked from making Songrequests");
                    return;
                }

                if (ApiHandler.Spotify == null)
                {
                    Client.SendMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                // if Spotify is connected and working manipulate the string and call methods to get the song info accordingly
                if (e.ChatMessage.Message.StartsWith("spotify:track:"))
                {
                    // search for a track with the id
                    string trackId = e.ChatMessage.Message.Replace("spotify:track:", "");

                    // add the track to the spotify queue and pass the OnMessageReceivedArgs (contains user who requested the song etc)
                    AddSong(trackId, e);
                }

                else if (e.ChatMessage.Message.StartsWith("https://open.spotify.com/track/"))
                {
                    string trackid = e.ChatMessage.Message.Replace("https://open.spotify.com/track/", "");
                    trackid = trackid.Split('?')[0];
                    AddSong(trackid, e);
                }

                else
                {
                    // search for a track with a search string from chat
                    SearchItem searchItem = ApiHandler.FindTrack(e.ChatMessage.Message);
                    if (searchItem.Tracks.Items.Count > 0)
                    {
                        // if a track was found convert the object to FullTrack (easier use than searchItem)
                        FullTrack fullTrack = searchItem.Tracks.Items[0];

                        // add the track to the spotify queue and pass the OnMessageReceivedArgs (contains user who requested the song etc)
                        AddSong(fullTrack.Id, e);
                    }
                    else
                    {
                        // if no track has been found inform the requester
                        string response = Settings.Settings.BotRespError;
                        response = response.Replace("{user}", e.ChatMessage.DisplayName);
                        response = response.Replace("{artist}", "");
                        response = response.Replace("{title}", "");
                        response = response.Replace("{maxreq}", "");
                        response = response.Replace("{errormsg}", "Couldn't find a song matching your request.");

                        Client.SendMessage(e.ChatMessage.Channel, response);
                        return;
                    }
                }

                return;
            }

            // Same code from above but it reacts to a command instead of rewards
            if (Settings.Settings.TwSrCommand && e.ChatMessage.Message.StartsWith("!ssr"))
            {
                // Do nothing if the user is blocked, don't even reply
                if (IsUserBlocked(e.ChatMessage.DisplayName))
                {
                    Client.SendWhisper(e.ChatMessage.DisplayName, "You are blocked from making Songrequests");
                    return;
                }

                // if onCooldown skip
                if (_onCooldown) return;

                if (ApiHandler.Spotify == null)
                {
                    Client.SendMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                // if Spotify is connected and working manipulate the string and call methods to get the song info accordingly
                string[] msgSplit = e.ChatMessage.Message.Split(' ');

                // Prevent crash on command without args
                if (msgSplit.Length <= 1)
                {
                    string response = Settings.Settings.BotRespNoSong;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    Client.SendMessage(e.ChatMessage.Channel, response);

                    StartCooldown();
                    return;
                }

                if (msgSplit[1].StartsWith("spotify:track:"))
                {
                    // search for a track with the id
                    string trackId = msgSplit[1].Replace("spotify:track:", "");
                    // add the track to the spotify queue and pass the OnMessageReceivedArgs (contains user who requested the song etc)
                    AddSong(trackId, e);
                }

                else if (msgSplit[1].StartsWith("https://open.spotify.com/track/"))
                {
                    string trackid = msgSplit[1].Replace("https://open.spotify.com/track/", "");
                    trackid = trackid.Split('?')[0];
                    AddSong(trackid, e);
                }
                else
                {
                    string searchString = e.ChatMessage.Message.Replace("!ssr ", "");
                    // search for a track with a search string from chat
                    SearchItem searchItem = ApiHandler.FindTrack(searchString);
                    if (searchItem.Tracks.Items.Count > 0)
                    {
                        // if a track was found convert the object to FullTrack (easier use than searchItem)
                        FullTrack fullTrack = searchItem.Tracks.Items[0];
                        // add the track to the spotify queue and pass the OnMessageReceivedArgs (contains user who requested the song etc)
                        AddSong(fullTrack.Id, e);
                    }
                    else
                    {
                        // if no track has been found inform the requester
                        string response = Settings.Settings.BotRespError;
                        response = response.Replace("{user}", e.ChatMessage.DisplayName);
                        response = response.Replace("{artist}", "");
                        response = response.Replace("{title}", "");
                        response = response.Replace("{maxreq}", "");
                        response = response.Replace("{errormsg}", "Couldn't find a song matching your request.");

                        Client.SendMessage(e.ChatMessage.Channel, response);
                        return;
                    }
                }

                // start the command cooldown
                StartCooldown();
            }

            if (e.ChatMessage.Message.StartsWith("!song") && Settings.Settings.BotCmdSong)
            {
                string currsong = GetCurrentSong();
                Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {currsong}");
            }

            if (e.ChatMessage.Message == "!pos" && Settings.Settings.BotCmdPos)
            {
                List<QueueItem> queueItems = GetQueueItems(e.ChatMessage.DisplayName);
                string output = "";
                if (queueItems.Count != 0)
                {
                    for (int i = 0; i < queueItems.Count; i++)
                    {
                        QueueItem item = queueItems[i];
                        output += $"Pos {item.position}: {item.title}";
                        if (i + 1 != queueItems.Count)
                            output += " | ";
                    }
                    Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {output}");
                }
                else
                {
                    Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} you have no Songs in the current Queue");
                }
            }

            if (e.ChatMessage.Message == "!next" && Settings.Settings.BotCmdNext)
            {
                List<QueueItem> queueItems = GetQueueItems();
                if (queueItems != null)
                {
                    Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {queueItems[0].title}");
                }
                else
                {
                    Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} there is no song next up.");
                }
            }
        }

        private static string GetCurrentSong()
        {
            string tmp = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        tmp = (window as MainWindow)?.CurrSong;
                    }
                }
            });
            return tmp;
        }

        private static bool IsUserBlocked(string displayName)
        {
            string[] userBlacklist = Settings.Settings.UserBlacklist.Split(new[] { "|||" }, StringSplitOptions.None);

            // checks if one of the artist in the requested song is on the blacklist
            return userBlacklist.Any(s => s == displayName);
        }

        private static void StartCooldown()
        {
            // starts the cooldown on the command
            _onCooldown = true;
            CooldownTimer.Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds;
            CooldownTimer.Start();
        }

        private static string CleanFormatString(string currSong)
        {
            const RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            currSong = regex.Replace(currSong, " ");
            currSong = currSong.Trim();
            // Add trailing spaces for better scroll
            return currSong;
        }

        private static void AddSong(string trackId, OnMessageReceivedArgs e)
        {
            // loads the blacklist from settings
            string[] blacklist = Settings.Settings.ArtistBlacklist.Split(new[] { "|||" }, StringSplitOptions.None);
            string response;
            // gets the track information using spotify api
            FullTrack track = ApiHandler.GetTrack(trackId);
            string artists = "";
            for (int i = 0; i < track.Artists.Count; i++)
                if (i != track.Artists.Count - 1)
                    artists += track.Artists[i].Name + ", ";
                else
                    artists += track.Artists[i].Name;

            // checks if one of the artist in the requested song is on the blacklist
            foreach (string s in blacklist)
                if (Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1)
                {
                    // if artist is on blacklist, skip and inform requester
                    response = Settings.Settings.BotRespBlacklist;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", s);
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", "");
                    response = response.Replace("{errormsg}", "");
                    response = CleanFormatString(response);

                    Client.SendMessage(e.ChatMessage.Channel, response);
                    return;
                }

            // checks if song length is longer or equal to 10 minutes
            if (track.DurationMs >= TimeSpan.FromMinutes(Settings.Settings.MaxSongLength).TotalMilliseconds)
            {
                // if track length exceeds 10 minutes skip and inform requster
                response = Settings.Settings.BotRespLength;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);

                Client.SendMessage(e.ChatMessage.Channel, response);
                return;
            }

            // checks if the song is already in the queue
            if (IsInQueue(track.Id))
            {
                // if the song is already in the queue skip and inform requester
                response = Settings.Settings.BotRespIsInQueue;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);

                Client.SendMessage(e.ChatMessage.Channel, response);
                return;
            }

            // checks if the user has already the max amount of songs in the queue
            if (MaxQueueItems(e.ChatMessage.DisplayName))
            {
                // if the user reached max requests in the queue skip and inform requester
                response = Settings.Settings.BotRespMaxReq;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", Settings.Settings.TwSrMaxReq.ToString());
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);
                Client.SendMessage(e.ChatMessage.Channel, response);
                return;
            }

            // generate the spotifyURI using the track id
            string spotifyUri = "spotify:track:" + trackId;

            // try adding the song to the queue using the URI
            ErrorResponse error = ApiHandler.AddToQ(spotifyUri);
            if (error.Error != null)
            {
                // if an error has been encountered, log it, inform the requester and skip 
                Logger.LogStr("TWITCH: " + error.Error.Message + "\n" + error.Error.Status);
                response = Settings.Settings.BotRespError;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", error.Error.Message);

                Client.SendMessage(e.ChatMessage.Channel, response);
                return;
            }

            // if everything worked so far, inform the user that the song has been added to the queue
            response = Settings.Settings.BotRespSuccess;
            response = response.Replace("{user}", e.ChatMessage.DisplayName);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", "");
            Client.SendMessage(e.ChatMessage.Channel, response);

            // Upload the track and who requested it to the queue on the server
            UploadToQueue(track, e.ChatMessage.DisplayName);

            // Add the song to the internal queue and update the queue window if its open
            Application.Current.Dispatcher.Invoke(() =>
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
                    (mw as MainWindow)?.ReqList.Add(new RequestObject
                    {
                        Requester = e.ChatMessage.DisplayName,
                        TrackID = track.Id,
                        Title = track.Name,
                        Artists = artists,
                        Length = FormattedTime(track.DurationMs)
                    });

                if (qw != null)
                    //(qw as Window_Queue).dgv_Queue.ItemsSource.
                    (qw as Window_Queue)?.dgv_Queue.Items.Refresh();
            });
        }

        private static string FormattedTime(int duration)
        {
            // duration in milliseconds gets converted to mm:ss
            string seconds;

            TimeSpan t = TimeSpan.FromMilliseconds(duration);
            string minutes = t.Minutes.ToString();

            if (t.Seconds < 10)
                seconds = "0" + t.Seconds;
            else
                seconds = t.Seconds.ToString();

            return minutes + ":" + seconds;
        }

        private static void UploadToQueue(FullTrack track, string displayName)
        {
            string artists = "";
            int counter = 0;
            // put all artists from the song in one string
            foreach (SimpleArtist artist in track.Artists.Where(artist => counter <= 3))
            {
                artists += artist.Name + ", ";
                counter++;
            }

            // remove the last ", "
            artists = artists.Remove(artists.Length - 2, 2);

            string length = FormattedTime(track.DurationMs);

            // upload tot the queue
            WebHelper.UpdateWebQueue(track.Id, artists, track.Name, length, displayName, "0", "i");
        }

        private static bool IsInQueue(string id)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            var temp = new List<RequestObject>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                        temp = (window as MainWindow)?.ReqList.FindAll(x => x.TrackID == id);
            });
            return temp.Count > 0;
        }

        private static List<QueueItem> GetQueueItems(string requester = null)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<RequestObject> temp = new List<RequestObject>();
            List<QueueItem> temp3 = new List<QueueItem>();
            string currsong = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                    {
                        temp = (window as MainWindow)?.ReqList;
                        currsong = $"{(window as MainWindow)?._artist} - {(window as MainWindow)?._title}";
                    }
            });

            if (requester != null)
            {
                List<RequestObject> temp2 = temp.FindAll(x => x.Requester == requester);
                foreach (RequestObject requestObject in temp2)
                {
                    int pos = temp.IndexOf(requestObject) + 1;
                    temp3.Add(new QueueItem
                    {
                        position = pos,
                        title = requestObject.Artists + " - " + requestObject.Title,
                        requester = requestObject.Requester
                    });
                }
                return temp3;
            }
            else
            {
                if (temp.Count > 0)
                {
                    if (temp.Count == 1 && $"{temp[0].Artists} - {temp[0].Title}" != currsong)
                    {
                        temp3.Add(new QueueItem
                        {
                            title = $"{temp[0].Artists} - {temp[0].Title}",
                            requester = $"{temp[0].Requester}"
                        });
                        return temp3;
                    }
                    else if (temp.Count > 1)
                    {
                        temp3.Add(new QueueItem
                        {
                            title = $"{temp[1].Artists} - {temp[1].Title}",
                            requester = $"{temp[1].Requester}"
                        });
                        return temp3;
                    }
                }
            }
            return null;
        }

        private static bool MaxQueueItems(string requester)
        {
            // Checks if the requester already reached max songrequests
            var temp = new List<RequestObject>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                        temp = (window as MainWindow)?.ReqList.FindAll(x => x.Requester == requester);
            });

            return temp.Count >= Settings.Settings.TwSrMaxReq;
        }

        public static void SendCurrSong(string song)
        {
            if (Client != null && Client.IsConnected)
                Client.SendMessage(Settings.Settings.TwChannel, song);
        }
    }

    class QueueItem
    {
        public string requester { get; set; }
        public string title { get; set; }
        public int position { get; set; }
    }
}