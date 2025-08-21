using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
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
            // Get ClientId and ClientSecret by register an Application here: https://dev.twitch.tv/console/apps
            // https://dev.twitch.tv/docs/authentication/register-app/
            _twitchApi.Settings.ClientId = TwitchHandler.ClientId;
            // Get Application Token with Client credentials grant flow.
            // https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#client-credentials-grant-flow
            _twitchApi.Settings.AccessToken = Settings.Settings.TwitchAccessToken;

            // You need the UserID for the User/Channel you want to get Events from.
            // You can use await _api.Helix.Users.GetUsersAsync() for that.
            _userId = Settings.Settings.TwitchUser.Id;
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

            if (!e.IsRequestedReconnect)
            {
                // subscribe to topics
                // create condition Dictionary
                // You need BOTH broadcaster and moderator values or EventSub returns an Error!
                var condition = new Dictionary<string, string> { { "broadcaster_user_id", _userId }, { "moderator_user_id", _userId } };
                // Create and send EventSubscription
                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", condition, EventSubTransportMethod.Websocket,
                _eventSubWebsocketClient.SessionId, accessToken: Settings.Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.cheer", "1", condition, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("stream.online", "1", condition, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.Settings.TwitchAccessToken);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("stream.offline", "1", condition, EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId, accessToken: Settings.Settings.TwitchAccessToken);

                // If you want to get Events for special Events you need to additionally add the AccessToken of the ChannelOwner to the request.
                // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
            }
        }

        private async Task OnWebsocketDisconnected(object sender, EventArgs e)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

            // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
            while (!await _eventSubWebsocketClient.ReconnectAsync())
            {
                _logger.LogError("Websocket reconnect failed!");
                await Task.Delay(1000);
            }
        }

        private async Task OnWebsocketReconnected(object sender, EventArgs e)
        {
            _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
        }

        private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        }

        #region Events

        private async Task EventSubWebsocketClientOnChannelCheer(object sender, ChannelCheerArgs args)
        {
            if(!Settings.Settings.SrForBits)
                return;
            ChannelCheer eventData = args.Notification.Payload.Event;
            if (eventData.BroadcasterUserId != Settings.Settings.TwitchUser.Id)
                return;
            _logger.LogInformation($"{eventData.UserName} cheered {eventData.Bits} bits at {eventData.BroadcasterUserName}. Their message was {eventData.Message}");
            // Handle the cheer event here, e.g., update UI or notify users
            if (eventData.Bits >= Settings.Settings.MinimumBitsForSR)
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

        private async Task EventSubWebsocketClientOnStreamOnline(object sender, StreamOnlineArgs args)
        {
            Logger.LogStr("TWITCH: Stream live");
        }

        private async Task EventSubWebsocketClientOnStreamOffline(object sender, StreamOfflineArgs args)
        {
            Logger.LogStr("TWITCH: Stream offline");
        }

        private async Task EventSubWebsocketClientOnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
        {
            ChannelPointsCustomRewardRedemption eventData = e.Notification.Payload.Event;

            if (eventData.BroadcasterUserId != Settings.Settings.TwitchUser.Id)
                return;

            _logger.LogInformation($"{eventData.UserName} redeemed {eventData.Reward.Title}");
            if (Settings.Settings.TwRewardId.Any(id => id == eventData.Reward.Id) &&
                Settings.Settings.TwSrReward)
            {
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

            if (Settings.Settings.TwRewardSkipId.Any(id => id == eventData.Reward.Id))
            {
                // Handle Skip Reward
                await TwitchHandler.HandleSkipReward();
            }
        }

        #endregion
    }
}