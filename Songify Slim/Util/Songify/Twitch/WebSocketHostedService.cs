using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Views;
using Swan.Formatters;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Stream;

namespace Songify_Slim.Util.Songify.Twitch
{
    public class WebsocketHostedService : IHostedService
    {
        private readonly ILogger<WebsocketHostedService> _logger;
        private readonly EventSubWebsocketClient _eventSubWebsocketClient;
        private readonly TwitchAPI _twitchApi = new();
        private string _userId;

        public WebsocketHostedService(ILogger<WebsocketHostedService> logger, EventSubWebsocketClient eventSubWebsocketClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
            _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
            _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
            _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
            _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

            //_eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
            _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClientOnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebsocketClient.StreamOffline += EventSubWebsocketClientOnStreamOffline;
            _eventSubWebsocketClient.StreamOnline += EventSubWebsocketClientOnStreamOnline;
            _eventSubWebsocketClient.ChannelCheer += EventSubWebsocketClientOnChannelCheer;
            _eventSubWebsocketClient.ChannelChatMessage += _eventSubWebsocketClient_ChannelChatMessage;

            // Get ClientId and ClientSecret by register an Application here: https://dev.twitch.tv/console/apps
            // https://dev.twitch.tv/docs/authentication/register-app/
            _twitchApi.Settings.ClientId = TwitchHandler.ClientId;
            // Get Application Token with Client credentials grant flow.
            // https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#client-credentials-grant-flow
            _twitchApi.Settings.AccessToken = Settings.TwitchAccessToken;

            // You need the UserID for the User/Channel you want to get Events from.
            // You can use await _api.Helix.Users.GetUsersAsync() for that.
            _userId = Settings.TwitchUser.Id;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _eventSubWebsocketClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _eventSubWebsocketClient.DisconnectAsync();
        }

        private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
        {
            _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
            Logger.Info(LogSource.Twitch, $"EventSub connected. ({_eventSubWebsocketClient.SessionId})");

            if (!e.IsRequestedReconnect)
            {
                // subscribe to topics
                // create condition Dictionary
                // You need BOTH broadcaster and moderator values or EventSub returns an Error!
                Dictionary<string, string> conditionBm = new() { { "broadcaster_user_id", _userId }, { "moderator_user_id", _userId } };
                Dictionary<string, string> conditionBu = new() { { "broadcaster_user_id", _userId }, { "user_id", _userId } };
                // Create and send EventSubscription
                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", conditionBm, EventSubTransportMethod.Websocket,
                _eventSubWebsocketClient.SessionId, accessToken: Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.cheer", "1", conditionBm, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("stream.online", "1", conditionBm, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("stream.offline", "1", conditionBm, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.chat.message", "1", conditionBu,
                    EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId, accessToken: Settings.TwitchAccessToken);

                // If you want to get Events for special Events you need to additionally add the AccessToken of the ChannelOwner to the request.
                // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
            }

            // Update UI
            List<EventSubSubscription> evSubs = await TwitchApiHelper.GetEventSubscriptions();
            evSubs = evSubs.Where(sub => sub.Status == "enabled").ToList();
            if (evSubs.Any(s => s.Type == "channel.chat.message"))
            {
                Application.Current.Dispatcher.Invoke((() =>
                {
                    (((MainWindow)Application.Current.MainWindow)!).IconTwitchBot.Foreground = evSubs.Any(sub => sub.Type == "channel.chat.message" && sub.Status == "enabled") ? Brushes.GreenYellow : Brushes.IndianRed;
                }));
            }
        }

        private async Task OnWebsocketDisconnected(object sender, EventArgs e)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");
            Logger.Info(LogSource.Twitch, $"Websocket disconnected...");
            await TwitchHandler.StartOrRestartAsync();
            // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
            while (!await _eventSubWebsocketClient.ReconnectAsync())
            {
                _logger.LogError("Websocket reconnect failed!");
                Logger.Error(LogSource.Twitch, $"Websocket reconnect failed!");
                await Task.Delay(1000);
            }
        }

        private async Task OnWebsocketReconnected(object sender, EventArgs e)
        {
            _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
            Logger.Error(LogSource.Twitch, $"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        }

        private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
            Logger.Error(LogSource.Twitch, $"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        }

        #region Events

        private async Task EventSubWebsocketClientOnChannelCheer(object sender, ChannelCheerArgs args)
        {
            if (!Settings.SrForBits)
                return;
            ChannelCheer eventData = args.Notification.Payload.Event;
            if (eventData.BroadcasterUserId != Settings.TwitchUser.Id)
                return;
            _logger.LogInformation($"{eventData.UserName} cheered {eventData.Bits} bits at {eventData.BroadcasterUserName}. Their message was {eventData.Message}");
            // Handle the cheer event here, e.g., update UI or notify users
            if (eventData.Bits >= Settings.MinimumBitsForSr)
            {
                string input = eventData.Message;
                string pattern = @"\bcheer\w*?\d+\b";
                // Replace all matches with an empty string
                string result = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);

                // Optionally, replace double spaces with single spaces (since you might get extra spaces)
                result = Regex.Replace(result, @"\s+", " ").Trim();

                await TwitchHandler.HandleBitsSongRequest(
                    userId: eventData.UserId,
                    userName: eventData.UserName,
                    userInput: result,
                    channel: eventData.BroadcasterUserLogin
                );
            }
        }

        private static Task EventSubWebsocketClientOnStreamOnline(object sender, StreamOnlineArgs args)
        {
            Settings.IsLive = true;
            Logger.Info(LogSource.Twitch, "Stream live");
            return Task.CompletedTask;
        }

        private static Task EventSubWebsocketClientOnStreamOffline(object sender, StreamOfflineArgs args)
        {
            Settings.IsLive = false;
            Logger.Info(LogSource.Twitch, "Stream offline");
            return Task.CompletedTask;
        }

        private async Task EventSubWebsocketClientOnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
        {
            ChannelPointsCustomRewardRedemption eventData = e.Notification.Payload.Event;

            if (eventData.BroadcasterUserId != Settings.TwitchUser.Id)
                return;

            _logger.LogInformation($"{eventData.UserName} redeemed {eventData.Reward.Title}");
            if (Settings.TwRewardId.Any(id => id == eventData.Reward.Id) &&
                Settings.TwSrReward)
            {
                await TwitchHandler.RunTwitchUserSync();
                Logger.Info(LogSource.Twitch, $"Redeem: {eventData.Reward.Title} by {eventData.UserName}");

                // Handle Song Request Command
                await TwitchHandler.HandleChannelPointSongRequst(
                    isBroadcaster: eventData.UserId == eventData.BroadcasterUserId,
                    userId: eventData.UserId,
                    userName: eventData.UserName,
                    userInput: eventData.UserInput,
                    channel: eventData.BroadcasterUserLogin,
                    rewardId: eventData.Reward.Id,
                    redemptionId: eventData.Id
                );
            }

            if (Settings.TwRewardSkipId.Any(id => id == eventData.Reward.Id))
            {
                // Handle Skip Reward
                await TwitchHandler.HandleSkipReward();
            }
        }

        private static Task _eventSubWebsocketClient_ChannelChatMessage(object sender, ChannelChatMessageArgs args)
        {
            ChannelChatMessage chatMsg = args.Notification.Payload.Event;
            //if (chatMsg.ChatterUserId == Settings.Settings.TwitchChatAccount.Id)
            //    return;
            if (!chatMsg.Message.Text.StartsWith("!"))
                return Task.CompletedTask;
            if (chatMsg.SourceBroadcasterUserId != null && chatMsg.SourceBroadcasterUserId != Settings.TwitchUser.Id)
                return Task.CompletedTask;
            TwitchHandler.ExecuteChatCommand(chatMsg);
                                                                        string x = Json.Serialize(chatMsg.Badges);
                        return Task.CompletedTask;
        }

        #endregion Events
    }
}