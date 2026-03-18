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
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.Stream;
using TwitchLib.EventSub.Core.Models.Polls;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace Songify_Slim.Util.Songify.Twitch
{
    public class WebsocketHostedService : IHostedService
    {
        private readonly ILogger<WebsocketHostedService> _logger;
        private readonly EventSubWebsocketClient _eventSubWebsocketClient;
        private readonly TwitchAPI _twitchApi = new();
        private readonly string _userId = Settings.TwitchUser.Id;

        private readonly SemaphoreSlim _subscriptionSyncLock = new(1, 1);
        private volatile bool _isStopping;
        private volatile bool _started;

        public WebsocketHostedService(
            ILogger<WebsocketHostedService> logger,
            EventSubWebsocketClient eventSubWebsocketClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));

            _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
            _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
            _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
            _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

            _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClientOnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebsocketClient.StreamOffline += EventSubWebsocketClientOnStreamOffline;
            _eventSubWebsocketClient.StreamOnline += EventSubWebsocketClientOnStreamOnline;
            _eventSubWebsocketClient.ChannelCheer += EventSubWebsocketClientOnChannelCheer;
            _eventSubWebsocketClient.ChannelChatMessage += _eventSubWebsocketClient_ChannelChatMessage;
            _eventSubWebsocketClient.ChannelPollBegin += _eventSubWebsocketClient_ChannelPollBegin;
            _eventSubWebsocketClient.ChannelPollProgress += EventSubWebsocketClientOnChannelPollProgress;
            _eventSubWebsocketClient.ChannelPollEnd += _eventSubWebsocketClient_ChannelPollEnd;

            _twitchApi.Settings.ClientId = TwitchHandler.ClientId;
            _twitchApi.Settings.AccessToken = Settings.TwitchAccessToken;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_started)
            {
                Logger.Info(LogSource.Twitch, "EventSub StartAsync skipped: already started.");
                return;
            }

            _started = true;
            _isStopping = false;

            Logger.Info(LogSource.Twitch, "EventSub connecting...");
            await _eventSubWebsocketClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _isStopping = true;
            Logger.Info(LogSource.Twitch, "EventSub stopping...");

            try
            {
                await _eventSubWebsocketClient.DisconnectAsync();
                await ResetEventSubSubscriptions();
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch, "EventSub disconnect failed", ex);
            }

            _started = false;
        }

        private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
        {
            string currentSessionId = _eventSubWebsocketClient.SessionId;

            _logger.LogInformation("Websocket {SessionId} connected. RequestedReconnect={RequestedReconnect}",
                currentSessionId, e.IsRequestedReconnect);
            Logger.Info(LogSource.Twitch,
                $"EventSub connected. Session={currentSessionId}, RequestedReconnect={e.IsRequestedReconnect}");

            await SyncSubscriptionsForCurrentSession(currentSessionId);

            await UpdateUiIndicatorSafe();
        }

        private async Task SyncSubscriptionsForCurrentSession(string currentSessionId)
        {
            if (string.IsNullOrWhiteSpace(currentSessionId))
            {
                Logger.Error(LogSource.Twitch, "EventSub sync skipped: session id is null or empty.");
                return;
            }

            await _subscriptionSyncLock.WaitAsync();
            try
            {
                if (!IsCurrentSessionStillValid(currentSessionId))
                {
                    Logger.Info(LogSource.Twitch,
                        $"EventSub sync aborted before start because session changed. Expected={currentSessionId}, Current={_eventSubWebsocketClient.SessionId}");
                    return;
                }

                Logger.Debug(LogSource.Twitch, $"EventSub sync start for session {currentSessionId}");

                List<EventSubSubscription> existing = await GetEventSubscriptionsSafe() ?? new List<EventSubSubscription>();

                if (!IsCurrentSessionStillValid(currentSessionId))
                {
                    Logger.Info(LogSource.Twitch,
                        $"EventSub sync aborted after fetching subscriptions because session changed. Expected={currentSessionId}, Current={_eventSubWebsocketClient.SessionId}");
                    return;
                }

                LogExistingSubscriptionOverview(existing, currentSessionId);
                await DeleteStaleWebsocketSubscriptions(existing, currentSessionId);

                if (!IsCurrentSessionStillValid(currentSessionId))
                {
                    Logger.Info(LogSource.Twitch,
                        $"EventSub sync aborted after cleanup because session changed. Expected={currentSessionId}, Current={_eventSubWebsocketClient.SessionId}");
                    return;
                }

                existing = await GetEventSubscriptionsSafe() ?? new List<EventSubSubscription>();

                Dictionary<string, string> condBroadcaster = new()
                {
                    ["broadcaster_user_id"] = _userId
                };

                Dictionary<string, string> condBroadcasterAndUser = new()
                {
                    ["broadcaster_user_id"] = _userId,
                    ["user_id"] = _userId
                };

                Dictionary<string, string> condBroadcasterAndModerator = new()
                {
                    ["broadcaster_user_id"] = _userId,
                    ["moderator_user_id"] = _userId
                };

                await EnsureSubscription(existing, "channel.channel_points_custom_reward_redemption.add", "1", condBroadcasterAndModerator, currentSessionId);
                await EnsureSubscription(existing, "channel.cheer", "1", condBroadcasterAndModerator, currentSessionId);
                await EnsureSubscription(existing, "stream.online", "1", condBroadcaster, currentSessionId);
                await EnsureSubscription(existing, "stream.offline", "1", condBroadcaster, currentSessionId);
                await EnsureSubscription(existing, "channel.chat.message", "1", condBroadcasterAndUser, currentSessionId);
                await EnsureSubscription(existing, "channel.poll.begin", "1", condBroadcasterAndModerator, currentSessionId);
                await EnsureSubscription(existing, "channel.poll.progress", "1", condBroadcasterAndModerator, currentSessionId);
                await EnsureSubscription(existing, "channel.poll.end", "1", condBroadcasterAndModerator, currentSessionId);

                Logger.Info(LogSource.Twitch, $"EventSub sync complete for session {currentSessionId}");
            }
            finally
            {
                _subscriptionSyncLock.Release();
            }
        }

        private static async Task<List<EventSubSubscription>> GetEventSubscriptionsSafe()
        {
            try
            {
                List<EventSubSubscription> subs = await TwitchApiHelper.GetEventSubscriptions();
                return subs ?? new List<EventSubSubscription>();
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch, "Failed to get EventSub subscriptions", ex);
                return new List<EventSubSubscription>();
            }
        }

        private static void LogExistingSubscriptionOverview(List<EventSubSubscription> existing, string currentSessionId)
        {
            List<EventSubSubscription> enabledWebsocketSubs = existing
                .Where(IsEnabledWebsocketSubscription)
                .ToList();

            List<string> sessionIds = enabledWebsocketSubs
                .Select(s => GetTransportSessionId(s))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Logger.Info(LogSource.Twitch,
                $"EventSub subscriptions: total={existing.Count}, enabledWebsocket={enabledWebsocketSubs.Count}, distinctSessions={sessionIds.Count}, currentSession={currentSessionId}");

            foreach (EventSubSubscription sub in enabledWebsocketSubs)
            {
                Logger.Debug(LogSource.Twitch,
                    $"EventSub existing sub: id={sub.Id}, type={sub.Type}, version={sub.Version}, status={sub.Status}, session={GetTransportSessionId(sub)}");
            }
        }

        private async Task DeleteStaleWebsocketSubscriptions(List<EventSubSubscription> existing, string currentSessionId)
        {
            string[] managedTypes =
            {
                "channel.channel_points_custom_reward_redemption.add",
                "channel.cheer",
                "stream.online",
                "stream.offline",
                "channel.chat.message",
                "channel.poll.begin",
                "channel.poll.progress",
                "channel.poll.end"
            };

            List<EventSubSubscription> staleSubs = existing
                .Where(s =>
                    IsWebsocketSubscription(s) &&
                    managedTypes.Contains(s.Type, StringComparer.OrdinalIgnoreCase) &&
                    !string.Equals(GetTransportSessionId(s), currentSessionId, StringComparison.Ordinal))
                .ToList();

            if (staleSubs.Count == 0)
            {
                Logger.Debug(LogSource.Twitch, "EventSub cleanup: no stale websocket subscriptions found.");
                return;
            }

            Logger.Info(LogSource.Twitch,
                $"EventSub cleanup: deleting {staleSubs.Count} stale websocket subscription(s).");

            foreach (EventSubSubscription stale in staleSubs)
            {
                try
                {
                    Logger.Debug(LogSource.Twitch,
                        $"Deleting stale EventSub subscription: id={stale.Id}, type={stale.Type}, status={stale.Status}, oldSession={GetTransportSessionId(stale)}");

                    await _twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(
                        stale.Id,
                        accessToken: Settings.TwitchAccessToken);

                    await Task.Delay(150);
                }
                catch (Exception ex)
                {
                    Logger.Error(LogSource.Twitch,
                        $"Failed to delete stale EventSub subscription {stale.Id} ({stale.Type})", ex);
                }
            }
        }

        public async Task ResetEventSubSubscriptions()
        {
            List<EventSubSubscription> existing = await GetEventSubscriptionsSafe() ?? new List<EventSubSubscription>();

            string[] managedTypes =
            {
                "channel.channel_points_custom_reward_redemption.add",
                "channel.cheer",
                "stream.online",
                "stream.offline",
                "channel.chat.message",
                "channel.poll.begin",
                "channel.poll.progress",
                "channel.poll.end"
            };

            foreach (var sub in existing.Where(s =>
                         IsWebsocketSubscription(s) &&
                         managedTypes.Contains(s.Type, StringComparer.OrdinalIgnoreCase)))
            {
                try
                {
                    await _twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(
                        sub.Id,
                        accessToken: Settings.TwitchAccessToken);
                }
                catch (Exception ex)
                {
                    Logger.Error(LogSource.Twitch, $"Failed to delete EventSub subscription {sub.Id}", ex);
                }
            }
        }


        private async Task EnsureSubscription(
            List<EventSubSubscription> existing,
            string type,
            string version,
            Dictionary<string, string> condition,
            string currentSessionId)
        {
            bool alreadyExists = existing.Any(s =>
                IsEnabledWebsocketSubscription(s) &&
                string.Equals(s.Type, type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Version, version, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetTransportSessionId(s), currentSessionId, StringComparison.Ordinal) &&
                SubscriptionConditionsMatch(s, condition));

            if (alreadyExists)
            {
                Logger.Debug(LogSource.Twitch,
                    $"EventSub ensure: already exists for current session. type={type}, version={version}");
                return;
            }

            Logger.Debug(LogSource.Twitch,
                $"EventSub ensure: creating missing subscription. type={type}, version={version}, session={currentSessionId}");

            await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                type,
                version,
                condition,
                EventSubTransportMethod.Websocket,
                currentSessionId,
                accessToken: Settings.TwitchAccessToken);
        }

        private static bool IsEnabledWebsocketSubscription(EventSubSubscription sub)
        {
            if (sub == null)
                return false;

            if (!string.Equals(sub.Status, "enabled", StringComparison.OrdinalIgnoreCase))
                return false;

            string method = GetTransportMethod(sub);
            return string.Equals(method, "websocket", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTransportMethod(EventSubSubscription sub)
        {
            try
            {
                return sub?.Transport?.Method;
            }
            catch
            {
                return null;
            }
        }

        private static string GetTransportSessionId(EventSubSubscription sub)
        {
            try
            {
                return sub?.Transport?.SessionId;
            }
            catch
            {
                return null;
            }
        }

        private static bool SubscriptionConditionsMatch(
            EventSubSubscription subscription,
            Dictionary<string, string> expected)
        {
            if (subscription == null || expected == null)
                return false;

            try
            {
                object conditionObj = subscription.Condition;
                if (conditionObj == null)
                    return false;

                Dictionary<string, string> actual = conditionObj
                    .GetType()
                    .GetProperties()
                    .ToDictionary(
                        p => p.Name,
                        p => p.GetValue(conditionObj)?.ToString(),
                        StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, string> kv in expected)
                {
                    if (!actual.TryGetValue(kv.Key, out string actualValue))
                        return false;

                    if (!string.Equals(actualValue, kv.Value, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task UpdateUiIndicatorSafe()
        {
            try
            {
                List<EventSubSubscription> evSubs = await GetEventSubscriptionsSafe();
                evSubs = evSubs.Where(sub => string.Equals(sub.Status, "enabled", StringComparison.OrdinalIgnoreCase)).ToList();

                bool chatEnabled = evSubs.Any(s => string.Equals(s.Type, "channel.chat.message", StringComparison.OrdinalIgnoreCase));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.IconTwitchBot != null)
                        mainWindow.IconTwitchBot.Foreground = chatEnabled ? Brushes.GreenYellow : Brushes.IndianRed;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch, "Failed to update EventSub UI state", ex);
            }
        }

        private async Task OnWebsocketDisconnected(object sender, WebsocketDisconnectedArgs e)
        {
            _logger.LogError("Websocket {SessionId} disconnected!", _eventSubWebsocketClient.SessionId);
            Logger.Info(LogSource.Twitch, "EventSub websocket disconnected.");

            if (_isStopping)
            {
                Logger.Info(LogSource.Twitch, "EventSub disconnect during shutdown. Reconnect skipped.");
                return;
            }

            int attempt = 0;
            int delayMs = 1000;

            while (!_isStopping)
            {
                attempt++;

                try
                {
                    Logger.Info(LogSource.Twitch, $"EventSub reconnect attempt {attempt}");

                    bool ok = await _eventSubWebsocketClient.ReconnectAsync();
                    if (ok)
                    {
                        Logger.Info(LogSource.Twitch, $"EventSub reconnect succeeded on attempt {attempt}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(LogSource.Twitch, $"EventSub reconnect attempt {attempt} failed", ex);
                }

                await Task.Delay(delayMs);
                delayMs = Math.Min(delayMs * 2, 30000);
            }
        }

        private Task OnWebsocketReconnected(object sender, EventArgs e)
        {
            _logger.LogWarning("Websocket {SessionId} reconnected", _eventSubWebsocketClient.SessionId);
            Logger.Info(LogSource.Twitch, $"EventSub websocket reconnected. Session={_eventSubWebsocketClient.SessionId}");
            return Task.CompletedTask;
        }

        private Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
        {
            _logger.LogError("EventSub ErrorOccurred: {Exception}", e.Exception);
            Logger.Error(LogSource.Twitch, $"EventSub ErrorOccurred: {e.Exception}");
            return Task.CompletedTask;
        }

        private bool IsCurrentSessionStillValid(string expectedSessionId)
        {
            string currentSessionId = _eventSubWebsocketClient.SessionId;
            return !string.IsNullOrWhiteSpace(expectedSessionId) &&
                   string.Equals(currentSessionId, expectedSessionId, StringComparison.Ordinal);
        }

        private static bool IsWebsocketSubscription(EventSubSubscription sub)
        {
            if (sub == null)
                return false;

            return string.Equals(GetTransportMethod(sub), "websocket", StringComparison.OrdinalIgnoreCase);
        }



        #region Events

        private async Task EventSubWebsocketClientOnChannelCheer(object sender, ChannelCheerArgs args)
        {
            if (!Settings.SrForBits)
                return;

            ChannelCheer eventData = args.Payload.Event;
            if (eventData.BroadcasterUserId != Settings.TwitchUser.Id)
                return;

            _logger.LogInformation("{UserName} cheered {Bits} bits at {Broadcaster}. Their message was {Message}",
                eventData.UserName, eventData.Bits, eventData.BroadcasterUserName, eventData.Message);

            if (eventData.Bits >= Settings.MinimumBitsForSr)
            {
                string input = eventData.Message;
                string keyword = Settings.SrForBitsKeyWord;

                bool keywordMatches =
                    string.IsNullOrWhiteSpace(keyword) ||
                    input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                if (keywordMatches)
                {
                    const string pattern = @"\bcheer\w*?\d+\b";
                    string result = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        result = Regex.Replace(
                            result,
                            Regex.Escape(keyword),
                            "",
                            RegexOptions.IgnoreCase);
                    }

                    result = Regex.Replace(result, @"\s+", " ").Trim();

                    await TwitchHandler.HandleBitsSongRequest(
                        userId: eventData.UserId,
                        userName: eventData.UserName,
                        userInput: result,
                        channel: eventData.BroadcasterUserLogin
                    );
                }
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
            ChannelPointsCustomRewardRedemption eventData = e.Payload.Event;

            if (eventData.BroadcasterUserId != Settings.TwitchUser.Id)
                return;

            Logger.Info(LogSource.Twitch, $"{eventData.UserName} redeemed {eventData.Reward.Title}");

            if (Settings.TwRewardId.Any(id => id == eventData.Reward.Id) &&
                Settings.TwSrReward)
            {
                await TwitchHandler.RunTwitchUserSync();
                Logger.Info(LogSource.Twitch, $"Redeem: {eventData.Reward.Title} by {eventData.UserName}");

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
                await TwitchHandler.HandleSkipReward();

            if (Settings.TwRewardSkipPoll.Any(id => id == eventData.Reward.Id))
                await TwitchHandler.StartSkipPoll(eventData.Id, eventData.Reward.Id);
        }

        private static Task _eventSubWebsocketClient_ChannelChatMessage(object sender, ChannelChatMessageArgs e)
        {
            ChannelChatMessage chatMsg = e.Payload.Event;

            if (!chatMsg.Message.Text.StartsWith("!"))
                return Task.CompletedTask;

            if (!Settings.SharedChatEnabled &&
                chatMsg.SourceBroadcasterUserId != null &&
                chatMsg.SourceBroadcasterUserId != Settings.TwitchUser.Id)
                return Task.CompletedTask;

            TwitchHandler.ExecuteChatCommand(chatMsg);
            return Task.CompletedTask;
        }

        private static Task _eventSubWebsocketClient_ChannelPollEnd(object sender, ChannelPollEndArgs e)
        {
            ChannelPollEnd pollEvent = e.Payload.Event;

            if (pollEvent.Status.ToUpper() == "ARCHIVED"
                || pollEvent.Status.ToUpper() == "TERMINATED"
                || pollEvent.Id != GlobalObjects.CurrentSkipPoll.Id)
                return Task.CompletedTask;

            GlobalObjects.CurrentSkipPoll.IsActive = false;

            string matchedChoice = Settings.TwitchPollSettings.WinningChoice;
            if (string.IsNullOrWhiteSpace(matchedChoice))
                return Task.CompletedTask;

            var choices = pollEvent.Choices
                .Select(c => new
                {
                    Choice = c,
                    Votes = c.Votes ?? 0
                })
                .ToList();

            if (choices.Count == 0)
                return Task.CompletedTask;

            int totalVotes = choices.Sum(c => c.Votes);
            int maxVotes = choices.Max(c => c.Votes);

            var topChoices = choices.Where(c => c.Votes == maxVotes).ToList();

            if (topChoices.Count != 1)
                return Task.CompletedTask;

            PollChoice winningChoice = topChoices[0].Choice;

            if (!string.Equals(winningChoice.Title, matchedChoice, StringComparison.Ordinal))
                return Task.CompletedTask;

            int matchedVotes = choices
                .FirstOrDefault(c => string.Equals(c.Choice.Title, matchedChoice, StringComparison.Ordinal))
                ?.Votes ?? 0;

            double winPercentage = 0;
            double losePercentage = 0;

            if (totalVotes > 0)
            {
                winPercentage = Math.Round((double)matchedVotes / totalVotes * 100, 2);
                losePercentage = Math.Round(100 - winPercentage, 2);
            }

            TwitchHandler.ExecuteSkipPollChoice(matchedChoice, totalVotes, winPercentage, losePercentage);
            return Task.CompletedTask;
        }

        private static Task EventSubWebsocketClientOnChannelPollProgress(object sender, ChannelPollProgressArgs args)
        {
            Debug.WriteLine(args);
            return Task.CompletedTask;
        }

        private static Task _eventSubWebsocketClient_ChannelPollBegin(object sender, ChannelPollBeginArgs e)
        {
            ChannelPollBegin poll = e.Payload.Event;
            if (GlobalObjects.CurrentSkipPoll.Id == poll.Id)
                GlobalObjects.CurrentSkipPoll.IsActive = true;

            return Task.CompletedTask;
        }

        #endregion Events
    }
}