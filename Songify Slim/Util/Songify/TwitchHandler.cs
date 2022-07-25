using Songify_Slim.Models;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
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
        //create a list with Twitch UserTypes and assign int values to them 
        public enum TwitchUserLevels
        {
            Everyone = 0,
            Vip = 1,
            Subscriber = 2,
            Moderator = 3,
            Broadcaster = 4
        }

        public static TwitchClient Client;
        private static bool _onCooldown;
        private static bool _skipCooldown;
        public static bool ForceDisconnect;
        private static List<string> SkipVotes = new List<string>();
        private static readonly Timer CooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds
        };
        private static readonly Timer SkipCooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(5).TotalMilliseconds
        };

        public static void ResetVotes()
        {
            SkipVotes.Clear();
            Console.WriteLine("Reset votes");
        }

        public static void BotConnect()
        {
            if (Client != null && Client.IsConnected)
                return;
            if (Client != null && !Client.IsConnected)
                Client.Connect();
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

                Client.OnMessageReceived += Client_OnMessageReceived;
                Client.OnConnected += Client_OnConnected;
                Client.OnDisconnected += Client_OnDisconnected;

                Client.Connect();

                // subscirbes to the cooldowntimer elapsed event for the command cooldown
                CooldownTimer.Elapsed += CooldownTimer_Elapsed;
                SkipCooldownTimer.Elapsed += SkipCooldownTimer_Elapsed;
            }
            catch (Exception)
            {
                Logger.LogStr("TWITCH: Couldn't connect to Twitch, maybe credentials are wrong?");
            }
        }

        private static void SkipCooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _skipCooldown = false;
            SkipCooldownTimer.Stop();
        }

        private static void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
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
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[0].Enabled = true;
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[1].Enabled = false;
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

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
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
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[0].Enabled = false;
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[1].Enabled = true;
                }
            });
            Logger.LogStr("TWITCH: Connected to Twitch");
        }

        private static async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
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
                int userlevel = CheckUserLevel(e.ChatMessage);
                if (userlevel < Settings.Settings.TwSrUserLevel)
                {
                    //Send a Message to the user, that his Userlevel is too low
                    Client.SendMessage(e.ChatMessage.Channel, $"Sorry, only {Enum.GetName(typeof(TwitchUserLevels), Settings.Settings.TwSrUserLevel)} or higher can request songs.");
                    return;
                }

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
                int userlevel = CheckUserLevel(e.ChatMessage);
                if (userlevel < Settings.Settings.TwSrUserLevel)
                {
                    //Send a Message to the user, that his Userlevel is too low
                    Client.SendMessage(e.ChatMessage.Channel, $"Sorry, only {Enum.GetName(typeof(TwitchUserLevels), Settings.Settings.TwSrUserLevel)} or higher are allowed request songs.");
                    return;
                }
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

            switch (e.ChatMessage.Message)
            {
                case "!skip":
                    {
                        if (_skipCooldown)
                            return;
                        string msg = "";
                        int count = 0;
                        string name = "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            int? reqListCount = ((MainWindow)Application.Current.MainWindow)?.ReqList.Count;
                            if (reqListCount != null)
                                count = (int)reqListCount;
                            if (count > 0)
                                name = ((MainWindow)Application.Current.MainWindow)?.ReqList.First().Requester;
                        });

                        if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster || (count > 0 && name == e.ChatMessage.DisplayName))
                        {
                            if (!Settings.Settings.BotCmdSkip)
                                return;
                            msg = Settings.Settings.BotRespModSkip;
                            msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                            ErrorResponse response = await ApiHandler.SkipSong();
                            if (response.Error != null)
                            {
                                Client.SendMessage(e.ChatMessage.Channel, "Error: " + response.Error.Message);
                            }
                            else
                            {
                                Client.SendMessage(e.ChatMessage.Channel, msg);
                                _skipCooldown = true;
                                SkipCooldownTimer.Start();
                            }
                        }
                        else
                        {
                            if (!Settings.Settings.BotCmdSkipVote)
                                return;
                            //Start a skip vote, add the user to SkipVotes, if at least 5 users voted, skip the song
                            if (!SkipVotes.Contains(e.ChatMessage.DisplayName))
                            {
                                SkipVotes.Add(e.ChatMessage.DisplayName);

                                msg = Settings.Settings.BotRespVoteSkip;
                                msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                                msg = msg.Replace("{votes}", $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}");

                                Client.SendMessage(e.ChatMessage.Channel, msg);

                                if (SkipVotes.Count >= Settings.Settings.BotCmdSkipVoteCount)
                                {
                                    ErrorResponse response = await ApiHandler.SkipSong();
                                    if (response.Error != null)
                                    {
                                        Client.SendMessage(e.ChatMessage.Channel, "Error: " + response.Error.Message);
                                    }
                                    else
                                    {
                                        Client.SendMessage(e.ChatMessage.Channel, "Skipping song by vote...");
                                        _skipCooldown = true;
                                        SkipCooldownTimer.Start();
                                    }
                                    SkipVotes.Clear();
                                }
                            }
                        }
                        break;
                    }
                case "!song" when Settings.Settings.BotCmdSong:
                    {
                        string currsong = GetCurrentSong();
                        Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {currsong}");
                        break;
                    }
                case "!pos" when Settings.Settings.BotCmdPos:
                    {
                        List<QueueItem> queueItems = GetQueueItems(e.ChatMessage.DisplayName);
                        string output = "";
                        if (queueItems.Count != 0)
                        {
                            for (int i = 0; i < queueItems.Count; i++)
                            {
                                QueueItem item = queueItems[i];
                                output += $"Pos {item.Position}: {item.Title}";
                                if (i + 1 != queueItems.Count)
                                    output += " | ";
                            }
                            Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {output}");
                        }
                        else
                        {
                            Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} you have no Songs in the current Queue");
                        }

                        break;
                    }
                case "!next" when Settings.Settings.BotCmdNext:
                    {
                        List<QueueItem> queueItems = GetQueueItems();
                        Client.SendMessage(e.ChatMessage.Channel,
                            queueItems != null
                                ? $"@{e.ChatMessage.DisplayName} {queueItems[0].Title}"
                                : $"@{e.ChatMessage.DisplayName} there is no song next up.");

                        break;
                    }
            }
        }

        private static int CheckUserLevel(ChatMessage o)
        {
            if (o.IsBroadcaster) return 4;
            if (o.IsModerator) return 3;
            if (o.IsSubscriber) return 2;
            return o.IsVip ? 1 : 0;
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
                        tmp = (window as MainWindow)?.CurrSongTwitch;
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
                response = response.Replace("{maxlength}", Settings.Settings.MaxSongLength.ToString());
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
            if (MaxQueueItems(e.ChatMessage.DisplayName, e.ChatMessage))
            {
                // if the user reached max requests in the queue skip and inform requester
                response = Settings.Settings.BotRespMaxReq;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", $"{(TwitchUserLevels)CheckUserLevel(e.ChatMessage)} {GetMaxRequestsForUserlevel(e.ChatMessage)}");
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

        private static int GetMaxRequestsForUserlevel(ChatMessage chatMessage)
        {
            switch ((TwitchUserLevels)CheckUserLevel(chatMessage))
            {
                case TwitchUserLevels.Everyone:
                    return Settings.Settings.TwSrMaxReqEveryone;
                case TwitchUserLevels.Vip:
                    return Settings.Settings.TwSrMaxReqVip;

                case TwitchUserLevels.Subscriber:
                    return Settings.Settings.TwSrMaxReqSubscriber;

                case TwitchUserLevels.Moderator:
                    return Settings.Settings.TwSrMaxReqModerator;

                case TwitchUserLevels.Broadcaster:
                    return 999;
                default:
                    return 0;
            }
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
                        Position = pos,
                        Title = requestObject.Artists + " - " + requestObject.Title,
                        Requester = requestObject.Requester
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
                            Title = $"{temp[0].Artists} - {temp[0].Title}",
                            Requester = $"{temp[0].Requester}"
                        });
                        return temp3;
                    }
                    else if (temp.Count > 1)
                    {
                        temp3.Add(new QueueItem
                        {
                            Title = $"{temp[1].Artists} - {temp[1].Title}",
                            Requester = $"{temp[1].Requester}"
                        });
                        return temp3;
                    }
                }
            }
            return null;
        }

        private static bool MaxQueueItems(string requester, ChatMessage chatMessage)
        {
            int maxreq = 1;
            // Checks if the requester already reached max songrequests
            var temp = new List<RequestObject>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                        temp = (window as MainWindow)?.ReqList.FindAll(x => x.Requester == requester);
            });

            switch ((TwitchUserLevels)CheckUserLevel(chatMessage))
            {
                case TwitchUserLevels.Everyone:
                    maxreq = Settings.Settings.TwSrMaxReqEveryone;
                    break;
                case TwitchUserLevels.Vip:
                    maxreq = Settings.Settings.TwSrMaxReqVip;
                    break;
                case TwitchUserLevels.Subscriber:
                    maxreq = Settings.Settings.TwSrMaxReqSubscriber;
                    break;
                case TwitchUserLevels.Moderator:
                    maxreq = Settings.Settings.TwSrMaxReqModerator;
                    break;
                case TwitchUserLevels.Broadcaster:
                    maxreq = 999;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return temp.Count >= maxreq;
        }

        public static void SendCurrSong(string song)
        {
            if (Client != null && Client.IsConnected)
                Client.SendMessage(Settings.Settings.TwChannel, song);
        }
    }

    class QueueItem
    {
        public string Requester { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }
    }
}