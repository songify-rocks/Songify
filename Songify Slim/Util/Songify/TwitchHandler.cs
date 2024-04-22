using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Properties;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify.TwitchOAuth;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateRedemptionStatus;
using TwitchLib.Api.Helix.Models.Chat;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using Unosquare.Swan;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Reward = TwitchLib.PubSub.Models.Responses.Messages.Redemption.Reward;
using Timer = System.Timers.Timer;
using TwitchLib.Api.Helix.Models.Soundtrack;
using TwitchLib.PubSub.Models.Responses;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding to twitch.tv
    public static class TwitchHandler
    {
        public const bool PubSubEnabled = false;

        public static ValidateAccessTokenResponse BotTokenCheck;

        public static TwitchClient Client;

        public static bool ForceDisconnect;

        public static TwitchClient MainClient;

        public static ValidateAccessTokenResponse TokenCheck;

        public static TwitchAPI TwitchApi;

        public static TwitchAPI TwitchApiBot;

        public static List<TwitchUser> Users = new();

        private const string ClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";

        private static readonly Timer CooldownTimer = new()
        {
            Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds
        };

        private static readonly Timer SkipCooldownTimer = new()
        {
            Interval = TimeSpan.FromSeconds(5).TotalMilliseconds
        };

        private static readonly Stopwatch CooldownStopwatch = new();

        private static readonly List<string> SkipVotes = new();

        private static readonly TwitchPubSub TwitchPubSub = new();

        private static string _currentState;

        private static bool _onCooldown;

        private static bool _skipCooldown;

        private static string _userId;

        public enum TwitchAccount
        {
            Main,
            Bot
        }

        //create a list with Twitch UserTypes and assign int values to them
        public enum TwitchUserLevels
        {
            Everyone = 0,
            Subscriber = 1,
            Vip = 2,
            Moderator = 3,
            Broadcaster = 4
        }

        public static void ApiConnect(TwitchAccount account)
        {
            ImplicitOAuth ioa = new(1234);

            // This event is triggered when the application recieves a new token and state from the "RequestClientAuthorization" method.
            ioa.OnRevcievedValues += async (state, token) =>
            {
                if (state != _currentState)
                {
                    Console.WriteLine(@"State does not match up. Possible CSRF attack or other error.");
                    return;
                }

                switch (account)
                {
                    case TwitchAccount.Main:
                        // Don't actually print the user token on screen or to the console.
                        // Here you should save it where the application can access it whenever it wants to, such as in appdata.
                        Settings.Settings.TwitchAccessToken = token;
                        break;
                    case TwitchAccount.Bot:
                        // Don't actually print the user token on screen or to the console.
                        // Here you should save it where the application can access it whenever it wants to, such as in appdata.
                        Settings.Settings.TwitchBotToken = token;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(account), account, null);
                }

                await InitializeApi(account);

                Settings.Settings.TwChannel = Settings.Settings.TwitchUser.Login;
                bool shownInSettings = false;
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() != typeof(Window_Settings)) continue;
                        await ((Window_Settings)window).ShowMessageAsync(Resources.msgbx_BotAccount,
                            Resources.msgbx_UseAsBotAccount.Replace("{account}",
                                account == TwitchAccount.Main
                                    ? Settings.Settings.TwitchUser.DisplayName
                                    : Settings.Settings.TwitchBotUser.DisplayName),
                            MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                            {
                                AffirmativeButtonText = Resources.msgbx_Yes,
                                NegativeButtonText = Resources.msgbx_No,
                                DefaultButtonFocus = MessageDialogResult.Affirmative
                            }).ContinueWith(x =>
                        {
                            if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                            Settings.Settings.TwOAuth =
                                $"oauth:{(account == TwitchAccount.Main ? Settings.Settings.TwitchAccessToken : Settings.Settings.TwitchBotToken)}";
                            Settings.Settings.TwAcc = account == TwitchAccount.Main
                                ? Settings.Settings.TwitchUser.Login
                                : Settings.Settings.TwitchBotUser.Login;
                            return Task.CompletedTask;
                        });
                        ((Window_Settings)window).SetControls();
                        shownInSettings = true;
                        break;
                    }

                    if (!shownInSettings)
                    {
                        (Application.Current.MainWindow as MainWindow)?.ShowMessageAsync(Resources.msgbx_BotAccount,
                            Resources.msgbx_UseAsBotAccount.Replace("{account}",
                                account == TwitchAccount.Main
                                    ? Settings.Settings.TwitchUser.DisplayName
                                    : Settings.Settings.TwitchBotUser.DisplayName),
                            MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                            {
                                AffirmativeButtonText = Resources.msgbx_Yes,
                                NegativeButtonText = Resources.msgbx_No,
                                DefaultButtonFocus = MessageDialogResult.Affirmative
                            }).ContinueWith(x =>
                        {
                            if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                            Settings.Settings.TwOAuth =
                                $"oauth:{(account == TwitchAccount.Main ? Settings.Settings.TwitchAccessToken : Settings.Settings.TwitchBotToken)}";
                            Settings.Settings.TwAcc = account == TwitchAccount.Main
                                ? Settings.Settings.TwitchUser.Login
                                : Settings.Settings.TwitchBotUser.Login;
                            return Task.CompletedTask;
                        });
                    }

                    ForceDisconnect = true;
                    if (Client != null)
                    {
                        try
                        {
                            Client.Disconnect();
                            Client = null;
                        }
                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }
                    }

                    if (MainClient != null)
                    {
                        try
                        {
                            MainClient.Disconnect();
                            MainClient = null;
                        }
                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }
                    }

                    BotConnect();
                    MainConnect();

                    dynamic telemetryPayload = new
                    {
                        uuid = Settings.Settings.Uuid,
                        key = Settings.Settings.AccessKey,
                        tst = DateTime.Now.ToUnixEpochDate(),
                        twitch_id = Settings.Settings.TwitchUser == null ? "" : Settings.Settings.TwitchUser.Id,
                        twitch_name = Settings.Settings.TwitchUser == null
                            ? ""
                            : Settings.Settings.TwitchUser.DisplayName,
                        vs = GlobalObjects.AppVersion,
                        playertype = GlobalObjects.GetReadablePlayer(),
                    };
                    string json = Json.Serialize(telemetryPayload);
                    await WebHelper.TelemetryRequest(WebHelper.RequestMethod.Post, json);
                });
            };

            // This method initialize the flow of getting the token and returns a temporary random state that we will use to check authenticity.
            _currentState = ioa.RequestClientAuthorization();
        }

        public static void BotConnect()
        {
            try
            {
                MainConnect();
                if (Client != null && Client.IsConnected)
                    return;
                if (Client != null && !Client.IsConnected)
                {
                    Client.Connect();
                    Client.JoinChannel(Settings.Settings.TwChannel);
                    return;
                }

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
                ConnectionCredentials credentials = new(Settings.Settings.TwAcc, Settings.Settings.TwOAuth);
                ClientOptions clientOptions = new()
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                WebSocketClient customClient = new(clientOptions);
                Client = new TwitchClient(customClient);
                Client.Initialize(credentials, Settings.Settings.TwChannel);

                Client.OnMessageReceived += Client_OnMessageReceived;
                Client.OnConnected += Client_OnConnected;
                Client.OnDisconnected += Client_OnDisconnected;
                Client.OnJoinedChannel += ClientOnOnJoinedChannel;

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

        public static async Task<bool> CheckStreamIsUp()
        {
            try
            {
                if (TokenCheck == null) return false;
                GetStreamsResponse x = await TwitchApi.Helix.Streams.GetStreamsAsync(null, 20, null, null,
                    new List<string> { Settings.Settings.TwitchUser.Id }, null, Settings.Settings.TwitchAccessToken);
                if (x.Streams.Length != 0)
                {
                    return x.Streams[0].Type == "live";
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return false;
            }
        }

        public static async Task<List<CustomReward>> GetChannelRewards(bool b)
        {
            GetCustomRewardsResponse rewardsResponse = null;
            try
            {
                rewardsResponse =
                    await TwitchApi.Helix.ChannelPoints.GetCustomRewardAsync(Settings.Settings.TwitchChannelId, null,
                        b);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return rewardsResponse?.Data.ToList();
        }

        public static async Task InitializeApi(TwitchAccount twitchAccount)
        {
            GetUsersResponse users;
            User user;
            switch (twitchAccount)
            {
                #region Main

                case TwitchAccount.Main:
                    TwitchApi = new TwitchAPI
                    {
                        Settings =
                        {
                            ClientId = ClientId,
                            AccessToken = Settings.Settings.TwitchAccessToken
                        }
                    };

                    TokenCheck = await TwitchApi.Auth.ValidateAccessTokenAsync(Settings.Settings.TwitchAccessToken);

                    if (TokenCheck == null)
                    {
                        GlobalObjects.TwitchUserTokenExpired = true;
                        await Application.Current.Dispatcher.Invoke(async () =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.GetType() != typeof(MainWindow))
                                    continue;
                                ((MainWindow)window).IconTwitchAPI.Foreground = Brushes.IndianRed;
                                ((MainWindow)window).IconTwitchAPI.Kind =
                                    PackIconBootstrapIconsKind.ExclamationTriangleFill;
                                ((MainWindow)window).mi_TwitchAPI.IsEnabled = false;
                                MessageDialogResult msgResult = await ((MainWindow)window).ShowMessageAsync(
                                    "Twitch Account Issues",
                                    "Your Twitch Account token has expired. Please login again with Twtich",
                                    MessageDialogStyle.AffirmativeAndNegative,
                                    new MetroDialogSettings
                                    { AffirmativeButtonText = "Login (Main)", NegativeButtonText = "Cancel" });
                                if (msgResult == MessageDialogResult.Negative) return;
                                ApiConnect(TwitchAccount.Main);
                            }
                        });
                        return;
                    }

                    GlobalObjects.TwitchUserTokenExpired = false;
                    _userId = TokenCheck.UserId;

                    users = await TwitchApi.Helix.Users.GetUsersAsync(new List<string> { _userId }, null,
                        Settings.Settings.TwitchAccessToken);

                    user = users.Users.FirstOrDefault();
                    if (user == null)
                        return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(MainWindow))
                                continue;
                            ((MainWindow)window).IconTwitchAPI.Foreground = Brushes.GreenYellow;
                            ((MainWindow)window).IconTwitchAPI.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                            ((MainWindow)window).mi_TwitchAPI.IsEnabled = false;

                            Logger.LogStr($"TWITCH API: Logged into Twitch API ({user.DisplayName})");
                        }
                    });

                    Settings.Settings.TwitchUser = user;
                    Settings.Settings.TwitchChannelId = user.Id;

                    ConfigHandler.WriteAllConfig(Settings.Settings.Export());

                    //TODO: Enable PubSub when it's fixed in TwitchLib
                    if (PubSubEnabled)
                        CreatePubSubsConnection();

                    break;

                #endregion

                #region Bot

                case TwitchAccount.Bot:
                    TwitchApiBot = new TwitchAPI
                    {
                        Settings =
                        {
                            ClientId = ClientId,
                            AccessToken = Settings.Settings.TwitchBotToken
                        }
                    };
                    BotTokenCheck = await TwitchApiBot.Auth.ValidateAccessTokenAsync(Settings.Settings.TwitchBotToken);
                    if (BotTokenCheck == null)
                    {
                        GlobalObjects.TwitchBotTokenExpired = true;
                        await Application.Current.Dispatcher.Invoke(async () =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.GetType() != typeof(MainWindow))
                                    continue;
                                MessageDialogResult msgResult = await ((MainWindow)window).ShowMessageAsync(
                                    "Twitch Account Issues",
                                    "Your Twitch Bot Account token has expired. Please login again with Twtich",
                                    MessageDialogStyle.AffirmativeAndNegative,
                                    new MetroDialogSettings
                                    { AffirmativeButtonText = "Login (Bot)", NegativeButtonText = "Cancel" });
                                if (msgResult == MessageDialogResult.Negative) return;
                                ApiConnect(TwitchAccount.Bot);
                            }
                        });
                        return;
                    }

                    GlobalObjects.TwitchBotTokenExpired = false;

                    _userId = BotTokenCheck.UserId;

                    users = await TwitchApiBot.Helix.Users.GetUsersAsync(new List<string> { _userId }, null,
                        Settings.Settings.TwitchBotToken);

                    user = users.Users.FirstOrDefault();
                    if (user == null)
                        return;
                    Settings.Settings.TwitchBotUser = user;
                    break;

                #endregion

                default:
                    throw new ArgumentOutOfRangeException(nameof(twitchAccount), twitchAccount, null);
            }
        }

        public static void MainConnect()
        {
            if (MainClient != null && MainClient.IsConnected)
                return;
            if (MainClient != null && !MainClient.IsConnected)
            {
                MainClient.Connect();
                MainClient.JoinChannel(Settings.Settings.TwChannel);
                return;
            }

            try

            {
                // Checks if twitch credentials are present
                if (string.IsNullOrEmpty(Settings.Settings.TwitchUser.DisplayName) ||
                    string.IsNullOrEmpty(Settings.Settings.TwitchAccessToken) ||
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
                    new(Settings.Settings.TwitchUser.DisplayName,
                        $"oauth:{Settings.Settings.TwitchAccessToken}");
                ClientOptions clientOptions = new()
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                WebSocketClient customClient = new(clientOptions);
                MainClient = new TwitchClient(customClient);
                MainClient.Initialize(credentials, Settings.Settings.TwChannel);
                MainClient.Connect();
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        public static void ResetVotes()
        {
            SkipVotes.Clear();
        }

        public static async void SendCurrSong()
        {
            if (Client == null || !Client.IsConnected || Client.JoinedChannels.Count == 0) return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus)
                        SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            string msg = GetCurrentSong();
            msg = Regex.Replace(msg, @"(@)?\{user\}", "");
            msg = msg.Replace("{song}",
                $"{GlobalObjects.CurrentSong.Artists} {(GlobalObjects.CurrentSong.Title != "" ? " - " + GlobalObjects.CurrentSong.Title : "")}");
            msg = msg.Replace("{artist}", $"{GlobalObjects.CurrentSong.Artists}");
            msg = msg.Replace("{title}", $"{GlobalObjects.CurrentSong.Title}");
            msg = msg.Replace(@"\n", " - ").Replace("  ", " ");

            if (msg.StartsWith("[announce "))
            {
                await AnnounceInChat(msg);
            }
            else
            {
                SendChatMessage(Settings.Settings.TwChannel, msg);
            }
        }

        private static async void AddSong(string trackId, OnMessageReceivedArgs e)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                SendChatMessage(e.ChatMessage.Channel, "No song found.");
                return;
            }

            if (trackId == "shortened")
            {
                SendChatMessage(Settings.Settings.TwChannel,
                    "Spotify short links are not supported. Please type in the full title or get the Spotify URI (starts with \"spotify:track:\")");
                return;
            }

            if (Settings.Settings.LimitSrToPlaylist &&
                !string.IsNullOrEmpty(Settings.Settings.SpotifySongLimitPlaylist))
            {
                Tuple<bool, string> result = await IsInAllowedPlaylist(trackId);
                if (!result.Item1)
                {
                    SendChatMessage(e.ChatMessage.Channel, result.Item2);
                    return;
                }
            }

            if (IsSongBlacklisted(trackId))
            {
                SendChatMessage(Settings.Settings.TwChannel, "This song is blocked");
                return;
            }

            FullTrack track = SpotifyApiHandler.GetTrack(trackId);

            if (track == null)
            {
                SendChatMessage(Settings.Settings.TwChannel, CreateNoTrackFoundResponse(e));
                return;
            }

            if (IsTrackExplicit(track, e, out string response))
            {
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            //if (IsTrackUnavailable(track, e, out response))
            //{
            //    SendChatMessage(e.ChatMessage.Channel, response);
            //    return;
            //}

            if (IsArtistBlacklisted(track, e, out response))
            {
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            if (IsTrackTooLong(track, e, out response))
            {
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            if (IsTrackAlreadyInQueue(track, e, out response))
            {
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            if (IsUserAtMaxRequests(e, out response))
            {
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            ErrorResponse error = SpotifyApiHandler.AddToQ("spotify:track:" + trackId);
            if (error == null)
            {
                response = CreateErrorResponse(e.ChatMessage.DisplayName, "Spotify response was Null");
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            if (error?.Error != null)
            {
                response = CreateErrorResponse(e.ChatMessage.DisplayName, error.Error.Message);
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            if (Settings.Settings.AddSrToPlaylist)
                await AddToPlaylist(track.Id);

            response = CreateSuccessResponse(track, e.ChatMessage.DisplayName);
            SendChatMessage(e.ChatMessage.Channel, response);
            await UploadToQueue(track, e.ChatMessage.DisplayName);
            GlobalObjects.QueueUpdateQueueWindow();
        }

        private static string CreateNoTrackFoundResponse(OnMessageReceivedArgs e)
        {
            string response = Settings.Settings.BotRespNoTrackFound;
            response = response.Replace("{user}", e.ChatMessage.DisplayName);
            response = response.Replace("{artist}", "");
            response = response.Replace("{title}", "");
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count}");
            response = response.Replace("{errormsg}", "");
            return response;
        }

        private static bool IsTrackExplicit(FullTrack track, OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;
            if (!Settings.Settings.BlockAllExplicitSongs)
                return false;
            try
            {
                if (!track.Explicit)
                {
                    return false;
                }

                response = Settings.Settings.BotRespTrackExplicit;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Track Unavailable");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static async Task<Tuple<bool, string>> IsInAllowedPlaylist(string trackId)
        {
            string response = string.Empty;
            Tuple<bool, FullPlaylist> isAllowedSong =
                await CheckIsSongAllowed(trackId, Settings.Settings.SpotifySongLimitPlaylist);
            if (!isAllowedSong.Item1)
            {
                response = Settings.Settings.BotRespPlaylist;
                response = response.Replace("{playlist_name}", isAllowedSong.Item2.Name);
                response = response.Replace("{playlist_url}",
                    $"https://open.spotify.com/playlist/{isAllowedSong.Item2.Id}");
                GlobalObjects.AllowedPlaylistName = isAllowedSong.Item2.Name;
                GlobalObjects.AllowedPlaylistUrl = $"https://open.spotify.com/playlist/{isAllowedSong.Item2.Id}";
                return Tuple.Create(false, response);
            }

            return Tuple.Create(true, response);
        }

        private static async Task<Tuple<bool, FullPlaylist>> CheckIsSongAllowed(string trackId,
            string spotifySongLimitPlaylist)
        {
            FullPlaylist playlist = await SpotifyApiHandler.Spotify.GetPlaylistAsync(spotifySongLimitPlaylist);
            Paging<PlaylistTrack> tracks = await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(spotifySongLimitPlaylist);
            while (tracks != null && tracks.Items != null)
            {
                // Check if any track matches the given ID
                if (tracks.Items.Any(t => t.Track.Id == trackId))
                {
                    return new Tuple<bool, FullPlaylist>(true, playlist);
                }

                // Check if there are more pages, if not, exit the loop
                if (!tracks.HasNextPage())
                {
                    break;
                }

                // Fetch the next page of tracks
                tracks = await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100, tracks.Offset + tracks.Limit);
            }

            return new Tuple<bool, FullPlaylist>(false, playlist);
        }

        private static async Task<ReturnObject> AddSong2(string trackId, string username)
        {
            // loads the blacklist from settings
            string response;
            // gets the track information using spotify api
            FullTrack track = SpotifyApiHandler.GetTrack(trackId);

            if (track.IsPlayable != null && (bool)!track.IsPlayable)
            {
                return new ReturnObject
                {
                    Msg = "This track is not available in the streamers region.",
                    Success = false,
                    Refundcondition = 3
                };
            }

            string artists = "";
            for (int i = 0; i < track.Artists.Count; i++)
                if (i != track.Artists.Count - 1)
                    artists += track.Artists[i].Name + ", ";
                else
                    artists += track.Artists[i].Name;

            // checks if one of the artist in the requested song is on the blacklist
            foreach (string s in Settings.Settings.ArtistBlacklist.Where(s =>
                         Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
            {
                // if artist is on blacklist, skip and inform requester
                response = Settings.Settings.BotRespBlacklist;
                response = response.Replace("{user}", username);
                response = response.Replace("{artist}", s);
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);

                return new ReturnObject
                {
                    Msg = response,
                    Success = false,
                    Refundcondition = 4
                };
            }

            // checks if song length is longer or equal to 10 minutes
            if (track.DurationMs >= TimeSpan.FromMinutes(Settings.Settings.MaxSongLength).TotalMilliseconds)
            {
                // if track length exceeds 10 minutes skip and inform requster
                response = Settings.Settings.BotRespLength;
                response = response.Replace("{user}", username);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = response.Replace("{maxlength}", Settings.Settings.MaxSongLength.ToString());
                response = CleanFormatString(response);

                return new ReturnObject
                {
                    Msg = response,
                    Success = false,
                    Refundcondition = 5
                };
            }

            // checks if the song is already in the queue
            if (IsInQueue(track.Id))
            {
                // if the song is already in the queue skip and inform requester
                response = Settings.Settings.BotRespIsInQueue;
                response = response.Replace("{user}", username);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);

                return new ReturnObject
                {
                    Msg = response,
                    Success = false,
                    Refundcondition = 6
                };
            }

            // generate the spotifyURI using the track id
            string spotifyUri = "spotify:track:" + trackId;

            // try adding the song to the queue using the URI
            ErrorResponse error = SpotifyApiHandler.AddToQ(spotifyUri);
            if (error.Error != null)
            {
                // if an error has been encountered, log it, inform the requester and skip
                Logger.LogStr("TWITCH: " + error.Error.Message + "\n" + error.Error.Status);
                response = Settings.Settings.BotRespError;
                response = response.Replace("{user}", username);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", error.Error.Message);

                return new ReturnObject
                {
                    Msg = response,
                    Success = false,
                    Refundcondition = 7
                };
            }

            // if everything worked so far, inform the user that the song has been added to the queue
            response = Settings.Settings.BotRespSuccess;
            response = response.Replace("{user}", username);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", "");

            // Upload the track and who requested it to the queue on the server
            await UploadToQueue(track, username);

            // Add the song to the internal queue and update the queue window if its open
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window qw = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(WindowQueue))
                        qw = window;
                }

                //GlobalObjects.ReqList.Add(new RequestObject
                //{
                //    Requester = username,
                //    Trackid = track.Id,
                //    Albumcover = track.Album.Images[0].Url,
                //    Title = track.Name,
                //    Artist = artists,
                //    Length = FormattedTime(track.DurationMs)
                //});

                (qw as WindowQueue)?.dgv_Queue.Items.Refresh();
            });

            return new ReturnObject
            {
                Msg = response,
                Success = true,
                Refundcondition = 8
            };
        }

        private static async Task<bool> AddToPlaylist(string trackId, bool sendResponse = false)
        {
            try
            {
                Paging<PlaylistTrack> tracks =
                    await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId);

                while (tracks is { Items: not null })
                {
                    if (tracks.Items.Any(t => t.Track.Id == trackId))
                    {
                        if (sendResponse)
                        {
                            SendChatMessage(Settings.Settings.TwChannel,
                                $"The Song \"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}\" is already in the playlist.");
                        }
                        return true;
                    }

                    if (!tracks.HasNextPage())
                    {
                        break;  // Exit if no more pages
                    }

                    tracks = await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100, tracks.Offset + tracks.Limit);
                }

                ErrorResponse x = await SpotifyApiHandler.Spotify.AddPlaylistTrackAsync(Settings.Settings.SpotifyPlaylistId,
                    $"spotify:track:{trackId}");
                return x == null || x.HasError();
            }
            catch (Exception)
            {
                Logger.LogStr("Error adding song to playlist");
                return true;
            }
        }

        private static string GetFormattedRespone(string response, string username = "", string errormsg = "",
            string votes = "")
        {
            string returnString = response;
            Dictionary<string, string> replacements = new()
            {
                { "{user}", username },
                { "{artist}", GlobalObjects.CurrentSong.Artists },
                { "{title}", GlobalObjects.CurrentSong.Title },
                { "{maxreq}", Settings.Settings.TwSrMaxReq.ToString() },
                { "{errormsg}", errormsg },
                { "{maxlength}", Settings.Settings.MaxSongLength.ToString() },
                { "{votes}", votes },
                { "{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}" },
                { "{req}", GlobalObjects.Requester },
                { "{url}", GlobalObjects.CurrentSong.Url },
                { "{playlist_name}", GlobalObjects.AllowedPlaylistName },
                { "{playlist_url}", "https://open.spotify.com/playlist/2wKHJy4vO0pA1gXfACW8Qh?si=30184b3f0854459c" }
            };
            foreach (KeyValuePair<string, string> pair in replacements)
            {
                returnString = returnString.Replace(pair.Key, pair.Value);
            }

            RequestObject rq = null;

            if (GlobalObjects.ReqList.Count > 0)
            {
                rq = GlobalObjects.ReqList.FirstOrDefault(x => x.Trackid == GlobalObjects.CurrentSong.SongId);
                if (rq != null)
                {
                    returnString = returnString.Replace("{{", "")
                        .Replace("}}", "")
                        .Replace("{req}", rq.Requester);
                }
                else
                {
                    RemoveDelimitedSubstring(ref returnString, "{{", "}}");
                }
            }

            return returnString;
        }

        private static void RemoveDelimitedSubstring(ref string input, string startDelimiter, string endDelimiter)
        {
            try
            {
                int start = input.IndexOf(startDelimiter, StringComparison.Ordinal);
                int end = input.LastIndexOf(endDelimiter, StringComparison.Ordinal) + endDelimiter.Length;
                if (start >= 0)
                {
                    input = input.Remove(start, end - start);
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private static async Task AnnounceInChat(string msg)
        {
            Tuple<string, AnnouncementColors> tup = GetStringAndColor(msg);
            try
            {
                if (BotTokenCheck != null)
                {
                    await TwitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
                        Settings.Settings.TwitchBotUser.Id, $"{tup.Item1}", tup.Item2,
                        Settings.Settings.TwitchBotToken);
                    return;
                }

                if (TokenCheck != null)
                {
                    await TwitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
                        Settings.Settings.TwitchUser.Id, $"{tup.Item1}", tup.Item2,
                        Settings.Settings.TwitchAccessToken);
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("TWITCH API: Could not send announcement. Has the bot been created through the app?");
            }

            SendChatMessage(Settings.Settings.TwChannel, $"{tup.Item1}");
        }

        private static bool CheckLiveStatus()
        {
            if (Settings.Settings.IsLive || !Settings.Settings.BotOnlyWorkWhenLive)
            {
                Logger.LogStr("STREAM: Stream is live.");
                Application.Current.BeginInvoke(
                    () =>
                    {
                        (Application.Current.MainWindow as MainWindow)?.Invoke(() =>
                        {
                            ((MainWindow)Application.Current.MainWindow).LblStatus.Content =
                                "Command accepted. Stream is up.";
                        });
                    }, DispatcherPriority.Normal);
                return true;
            }

            Logger.LogStr("STREAM: Stream is down.");
            Application.Current.BeginInvoke(
                () =>
                {
                    (Application.Current.MainWindow as MainWindow)?.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).LblStatus.Content =
                            "Command cancelled. Stream is offline.";
                    });
                }, DispatcherPriority.Normal);
            return false;
        }

        private static int CheckUserLevel(ChatMessage o)
        {
            if (o.IsBroadcaster) return 4;
            if (o.IsModerator) return 3;
            if (o.IsVip) return 2;
            return o.IsSubscriber ? 1 : 0;
        }

        private static string CleanFormatString(string currSong)
        {
            const RegexOptions options = RegexOptions.None;
            Regex regex = new("[ ]{2,}", options);
            currSong = regex.Replace(currSong, " ");
            currSong = currSong.Trim();
            // Add trailing spaces for better scroll
            return currSong;
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
                    ((MainWindow)window).IconTwitchBot.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                    ((MainWindow)window).IconTwitchBot.Foreground = Brushes.GreenYellow;
                }
            });
            Logger.LogStr($"TWITCH: Connected to Twitch. User: {Client.TwitchUsername}");
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
                    ((MainWindow)window).IconTwitchBot.Foreground = Brushes.IndianRed;
                    ((MainWindow)window).IconTwitchBot.Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill;
                }
            });
            Logger.LogStr("TWITCH: Disconnected from Twitch Chat");
        }

        private static async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            // Attempt to find the user in the existing list.
            TwitchUser existingUser = Users.FirstOrDefault(o => o.UserId == e.ChatMessage.UserId);

            if (existingUser == null)
            {
                // If the user doesn't exist, add them.
                TwitchUser newUser = new()
                {
                    UserId = e.ChatMessage.UserId,
                    UserName = e.ChatMessage.Username,
                    DisplayName = e.ChatMessage.DisplayName,
                    UserLevel = CheckUserLevel(e.ChatMessage)
                };
                Users.Add(newUser);
            }
            else
            {
                // If the user exists, update their information.
                existingUser.Update(e.ChatMessage.Username, e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage));
            }

            if (Settings.Settings.TwRewardId.Count > 0 &&
                Settings.Settings.TwRewardId.Any(o => o == e.ChatMessage.CustomRewardId) && !PubSubEnabled &&
                Settings.Settings.TwSrReward)
            {
                Settings.Settings.IsLive = await CheckStreamIsUp();

                int userlevel = CheckUserLevel(e.ChatMessage);
                if (userlevel < 4 || !e.ChatMessage.IsBroadcaster)
                    if (!Settings.Settings.UserLevelsReward.Contains(userlevel) &&
                        !Settings.Settings.UserLevelsReward.Contains(0))
                    {
                        //Send a Message to the user, that his Userlevel is too low
                        SendChatMessage(e.ChatMessage.Channel,
                            $"Sorry, {Enum.GetName(typeof(TwitchUserLevels), userlevel)}s are not allowed to request songs.");
                        return;
                    }

                // Do nothing if the user is blocked, don't even reply
                if (IsUserBlocked(e.ChatMessage.DisplayName))
                {
                    Client.SendWhisper(e.ChatMessage.DisplayName, "You are blocked from making Songrequests");
                    return;
                }

                // if onCooldown skips
                //if (_onCooldown)
                //{
                //    Client.SendMessage(Settings.Settings.TwChannel, CreateCooldownResponse(e));
                //    return;
                //}

                if (SpotifyApiHandler.Spotify == null)
                {
                    SendChatMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(await GetTrackIdFromInput(e.ChatMessage.Message), e);
                return;
            }

            // Same code from above but it reacts to a command instead of rewards
            // Songrequst Command (!ssr)
            if (Settings.Settings.Player == 0 && Settings.Settings.TwSrCommand &&
                e.ChatMessage.Message.StartsWith($"!{Settings.Settings.BotCmdSsrTrigger.ToLower()} ", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                int userlevel = CheckUserLevel(e.ChatMessage);
                if (userlevel < 4 || !e.ChatMessage.IsBroadcaster)
                    if (!Settings.Settings.UserLevelsCommand.Contains(userlevel) &&
                        !Settings.Settings.UserLevelsCommand.Contains(0))
                    {
                        //Send a Message to the user, that his Userlevel is too low
                        SendChatMessage(e.ChatMessage.Channel,
                            $"Sorry, {Enum.GetName(typeof(TwitchUserLevels), userlevel)}s are not allowed to request songs.");
                        return;
                    }

                // Do nothing if the user is blocked, don't even reply
                if (IsUserBlocked(e.ChatMessage.DisplayName))
                {
                    Client.SendWhisper(e.ChatMessage.DisplayName, "You are blocked from making Songrequests");
                    return;
                }

                // if onCooldown skips
                if (_onCooldown)
                {
                    Client.SendMessage(Settings.Settings.TwChannel, CreateCooldownResponse(e));
                    return;
                }

                if (SpotifyApiHandler.Spotify == null)
                {
                    SendChatMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(
                    await GetTrackIdFromInput(e.ChatMessage.Message
                        .Replace($"!{Settings.Settings.BotCmdSsrTrigger}", "")
                        .Trim()), e);

                // start the command cooldown
                StartCooldown();
            }

            // Skip Command for mods (!skip)
            if (Settings.Settings.Player == 0 && e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdSkipTrigger.ToLower()}" &&
                Settings.Settings.BotCmdSkip)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                if (_skipCooldown)
                    return;

                int count = 0;
                string name = "";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    count = GlobalObjects.ReqList.Count;
                    if (count > 0)
                    {
                        RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
                        if (firstRequest != null && firstRequest.Trackid == GlobalObjects.CurrentSong.SongId)
                        {
                            name = firstRequest.Requester;
                        }
                    }
                });

                if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster ||
                    (count > 0 && name == e.ChatMessage.DisplayName))
                {
                    string msg = Settings.Settings.BotRespModSkip;
                    msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                    msg = msg.Replace("{song}",
                        $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    ErrorResponse response = await SpotifyApiHandler.SkipSong();
                    if (response.Error != null)
                    {
                        SendChatMessage(e.ChatMessage.Channel, "Error: " + response.Error.Message);
                    }
                    else
                    {
                        if (msg.StartsWith("[announce "))
                        {
                            await AnnounceInChat(msg);
                        }
                        else
                        {
                            SendChatMessage(e.ChatMessage.Channel, msg);
                        }

                        _skipCooldown = true;
                        SkipCooldownTimer.Start();
                    }
                }
            }
            // Voteskip command (!voteskip)
            else if (Settings.Settings.Player == 0 &&
                     e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdVoteskipTrigger.ToLower()}" &&
                     Settings.Settings.BotCmdSkipVote)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                if (_skipCooldown)
                    return;
                //Start a skip vote, add the user to SkipVotes, if at least 5 users voted, skip the song
                if (SkipVotes.Any(o => o == e.ChatMessage.DisplayName)) return;
                SkipVotes.Add(e.ChatMessage.DisplayName);

                string msg = Settings.Settings.BotRespVoteSkip;
                msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                msg = msg.Replace("{votes}", $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}");

                if (msg.StartsWith("[announce "))
                {
                    await AnnounceInChat(msg);
                }
                else
                {
                    SendChatMessage(e.ChatMessage.Channel, msg);
                }

                if (SkipVotes.Count >= Settings.Settings.BotCmdSkipVoteCount)
                {
                    ErrorResponse response = await SpotifyApiHandler.SkipSong();
                    if (response.Error != null)
                    {
                        SendChatMessage(e.ChatMessage.Channel, "Error: " + response.Error.Message);
                    }
                    else
                    {
                        SendChatMessage(e.ChatMessage.Channel, "Skipping song by vote...");
                        _skipCooldown = true;
                        SkipCooldownTimer.Start();
                    }

                    SkipVotes.Clear();
                    _skipCooldown = true;
                    SkipCooldownTimer.Start();
                }
            }
            // Song command (!song)
            else if (e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdSongTrigger.ToLower()}" && Settings.Settings.BotCmdSong)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }

                    string msg = GetCurrentSong();
                    string artist = GlobalObjects.CurrentSong.Artists;
                    string title = !string.IsNullOrEmpty(GlobalObjects.CurrentSong.Title)
                        ? GlobalObjects.CurrentSong.Title
                        : "";
                    msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                    msg = msg.Replace("{song}", $"{artist} {(title != "" ? " - " + title : "")}");
                    if (msg.StartsWith("[announce "))
                    {
                        await AnnounceInChat(msg);
                    }
                    else
                    {
                        SendChatMessage(e.ChatMessage.Channel, msg);
                    }
                }
                catch
                {
                    Logger.LogStr("Error sending song info.");
                }
            }
            // Pos command (!pos)
            else if (Settings.Settings.Player == 0 &&
                     e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdPosTrigger.ToLower()}" && Settings.Settings.BotCmdPos)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                List<QueueItem> queueItems = GetQueueItems(e.ChatMessage.DisplayName);
                if (queueItems.Count != 0)
                {
                    if (Settings.Settings.BotRespPos == null)
                        return;
                    string response = Settings.Settings.BotRespPos;
                    if (!response.Contains("{songs}") || !response.Contains("{/songs}")) return;
                    //Split string into 3 parts, before, between and after the {songs} and {/songs} tags
                    string[] split = response.Split(new[] { "{songs}", "{/songs}" }, StringSplitOptions.None);
                    string before = split[0].Replace("{user}", e.ChatMessage.DisplayName);
                    string between = split[1].Replace("{user}", e.ChatMessage.DisplayName);
                    string after = split[2].Replace("{user}", e.ChatMessage.DisplayName);

                    string tmp = "";
                    for (int i = 0; i < queueItems.Count; i++)
                    {
                        QueueItem item = queueItems[i];
                        tmp += between.Replace("{pos}", "#" + item.Position).Replace("{song}", item.Title);
                        //If the song is the last one, don't add a newline
                        if (i != queueItems.Count - 1)
                            tmp += " | ";
                    }

                    between = tmp;
                    // Combine the 3 parts into one string
                    string output = before + between + after;
                    if (response.StartsWith("[announce "))
                    {
                        await AnnounceInChat(response);
                    }
                    else
                    {
                        SendChatMessage(e.ChatMessage.Channel, output);
                    }
                }
                else
                {
                    SendChatMessage(e.ChatMessage.Channel,
                        $"@{e.ChatMessage.DisplayName} you have no Songs in the current Queue");
                }
            }
            // Next command (!next)
            else if (Settings.Settings.Player == 0 &&
                     e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdNextTrigger.ToLower()}" && Settings.Settings.BotCmdNext)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                string response = Settings.Settings.BotRespNext;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);

                //if (GlobalObjects.ReqList.Count == 0)
                //    return;
                response = response.Replace("{song}", GetNextSong());
                if (response.StartsWith("[announce "))
                {
                    await AnnounceInChat(response);
                }
                else
                {
                    SendChatMessage(e.ChatMessage.Channel, response);
                }
            }
            // Remove command (!remove)
            else if (Settings.Settings.Player == 0 &&
                     e.ChatMessage.Message.StartsWith($"!{Settings.Settings.BotCmdRemoveTrigger.ToLower()}", StringComparison.CurrentCultureIgnoreCase) &&
                     Settings.Settings.BotCmdRemove)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                bool modAction = false;
                RequestObject reqObj = null;

                if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
                {
                    if (e.ChatMessage.Message.Split(' ').Length > 1)
                    {
                        if (int.TryParse(e.ChatMessage.Message.Split(' ')[1], out int queueId))
                        {
                            modAction = true;
                            reqObj = GlobalObjects.ReqList.FirstOrDefault(o => o.Queueid == queueId);
                        }
                    }
                    else
                    {
                        reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
                            o.Requester == e.ChatMessage.DisplayName);
                    }
                }
                else
                {
                    reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
                        o.Requester == e.ChatMessage.DisplayName);
                }

                if (reqObj == null)
                    return;
                string tmp = $"{reqObj.Artist} - {reqObj.Title}";
                GlobalObjects.SkipList.Add(reqObj);
                dynamic payload = new
                {
                    uuid = Settings.Settings.Uuid,
                    key = Settings.Settings.AccessKey,
                    queueid = reqObj.Queueid,
                };

                await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GlobalObjects.ReqList.Remove(reqObj);
                }));
                //WebHelper.UpdateWebQueue(reqObj.Trackid, "", "", "", "", "1", "u");
                GlobalObjects.QueueUpdateQueueWindow();

                string response = modAction
                    ? $"The request {tmp} requested by @{reqObj.Requester} will be skipped."
                    : Settings.Settings.BotRespRemove;
                response = response
                    .Replace("{song}", tmp)
                    .Replace("{user}", e.ChatMessage.DisplayName);
                if (response.StartsWith("[announce "))
                {
                    await AnnounceInChat(response);
                }
                else
                {
                    SendChatMessage(e.ChatMessage.Channel, response);
                }
            }
            // Songlike command (!songlike)
            else if (Settings.Settings.Player == 0 &&
                     e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdSonglikeTrigger.ToLower()}" &&
                     (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) && Settings.Settings.BotCmdSonglike)
            {
                if (string.IsNullOrWhiteSpace(Settings.Settings.SpotifyPlaylistId))
                {
                    SendChatMessage(Settings.Settings.TwChannel,
                        "No playlist has been specified. Go to Settings -> Spotify and select the playlist you want to use.");
                    return;
                }

                try
                {
                    if (await AddToPlaylist(GlobalObjects.CurrentSong.SongId, true)) return;

                    string response = Settings.Settings.BotRespSongLike;
                    response = response.Replace("{song}",
                        $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    if (response.StartsWith("[announce "))
                    {
                        await AnnounceInChat(response);
                    }
                    else
                    {
                        SendChatMessage(e.ChatMessage.Channel, response);
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogStr("SPOTIFY: Error while adding song to playlist");
                    Logger.LogExc(exception);
                }
            }
            // Play / Pause command (!play; !pause)
            else
                switch (e.ChatMessage.Message.ToLower())
                // ReSharper disable once BadChildStatementIndent
                {
                    case "!play" when ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
                                       Settings.Settings.BotCmdPlayPause):
                        await SpotifyApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, "");
                        break;
                    case "!pause" when ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
                                        Settings.Settings.BotCmdPlayPause):
                        await SpotifyApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
                        break;
                }
        }

        private static string CreateCooldownResponse(OnMessageReceivedArgs e)
        {
            string response = Settings.Settings.BotRespCooldown;
            response = response.Replace("{user}", e.ChatMessage.DisplayName);
            response = response.Replace("{artist}", "");
            response = response.Replace("{title}", "");
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", "");
            int time = (int)((CooldownTimer.Interval / 1000) - CooldownStopwatch.Elapsed.TotalSeconds);
            response = response.Replace("{cd}", time.ToString());
            return response;
        }

        private static void ClientOnOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Logger.LogStr($"TWITCH: Joined channel {e.Channel}");
        }

        private static void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Resets the cooldown for the !ssr command
            _onCooldown = false;
            CooldownStopwatch.Stop();
            CooldownStopwatch.Reset();
            CooldownTimer.Stop();
        }

        private static string CreateErrorResponse(string displayName, string errorMessage)
        {
            string response = Settings.Settings.BotRespError;
            response = response.Replace("{user}", displayName);
            response = response.Replace("{artist}", "");
            response = response.Replace("{title}", "");
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", errorMessage);
            response = CleanFormatString(response);

            return response;
        }

        private static void CreatePubSubEventHandlers()
        {
            TwitchPubSub.OnListenResponse += OnListenResponse;
            TwitchPubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            TwitchPubSub.OnPubSubServiceClosed += OnPubSubServiceClosed;
            TwitchPubSub.OnPubSubServiceError += OnPubSubServiceError;
            TwitchPubSub.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
            TwitchPubSub.OnStreamUp += OnStreamUp;
            TwitchPubSub.OnStreamDown += OnStreamDown;
        }

        private static void CreatePubSubListenEvents()
        {
            TwitchPubSub.ListenToVideoPlayback(Settings.Settings.TwitchChannelId);
            TwitchPubSub.ListenToChannelPoints(Settings.Settings.TwitchChannelId);
        }

        private static void CreatePubSubsConnection()
        {
            CreatePubSubEventHandlers();
            CreatePubSubListenEvents();
            TwitchPubSub.Connect();
        }

        private static string CreateSuccessResponse(FullTrack track, string displayName)
        {
            string response = Settings.Settings.BotRespSuccess;
            string artists = "";
            try
            {
                artists = string.Join(", ", track.Artists.Select(o => o.Name).ToList());
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                IOManager.WriteOutput($"{GlobalObjects.RootDirectory}/dev_log.txt", Json.Serialize(track));
            }

            response = response.Replace("{user}", displayName);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count}");
            response = response.Replace("{errormsg}", "");
            response = CleanFormatString(response);

            return response;
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

        private static string GetCurrentSong()
        {
            string currentSong = Settings.Settings.BotRespSong;

            currentSong = currentSong.Format(
                            artist => GlobalObjects.CurrentSong.Artists,
                            title => GlobalObjects.CurrentSong.Title,
                            extra => "",
                            uri => GlobalObjects.CurrentSong.SongId,
                            url => GlobalObjects.CurrentSong.Url
                    ).Format();

            RequestObject rq = GlobalObjects.ReqList.FirstOrDefault(x => x.Trackid == GlobalObjects.CurrentSong.SongId);
            if (rq != null)
            {
                currentSong = currentSong.Replace("{{", "");
                currentSong = currentSong.Replace("}}", "");
                currentSong = currentSong.Replace("{req}", rq.Requester);
            }
            else
            {
                int start = currentSong.IndexOf("{{", StringComparison.Ordinal);
                int end = currentSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                if (start >= 0) currentSong = currentSong.Remove(start, end - start);
            }

            return currentSong;
        }

        private static int GetMaxRequestsForUserlevel(int userLevel)
        {
            switch ((TwitchUserLevels)userLevel)
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

        private static string GetNextSong()
        {
            int index = 0;
            if (GlobalObjects.ReqList.Count == 0)
            {
                return "There is no song next up.";
            }

            if (GlobalObjects.ReqList.Count > 0 && GlobalObjects.ReqList[0].Trackid == GlobalObjects.CurrentSong.SongId)
            {
                if (GlobalObjects.ReqList.Count <= 1)
                {
                    return "There is no song next up.";
                }

                index = 1;
            }

            return $"{GlobalObjects.ReqList[index].Artist} - {GlobalObjects.ReqList[index].Title}";
        }

        private static List<QueueItem> GetQueueItems(string requester = null)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<QueueItem> temp3 = new();
            string currsong = "";
            List<RequestObject> temp = new(GlobalObjects.ReqList);

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                    {
                        currsong = $"{(window as MainWindow)?.SongArtist} - {(window as MainWindow)?.SongTitle}";
                    }
            });

            if (requester != null)
            {
                List<RequestObject> temp2 = temp.FindAll(x => x.Requester == requester);
                temp3.AddRange(from requestObject in temp2
                               let pos = temp.IndexOf(requestObject) + 1
                               select new QueueItem
                               {
                                   Position = pos,
                                   Title = requestObject.Artist + " - " + requestObject.Title,
                                   Requester = requestObject.Requester
                               });
                return temp3;
            }

            if (temp.Count <= 0) return null;
            if (temp.Count == 1 && $"{temp[0].Artist} - {temp[0].Title}" != currsong)
            {
                temp3.Add(new QueueItem
                {
                    Title = $"{temp[0].Artist} - {temp[0].Title}",
                    Requester = $"{temp[0].Requester}"
                });
                return temp3;
            }

            if (temp.Count <= 1) return null;
            temp3.Add(new QueueItem
            {
                Title = $"{temp[1].Artist} - {temp[1].Title}",
                Requester = $"{temp[1].Requester}"
            });
            return temp3;
        }

        private static Tuple<string, AnnouncementColors> GetStringAndColor(string response)
        {
            AnnouncementColors colors = AnnouncementColors.Purple;
            int startIndex = 9;
            int endIndex = response.IndexOf("]", startIndex, StringComparison.Ordinal);

            string colorName = response.Substring(startIndex, endIndex - startIndex).ToLower().Trim();

            switch (colorName)
            {
                case "green":
                    colors = AnnouncementColors.Green;
                    break;
                case "orange":
                    colors = AnnouncementColors.Orange;
                    break;
                case "blue":
                    colors = AnnouncementColors.Blue;
                    break;
                case "purple":
                    colors = AnnouncementColors.Purple;
                    break;
                case "primary":
                    colors = AnnouncementColors.Primary;
                    break;
            }

            response = response.Replace($"[announce {colorName}]", string.Empty).Trim();
            return new Tuple<string, AnnouncementColors>(item1: response, item2: colors);
        }

        private static async Task<string> GetTrackIdFromInput(string input)
        {
            if (input.StartsWith("https://spotify.link/"))
            {
                input = await GetFullSpotifyUrl(input);
                //return "shortened";
            }

            if (input.StartsWith("spotify:track:"))
            {
                // search for a track with the id
                return input.Replace("spotify:track:", "");
            }

            if (input.StartsWith("https://open.spotify.com/"))
            {
                // Extract the ID using regular expressions
                Match match = Regex.Match(input, @"/track/([^/?]+)");

                if (match.Success)
                {
                    string trackId = match.Groups[1].Value;
                    return trackId;
                }
                //return input.Replace("https://open.spotify.com/track/", "").Split('?')[0];
            }

            // search for a track with a search string from chat
            SearchItem searchItem = SpotifyApiHandler.FindTrack(HttpUtility.UrlEncode(input));
            if (searchItem.HasError())
            {
                SendChatMessage(Settings.Settings.TwChannel, searchItem.Error.Message);
                return "";
            }

            if (searchItem.Tracks.Items.Count <= 0) return "";
            // if a track was found convert the object to FullTrack (easier use than searchItem)
            FullTrack fullTrack = searchItem.Tracks.Items[0];
            return fullTrack.Id;
        }

        private static async Task<string> GetFullSpotifyUrl(string input)
        {
            using HttpClient httpClient = new();
            HttpRequestMessage request = new(HttpMethod.Get, input);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            return response.RequestMessage.RequestUri != null ? response.RequestMessage.RequestUri.AbsoluteUri : "";
        }

        private static bool IsArtistBlacklisted(FullTrack track, OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;

            try
            {
                foreach (string s in Settings.Settings.ArtistBlacklist.Where(s =>
                             Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
                {
                    response = Settings.Settings.BotRespBlacklist;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", s);
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", "");
                    response = response.Replace("{errormsg}", "");
                    response = CleanFormatString(response);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Artist Blacklist");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsInQueue(string id)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<RequestObject> temp = GlobalObjects.ReqList.Where(x => x.Trackid == id).ToList();

            return temp.Count > 0;
        }

        private static bool IsSongBlacklisted(string trackId)
        {
            try
            {
                if (Settings.Settings.SongBlacklist != null &&
                    Settings.Settings.SongBlacklist.Any(s => s.TrackId == trackId))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Song Blacklist");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsTrackAlreadyInQueue(FullTrack track, OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;
            try
            {
                if (IsInQueue(track.Id))
                {
                    response = Settings.Settings.BotRespIsInQueue;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", "");
                    response = response.Replace("{errormsg}", "");
                    response = CleanFormatString(response);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Track Already In Queue");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsTrackTooLong(FullTrack track, OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;

            try
            {
                if (track.DurationMs >= TimeSpan.FromMinutes(Settings.Settings.MaxSongLength).TotalMilliseconds)
                {
                    response = Settings.Settings.BotRespLength;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", "");
                    response = response.Replace("{errormsg}", "");
                    response = response.Replace("{maxlength}", Settings.Settings.MaxSongLength.ToString());
                    response = CleanFormatString(response);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Track Too Long");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsTrackUnavailable(FullTrack track, OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;
            try
            {
                if (track.IsPlayable != null && (bool)track.IsPlayable)
                {
                    return false;
                }

                response = Settings.Settings.BotRespUnavailable;
                response = response.Replace("{user}", e.ChatMessage.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking Track Unavailable");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsUserAtMaxRequests(OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;

            try
            {
                if (!Settings.Settings.TwSrUnlimitedSr &&
                    MaxQueueItems(e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage)))
                {
                    response = Settings.Settings.BotRespMaxReq;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}",
                        $"{(TwitchUserLevels)CheckUserLevel(e.ChatMessage)} {GetMaxRequestsForUserlevel(CheckUserLevel(e.ChatMessage))}");
                    response = response.Replace("{errormsg}", "");
                    response = CleanFormatString(response);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Issue checking User At Max Requests");
                Logger.LogExc(ex);
            }

            return false;
        }

        private static bool IsUserBlocked(string displayName)
        {
            // checks if one of the artist in the requested song is on the blacklist
            return Settings.Settings.UserBlacklist.Any(s =>
                s.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));
        }

        private static bool MaxQueueItems(string requester, int userLevel)
        {
            int maxreq;
            // Checks if the requester already reached max songrequests
            List<RequestObject> temp = GlobalObjects.ReqList.Where(x => x.Requester == requester).ToList();

            switch ((TwitchUserLevels)userLevel)
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

        private static void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} PubSub: Response received: {e.Response}");
        }

        private static void OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(MainWindow))
                        continue;
                    ((MainWindow)window).IconTwitchPubSub.Foreground = Brushes.IndianRed;
                    ((MainWindow)window).IconTwitchPubSub.Kind = PackIconBootstrapIconsKind.TriangleFill;
                }
            });
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Closed");
            Logger.LogStr("PUBSUB: Disconnected");
        }

        private static void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            TwitchPubSub.SendTopics(Settings.Settings.TwitchAccessToken);
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(MainWindow))
                        continue;
                    ((MainWindow)window).IconTwitchPubSub.Foreground = Brushes.GreenYellow;
                    ((MainWindow)window).IconTwitchPubSub.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                }
            });
            Logger.LogStr("PUBSUB: Connected");
            SendChatMessage(Settings.Settings.TwChannel, "Connected to PubSub");
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Connected");
        }

        private static async void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Error {e.Exception}");
            Logger.LogStr("PUBSUB: Error");
            Logger.LogExc(e.Exception);
            TwitchPubSub.Disconnect();
            //TODO: Enable this again once the PubSub issues are fixed
            if (PubSubEnabled)
                TwitchPubSub.Connect();
            await Task.Delay(30000);
            try
            {
                CreatePubSubListenEvents();
                TwitchPubSub.Connect();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private static void OnStreamDown(object sender, OnStreamDownArgs args)
        {
            Logger.LogStr("TWITCH API: Stream is down");
            Settings.Settings.IsLive = false;
        }

        private static void OnStreamUp(object sender, OnStreamUpArgs args)
        {
            Logger.LogStr("TWITCH API: Stream is up");
            Settings.Settings.IsLive = true;
        }

        private static async void PubSub_OnChannelPointsRewardRedeemed(object sender,
            OnChannelPointsRewardRedeemedArgs e)
        {
            Settings.Settings.IsLive = await CheckStreamIsUp();
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus)
                        SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            if (Client == null || !Client.IsConnected)
                return;
            Redemption redemption = e.RewardRedeemed.Redemption;
            Reward reward = e.RewardRedeemed.Redemption.Reward;
            TwitchLib.PubSub.Models.Responses.Messages.User redeemedUser = e.RewardRedeemed.Redemption.User;
            string trackId;

            List<CustomReward> managableRewards = await GetChannelRewards(true);
            bool isManagable = managableRewards.Find(r => r.Id == reward.Id) != null;

            if (Settings.Settings.TwRewardId.Any(o => o == reward.Id))
            {
                Logger.LogStr($"PUBSUB: Channel reward {reward.Title} redeemed by {redeemedUser.DisplayName}");
                int userlevel = Users.Find(o => o.UserId == redeemedUser.Id).UserLevel;
                Logger.LogStr(
                    $"{redeemedUser.DisplayName}s userlevel = {userlevel} ({Enum.GetName(typeof(TwitchUserLevels), userlevel)})");
                string msg;
                if (userlevel < Settings.Settings.TwSrUserLevel)
                {
                    msg =
                        $"Sorry, only {Enum.GetName(typeof(TwitchUserLevels), Settings.Settings.TwSrUserLevel)} or higher can request songs.";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 0) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus =
                            await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                Settings.Settings.TwitchUser.Id, reward.Id,
                                new List<string> { e.RewardRedeemed.Redemption.Id },
                                new UpdateCustomRewardRedemptionStatusRequest
                                { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += $" {Settings.Settings.BotRespRefund}";
                        }
                    }

                    if (!string.IsNullOrEmpty(msg))
                        SendChatMessage(Settings.Settings.TwChannel, msg);
                    return;
                }

                if (IsUserBlocked(redeemedUser.DisplayName))
                {
                    msg = "You are blocked from making Songrequests";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 1) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus =
                            await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                Settings.Settings.TwitchUser.Id, reward.Id,
                                new List<string> { e.RewardRedeemed.Redemption.Id },
                                new UpdateCustomRewardRedemptionStatusRequest
                                { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += $" {Settings.Settings.BotRespRefund}";
                        }
                    }

                    if (!string.IsNullOrEmpty(msg))
                        SendChatMessage(Settings.Settings.TwChannel, msg);
                    return;
                }

                // checks if the user has already the max amount of songs in the queue
                if (!Settings.Settings.TwSrUnlimitedSr && MaxQueueItems(redeemedUser.DisplayName, userlevel))
                {
                    // if the user reached max requests in the queue skip and inform requester
                    string response = Settings.Settings.BotRespMaxReq;
                    response = response.Replace("{user}", redeemedUser.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}",
                        $"{(TwitchUserLevels)userlevel} {GetMaxRequestsForUserlevel(userlevel)}");
                    response = response.Replace("{errormsg}", "");
                    response = CleanFormatString(response);
                    if (!string.IsNullOrEmpty(response))
                        SendChatMessage(Settings.Settings.TwChannel, response);
                    return;
                }

                if (SpotifyApiHandler.Spotify == null)
                {
                    msg = "It seems that Spotify is not connected right now.";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 2) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus =
                            await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                Settings.Settings.TwitchUser.Id, reward.Id,
                                new List<string> { e.RewardRedeemed.Redemption.Id },
                                new UpdateCustomRewardRedemptionStatusRequest
                                { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += $" {Settings.Settings.BotRespRefund}";
                        }
                    }

                    SendChatMessage(Settings.Settings.TwChannel, msg);
                    return;
                }

                // if Spotify is connected and working manipulate the string and call methods to get the song info accordingly
                trackId = await GetTrackIdFromInput(redemption.UserInput);
                if (trackId == "shortened")
                {
                    SendChatMessage(Settings.Settings.TwChannel,
                        "Spotify short links are not supported. Please type in the full title or get the Spotify URI (starts with \"spotify:track:\")");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(trackId))
                {
                    if (Settings.Settings.SongBlacklist.Any(s => s.TrackId == trackId))
                    {
                        Debug.WriteLine("This song is blocked");
                        SendChatMessage(Settings.Settings.TwChannel, "This song is blocked");
                        return;
                    }

                    ReturnObject returnObject = await AddSong2(trackId, redeemedUser.DisplayName);
                    msg = returnObject.Msg;
                    if (Settings.Settings.RefundConditons.Any(i => i == returnObject.Refundcondition) && isManagable)
                    {
                        try
                        {
                            UpdateRedemptionStatusResponse updateRedemptionStatus =
                                await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                    Settings.Settings.TwitchUser.Id, reward.Id,
                                    new List<string> { e.RewardRedeemed.Redemption.Id },
                                    new UpdateCustomRewardRedemptionStatusRequest
                                    { Status = CustomRewardRedemptionStatus.CANCELED },
                                    Settings.Settings.TwitchAccessToken);
                            if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                            {
                                msg += $" {Settings.Settings.BotRespRefund}";
                            }
                        }
                        catch (Exception)
                        {
                            Logger.LogStr(
                                "PUBSUB: Could not refund points. Has the reward been created through the app?");
                        }
                    }

                    if (!string.IsNullOrEmpty(msg))
                    {
                        if (msg.StartsWith("[announce "))
                        {
                            await AnnounceInChat(msg);
                        }
                        else
                        {
                            SendChatMessage(Settings.Settings.TwChannel, msg);
                        }
                    }
                }
                else
                {
                    // if no track has been found inform the requester
                    string response = Settings.Settings.BotRespError;
                    response = response.Replace("{user}", redeemedUser.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", "");
                    response = response.Replace("{errormsg}", "Couldn't find a song matching your request.");

                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 7) && isManagable)
                    {
                        try
                        {
                            UpdateRedemptionStatusResponse updateRedemptionStatus =
                                await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                    Settings.Settings.TwitchUser.Id, reward.Id,
                                    new List<string> { e.RewardRedeemed.Redemption.Id },
                                    new UpdateCustomRewardRedemptionStatusRequest
                                    { Status = CustomRewardRedemptionStatus.CANCELED });
                            if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                            {
                                response += $" {Settings.Settings.BotRespRefund}";
                            }
                        }
                        catch (Exception)
                        {
                            Logger.LogStr(
                                "PUBSUB: Could not refund points. Has the reward been created through the app?");
                        }
                    }

                    if (!string.IsNullOrEmpty(response))
                    {
                        if (response.StartsWith("[announce "))
                        {
                            await AnnounceInChat(response);
                        }
                        else
                        {
                            SendChatMessage(Settings.Settings.TwChannel, response);
                        }
                    }
                }
            }

            if (reward.Id == Settings.Settings.TwRewardSkipId)
            {
                if (_skipCooldown)
                    return;
                ErrorResponse response = await SpotifyApiHandler.SkipSong();
                if (response.Error != null)
                {
                    SendChatMessage(Settings.Settings.TwChannel, "Error: " + response.Error.Message);
                }
                else
                {
                    SendChatMessage(Settings.Settings.TwChannel, "Skipping current song...");
                    _skipCooldown = true;
                    SkipCooldownTimer.Start();
                }
            }

            if (reward.Id == Settings.Settings.TwRewardGoalRewardId)
            {
                if (!Settings.Settings.RewardGoalEnabled) return;
                GlobalObjects.RewardGoalCount++;
                //Debug.WriteLine($"{GlobalObjects.RewardGoalCount} / {Settings.Settings.RewardGoalAmount}");

                if (GlobalObjects.RewardGoalCount % 10 == 0)
                {
                    Console.WriteLine(@"Reached count " + GlobalObjects.RewardGoalCount);
                    SendChatMessage(Settings.Settings.TwChannel,
                        $"Reached {GlobalObjects.RewardGoalCount} of {Settings.Settings.RewardGoalAmount}");
                }

                if (Settings.Settings.RewardGoalAmount - GlobalObjects.RewardGoalCount < 10)
                {
                    Console.WriteLine(@"Reached count " + GlobalObjects.RewardGoalCount);
                    SendChatMessage(Settings.Settings.TwChannel,
                        $"Reached {GlobalObjects.RewardGoalCount} of {Settings.Settings.RewardGoalAmount}");
                }

                if (GlobalObjects.RewardGoalCount >= Settings.Settings.RewardGoalAmount)
                {
                    GlobalObjects.RewardGoalCount = 0;
                    //Debug.WriteLine($"{GlobalObjects.RewardGoalCount} / {Settings.Settings.RewardGoalAmount}");
                    SendChatMessage(Settings.Settings.TwChannel,
                        "The reward goal has been reached! " + Settings.Settings.RewardGoalAmount);
                    string input = Settings.Settings.RewardGoalSong;
                    Match match = Regex.Match(input, @"track\/([^\?]+)");
                    if (match.Success)
                    {
                        string songId = match.Groups[1].Value;
                        ErrorResponse response = SpotifyApiHandler.AddToQ($"spotify:track:{songId}");
                        if (response != null && !response.HasError())
                            await SpotifyApiHandler.SkipSong();
                    }
                }
            }
        }

        private static async void SendChatMessage(string channel, string message)
        {
            if (message.StartsWith("[announce "))
            {
                await AnnounceInChat(message);
                return;
            }

            if (Client.IsConnected && Client.JoinedChannels.Any(c => c.Channel == channel))
                Client.SendMessage(channel, message);
        }

        private static void SkipCooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _skipCooldown = false;
            SkipCooldownTimer.Stop();
        }

        private static void StartCooldown()
        {
            // starts the cooldown on the command
            _onCooldown = true;
            CooldownTimer.Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds;
            CooldownTimer.Start();
            CooldownStopwatch.Reset();
            CooldownStopwatch.Start();
        }

        private static async Task UploadToQueue(FullTrack track, string displayName)
        {
            try
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

                string length = FormattedTime((int)track.DurationMs);

                // upload to the queue
                //WebHelper.UpdateWebQueue(track.Id, artists, track.Name, length, displayName, "0", "i");

                dynamic payload = new
                {
                    uuid = Settings.Settings.Uuid,
                    key = Settings.Settings.AccessKey,
                    queueItem = new RequestObject
                    {
                        Trackid = track.Id,
                        Artist = artists,
                        Title = track.Name,
                        Length = length,
                        Requester = displayName,
                        Played = 0,
                        Albumcover = track.Album.Images[0].Url,
                    }
                };

                await WebHelper.QueueRequest(WebHelper.RequestMethod.Post, Json.Serialize(payload));
                GlobalObjects.QueueUpdateQueueWindow();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }
    }

    public class TwitchUser
    {
        public string DisplayName { get; set; }
        public string UserId { get; set; }
        public int UserLevel { get; set; }
        public string UserName { get; set; }

        public void Update(string username, string displayname, int userlevel)
        {
            UserName = username;
            DisplayName = displayname;
            UserLevel = userlevel;
        }
    }

    internal class QueueItem
    {
        public int Position { get; set; }
        public string Requester { get; set; }
        public string Title { get; set; }
    }

    internal class ReturnObject
    {
        public string Msg { get; set; }
        public int Refundcondition { get; set; }
        public bool Success { get; set; }
    }
}