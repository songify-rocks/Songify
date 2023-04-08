using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Properties;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify.TwitchOAuth;
using Songify_Slim.Views;
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
using System.Windows.Threading;
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
using Unosquare.Swan;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

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
        public static TwitchClient MainClient;
        private static bool _onCooldown;
        private static bool _skipCooldown;
        public const bool PubSubEnabled = false;
        public static bool ForceDisconnect;
        private static readonly List<string> SkipVotes = new List<string>();

        private static readonly Timer CooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown).TotalMilliseconds
        };

        private static readonly Timer SkipCooldownTimer = new Timer
        {
            Interval = TimeSpan.FromSeconds(5).TotalMilliseconds
        };

        public static TwitchAPI TwitchApi;
        public static TwitchAPI TwitchApiBot;
        private static readonly TwitchPubSub TwitchPubSub = new TwitchPubSub();
        private const string ClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";
        private static string _userId;
        public static List<TwitchUser> Users = new List<TwitchUser>();
        public static ValidateAccessTokenResponse TokenCheck;
        public static ValidateAccessTokenResponse BotTokenCheck;
        private static string _currentState;

        public enum TwitchAccount
        {
            Main,
            Bot
        }

        public static void ResetVotes()
        {
            SkipVotes.Clear();
        }

        public static void ApiConnect(TwitchAccount account)
        {
            ImplicitOAuth ioa = new ImplicitOAuth(1234);

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
                        tst = DateTime.Now.ToUnixEpochDate(),
                        twitch_id = Settings.Settings.TwitchUser.Id,
                        twitch_name = Settings.Settings.TwitchUser.DisplayName,
                        vs = GlobalObjects.AppVersion,
                        playertype = GlobalObjects.GetReadablePlayer(),
                    };
                    WebHelper.TelemetryRequest(WebHelper.RequestMethod.Post, Json.Serialize(telemetryPayload));
                });
            };

            // This method initialize the flow of getting the token and returns a temporary random state that we will use to check authenticity.
            _currentState = ioa.RequestClientAuthorization();
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
                    //if (PubSubEnabled)
                    //    CreatePubSubsConnection();

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

        private static void CreatePubSubsConnection()
        {
            CreatePubSubEventHandlers();
            CreatePubSubListenEvents();
            TwitchPubSub.Connect();
        }

        private static void CreatePubSubListenEvents()
        {
            TwitchPubSub.ListenToVideoPlayback(Settings.Settings.TwitchChannelId);
            TwitchPubSub.ListenToChannelPoints(Settings.Settings.TwitchChannelId);
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
            await CheckStreamIsUp();
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
            var redemption = e.RewardRedeemed.Redemption;
            var reward = e.RewardRedeemed.Redemption.Reward;
            var redeemedUser = e.RewardRedeemed.Redemption.User;
            string trackId;

            List<CustomReward> managableRewards = await GetChannelRewards(true);
            bool isManagable = managableRewards.Find(r => r.Id == reward.Id) != null;

            if (reward.Id == Settings.Settings.TwRewardId)
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

                if (ApiHandler.Spotify == null)
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
                trackId = GetTrackIdFromInput(redemption.UserInput);
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

                    ReturnObject returnObject = AddSong2(trackId, redeemedUser.DisplayName);
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
                ErrorResponse response = await ApiHandler.SkipSong();
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
                Debug.WriteLine($"{GlobalObjects.RewardGoalCount} / {Settings.Settings.RewardGoalAmount}");

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
                    Debug.WriteLine($"{GlobalObjects.RewardGoalCount} / {Settings.Settings.RewardGoalAmount}");
                    SendChatMessage(Settings.Settings.TwChannel,
                        "The reward goal has been reached! " + Settings.Settings.RewardGoalAmount);
                    string input = Settings.Settings.RewardGoalSong;
                    var match = Regex.Match(input, @"track\/([^\?]+)");
                    if (match.Success)
                    {
                        string songId = match.Groups[1].Value;
                        ErrorResponse response = ApiHandler.AddToQ($"spotify:track:{songId}");
                        if (response != null && !response.HasError())
                            await ApiHandler.SkipSong();
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

        private static async void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} PubSub: Error {e.Exception}");
            Logger.LogStr("PUBSUB: Error");
            Logger.LogExc(e.Exception);
            TwitchPubSub.Disconnect();
            //TODO: Enable this again once the PubSub issues are fixed
            //if (PubSubEnabled)
            //    TwitchPubSub.Connect();
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

        private static void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            //Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} PubSub: Response received: {e.Response}");
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
                    new ConnectionCredentials(Settings.Settings.TwitchUser.DisplayName,
                        $"oauth:{Settings.Settings.TwitchAccessToken}");
                ClientOptions clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                WebSocketClient customClient = new WebSocketClient(clientOptions);
                MainClient = new TwitchClient(customClient);
                MainClient.Initialize(credentials, Settings.Settings.TwChannel);
                MainClient.Connect();
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        public static void BotConnect()
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

        private static void ClientOnOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Logger.LogStr($"TWITCH: Joined channel {e.Channel}");
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
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[0].Enabled = false;
                    ((MainWindow)window).NotifyIcon.ContextMenu.MenuItems[0].MenuItems[1].Enabled = true;
                    ((MainWindow)window).IconTwitchBot.Kind = PackIconBootstrapIconsKind.CheckCircleFill;
                    ((MainWindow)window).IconTwitchBot.Foreground = Brushes.GreenYellow;
                }
            });
            Logger.LogStr($"TWITCH: Connected to Twitch. User: {Client.TwitchUsername}");
        }

        private static async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            await CheckStreamIsUp();
            if (Users.All(o => o.UserId != e.ChatMessage.UserId))
            {
                Users.Add(new TwitchUser
                {
                    UserId = e.ChatMessage.UserId,
                    UserName = e.ChatMessage.Username,
                    DisplayName = e.ChatMessage.DisplayName,
                    UserLevel = CheckUserLevel(e.ChatMessage)
                });
            }
            else
            {
                Users.Find(o => o.UserId == e.ChatMessage.UserId).Update(e.ChatMessage.Username,
                    e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage));
            }

            if (e.ChatMessage.CustomRewardId == Settings.Settings.TwRewardId && !PubSubEnabled)
            {
                int userlevel = CheckUserLevel(e.ChatMessage);
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

                // if onCooldown skip
                if (_onCooldown) return;

                if (ApiHandler.Spotify == null)
                {
                    SendChatMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(GetTrackIdFromInput(e.ChatMessage.Message), e);
            }

            // Same code from above but it reacts to a command instead of rewards
            if (Settings.Settings.TwSrCommand &&
                e.ChatMessage.Message.StartsWith($"!{Settings.Settings.BotCmdSsrTrigger}"))
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

                // if onCooldown skip
                if (_onCooldown) return;

                if (ApiHandler.Spotify == null)
                {
                    SendChatMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(
                    GetTrackIdFromInput(e.ChatMessage.Message.Replace($"!{Settings.Settings.BotCmdSsrTrigger}", "")
                        .Trim()), e);

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
                    int? reqListCount = GlobalObjects.ReqList.Count;
                    count = (int)reqListCount;
                    if (count > 0)
                        name = GlobalObjects.ReqList.First().Requester;
                });

                if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster ||
                    (count > 0 && name == e.ChatMessage.DisplayName))
                {
                    string msg = Settings.Settings.BotRespModSkip;
                    msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                    msg = msg.Replace("{song}",
                        $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                    ErrorResponse response = await ApiHandler.SkipSong();
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
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdVoteskipTrigger}" &&
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
                if (!SkipVotes.Contains(e.ChatMessage.DisplayName))
                {
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
                        ErrorResponse response = await ApiHandler.SkipSong();
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
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdSongTrigger}" && Settings.Settings.BotCmdSong)
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

                string msg = GetCurrentSong();
                msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
                msg = msg.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
                if (msg.StartsWith("[announce "))
                {
                    await AnnounceInChat(msg);
                }
                else
                {
                    SendChatMessage(e.ChatMessage.Channel, msg);
                }
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdPosTrigger}" && Settings.Settings.BotCmdPos)
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
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdNextTrigger}" && Settings.Settings.BotCmdNext)
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
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdRemoveTrigger}" &&
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

                RequestObject reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
                    o.Requester == e.ChatMessage.DisplayName);
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
                WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
                await Application.Current.Dispatcher.BeginInvoke(new Action(() => { GlobalObjects.ReqList.Remove(reqObj); }));
                //WebHelper.UpdateWebQueue(reqObj.Trackid, "", "", "", "", "1", "u");


                UpdateQueueWindow();
                SendChatMessage(e.ChatMessage.Channel,
                    $"@{e.ChatMessage.DisplayName} your previous request ({tmp}) will be skipped");
            }
            else if (e.ChatMessage.Message == $"!{Settings.Settings.BotCmdSonglikeTrigger}" &&
                     (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) && Settings.Settings.BotCmdSonglike)
            {
                if (string.IsNullOrWhiteSpace(Settings.Settings.SpotifyPlaylistId))
                {
                    SendChatMessage(Settings.Settings.TwChannel,
                        "No playlist has been specified. Go to Settings -> Spotify and select the playlist you want to use.");
                    return;
                }

                var tracks = await ApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId);
                do
                {
                    if (tracks.Items.Any(t => t.Track.Id == GlobalObjects.CurrentSong.SongId))
                    {
                        SendChatMessage(Settings.Settings.TwChannel,
                            $"The Song \"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}\" is already in the playlist.");
                        return;
                    }

                    tracks = await ApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "",
                        100, tracks.Offset + tracks.Limit);
                } while (tracks.HasNextPage());

                ErrorResponse x = await ApiHandler.Spotify.AddPlaylistTrackAsync(Settings.Settings.SpotifyPlaylistId,
                    $"spotify:track:{GlobalObjects.CurrentSong.SongId}");
                if (x.HasError()) return;
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
            else
                switch (e.ChatMessage.Message)
                // ReSharper disable once BadChildStatementIndent
                {
                    case "!play" when ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
                                       Settings.Settings.BotCmdPlayPause):
                        await ApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, "");
                        break;
                    case "!pause" when ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
                                        Settings.Settings.BotCmdPlayPause):
                        await ApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
                        break;
                }
        }

        private static string GetTrackIdFromInput(string input)
        {
            if (input.StartsWith("https://spotify.link/"))
            {
                return "shortened";
            }

            if (input.StartsWith("spotify:track:"))
            {
                // search for a track with the id
                return input.Replace("spotify:track:", "");
            }

            if (input.StartsWith("https://open.spotify.com/track/"))
            {
                return input.Replace("https://open.spotify.com/track/", "").Split('?')[0];
            }

            // search for a track with a search string from chat
            SearchItem searchItem = ApiHandler.FindTrack(HttpUtility.UrlEncode(input));
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

        private static bool CheckLiveStatus()
        {
            if (Settings.Settings.IsLive || !Settings.Settings.BotOnlyWorkWhenLive) return true;
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
            return Settings.Settings.UserBlacklist.Any(s =>
                s.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));
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

            if (IsSongBlacklisted(trackId))
            {
                SendChatMessage(Settings.Settings.TwChannel, "This song is blocked");
                return;
            }

            FullTrack track = ApiHandler.GetTrack(trackId);
            if (track == null)
            {
                SendChatMessage(Settings.Settings.TwChannel, "No track was found.");
                return;
            }

            if (IsTrackUnavailable(track))
            {
                SendChatMessage(e.ChatMessage.Channel, "This track is not available in the streamers region.");
                return;
            }

            if (IsArtistBlacklisted(track, e, out string response))
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

            ErrorResponse error = ApiHandler.AddToQ("spotify:track:" + trackId);
            if (error.Error != null)
            {
                response = CreateErrorResponse(e.ChatMessage.DisplayName, error.Error.Message);
                SendChatMessage(e.ChatMessage.Channel, response);
                return;
            }

            response = CreateSuccessResponse(track, e.ChatMessage.DisplayName);
            SendChatMessage(e.ChatMessage.Channel, response);
            UploadToQueue(track, e.ChatMessage.DisplayName);
            UpdateQueueWindow();
        }

        private static string CreateSuccessResponse(FullTrack track, string displayName)
        {
            string artists = string.Join(", ", track.Artists.Select(o => o.Name).ToList());
            string response = Settings.Settings.BotRespSuccess;
            response = response.Replace("{user}", displayName);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count + 1}");
            response = response.Replace("{errormsg}", "");
            response = CleanFormatString(response);

            return response;
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

        private static bool IsUserAtMaxRequests(OnMessageReceivedArgs e, out string response)
        {
            response = string.Empty;

            try
            {
                if (!Settings.Settings.TwSrUnlimitedSr && MaxQueueItems(e.ChatMessage.DisplayName, CheckUserLevel(e.ChatMessage)))
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

        private static bool IsTrackUnavailable(FullTrack track)
        {
            return track.IsPlayable != null && !(bool)track.IsPlayable;
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

        private static void UpdateQueueWindow()
        {
            // Add the song to the internal queue and update the queue window if its open
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window qw = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(WindowQueue))
                        qw = window;
                }

                (qw as WindowQueue)?.dgv_Queue.Items.Refresh();
            });
        }

        private static ReturnObject AddSong2(string trackId, string username)
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
                    Albumcover = track.Album.Images[0].Url
                }
            };

            WebHelper.QueueRequest(WebHelper.RequestMethod.Post, Json.Serialize(payload));
            UpdateQueueWindow();
        }

        private static bool IsInQueue(string id)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<RequestObject> temp = GlobalObjects.ReqList.Where(x => x.Trackid == id).ToList();

            return temp.Count > 0;
        }

        private static List<QueueItem> GetQueueItems(string requester = null)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<QueueItem> temp3 = new List<QueueItem>();
            string currsong = "";
            List<RequestObject> temp = new List<RequestObject>(GlobalObjects.ReqList);

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
            msg = msg.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
            msg = msg.Replace("{artist}", $"{GlobalObjects.CurrentSong.Artists}");
            msg = msg.Replace("{title}", $"{GlobalObjects.CurrentSong.Title}");

            if (msg.StartsWith("[announce "))
            {
                await AnnounceInChat(msg);
            }
            else
            {
                SendChatMessage(Settings.Settings.TwChannel, msg);
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