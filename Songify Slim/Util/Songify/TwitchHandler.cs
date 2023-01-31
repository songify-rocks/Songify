using Songify_Slim.Models;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateRedemptionStatus;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
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
using Timer = System.Timers.Timer;
using VonRiddarn.Twitch.ImplicitOAuth;
using Application = System.Windows.Application;
using Window = System.Windows.Window;
using System.Windows.Forms;
using System.Windows.Threading;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomRewardRedemption;

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
        private static List<string> _skipVotes = new List<string>();
        private static readonly Timer CooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds
        };
        private static readonly Timer SkipCooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(5).TotalMilliseconds
        };
        public static TwitchAPI _twitchApi;
        private static TwitchPubSub _twitchPubSub;
        private static string _clientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";
        private static string userId;
        public static List<TwitchUser> users = new List<TwitchUser>();
        public static ValidateAccessTokenResponse TokenCheck;

        public static void ResetVotes()
        {
            _skipVotes.Clear();
            Logger.LogStr("TWITCH IRC: Reset votes");
        }

        public static void APIConnect()
        {
            ImplicitOAuth ioa = new ImplicitOAuth(1234);
            string currentState = null;

            // This event is triggered when the application recieves a new token and state from the "RequestClientAuthorization" method.
            ioa.OnRevcievedValues += async (state, token) =>
            {
                if (state != currentState)
                {
                    Console.WriteLine("State does not match up. Possible CSRF attack or other error.");
                    return;
                }

                // Don't actually print the user token on screen or to the console.
                // Here you should save it where the application can access it whenever it wants to, such as in appdata.
                Settings.Settings.TwitchAccessToken = token;
                await InitializeApi();

                Settings.Settings.TwChannel = Settings.Settings.TwitchUser.Login;
                bool shownInSettings = false;
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() != typeof(Window_Settings)) continue;
                        await ((Window_Settings)window).ShowMessageAsync(Properties.Resources.msgbx_BotAccount, Properties.Resources.msgbx_UseAsBotAccount.Replace("{account}", Settings.Settings.TwitchUser.DisplayName), MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Properties.Resources.msgbx_Yes,
                            NegativeButtonText = Properties.Resources.msgbx_No,
                            DefaultButtonFocus = MessageDialogResult.Affirmative
                        }).ContinueWith(x =>
                        {
                            if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                            Settings.Settings.TwOAuth = $"oauth:{Settings.Settings.TwitchAccessToken}";
                            Settings.Settings.TwAcc = Settings.Settings.TwitchUser.Login;
                            return Task.CompletedTask;
                        });
                        ((Window_Settings)window).SetControls();
                        shownInSettings = true;
                        break;
                    }
                    if (!shownInSettings)
                    {
                        (Application.Current.MainWindow as MainWindow)?.ShowMessageAsync(Properties.Resources.msgbx_BotAccount, Properties.Resources.msgbx_UseAsBotAccount.Replace("{account}", Settings.Settings.TwitchUser.DisplayName), MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Properties.Resources.msgbx_Yes,
                            NegativeButtonText = Properties.Resources.msgbx_No,
                            DefaultButtonFocus = MessageDialogResult.Affirmative
                        }).ContinueWith(x =>
                        {
                            if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                            Settings.Settings.TwOAuth = $"oauth:{Settings.Settings.TwitchAccessToken}";
                            Settings.Settings.TwAcc = Settings.Settings.TwitchUser.Login;
                            return Task.CompletedTask;
                        });
                    }
                    BotConnect();
                    WebHelper.SendTelemetry();
                });
            };

            // This method initialize the flow of getting the token and returns a temporary random state that we will use to check authenticity.
            currentState = ioa.RequestClientAuthorization();
        }

        public static async Task<bool> CheckStreamIsUp()
        {
            if (TokenCheck == null) return false;
            Logger.LogStr("TWITCH API: Checking if stream is up...");
            GetStreamsResponse x = await _twitchApi.Helix.Streams.GetStreamsAsync(null, 20, null, null,
                new List<string> { Settings.Settings.TwitchUser.Id }, null, Settings.Settings.TwitchAccessToken);
            if (x.Streams.Length != 0)
            {
                Logger.LogStr("TWITCH API: Stream is up");
                return x.Streams[0].Type == "live";
            }
            Logger.LogStr("TWITCH API: Stream is down");
            return false;
        }

        public static async Task InitializeApi()
        {
            _twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _clientId,
                    AccessToken = Settings.Settings.TwitchAccessToken
                }
            };

            TokenCheck = await _twitchApi.Auth.ValidateAccessTokenAsync(Settings.Settings.TwitchAccessToken);

            if (TokenCheck == null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() != typeof(MainWindow))
                            continue;
                        ((MainWindow)window).IconTwitchAPI.Foreground = Brushes.IndianRed;
                        ((MainWindow)window).IconTwitchAPI.Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill;
                    }
                });
                return;
            }

            userId = TokenCheck.UserId;

            var users = await _twitchApi.Helix.Users.GetUsersAsync(new List<string> { userId }, null, Settings.Settings.TwitchAccessToken);

            User user = users.Users.FirstOrDefault();
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
            _twitchPubSub = new TwitchPubSub();
            _twitchPubSub.OnListenResponse += OnListenResponse;
            _twitchPubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _twitchPubSub.OnPubSubServiceClosed += OnPubSubServiceClosed;
            _twitchPubSub.OnPubSubServiceError += OnPubSubServiceError;

            _twitchPubSub.OnStreamUp += (sender, args) =>
            {
                Logger.LogStr("TWITCH API: Stream is up");
                Settings.Settings.IsLive = true;
            };
            _twitchPubSub.OnStreamDown += (sender, args) =>
            {
                Logger.LogStr("TWITCH API: Stream is down");
                Settings.Settings.IsLive = false;
            };

            _twitchPubSub.ListenToVideoPlayback(Settings.Settings.TwitchChannelId);
            _twitchPubSub.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
            _twitchPubSub.ListenToChannelPoints(Settings.Settings.TwitchChannelId);
            _twitchPubSub.Connect();
        }

        private static async void PubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            await CheckStreamIsUp();
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus)
                        Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            if (Client == null || !Client.IsConnected)
                return;
            var redemption = e.RewardRedeemed.Redemption;
            var reward = e.RewardRedeemed.Redemption.Reward;
            var redeemedUser = e.RewardRedeemed.Redemption.User;
            string trackId = "";

            List<CustomReward> managableRewards = await GetChannelRewards(true);
            bool isManagable = managableRewards.Find(r => r.Id == reward.Id) != null;

            if (reward.Id == Settings.Settings.TwRewardId)
            {
                Logger.LogStr($"PUBSUB: Channel reward {reward.Title} redeemed by {redeemedUser.DisplayName}");
                int userlevel = users.Find(o => o.UserId == redeemedUser.Id).UserLevel;
                Logger.LogStr($"{redeemedUser.DisplayName}s userlevel = {userlevel} ({Enum.GetName(typeof(TwitchUserLevels), userlevel)})");
                string msg = "";
                if (userlevel < Settings.Settings.TwSrUserLevel)
                {
                    msg = $"Sorry, only {Enum.GetName(typeof(TwitchUserLevels), Settings.Settings.TwSrUserLevel)} or higher can request songs.";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 0) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus = await _twitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(userId, reward.Id,
                            new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += " Your points have been refunded.";
                        }
                    }

                    Client.SendMessage(Settings.Settings.TwChannel, msg);
                    return;
                }
                if (IsUserBlocked(redeemedUser.DisplayName))
                {
                    msg = "You are blocked from making Songrequests";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 1) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus = await _twitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(userId, reward.Id,
                            new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += " Your points have been refunded.";
                        }
                    }

                    Client.SendMessage(Settings.Settings.TwChannel, msg);
                    return;
                }
                // checks if the user has already the max amount of songs in the queue
                if (!Settings.Settings.TwSrUnlimitedSr)
                    if (MaxQueueItems(redeemedUser.DisplayName, userlevel))
                    {
                        // if the user reached max requests in the queue skip and inform requester
                        string response = Settings.Settings.BotRespMaxReq;
                        response = response.Replace("{user}", redeemedUser.DisplayName);
                        response = response.Replace("{artist}", "");
                        response = response.Replace("{title}", "");
                        response = response.Replace("{maxreq}", $"{(TwitchUserLevels)userlevel} {GetMaxRequestsForUserlevel(userlevel)}");
                        response = response.Replace("{errormsg}", "");
                        response = CleanFormatString(response);
                        Client.SendMessage(Settings.Settings.TwChannel, response);
                        return;
                    }
                if (ApiHandler.Spotify == null)
                {
                    msg = "It seems that Spotify is not connected right now.";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 2) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus = await _twitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(userId, reward.Id,
                            new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
                        if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                        {
                            msg += " Your points have been refunded.";
                        }
                    }

                    Client.SendMessage(Settings.Settings.TwChannel, msg);
                    return;
                }
                // if Spotify is connected and working manipulate the string and call methods to get the song info accordingly
                if (redemption.UserInput.StartsWith("spotify:track:"))
                {
                    // search for a track with the id
                    trackId = redemption.UserInput.Replace("spotify:track:", "");
                }
                else if (redemption.UserInput.StartsWith("https://open.spotify.com/track/"))
                {
                    trackId = redemption.UserInput.Replace("https://open.spotify.com/track/", "");
                    trackId = trackId.Split('?')[0];
                }
                else
                {
                    // search for a track with a search string from chat
                    SearchItem searchItem = ApiHandler.FindTrack(HttpUtility.UrlEncode(redemption.UserInput));
                    if (searchItem.HasError())
                    {
                        Client.SendMessage(Settings.Settings.TwChannel, searchItem.Error.Message);
                        return;
                    }

                    if (searchItem.Tracks.Items.Count > 0)
                    {
                        // if a track was found convert the object to FullTrack (easier use than searchItem)
                        FullTrack fullTrack = searchItem.Tracks.Items[0];
                        trackId = fullTrack.Id;
                    }
                }
                if (!string.IsNullOrWhiteSpace(trackId))
                {
                    ReturnObject returnObject = AddSong2(trackId, Settings.Settings.TwChannel, redeemedUser.DisplayName);
                    msg = returnObject.Msg;
                    if (Settings.Settings.RefundConditons.Any(i => i == returnObject.Refundcondition) && isManagable)
                    {
                        try
                        {
                            UpdateRedemptionStatusResponse updateRedemptionStatus = await _twitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(userId, reward.Id,
                        new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
                            if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                            {
                                msg += " Your points have been refunded.";
                            }
                        }
                        catch (Exception)
                        {
                            Logger.LogStr("PUBSUB: Could not refund points. Has the reward been created through the app?");
                        }
                    }
                    Client.SendMessage(Settings.Settings.TwChannel, msg);

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
                            UpdateRedemptionStatusResponse updateRedemptionStatus = await _twitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(userId, reward.Id,
                            new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
                            if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                            {
                                response += " Your points have been refunded.";
                            }
                        }
                        catch (Exception)
                        {
                            Logger.LogStr("PUBSUB: Could not refund points. Has the reward been created through the app?");
                        }
                    }

                    Client.SendMessage(Settings.Settings.TwChannel, response);
                }
            }
            else if (reward.Id == Settings.Settings.TwRewardSkipId)
            {
                if (_skipCooldown)
                    return;
                ErrorResponse response = await ApiHandler.SkipSong();
                if (response.Error != null)
                {
                    Client.SendMessage(Settings.Settings.TwChannel, "Error: " + response.Error.Message);
                }
                else
                {
                    Client.SendMessage(Settings.Settings.TwChannel, "Skipping current song...");
                    _skipCooldown = true;
                    SkipCooldownTimer.Start();
                }
            }
        }

        private static void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Error {e.Exception}");
            Logger.LogStr($"PUBSUB: Error");
            Logger.LogExc(e.Exception);
            _twitchPubSub.Disconnect();
            _twitchPubSub.Connect();
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
            Logger.LogStr($"PUBSUB: Disconnected");
        }

        private static void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            _twitchPubSub.SendTopics(Settings.Settings.TwitchAccessToken);
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
            Logger.LogStr($"PUBSUB: Connected");
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Connected");
        }

        private static void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} PubSub: Response received: {e.Response}");
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
                    ((MainWindow)window).notifyIcon.ContextMenu.MenuItems[0].MenuItems[0].Enabled = true;
                    ((MainWindow)window).notifyIcon.ContextMenu.MenuItems[0].MenuItems[1].Enabled = false;
                    ((MainWindow)window).IconTwitchBot.Foreground = Brushes.IndianRed;
                    ((MainWindow)window).IconTwitchBot.Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill;

                }
            });
            Logger.LogStr("TWITCH: Disconnected from Twitch Chat");
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
                    ((MainWindow)window).notifyIcon.ContextMenu.MenuItems[0].MenuItems[0].Enabled = false;
                    ((MainWindow)window).notifyIcon.ContextMenu.MenuItems[0].MenuItems[1].Enabled = true;
                    ((MainWindow)window).IconTwitchBot.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                    ((MainWindow)window).IconTwitchBot.Foreground = Brushes.GreenYellow;

                }
            });
            Logger.LogStr("TWITCH: Connected to Twitch");
        }

        private static async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            await CheckStreamIsUp();
            if (users.All(o => o.UserId != e.ChatMessage.UserId))
            {
                users.Add(new TwitchUser
                {
                    UserId = e.ChatMessage.UserId,
                    UserName = e.ChatMessage.Username,
                    DisplayName = e.ChatMessage.DisplayName,
                    UserLevel = CheckUserLevel(e.ChatMessage)
                });
            }
            else
            {
                users.Find(o => o.UserId == e.ChatMessage.UserId).Update(e.ChatMessage.Username, e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage));
            }

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

            // Same code from above but it reacts to a command instead of rewards
            if (Settings.Settings.TwSrCommand && e.ChatMessage.Message.StartsWith($"!{Settings.Settings.BotCmdSsrTrigger}"))
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
                    string searchString = e.ChatMessage.Message.Replace($"!{Settings.Settings.BotCmdSsrTrigger} ", "");
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

            if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdSkipTrigger}" && Settings.Settings.BotCmdSkip)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
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
                    int? reqListCount = GlobalObjects.ReqList.Count;
                    if (reqListCount != null)
                        count = (int)reqListCount;
                    if (count > 0)
                        name = GlobalObjects.ReqList.First().Requester;
                });

                if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster ||
                    (count > 0 && name == e.ChatMessage.DisplayName))
                {
                    string msg = Settings.Settings.BotRespModSkip;
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
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdVoteskipTrigger}" && Settings.Settings.BotCmdSkipVote)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
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
                if (!_skipVotes.Contains(e.ChatMessage.DisplayName))
                {
                    _skipVotes.Add(e.ChatMessage.DisplayName);

                    string msg = Settings.Settings.BotRespVoteSkip;
                    msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                    msg = msg.Replace("{votes}", $"{_skipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}");

                    Client.SendMessage(e.ChatMessage.Channel, msg);

                    if (_skipVotes.Count >= Settings.Settings.BotCmdSkipVoteCount)
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

                        _skipVotes.Clear();
                        _skipCooldown = true;
                        SkipCooldownTimer.Start();
                    }
                }
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdSongTrigger}" && Settings.Settings.BotCmdSong)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }
                string currsong = GetCurrentSong();
                Client.SendMessage(e.ChatMessage.Channel, $"@{e.ChatMessage.DisplayName} {currsong}");
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdPosTrigger}" && Settings.Settings.BotCmdPos)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }
                List<QueueItem> queueItems = GetQueueItems(e.ChatMessage.DisplayName);
                string output = "";
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
                        tmp += between.Replace("{pos}", item.Position.ToString()).Replace("{song}", item.Title);
                        //If the song is the last one, don't add a newline
                        if (i != queueItems.Count - 1)
                            tmp += " | ";
                    }

                    between = tmp;
                    // Combine the 3 parts into one string
                    output = before + between + after;
                    Client.SendMessage(e.ChatMessage.Channel, $"{output}");
                }
                else
                {
                    Client.SendMessage(e.ChatMessage.Channel,
                        $"@{e.ChatMessage.DisplayName} you have no Songs in the current Queue");
                }
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdNextTrigger}" && Settings.Settings.BotCmdNext)
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
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

                Client.SendMessage(e.ChatMessage.Channel, response);
            }
            else if (e.ChatMessage.Message == "!remove")
            {
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus)
                            Client.SendMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }
                string tmp = "";
                RequestObject reqObj = GlobalObjects.ReqList.FindLast(o =>
                    o.Requester == e.ChatMessage.DisplayName);
                if (reqObj == null) return;
                tmp = $"{reqObj.Artists} - {reqObj.Title}";
                GlobalObjects.SkipList.Add(reqObj);
                GlobalObjects.ReqList.Remove(reqObj);
                WebHelper.UpdateWebQueue(reqObj.TrackID, "", "", "", "", "1", "u");
                UpdateQueueWindow();
                Client.SendMessage(e.ChatMessage.Channel,
                    $"@{e.ChatMessage.DisplayName} your previous requst ({tmp}) will be skipped");
            }
        }

        private static bool CheckLiveStatus()
        {
            if (Settings.Settings.IsLive || !Settings.Settings.BotOnlyWorkWhenLive) return true;
            Application.Current.BeginInvoke(() =>
            {
                (Application.Current.MainWindow as MainWindow)?.Invoke(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).LblStatus.Content = "Command cancelled. Stream is offline.";
                });
            }, DispatcherPriority.Normal);
            return false;
        }

        private static string GetNextSong()
        {
            int index = 0;
            if (GlobalObjects.ReqList.Count == 0)
            {
                return "There is no song next up.";
            }

            if (GlobalObjects.ReqList.Count > 0 && GlobalObjects.ReqList[0].TrackID == GlobalObjects.CurrentSong.SongID)
            {
                if (GlobalObjects.ReqList.Count <= 1)
                {
                    return "There is no song next up.";
                }

                index = 1;
            }

            return $"{GlobalObjects.ReqList[index].Artists} - {GlobalObjects.ReqList[index].Title}";
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
            // checks if one of the artist in the requested song is on the blacklist
            return Settings.Settings.UserBlacklist.Any(s => s.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));
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
            string response;
            // gets the track information using spotify api
            FullTrack track = ApiHandler.GetTrack(trackId);
            if (track.IsPlayable != null && (bool)!track.IsPlayable)
            {
                Client.SendMessage(e.ChatMessage.Channel, "This track is not available in the streamers region.");
                return;
            }

            string artists = "";
            for (int i = 0; i < track.Artists.Count; i++)
                if (i != track.Artists.Count - 1)
                    artists += track.Artists[i].Name + ", ";
                else
                    artists += track.Artists[i].Name;

            // checks if one of the artist in the requested song is on the blacklist
            foreach (string s in Settings.Settings.ArtistBlacklist.Where(s => Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
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
            if (!Settings.Settings.TwSrUnlimitedSr)
                if (MaxQueueItems(e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage)))
                {
                    // if the user reached max requests in the queue skip and inform requester
                    response = Settings.Settings.BotRespMaxReq;
                    response = response.Replace("{user}", e.ChatMessage.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", $"{(TwitchUserLevels)CheckUserLevel(e.ChatMessage)} {GetMaxRequestsForUserlevel(CheckUserLevel(e.ChatMessage))}");
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
            GlobalObjects.ReqList.Add(new RequestObject
            {
                Requester = e.ChatMessage.DisplayName,
                TrackID = track.Id,
                Title = track.Name,
                Artists = artists,
                Length = FormattedTime(track.DurationMs)
            });
            UpdateQueueWindow();
        }

        private static void UpdateQueueWindow()
        {
            // Add the song to the internal queue and update the queue window if its open
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window qw = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(Window_Queue))
                        qw = window;
                }

                (qw as Window_Queue)?.dgv_Queue.Items.Refresh();
            });
        }

        private static ReturnObject AddSong2(string trackId, string channel, string username)
        {
            // loads the blacklist from settings
            string response;
            // gets the track information using spotify api
            FullTrack track = ApiHandler.GetTrack(trackId);

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
            foreach (string s in Settings.Settings.ArtistBlacklist.Where(s => Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
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
            ErrorResponse error = ApiHandler.AddToQ(spotifyUri);
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
            UploadToQueue(track, username);

            // Add the song to the internal queue and update the queue window if its open
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window qw = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(Window_Queue))
                        qw = window;
                }

                GlobalObjects.ReqList.Add(new RequestObject
                {
                    Requester = username,
                    TrackID = track.Id,
                    Title = track.Name,
                    Artists = artists,
                    Length = FormattedTime(track.DurationMs)
                });

                (qw as Window_Queue)?.dgv_Queue.Items.Refresh();
            });

            return new ReturnObject
            {
                Msg = response,
                Success = true,
                Refundcondition = 8
            };
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
            temp = GlobalObjects.ReqList.FindAll(x => x.TrackID == id);

            return temp.Count > 0;
        }

        private static List<QueueItem> GetQueueItems(string requester = null)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<RequestObject> temp = new List<RequestObject>();
            List<QueueItem> temp3 = new List<QueueItem>();
            string currsong = "";
            temp = GlobalObjects.ReqList;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(MainWindow))
                    {
                        currsong = $"{(window as MainWindow)?._artist} - {(window as MainWindow)?._title}";
                    }
            });

            if (requester != null)
            {
                List<RequestObject> temp2 = temp.FindAll(x => x.Requester == requester);
                temp3.AddRange(from requestObject in temp2 let pos = temp.IndexOf(requestObject) + 1 select new QueueItem { Position = pos, Title = requestObject.Artists + " - " + requestObject.Title, Requester = requestObject.Requester });
                return temp3;
            }

            if (temp.Count <= 0) return null;
            if (temp.Count == 1 && $"{temp[0].Artists} - {temp[0].Title}" != currsong)
            {
                temp3.Add(new QueueItem
                {
                    Title = $"{temp[0].Artists} - {temp[0].Title}",
                    Requester = $"{temp[0].Requester}"
                });
                return temp3;
            }

            if (temp.Count <= 1) return null;
            temp3.Add(new QueueItem
            {
                Title = $"{temp[1].Artists} - {temp[1].Title}",
                Requester = $"{temp[1].Requester}"
            });
            return temp3;
        }

        private static bool MaxQueueItems(string requester, int userLevel)
        {
            int maxreq;
            // Checks if the requester already reached max songrequests
            var temp = new List<RequestObject>();
            temp = GlobalObjects.ReqList.FindAll(x => x.Requester == requester);

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

        public static void SendCurrSong(string song)
        {
            if (Client != null && Client.IsConnected)
                Client.SendMessage(Settings.Settings.TwChannel, song);
        }

        public static async Task<List<CustomReward>> GetChannelRewards(bool b)
        {
            GetCustomRewardsResponse rewardsResponse = null;
            try
            {
                rewardsResponse = await _twitchApi.Helix.ChannelPoints.GetCustomRewardAsync(Settings.Settings.TwitchChannelId, null, b);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return rewardsResponse?.Data.ToList();
        }
    }

    internal class ReturnObject
    {
        public string Msg { get; set; }
        public bool Success { get; set; }
        public int Refundcondition { get; set; }

    }

    internal class QueueItem
    {
        public string Requester { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }
    }

    public class TwitchUser
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public int UserLevel { get; set; }

        public void Update(string username, string displayname, int userlevel)
        {
            UserName = username;
            DisplayName = displayname;
            UserLevel = userlevel;
        }
    }
}