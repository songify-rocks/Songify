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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
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
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Subscriptions;
using static Songify_Slim.Util.General.Enums;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using System.Web;
using System.Windows.Interop;
using Windows.Media.Playback;
using Markdig.Syntax;
using Microsoft.Toolkit.Uwp.Notifications;
using Songify_Slim.Util.Spotify;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using Image = Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models.Image;
using TwitchLib.Api.Helix.Models.Soundtrack;
using TwitchLib.PubSub.Models.Responses;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;
using TwitchCommandParams = Songify_Slim.Models.TwitchCommandParams;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.V5.Models.Clips;
using Newtonsoft.Json.Linq;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Models.Responses.Messages.AutomodCaughtMessage;
using Message = TwitchLib.PubSub.Models.Responses.Message;


namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding twitch.tv
    public static class TwitchHandler
    {
        public const bool PubSubEnabled = false;
        public static ValidateAccessTokenResponse BotTokenCheck;
        public static TwitchClient Client;
        public static bool ForceDisconnect;
        public static ValidateAccessTokenResponse TokenCheck;
        public static TwitchAPI TwitchApi;
        private const string ClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";
        private const int MaxConsecutiveFailures = 5;
        private static readonly Stopwatch CooldownStopwatch = new();
        private static readonly Timer CooldownTimer = new() { Interval = TimeSpan.FromSeconds(Settings.Settings.TwSrCooldown < 1 ? 0 : Settings.Settings.TwSrCooldown).TotalMilliseconds };
        private static readonly Timer SkipCooldownTimer = new() { Interval = TimeSpan.FromSeconds(5).TotalMilliseconds };
        private static readonly List<string> SkipVotes = [];
        private static bool toastSent = false;

        private static readonly DispatcherTimer StreamUpTimer = new()
        {
            Interval = TimeSpan.FromSeconds(5)
        };

        private static readonly TwitchPubSub TwitchPubSub = new();

        // Threshold for setting IsLive to false
        private static readonly DispatcherTimer TwitchUserSyncTimer = new()
        {
            Interval = TimeSpan.FromSeconds(30)
        };

        private static int _consecutiveFailures;
        private static string _currentState;
        private static string _joinedChannelId = "";
        private static TwitchClient _mainClient;
        private static bool _onCooldown;
        private static bool _skipCooldown;
        private static TwitchAPI _twitchApiBot;
        private static string _userId;
        private static Subscription[] _subscriptions = [];

        public static void ApiConnect(TwitchAccount account)
        {
            // generate a random int salt
            Random random = new();
            int salt = random.Next(1, 1000);

            ImplicitOAuth ioa = new(salt);

            // This event is triggered when the application receives a new token and state from the "RequestClientAuthorization" method.
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
                if (string.IsNullOrEmpty(Settings.Settings.TwChannel))
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
                        await ((Window_Settings)window).SetControls();
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
                            foreach (JoinedChannel clientJoinedChannel in Client.JoinedChannels)
                            {
                                Client.LeaveChannel(clientJoinedChannel);
                                Debug.WriteLine($"Leaving Channel: {clientJoinedChannel.Channel}");
                            }

                            Client = null;
                        }
                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }
                    }


                    if (_mainClient != null)
                    {
                        try
                        {
                            foreach (JoinedChannel mainClientJoinedChannel in _mainClient.JoinedChannels)
                            {
                                _mainClient.LeaveChannel(mainClientJoinedChannel);

                            }

                            _mainClient = null;
                        }
                        catch (Exception e)
                        {
                            Logger.LogExc(e);
                        }
                    }


                    await BotConnect();


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
                    Debug.WriteLine($"Sending Telemetry");

                    await WebHelper.TelemetryRequest(WebHelper.RequestMethod.Post, json);
                    Debug.WriteLine($"Done");

                });
            };

            // This method initialize the flow of getting the token and returns a temporary random state that we will use to check authenticity.
            _currentState = ioa.RequestClientAuthorization();
        }

        public static async Task BotConnect()
        {
            try
            {
                await MainConnect();
                switch (Client)
                {
                    case { IsConnected: true }:
                        return;

                    case { IsConnected: false }:
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
                    ThrottlingPeriod = TimeSpan.FromSeconds(30),
                };
                WebSocketClient customClient = new(clientOptions);

                // Register all TwitchChatCommands
                InitializeCommands(Settings.Settings.Commands);

                Client = new TwitchClient(customClient);
                Client.Initialize(credentials);
                Client.OnMessageReceived += Client_OnMessageReceived;
                Client.OnConnected += Client_OnConnected;
                Client.OnDisconnected += Client_OnDisconnected;
                Client.OnJoinedChannel += ClientOnOnJoinedChannel;
                Client.OnLeftChannel += ClientOnOnLeftChannel;
                Client.Connect();

                // subscribes to the cooldown timer elapsed event for the command cooldown
                CooldownTimer.Elapsed += CooldownTimer_Elapsed;
                SkipCooldownTimer.Elapsed += SkipCooldownTimer_Elapsed;
            }
            catch (Exception)
            {
                Logger.LogStr("TWITCH: Couldn't connect to Twitch, maybe credentials are wrong?");
            }
        }

        public static void InitializeCommands(List<TwitchCommand> commands)
        {
            // Unregister all commands first.
            TwitchCommandHandler.ClearCommands();

            foreach (TwitchCommand command in commands)
            {
                // Determine the appropriate handler based on the command's trigger or name.
                CommandHandlerDelegate handler = command.CommandType switch
                {
                    CommandType.SongRequest => HandleSongRequestCommand,
                    CommandType.Next => HandleNextCommand,
                    CommandType.Play => HandlePlayCommand,
                    CommandType.Pause => HandlePauseCommand,
                    CommandType.Position => HandlePositionCommand,
                    CommandType.Queue => HandleQueueCommand,
                    CommandType.Remove => HandleRemoveCommand,
                    CommandType.Skip => HandleSkipCommand,
                    CommandType.Voteskip => HandleVoteSkipCommand,
                    CommandType.Song => HandleSongCommand,
                    CommandType.Songlike => HandleSongLikeCommand,
                    CommandType.Volume => HandleVolumeCommand,
                    CommandType.Commands => HandleCommandsCommand,
                    CommandType.BanSong => HandleBanSongCommand,
                    _ => null
                };

                if (handler == null) continue;
                // Register Custom CommandProperties
                // Add Custom Property to the command

                switch (command.CommandType)
                {
                    case CommandType.SongRequest:
                    case CommandType.Next:
                    case CommandType.Play:
                    case CommandType.Pause:
                    case CommandType.Position:
                    case CommandType.Queue:
                    case CommandType.Remove:
                    case CommandType.Skip:
                        break;
                    case CommandType.Voteskip:
                        if (command.CustomProperties?.ContainsKey("SkipCount") == false)
                        {
                            command.CustomProperties["SkipCount"] = Settings.Settings.BotCmdSkipVoteCount;
                        }
                        break;
                    case CommandType.Song:
                    case CommandType.Songlike:
                        break;
                    case CommandType.Volume:
                        if (command.CustomProperties?.ContainsKey("VolumeSetResponse") == false)
                        {
                            command.CustomProperties["VolumeSetResponse"] = "Volume set to {vol}%";
                        }
                        break;
                    case CommandType.Commands:
                    case CommandType.BanSong:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                TwitchCommandHandler.RegisterCommand(command, handler);
            }
        }

        private static async void HandleBanSongCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdparams)
        {
            try
            {
                TrackInfo currentSong = GlobalObjects.CurrentSong;
                if (currentSong == null ||
                    Settings.Settings.SongBlacklist.Any(track => track.TrackId == currentSong.SongId))
                    return;

                List<TrackItem> blacklist = Settings.Settings.SongBlacklist;
                blacklist.Add(new TrackItem
                {
                    Artists = currentSong.Artists,
                    TrackName = currentSong.Title,
                    TrackId = currentSong.SongId,
                    TrackUri = $"spotify:track:{currentSong.SongId}",
                    ReadableName = $"{currentSong.Artists} - {currentSong.Title}"
                });
                Settings.Settings.SongBlacklist = Settings.Settings.SongBlacklist;

                string response = cmd.Response;
                response = response.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");

                SendOrAnnounceMessage(message.Channel, response, cmd);

                await SpotifyApiHandler.SkipSong();
            }
            catch (Exception ex)
            {
                Logger.LogStr("Error while banning song");
                Logger.LogExc(ex);
            }
        }

        private static async void HandleCommandsCommand(ChatMessage message, TwitchCommand cmd,
            TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }
            List<TwitchCommand> x = Settings.Settings.Commands.Where(c => c.IsEnabled).ToList();

            List<string> cmds =
                x.Select(twitchCommand =>
                    twitchCommand.Trigger.StartsWith("!")
                        ? twitchCommand.Trigger
                        : "!" + twitchCommand.Trigger).ToList();

            string response = cmd.Response;
            response = response.Replace("{user}", message.DisplayName);
            response = response.Replace("{commands}", string.Join(", ", cmds));

            if (cmd.IsAnnouncement)
                await AnnounceChatMessage(response, cmd.AnnouncementColor);
            else
                SendChatMessage(message.Channel, response);
        }

        private static async void HandleVolumeCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            if (message.Message.Split(' ').Length > 1)
            {

                // Volume Set
                cmd.CustomProperties.TryGetValue("VolumeSetResponse", out object volSetResponse);
                string response = (string)volSetResponse;
                if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
                int? vol = await SetSpotifyVolume(message);
                if (vol == null)
                {
                    SendChatMessage(message.Channel, "Error setting volume.");
                    return;
                }

                if (response == null) return;
                response = response.Replace("{user}", message.DisplayName);
                response = response.Replace("{vol}", vol.ToString());

                SendOrAnnounceMessage(message.Channel, response, cmd);
            }
            else
            {
                // Volume Get
                if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
                PlaybackContext spotifyPlaybackAsync = await SpotifyApiHandler.Spotify.GetPlaybackAsync();
                if (spotifyPlaybackAsync?.Device == null) return;
                string response = cmd.Response;
                response = response.Replace("{user}", message.DisplayName);
                response = response.Replace("{vol}", spotifyPlaybackAsync?.Device.VolumePercent.ToString());

                SendOrAnnounceMessage(message.Channel, response, cmd);
            }
        }

        private static async void HandleSongLikeCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            if (string.IsNullOrWhiteSpace(Settings.Settings.SpotifyPlaylistId))
            {
                SendChatMessage(Settings.Settings.TwChannel, "No playlist has been specified. Go to Settings -> Spotify and select the playlist you want to use.");
                return;
            }

            try
            {
                if (await AddToPlaylist(GlobalObjects.CurrentSong.SongId, true)) return;

                string response = cmd.Response;
                response = response.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");


                SendOrAnnounceMessage(message.Channel, response, cmd);

            }
            catch (Exception exception)
            {
                Logger.LogStr("SPOTIFY: Error while adding song to playlist");
                Logger.LogExc(exception);
            }
        }

        private static void HandleSongCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            try
            {
                if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
                try
                {
                    if (!CheckLiveStatus())
                    {
                        if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                        return;
                    }
                }
                catch (Exception)
                {
                    Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
                }

                string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
                {
                    User = message.DisplayName,
                    SingleArtist = GlobalObjects.CurrentSong.FullArtists != null
                        ? GlobalObjects.CurrentSong.FullArtists.First().Name
                        : GlobalObjects.CurrentSong.Artists,
                    MaxReq = $"{Settings.Settings.TwSrMaxReq}",
                    ErrorMsg = null,
                    MaxLength = $"{Settings.Settings.MaxSongLength}",
                    Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
                    Req = GlobalObjects.Requester,
                    Cd = Settings.Settings.TwSrCooldown.ToString()
                }, cmd.Response);

                if (response.Contains("{single_artist}"))
                    response = response.Replace("{single_artist}", GlobalObjects.CurrentSong.FullArtists != null
                        ? GlobalObjects.CurrentSong.FullArtists.First().Name
                        : GlobalObjects.CurrentSong.Artists);


                SendOrAnnounceMessage(message.Channel, response, cmd);
            }
            catch
            {
                Logger.LogStr("Error sending song info.");
            }
        }

        private static async void HandleRemoveCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            bool modAction = false;
            RequestObject reqObj;

            string[] words = message.Message.Split(' ');
            if (message.IsModerator || message.IsBroadcaster)
            {
                if (words.Length > 1)
                {
                    string arg = words[1];

                    // Check if the argument is an ID (number)
                    if (int.TryParse(arg, out int queueId))
                    {
                        modAction = true;
                        reqObj = GlobalObjects.ReqList.FirstOrDefault(o => o.Queueid == queueId);
                    }
                    else
                    {
                        // Remove '@' if present
                        if (arg.StartsWith("@"))
                        {
                            arg = arg.Substring(1);
                        }

                        // Treat the argument as a username
                        string usernameToRemove = arg;

                        modAction = true;
                        reqObj = GlobalObjects.ReqList.LastOrDefault(o => o.Requester.Equals(usernameToRemove, StringComparison.InvariantCultureIgnoreCase));
                    }
                }
                else
                {
                    // No argument provided, remove the moderator's own last request
                    reqObj = GlobalObjects.ReqList.LastOrDefault(o => o.Requester.Equals(message.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                }
            }
            else
            {
                // Remove the user's own last request
                reqObj = GlobalObjects.ReqList.LastOrDefault(o => o.Requester.Equals(message.DisplayName, StringComparison.InvariantCultureIgnoreCase));
            }

            if (reqObj == null) return;

            string tmp = $"{reqObj.Artist} - {reqObj.Title}";
            GlobalObjects.SkipList.Add(reqObj);

            dynamic payload = new { uuid = Settings.Settings.Uuid, key = Settings.Settings.AccessKey, queueid = reqObj.Queueid, };

            await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));

            await Application.Current.Dispatcher.BeginInvoke(new Action(() => { GlobalObjects.ReqList.Remove(reqObj); }));

            switch (modAction)
            {
                case true:
                    GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName.Equals(reqObj.Requester, StringComparison.CurrentCultureIgnoreCase))
                        ?.UpdateCommandTime(true);
                    break;

                case false:
                    GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == message.UserId)
                        ?.UpdateCommandTime(true);
                    break;
            }

            await GlobalObjects.QueueUpdateQueueWindow();

            string response = modAction
                ? $"The request {tmp} requested by @{reqObj.Requester} has been removed."
                : cmd.Response;

            response = response.Replace("{song}", tmp)
                .Replace("{user}", message.DisplayName);

            SendOrAnnounceMessage(message.Channel, response, cmd);
        }

        private static void HandleQueueCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            string output = "";
            int counter = 1;
            foreach (RequestObject requestObject in GlobalObjects.QueueTracks.Take(5))
            {
                output += $"#{counter} {requestObject.Artist} - {requestObject.Title}";
                if (requestObject.Requester != "Spotify") output += $" (@{requestObject.Requester})";
                output += " | ";
                counter++;
            }
            output = output.TrimEnd(' ', '|');
            string response = cmd.Response;
            response = response.Replace("{queue}", output);
            response = response.Replace("{user}", message.DisplayName);

            SendOrAnnounceMessage(message.Channel, response, cmd);
        }

        private static void HandlePositionCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster)) return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            List<QueueItem> queueItems = GetQueueItems(message.DisplayName);
            if (queueItems.Count != 0)
            {
                if (cmd.Response == null) return;
                string response = cmd.Response;
                if (!response.Contains("{songs}") || !response.Contains("{/songs}")) return;
                //Split string into 3 parts, before, between and after the {songs} and {/songs} tags
                string[] split = response.Split(["{songs}", "{/songs}"], StringSplitOptions.None);
                string before = split[0].Replace("{user}", message.DisplayName);
                string between = split[1].Replace("{user}", message.DisplayName);
                string after = split[2].Replace("{user}", message.DisplayName);

                string tmp = "";
                for (int i = 0; i < queueItems.Count; i++)
                {
                    QueueItem item = queueItems[i];
                    tmp += between.Replace("{pos}", "#" + item.Position).Replace("{song}", item.Title);
                    //If the song is the last one, don't add a newline
                    if (i != queueItems.Count - 1) tmp += " | ";
                }

                between = tmp;
                // Combine the 3 parts into one string
                response = before + between + after;
                SendOrAnnounceMessage(message.Channel, response, cmd);
            }
            else
            {
                SendChatMessage(message.Channel, $"@{message.DisplayName} you have no Songs in the current Queue");
            }
        }

        private static async void HandlePauseCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }
            try
            {
                await SpotifyApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
            }
            catch
            {
                // ignored
            }
        }

        private static async void HandlePlayCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            try
            {
                await SpotifyApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, "");
            }
            catch
            {
                // ignored
            }
        }

        private static void HandleNextCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            string response = cmd.Response;
            response = response.Replace("{user}", message.DisplayName);

            //if (GlobalObjects.ReqList.Count == 0)
            //    return;
            response = response.Replace("{song}", GetNextSong());
            SendOrAnnounceMessage(message.Channel, response, cmd);

        }

        private static async void HandleVoteSkipCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }


            if (_skipCooldown) return;
            //Start a skip vote, add the user to SkipVotes, if at least 5 users voted, skip the song
            if (SkipVotes.Any(o => o == message.DisplayName)) return;
            SkipVotes.Add(message.DisplayName);

            string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            {
                User = message.DisplayName,
                MaxReq = $"{Settings.Settings.TwSrMaxReq}",
                ErrorMsg = null,
                MaxLength = $"{Settings.Settings.MaxSongLength}",
                Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
                Req = GlobalObjects.Requester,
                Cd = Settings.Settings.TwSrCooldown.ToString()
            }, cmd.Response);

            SendOrAnnounceMessage(message.Channel, response, cmd);

            if (SkipVotes.Count < Settings.Settings.BotCmdSkipVoteCount) return;
            await SpotifyApiHandler.SkipSong();

            SendChatMessage(message.Channel, "Skipping song by vote...");

            SkipVotes.Clear();
            _skipCooldown = true;
            SkipCooldownTimer.Start();
        }

        private static async void HandleSkipCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {

            int count = 0;
            string name = "";

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                count = GlobalObjects.ReqList.Count;
                if (count <= 0) return;
                RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
                if (firstRequest == null || firstRequest.Trackid != GlobalObjects.CurrentSong.SongId) return;
                name = firstRequest.Requester;
                GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName == name)
                    ?.UpdateCommandTime(true);
            });

            if (count > 0 && name.Equals(message.DisplayName, StringComparison.CurrentCultureIgnoreCase))
            {
                if (cmdParams.UserLevel.All(ul => ul != -1))
                {
                    cmdParams.UserLevel.Add(-1);
                }
            }

            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
                return;
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }

            if (_skipCooldown)
                return;

            count = 0;
            name = "";

            Application.Current.Dispatcher.Invoke(() =>
            {
                count = GlobalObjects.ReqList.Count;
                if (count <= 0) return;
                RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
                if (firstRequest == null || firstRequest.Trackid != GlobalObjects.CurrentSong.SongId) return;
                name = firstRequest.Requester;
                GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName == name)
                    ?.UpdateCommandTime(true);
            });

            string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            {
                User = message.DisplayName,
                MaxReq = $"{Settings.Settings.TwSrMaxReq}",
                ErrorMsg = null,
                MaxLength = $"{Settings.Settings.MaxSongLength}",
                Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
                Req = GlobalObjects.Requester,
                Cd = Settings.Settings.TwSrCooldown.ToString()
            }, cmd.Response);

            await SpotifyApiHandler.SkipSong();


            SendOrAnnounceMessage(message.Channel, response, cmd);


            _skipCooldown = true;
            SkipCooldownTimer.Start();

        }

        private static async void HandleSongRequestCommand(ChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
        {
            //PrintObjectProperties(cmdParams.ExistingUser);

            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster))
            {
                string response = Settings.Settings.BotRespUserlevelTooLowCommand;
                response = response.Replace("{user}", message.DisplayName);

                List<string> allowedUserLevels = Settings.Settings.Commands
                    .First(c => c.CommandType == CommandType.SongRequest)
                    .AllowedUserLevels
                    .Where(level => Enum.IsDefined(typeof(TwitchUserLevels), level)) // Ensure valid enums
                    .Select(level => ((TwitchUserLevels)level).ToString()) // Convert to name
                    .ToList();

                // Join the list into a single string
                string allowedUserLevelsString = string.Join(", ", allowedUserLevels);


                response = response.Replace("{userlevel}", allowedUserLevelsString);

                SendChatMessage(message.Channel, response);
                return;
            }


            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.Settings.ChatLiveStatus) SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            }


            if (message.Message.Split(' ').Length <= 1)
            {
                SendChatMessage(message.Channel, "No query provided.");
                return;
            }

            // Do nothing if the user is blocked, don't even reply
            if (IsUserBlocked(message.DisplayName))
            {
                return;
            }

            TimeSpan cooldown = TimeSpan.FromSeconds(Settings.Settings.TwSrPerUserCooldown); // Set your cooldown time here
            if (!cmdParams.ExistingUser.IsCooldownExpired(cooldown))
            {
                // Inform user about the cooldown
                if (cmdParams.ExistingUser.LastCommandTime == null) return;
                TimeSpan remaining = cooldown - (DateTime.Now - cmdParams.ExistingUser.LastCommandTime.Value);
                Logger.LogStr($"{cmdParams.ExistingUser.DisplayName} is on cooldown. ({remaining.Seconds} more seconds)");
                // if remaining is more than 1 minute format to mm:ss, else to ss
                string time = remaining.Minutes >= 1
                    ? $"{remaining.Minutes} minute{(remaining.Minutes > 1 ? "s" : "")} {remaining.Seconds} seconds"
                    : $"{remaining.Seconds} seconds";

                string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
                {
                    User = message.DisplayName,
                    MaxReq = $"{Settings.Settings.TwSrMaxReq}",
                    ErrorMsg = null,
                    MaxLength = $"{Settings.Settings.MaxSongLength}",
                    Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
                    Req = GlobalObjects.Requester,
                    Cd = time
                }, Settings.Settings.BotRespUserCooldown);
                SendChatMessage(message.Channel, response);
                return;
            }

            // if onCooldown skips
            if (_onCooldown)
            {
                Client.SendMessage(Settings.Settings.TwChannel, CreateCooldownResponse(message));
                return;
            }

            if (SpotifyApiHandler.Spotify == null)
            {
                SendChatMessage(message.Channel, "It seems that Spotify is not connected right now.");
                return;
            }

            string trackId =
                await GetTrackIdFromInput(Regex.Replace(message.Message, $"!{cmd.Trigger}", "", RegexOptions.IgnoreCase)
                    .Trim());

            AddSong(trackId, message, SongRequestSource.Command, cmdParams.ExistingUser);

            // start the command cooldown
            StartCooldown();
            cmdParams.ExistingUser.UpdateCommandTime();
        }

        private static async void SendOrAnnounceMessage(string channel, string message, TwitchCommand cmd)
        {
            if (cmd.IsAnnouncement)
                await AnnounceChatMessage(message, cmd.AnnouncementColor);
            else
                SendChatMessage(channel, message);
        }

        private static bool IsUserAllowed(List<int> allowedUserLevels, TwitchCommandParams cmdParams, bool messageIsBroadcaster)
        {
            if (allowedUserLevels.Count == 0)
                return messageIsBroadcaster;
            return messageIsBroadcaster || allowedUserLevels.Intersect(cmdParams.UserLevel).Any();
        }

        public static async Task<bool> CheckStreamIsUp()
        {
            try
            {
                if (TokenCheck == null) return false;
                if (TwitchApi == null) return false;
                if (Settings.Settings.TwitchUser == null) return false;
                if (string.IsNullOrEmpty(Settings.Settings.TwitchAccessToken)) return false;
                GetStreamsResponse x = await TwitchApi.Helix.Streams.GetStreamsAsync(null, 20, null, null,
                    [Settings.Settings.TwitchUser.Id], null, Settings.Settings.TwitchAccessToken);
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
                                    "Your Twitch Account token has expired. Please login again with Twitch",
                                    MessageDialogStyle.AffirmativeAndNegative,
                                    new MetroDialogSettings
                                    { AffirmativeButtonText = "Login (Main)", NegativeButtonText = "Cancel" });
                                if (msgResult == MessageDialogResult.Negative) return;
                                ApiConnect(TwitchAccount.Main);
                            }
                        });
                        return;
                    }

                    DateTime tokenExpiryDate = DateTime.Now.AddSeconds(TokenCheck.ExpiresIn);

                    List<string> missingItems = Scopes.GetScopes().Where(item => !TokenCheck.Scopes.Contains(item)).ToList();

                    if (!missingItems.Any())
                    {
                        Debug.WriteLine("All elements are present in the list.");
                    }
                    else
                    {
                        MessageDialogResult msgResult = await ((MainWindow)Application.Current.MainWindow).ShowMessageAsync(
                            "Missing Twitch Scopes",
                            $"You are missing the following scopes: {string.Join(", ", missingItems)}.\nThis can be resolved be logging out of Twitch and re-login.\n\nWould you like to logout now?",
                            MessageDialogStyle.AffirmativeAndNegative,
                            new MetroDialogSettings
                            { AffirmativeButtonText = "Login (Bot)", NegativeButtonText = "Cancel" });
                        if (msgResult == MessageDialogResult.Affirmative)
                        {
                            Settings.Settings.TwitchUser = null;
                            Settings.Settings.TwitchAccessToken = "";
                            TwitchApi = null;
                            ApiConnect(TwitchAccount.Main);
                            return;
                        }
                    }

                    Settings.Settings.TwitchAccessTokenExpiryDate = tokenExpiryDate;


                    GlobalObjects.TwitchUserTokenExpired = false;
                    _userId = TokenCheck.UserId;

                    users = await TwitchApi.Helix.Users.GetUsersAsync([_userId], null,
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
                    Settings.Settings.TwChannel = user.Login;

                    // Get User Color:
                    GetUserChatColorResponse chatColorResponse = await TwitchApi.Helix.Chat.GetUserChatColorAsync([user.Id], Settings.Settings.TwitchAccessToken);
                    Settings.Settings.TwitchUserColor = chatColorResponse.Data.Any() ? chatColorResponse.Data[0].Color : "#f8953c";

                    ConfigHandler.WriteAllConfig(Settings.Settings.Export());

                    StreamUpTimer.Tick += StreamUpTimer_Tick;
                    StreamUpTimer.Start();

                    TwitchUserSyncTimer.Tick += TwitchUserSyncTimer_Tick;
                    TwitchUserSyncTimer.Start();
                    await RunTwitchUserSync();
                    //TODO: Enable PubSub when it's fixed in TwitchLib
                    if (PubSubEnabled)
                        CreatePubSubsConnection();

                    break;

                #endregion Main

                #region Bot

                case TwitchAccount.Bot:
                    _twitchApiBot = new TwitchAPI
                    {
                        Settings =
                        {
                            ClientId = ClientId,
                            AccessToken = Settings.Settings.TwitchBotToken
                        }
                    };
                    BotTokenCheck = await _twitchApiBot.Auth.ValidateAccessTokenAsync(Settings.Settings.TwitchBotToken);
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
                                    "Your Twitch Bot Account token has expired. Please login again with Twitch",
                                    MessageDialogStyle.AffirmativeAndNegative,
                                    new MetroDialogSettings
                                    { AffirmativeButtonText = "Login (Bot)", NegativeButtonText = "Cancel" });
                                if (msgResult == MessageDialogResult.Negative) return;
                                ApiConnect(TwitchAccount.Bot);
                            }
                        });
                        return;
                    }

                    DateTime botTokenExpiryDate = DateTime.Now.AddSeconds(BotTokenCheck.ExpiresIn);

                    Settings.Settings.BotAccessTokenExpiryDate = botTokenExpiryDate;

                    GlobalObjects.TwitchBotTokenExpired = false;

                    _userId = BotTokenCheck.UserId;

                    users = await _twitchApiBot.Helix.Users.GetUsersAsync([_userId], null,
                        Settings.Settings.TwitchBotToken);

                    user = users.Users.FirstOrDefault();
                    if (user == null)
                        return;
                    Settings.Settings.TwitchBotUser = user;
                    break;

                #endregion Bot

                default:
                    throw new ArgumentOutOfRangeException(nameof(twitchAccount), twitchAccount, null);
            }
        }

        public static Task MainConnect()
        {
            switch (_mainClient)
            {
                case { IsConnected: true }:
                    return Task.CompletedTask;

                case { IsConnected: false }:
                    _mainClient.Connect();
                    _mainClient.JoinChannel(Settings.Settings.TwChannel);
                    return Task.CompletedTask;

                default:
                    try

                    {
                        // Checks if twitch credentials are present
                        if (Settings.Settings.TwitchUser != null &&
                            (string.IsNullOrEmpty(Settings.Settings.TwitchUser.DisplayName) ||
                             string.IsNullOrEmpty(Settings.Settings.TwitchAccessToken) ||
                             string.IsNullOrEmpty(Settings.Settings.TwChannel)))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (Window window in Application.Current.Windows)
                                    if (window.GetType() == typeof(MainWindow))
                                        //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Red);
                                        ((MainWindow)window).LblStatus.Content = "Please fill in Twitch credentials.";
                            });
                            return Task.CompletedTask;

                        }

                        if (Settings.Settings.TwitchUser == null) return Task.CompletedTask;

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
                        _mainClient = new TwitchClient(customClient);
                        _mainClient.Initialize(credentials, Settings.Settings.TwChannel);
                        _mainClient.Connect();
                    }
                    catch (Exception e)
                    {
                        Logger.LogExc(e);
                    }

                    break;
            }
            return Task.CompletedTask;
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
            msg = msg.Replace("{single_artist}", GlobalObjects.CurrentSong.FullArtists != null ? $"{GlobalObjects.CurrentSong.FullArtists.FirstOrDefault()?.Name}" : GlobalObjects.CurrentSong.Artists);
            msg = msg.Replace("{title}", $"{GlobalObjects.CurrentSong.Title}");
            msg = msg.Replace(@"\n", " - ").Replace("  ", " ");

            TwitchCommand cmd = Settings.Settings.Commands.Find(c => c.CommandType == CommandType.Song);
            if (cmd.IsAnnouncement)
                await AnnounceChatMessage(msg, cmd.AnnouncementColor);
            else
                SendChatMessage(Settings.Settings.TwChannel, msg);
        }

        // Counter for consecutive failures
        private static async void StreamUpTimer_Tick(object sender, EventArgs e)
        {
            bool isStreamUp = await CheckStreamIsUp();

            if (isStreamUp)
            {
                _consecutiveFailures = 0; // Reset failure counter on success

                if (Settings.Settings.IsLive) return; // Only update if the state has changed
                Settings.Settings.IsLive = true;
                Logger.LogStr("Stream is online");
            }
            else
            {
                _consecutiveFailures++; // Increment failure counter

                if (_consecutiveFailures < MaxConsecutiveFailures || !Settings.Settings.IsLive) return; // Only update if the state has changed
                Settings.Settings.IsLive = false;
                Logger.LogStr("Stream is offline");
            }
        }

        public static async void AddSong(string trackId, ChatMessage e, SongRequestSource source, TwitchUser user)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                SendChatMessage(e.Channel, "No song found.");
                return;
            }

            // Switch on trackId
            switch (trackId)
            {
                case "shortened":
                    SendChatMessage(Settings.Settings.TwChannel,
                        "Spotify short links are not supported. Please type in the full title or get the Spotify URI (starts with \"spotify:track:\")");
                    return;
                case "episode":
                    SendChatMessage(Settings.Settings.TwChannel, "Episodes are not supported. Please specify a track!");
                    return;
                case "artist":
                    SendChatMessage(Settings.Settings.TwChannel, "Artist links are not supported. Please specify a track!");
                    return;
                case "album":
                    SendChatMessage(Settings.Settings.TwChannel, "Album links are not supported. Please specify a track!");
                    return;
                case "playlist":
                    SendChatMessage(Settings.Settings.TwChannel, "Playlist links are not supported. Please specify a track!");
                    return;
                case "audiobook":
                    SendChatMessage(Settings.Settings.TwChannel, "Audiobook links are not supported. Please specify a track!");
                    return;
            }

            if (Settings.Settings.LimitSrToPlaylist &&
                !string.IsNullOrEmpty(Settings.Settings.SpotifySongLimitPlaylist))
            {
                Tuple<bool, string> result = await IsInAllowedPlaylist(trackId);
                if (!result.Item1)
                {
                    SendChatMessage(e.Channel, result.Item2);
                    return;
                }
            }

            if (IsSongBlacklisted(trackId))
            {
                SendChatMessage(Settings.Settings.TwChannel, "This song is blocked");
                return;
            }

            FullTrack track = await SpotifyApiHandler.GetTrack(trackId);

            if (track == null)
            {
                SendChatMessage(Settings.Settings.TwChannel, CreateNoTrackFoundResponse(e));
                return;
            }

            if (IsTrackExplicit(track, e, out string response))
            {
                SendChatMessage(e.Channel, response);
                return;
            }

            if (IsTrackUnavailable(track, e, out response))
            {
                SendChatMessage(e.Channel, response);
                return;
            }

            if (IsArtistBlacklisted(track, e, out response))
            {
                SendChatMessage(e.Channel, response);
                return;
            }

            if (IsTrackTooLong(track, e, out response))
            {
                SendChatMessage(e.Channel, response);
                return;
            }

            if (IsTrackAlreadyInQueue(track, e, out response))
            {
                SendChatMessage(e.Channel, response);
                return;
            }

            bool unlimitedSr = false;

            if (user.UserLevels is { Count: > 0 })
            {
                switch (source)
                {
                    case SongRequestSource.Reward:
                        if (Settings.Settings.UnlimitedSrUserlevelsReward != null &&
                            Settings.Settings.UnlimitedSrUserlevelsReward.Count > 0)
                        {
                            unlimitedSr = Settings.Settings.UnlimitedSrUserlevelsReward.Intersect(user.UserLevels).Any();
                        }
                        break;
                    case SongRequestSource.Command:
                        if (Settings.Settings.UnlimitedSrUserlevelsCommand != null &&
                            Settings.Settings.UnlimitedSrUserlevelsCommand.Count > 0)
                        {
                            unlimitedSr = Settings.Settings.UnlimitedSrUserlevelsCommand.Intersect(user.UserLevels).Any();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(source), source, null);
                }
            }

            if (!unlimitedSr && !e.IsBroadcaster)
                if (IsUserAtMaxRequests(e, user, out response))
                {
                    SendChatMessage(e.Channel, response);
                    return;
                }

            SpotifyApiHandler.AddToQ("spotify:track:" + trackId);

            if (Settings.Settings.AddSrToPlaylist)
                await AddToPlaylist(track.Id);

            string successResponse = Settings.Settings.Commands.First(cmd => cmd.Name == "Song Request").Response;
            response = CreateSuccessResponse(track, e.DisplayName, successResponse);
            TwitchCommand cmd = Settings.Settings.Commands.First(cmd => cmd.Name == "Song Request");

            // this will take the first 4 artists and join their names with ", "
            string artists = string.Join(", ", track.Artists
                .Take(4)
                .Select(a => a.Name));

            string length = FormattedTime((int)track.DurationMs);

            // Get the Requester Twitch User Object from the api 
            GetUsersResponse x = await TwitchApi.Helix.Users.GetUsersAsync([e.UserId], null, Settings.Settings.TwitchAccessToken);

            SimpleTwitchUser requestUser = null;
            if (x.Users.Length > 0)
            {
                requestUser = x.Users[0].ToSimpleUser();
            }


            RequestObject o = new()
            {
                Trackid = track.Id,
                PlayerType = Enums.RequestPlayerType.Spotify.ToString(),
                Artist = artists,
                Title = track.Name,
                Length = length,
                Requester = e.DisplayName,
                FullRequester = requestUser,
                Played = 0,
                Albumcover = track.Album.Images[0].Url,
            };

            await UploadToQueue(o);
            //GlobalObjects.QueueUpdateQueueWindow();


            if (Settings.Settings.Commands.First(cmd => cmd.Name == "Song Request").Response.Contains("{ttp}"))
            {
                try
                {
                    if (GlobalObjects.QueueTracks.Count > 0)
                    {


                        //await Task.Delay(2000);

                        int trackIndex = GlobalObjects.QueueTracks.IndexOf(
                            GlobalObjects.QueueTracks.First(qT => qT.Trackid == track.Id));

                        TimeSpan timeToplay = TimeSpan.Zero;
                        TrackInfo tI;
                        if (trackIndex == 0)
                        {
                            tI = await SpotifyApiHandler.GetSongInfo();
                            if (tI != null)
                            {
                                if (tI.SongId != trackId)
                                {
                                    timeToplay += TimeSpan.FromMilliseconds(tI.DurationTotal - tI.Progress);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < trackIndex; i++)
                            {
                                RequestObject item = GlobalObjects.QueueTracks[i];

                                if (i == 0 && item.Trackid == GlobalObjects.CurrentSong.SongId)
                                {
                                    tI = await SpotifyApiHandler.GetSongInfo();
                                    if (tI == null) continue;
                                    int timeLeft = Math.Max(0, tI.DurationTotal - tI.Progress);
                                    timeToplay += TimeSpan.FromMilliseconds(timeLeft);
                                }
                                else
                                {
                                    timeToplay += ParseLength(item.Length);
                                }
                            }
                        }
                        string ttpString = $"{(int)timeToplay.TotalMinutes}m {timeToplay.Seconds}s";
                        response = response.Replace("{ttp}", ttpString);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            response = response.Replace("{ttp}", "");
            SendOrAnnounceMessage(e.Channel, response, cmd);
        }

        private static string ValidateTrackId(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                return "No song found.";

            return trackId switch
            {
                "shortened" => "Spotify short links are not supported. Please type in the full title or use the Spotify URI (starts with \"spotify:track:\")",
                "episode" => "Episodes are not supported. Please specify a track!",
                "artist" => "Artist links are not supported. Please specify a track!",
                "album" => "Album links are not supported. Please specify a track!",
                "playlist" => "Playlist links are not supported. Please specify a track!",
                "audiobook" => "Audiobook links are not supported. Please specify a track!",
                _ => null
            };
        }

        private static async Task<(bool valid, FullTrack track, string message)> TryGetValidTrack(string trackId)
        {
            if (IsSongBlacklisted(trackId))
                return (false, null, "This song is blocked.");

            FullTrack track = await SpotifyApiHandler.GetTrack(trackId);
            if (track == null)
                return (false, null, "No song found.");

            if (IsTrackExplicit(track, null, out string msg)) return (false, null, msg);
            if (IsTrackUnavailable(track, null, out msg)) return (false, null, msg);
            if (IsArtistBlacklisted(track, null, out msg)) return (false, null, msg);
            if (IsTrackTooLong(track, null, out msg)) return (false, null, msg);
            if (IsTrackAlreadyInQueue(track, null, out msg)) return (false, null, msg);

            return (true, track, null);
        }

        private static RequestObject BuildRequestObject(FullTrack track, string requester = "")
        {
            string artists = string.Join(", ", track.Artists.Take(4).Select(a => a.Name));
            string length = FormattedTime((int)track.DurationMs);

            return new RequestObject
            {
                Trackid = track.Id,
                PlayerType = Enums.RequestPlayerType.Spotify.ToString(),
                Artist = artists,
                Title = track.Name,
                Length = length,
                Requester = requester,
                FullRequester = null,
                Played = 0,
                Albumcover = track.Album.Images.FirstOrDefault()?.Url
            };
        }

        public static async Task<string> AddSongFromWebsocket(string trackId, string requester = "")
        {
            string validationError = ValidateTrackId(trackId);
            if (validationError != null)
                return validationError;

            (bool valid, FullTrack track, string message) = await TryGetValidTrack(trackId);
            if (!valid)
                return message;

            SpotifyApiHandler.AddToQ("spotify:track:" + trackId);

            if (Settings.Settings.AddSrToPlaylist)
                await AddToPlaylist(track.Id);

            RequestObject o = BuildRequestObject(track, requester);
            await UploadToQueue(o);

            //GlobalObjects.QueueUpdateQueueWindow();

            return $"{string.Join(", ", track.Artists.Take(2).Select(a => a.Name))} - {track.Name} by has been added to the queue.";
        }


        public static TimeSpan ParseLength(string length)
        {
            string[] parts = length.Split(':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int minutes) &&
                int.TryParse(parts[1], out int seconds))
            {
                return new TimeSpan(0, 0, minutes, seconds);
            }

            return TimeSpan.Zero;
        }


        private static async Task<ReturnObject> AddSong2(string trackId, string username)
        {
            // loads the blacklist from settings
            string response;
            // gets the track information using spotify api
            FullTrack track = await SpotifyApiHandler.GetTrack(trackId);

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
                // if track length exceeds 10 minutes skip and inform requester
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
            SpotifyApiHandler.AddToQ(spotifyUri);

            // if everything worked so far, inform the user that the song has been added to the queue
            response = Settings.Settings.BotRespSuccess;
            response = response.Replace("{user}", username);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", "");

            // this will take the first 4 artists and join their names with ", "
            string artists2 = string.Join(", ", track.Artists
                .Take(4)
                .Select(a => a.Name));

            string length = FormattedTime((int)track.DurationMs);

            RequestObject o = new()
            {
                Trackid = track.Id,
                PlayerType = Enum.GetName(typeof(Enums.RequestPlayerType), Enums.RequestPlayerType.Spotify),
                Artist = artists2,
                Title = track.Name,
                Length = length,
                Requester = username,
                Played = 0,
                Albumcover = track.Album.Images[0].Url,
            };

            // Upload the track and who requested it to the queue on the server
            await UploadToQueue(o);

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
                if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                    Settings.Settings.SpotifyPlaylistId == "-1")
                {
                    ListResponse<bool> x = await SpotifyApiHandler.Spotify.CheckSavedTracksAsync([trackId]);
                    if (x.List.Count > 0)
                    {
                        switch (x.List[0])
                        {
                            case true:
                                if (sendResponse)
                                {
                                    SendChatMessage(Settings.Settings.TwChannel,
                                        $"The Song \"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}\" is already in the playlist.");
                                }
                                return true;

                            case false:
                                await SpotifyApiHandler.AddToPlaylist(trackId);
                                return false;
                        }
                    }
                }
                else
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

                    await SpotifyApiHandler.AddToPlaylist(trackId);
                    return false;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("Error adding song to playlist");
                return true;
            }

            return false;
        }

        private static async Task AnnounceChatMessage(string msg, AnnouncementColor color)
        {
            AnnouncementColors announcementColors = color switch
            {
                AnnouncementColor.Blue => AnnouncementColors.Blue,
                AnnouncementColor.Green => AnnouncementColors.Green,
                AnnouncementColor.Orange => AnnouncementColors.Orange,
                AnnouncementColor.Purple => AnnouncementColors.Purple,
                AnnouncementColor.Primary => AnnouncementColors.Primary,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
            try
            {
                if (BotTokenCheck != null)
                {
                    await _twitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
                        Settings.Settings.TwitchBotUser.Id, msg, announcementColors,
                        Settings.Settings.TwitchBotToken);
                    return;
                }

                if (TokenCheck != null)
                {
                    await _twitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
                        Settings.Settings.TwitchUser.Id, msg, announcementColors,
                        Settings.Settings.TwitchAccessToken);
                    return;
                }
            }
            catch (Exception)
            {
                Logger.LogStr("TWITCH API: Could not send announcement. Has the bot been created through the app?");
            }

            SendChatMessage(Settings.Settings.TwChannel, $"{msg}");
        }

        private static async Task AnnounceInChat(string msg)
        {
            Tuple<string, AnnouncementColors> tup = GetStringAndColor(msg);
            try
            {
                if (BotTokenCheck != null)
                {
                    await _twitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
                        Settings.Settings.TwitchBotUser.Id, $"{tup.Item1}", tup.Item2,
                        Settings.Settings.TwitchBotToken);
                    return;
                }

                if (TokenCheck != null)
                {
                    await _twitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.Settings.TwitchUser.Id,
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

        private static async Task<Tuple<bool, FullPlaylist>> CheckIsSongAllowed(string trackId,
            string spotifySongLimitPlaylist)
        {
            FullPlaylist playlist = await SpotifyApiHandler.Spotify.GetPlaylistAsync(spotifySongLimitPlaylist);
            Paging<PlaylistTrack> tracks = await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(spotifySongLimitPlaylist);
            while (tracks is { Items: not null })
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

        private static bool CheckLiveStatus()
        {
            if (Settings.Settings.IsLive)
            {
                Logger.LogStr("STREAM: Stream is live.");
                return true;
            }
            if (!Settings.Settings.BotOnlyWorkWhenLive)
                return true;

            Logger.LogStr("STREAM: Stream is down.");
            return false;
        }

        private static (TwitchUserLevels, bool) CheckUserLevel(ChatMessage o, int type = 0, int subtier = 0)
        {
            // Type 0 = Command, 1 = Reward
            List<TwitchUserLevels> userLevels = [];

            if (o.IsBroadcaster) userLevels.Add(TwitchUserLevels.Broadcaster);
            if (o.IsModerator) userLevels.Add(TwitchUserLevels.Moderator);
            if (o.IsVip) userLevels.Add(TwitchUserLevels.Vip);
            if (o.IsSubscriber && subtier is 0 or 1) userLevels.Add(TwitchUserLevels.Subscriber);
            if (o.IsSubscriber && subtier is 2) userLevels.Add(TwitchUserLevels.SubscriberT2);
            if (o.IsSubscriber && subtier is 3) userLevels.Add(TwitchUserLevels.SubscriberT3);

            TwitchUser user = GlobalObjects.TwitchUsers.FirstOrDefault(user => user.UserId == o.UserId);
            if (user != null)
            {
                if (user?.IsFollowing == true)
                {
                    userLevels.Add(TwitchUserLevels.Follower);
                }
                switch (user.SubTier)
                {
                    case 1:
                        userLevels.Add(TwitchUserLevels.Subscriber);
                        break;

                    case 2:
                        userLevels.Add(TwitchUserLevels.SubscriberT2);
                        break;

                    case 3:
                        userLevels.Add(TwitchUserLevels.SubscriberT3);
                        break;
                }
            }

            userLevels.Add(TwitchUserLevels.Viewer);

            // Determine if the user is allowed based on the type (Command or Reward)
            bool isAllowed = type == 0
                ? Settings.Settings.UserLevelsCommand.Any(level => userLevels.Contains((TwitchUserLevels)level))
                : Settings.Settings.UserLevelsReward.Any(level => userLevels.Contains((TwitchUserLevels)level));

            return (userLevels.Max(), isAllowed);
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
            Client.JoinChannel(Settings.Settings.TwChannel, true);
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
            if (!e.ChatMessage.Message.StartsWith("!") && string.IsNullOrEmpty(e.ChatMessage.CustomRewardId))
                return;

            // Attempt to find the user in the existing list.
            TwitchUser existingUser = GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == e.ChatMessage.UserId);

            int subtier = int.Parse(GlobalObjects.subscribers.FirstOrDefault(sub => sub.UserId == e.ChatMessage.UserId)?.Tier ?? "0") / 1000;
            List<int> userLevels;
            if (existingUser == null)
            {
                userLevels = await GetUserLevels(e.ChatMessage);

                // If the user doesn't exist, add them.
                TwitchUser newUser = new()
                {
                    UserId = e.ChatMessage.UserId,
                    UserName = e.ChatMessage.Username,
                    LastCommandTime = null,
                    DisplayName = e.ChatMessage.DisplayName,
                    UserLevels = userLevels, // Convert enum to int for storage
                    IsFollowing = false,
                    FollowInformation = new ChannelFollower(),
                    SubTier = subtier,
                    IsSrBlocked = IsUserBlocked(e.ChatMessage.DisplayName)
                };
                Application.Current.Dispatcher.Invoke(() => { GlobalObjects.TwitchUsers.Add(newUser); });
                existingUser = newUser;
            }
            else
            {
                userLevels = existingUser.UserLevels;
            }

            if (Settings.Settings.TwRewardSkipId.Count > 0 &&
                Settings.Settings.TwRewardSkipId.Any(o => o == e.ChatMessage.CustomRewardId) && !PubSubEnabled)
            {
                // Skip song
                if (_skipCooldown)
                    return;
                await SpotifyApiHandler.SkipSong();

                SendChatMessage(Settings.Settings.TwChannel, "Skipping current song...");
                _skipCooldown = true;
                SkipCooldownTimer.Start();
            }

            if (Settings.Settings.TwRewardId.Count > 0 &&
                Settings.Settings.TwRewardId.Any(o => o == e.ChatMessage.CustomRewardId) && !PubSubEnabled &&
                Settings.Settings.TwSrReward)
            {
                Settings.Settings.IsLive = await CheckStreamIsUp();

                // Check if the user level is lower than broadcaster or not allowed to request songs
                if (!e.ChatMessage.IsBroadcaster)
                {
                    if (!Settings.Settings.UserLevelsReward.Intersect(existingUser.UserLevels).Any())
                    {
                        // Send a message to the user that their user level is too low to request songs
                        string response = Settings.Settings.BotRespUserlevelTooLowCommand;
                        response = response.Replace("{user}", e.ChatMessage.DisplayName);

                        string userLevelNames = string.Join(",", Settings.Settings.UserLevelsReward.Select(level => Enum.GetName(typeof(TwitchUserLevels), level)).ToList());

                        response = response.Replace("{userlevel}",
                            $"{Enum.GetName(typeof(TwitchUserLevels), userLevelNames)}");

                        // Send a message to the user that their user level is too low to request songs
                        SendChatMessage(e.ChatMessage.Channel, response);
                        return;
                    }
                }

                // Do nothing if the user is blocked, don't even reply
                if (IsUserBlocked(e.ChatMessage.DisplayName))
                {
                    return;
                }

                if (SpotifyApiHandler.Spotify == null)
                {
                    SendChatMessage(e.ChatMessage.Channel, "It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(await GetTrackIdFromInput(e.ChatMessage.Message), e.ChatMessage, SongRequestSource.Reward, existingUser);
                return;
            }

            if (!e.ChatMessage.Message.StartsWith("!")) return;

            Settings.Settings.IsLive = await CheckStreamIsUp();

            bool executed = TwitchCommandHandler.TryExecuteCommand(e.ChatMessage, new TwitchCommandParams
            {
                Subtier = subtier,
                ExistingUser = existingUser,
                UserLevel = userLevels
            });

            if (!executed)
            {
                // Optionally handle the case where no command matched.
                Logger.LogStr("Command not found or not enabled.");
            }

            //return;

            //if (Settings.Settings.Player == 6 &&
            //    e.ChatMessage.Message.StartsWith($"!ytsr ", StringComparison.CurrentCultureIgnoreCase))
            //{
            //    // TODO: UNFINISHED
            //    if (Settings.Settings.BotOnlyWorkWhenLive)
            //        try
            //        {
            //            if (!CheckLiveStatus())
            //            {
            //                if (Settings.Settings.ChatLiveStatus)
            //                    SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //                return;
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //        }

            //    // Do nothing if the user is blocked, don't even reply
            //    if (IsUserBlocked(e.ChatMessage.DisplayName))
            //    {
            //        return;
            //    }

            //    TimeSpan cooldown =
            //        TimeSpan.FromSeconds(Settings.Settings.TwSrPerUserCooldown); // Set your cooldown time here
            //    if (!existingUser.IsCooldownExpired(cooldown))
            //    {
            //        // Inform user about the cooldown
            //        if (existingUser.LastCommandTime == null) return;
            //        TimeSpan remaining = cooldown - (DateTime.Now - existingUser.LastCommandTime.Value);
            //        Logger.LogStr($"{existingUser.DisplayName} is on cooldown. ({remaining.Seconds} more seconds)");
            //        // if remaining is more than 1 minute format to mm:ss, else to ss
            //        string time = remaining.Minutes >= 1
            //            ? $"{remaining.Minutes} minute{(remaining.Minutes > 1 ? "s" : "")} {remaining.Seconds} seconds"
            //            : $"{remaining.Seconds} seconds";

            //        string msg = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            //        {
            //            User = e.ChatMessage.DisplayName,
            //            MaxReq = $"{Settings.Settings.TwSrMaxReq}",
            //            ErrorMsg = null,
            //            MaxLength = $"{Settings.Settings.MaxSongLength}",
            //            Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
            //            Req = GlobalObjects.Requester,
            //            Cd = time
            //        }, Settings.Settings.BotRespUserCooldown);
            //        SendChatMessage(e.ChatMessage.Channel, msg);
            //        return;
            //    }

            //    // if onCooldown skips
            //    if (_onCooldown)
            //    {
            //        Client.SendMessage(Settings.Settings.TwChannel, CreateCooldownResponse(e.ChatMessage));
            //        return;
            //    }

            //    string videoId = ExtractYouTubeVideoIdFromText(e.ChatMessage.Message);

            //    string title =
            //        await WebTitleFetcher.GetWebsiteTitleAsync($"https://www.youtube.com/watch?v={videoId}");
            //    string videoThumbailUrl = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

            //    Debug.WriteLine($"{title} | thumb: {videoThumbailUrl}");
            //    SendChatMessage(e.ChatMessage.Channel, title);

            //    //TODO: Add song to the queue and start playing

            //    RequestObject o = new()
            //    {
            //        Uuid = Settings.Settings.Uuid,
            //        Trackid = videoId,
            //        PlayerType = Enum.GetName(typeof(Enums.RequestPlayerType), Enums.RequestPlayerType.Youtube),
            //        Artist = "",
            //        Title = title,
            //        Length = "",
            //        Requester = e.ChatMessage.DisplayName,
            //        Played = 0,
            //        Albumcover = videoThumbailUrl,
            //    };

            //    await UploadToQueue(o);
            //}

            //// Skip Command for mods (!skip)
            //if (Settings.Settings.Player == 0 && e.ChatMessage.Message.ToLower() ==
            //    $"!{Settings.Settings.BotCmdSkipTrigger.ToLower()}" &&
            //    Settings.Settings.BotCmdSkip)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //    }

            //    if (_skipCooldown)
            //        return;

            //    int count = 0;
            //    string name = "";

            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        count = GlobalObjects.ReqList.Count;
            //        if (count > 0)
            //        {
            //            RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
            //            if (firstRequest != null && firstRequest.Trackid == GlobalObjects.CurrentSong.SongId)
            //            {
            //                name = firstRequest.Requester;
            //                GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName == name)
            //                    ?.UpdateCommandTime(true);
            //            }
            //        }
            //    });

            //    if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster ||
            //        (count > 0 && name == e.ChatMessage.DisplayName))
            //    {
            //        string msg = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            //        {
            //            User = e.ChatMessage.DisplayName,
            //            MaxReq = $"{Settings.Settings.TwSrMaxReq}",
            //            ErrorMsg = null,
            //            MaxLength = $"{Settings.Settings.MaxSongLength}",
            //            Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
            //            Req = GlobalObjects.Requester,
            //            Cd = Settings.Settings.TwSrCooldown.ToString()
            //        }, Settings.Settings.BotRespModSkip);

            //        await SpotifyApiHandler.SkipSong();

            //        {
            //            if (msg.StartsWith("[announce "))
            //            {
            //                await AnnounceInChat(msg);
            //            }
            //            else
            //            {
            //                SendChatMessage(e.ChatMessage.Channel, msg);
            //            }

            //            _skipCooldown = true;
            //            SkipCooldownTimer.Start();
            //        }
            //    }
            //}

            //// Voteskip command (!voteskip)
            //else if (Settings.Settings.Player == 0 &&
            //         e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdVoteskipTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdSkipVote)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //    }

            //    if (_skipCooldown)
            //        return;
            //    //Start a skip vote, add the user to SkipVotes, if at least 5 users voted, skip the song
            //    if (SkipVotes.Any(o => o == e.ChatMessage.DisplayName)) return;
            //    SkipVotes.Add(e.ChatMessage.DisplayName);

            //    string msg = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            //    {
            //        User = e.ChatMessage.DisplayName,
            //        MaxReq = $"{Settings.Settings.TwSrMaxReq}",
            //        ErrorMsg = null,
            //        MaxLength = $"{Settings.Settings.MaxSongLength}",
            //        Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
            //        Req = GlobalObjects.Requester,
            //        Cd = Settings.Settings.TwSrCooldown.ToString()
            //    }, Settings.Settings.BotRespVoteSkip);

            //    if (msg.StartsWith("[announce "))
            //    {
            //        await AnnounceInChat(msg);
            //    }
            //    else
            //    {
            //        SendChatMessage(e.ChatMessage.Channel, msg);
            //    }

            //    if (SkipVotes.Count >= Settings.Settings.BotCmdSkipVoteCount)
            //    {
            //        await SpotifyApiHandler.SkipSong();

            //        SendChatMessage(e.ChatMessage.Channel, "Skipping song by vote...");

            //        SkipVotes.Clear();
            //        _skipCooldown = true;
            //        SkipCooldownTimer.Start();
            //    }
            //}

            //// Song command (!song)
            //else if (e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdSongTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdSong)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }

            //        //string msg = GetCurrentSong();
            //        //string artist = GlobalObjects.CurrentSong.Artists;
            //        //string title = !string.IsNullOrEmpty(GlobalObjects.CurrentSong.Title)
            //        //    ? GlobalObjects.CurrentSong.Title
            //        //    : "";
            //        //msg = msg.Replace("{user}", e.ChatMessage.DisplayName);
            //        //msg = msg.Replace("{song}", $"{artist} {(title != "" ? " - " + title : "")}");

            //        string msg = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            //        {
            //            User = e.ChatMessage.DisplayName,
            //            SingleArtist = GlobalObjects.CurrentSong.FullArtists != null
            //                ? GlobalObjects.CurrentSong.FullArtists.First().Name
            //                : GlobalObjects.CurrentSong.Artists,
            //            MaxReq = $"{Settings.Settings.TwSrMaxReq}",
            //            ErrorMsg = null,
            //            MaxLength = $"{Settings.Settings.MaxSongLength}",
            //            Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
            //            Req = GlobalObjects.Requester,
            //            Cd = Settings.Settings.TwSrCooldown.ToString()
            //        }, Settings.Settings.BotRespSong);

            //        if (msg.Contains("{single_artist}"))
            //            msg = msg.Replace("{single_artist}",
            //                GlobalObjects.CurrentSong.FullArtists != null
            //                    ? GlobalObjects.CurrentSong.FullArtists.First().Name
            //                    : GlobalObjects.CurrentSong.Artists);

            //        if (msg.StartsWith("[announce "))
            //        {
            //            await AnnounceInChat(msg);
            //        }
            //        else
            //        {
            //            SendChatMessage(e.ChatMessage.Channel, msg);
            //        }
            //    }
            //    catch
            //    {
            //        Logger.LogStr("Error sending song info.");
            //    }
            //}

            //// Pos command (!pos)
            //else if (Settings.Settings.Player == 0 &&
            //         e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdPosTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdPos)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //    }

            //    List<QueueItem> queueItems = GetQueueItems(e.ChatMessage.DisplayName);
            //    if (queueItems.Count != 0)
            //    {
            //        if (Settings.Settings.BotRespPos == null)
            //            return;
            //        string response = Settings.Settings.BotRespPos;
            //        if (!response.Contains("{songs}") || !response.Contains("{/songs}")) return;
            //        //Split string into 3 parts, before, between and after the {songs} and {/songs} tags
            //        string[] split = response.Split(["{songs}", "{/songs}"], StringSplitOptions.None);
            //        string before = split[0].Replace("{user}", e.ChatMessage.DisplayName);
            //        string between = split[1].Replace("{user}", e.ChatMessage.DisplayName);
            //        string after = split[2].Replace("{user}", e.ChatMessage.DisplayName);

            //        string tmp = "";
            //        for (int i = 0; i < queueItems.Count; i++)
            //        {
            //            QueueItem item = queueItems[i];
            //            tmp += between.Replace("{pos}", "#" + item.Position).Replace("{song}", item.Title);
            //            //If the song is the last one, don't add a newline
            //            if (i != queueItems.Count - 1)
            //                tmp += " | ";
            //        }

            //        between = tmp;
            //        // Combine the 3 parts into one string
            //        string output = before + between + after;
            //        if (response.StartsWith("[announce "))
            //        {
            //            await AnnounceInChat(response);
            //        }
            //        else
            //        {
            //            SendChatMessage(e.ChatMessage.Channel, output);
            //        }
            //    }
            //    else
            //    {
            //        SendChatMessage(e.ChatMessage.Channel,
            //            $"@{e.ChatMessage.DisplayName} you have no Songs in the current Queue");
            //    }
            //}

            //// Next command (!next)
            //else if (Settings.Settings.Player == 0 &&
            //         e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdNextTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdNext)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //    }

            //    string response = Settings.Settings.BotRespNext;
            //    response = response.Replace("{user}", e.ChatMessage.DisplayName);

            //    //if (GlobalObjects.ReqList.Count == 0)
            //    //    return;
            //    response = response.Replace("{song}", GetNextSong());
            //    if (response.StartsWith("[announce "))
            //    {
            //        await AnnounceInChat(response);
            //    }
            //    else
            //    {
            //        SendChatMessage(e.ChatMessage.Channel, response);
            //    }
            //}

            //// Remove command (!remove)
            //else if (Settings.Settings.Player == 0 &&
            //         e.ChatMessage.Message.StartsWith($"!{Settings.Settings.BotCmdRemoveTrigger.ToLower()}",
            //             StringComparison.CurrentCultureIgnoreCase) &&
            //         Settings.Settings.BotCmdRemove)
            //{
            //    try
            //    {
            //        if (!CheckLiveStatus())
            //        {
            //            if (Settings.Settings.ChatLiveStatus)
            //                SendChatMessage(Settings.Settings.TwChannel, "The stream is not live right now.");
            //            return;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Logger.LogStr("Error sending chat message \"The stream is not live right now.\"");
            //    }

            //    bool modAction = false;
            //    RequestObject reqObj;

            //    string[] words = e.ChatMessage.Message.Split(' ');
            //    if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
            //    {
            //        if (words.Length > 1)
            //        {
            //            string arg = words[1];

            //            // Check if the argument is an ID (number)
            //            if (int.TryParse(arg, out int queueId))
            //            {
            //                modAction = true;
            //                reqObj = GlobalObjects.ReqList.FirstOrDefault(o => o.Queueid == queueId);
            //            }
            //            else
            //            {
            //                // Remove '@' if present
            //                if (arg.StartsWith("@"))
            //                {
            //                    arg = arg.Substring(1);
            //                }

            //                // Treat the argument as a username
            //                string usernameToRemove = arg;

            //                modAction = true;
            //                reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
            //                    o.Requester.Equals(usernameToRemove, StringComparison.InvariantCultureIgnoreCase));
            //            }
            //        }
            //        else
            //        {
            //            // No argument provided, remove the moderator's own last request
            //            reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
            //                o.Requester.Equals(e.ChatMessage.DisplayName,
            //                    StringComparison.InvariantCultureIgnoreCase));
            //        }
            //    }
            //    else
            //    {
            //        // Remove the user's own last request
            //        reqObj = GlobalObjects.ReqList.LastOrDefault(o =>
            //            o.Requester.Equals(e.ChatMessage.DisplayName, StringComparison.InvariantCultureIgnoreCase));
            //    }

            //    if (reqObj == null)
            //        return;

            //    string tmp = $"{reqObj.Artist} - {reqObj.Title}";
            //    GlobalObjects.SkipList.Add(reqObj);

            //    dynamic payload = new
            //    {
            //        uuid = Settings.Settings.Uuid,
            //        key = Settings.Settings.AccessKey,
            //        queueid = reqObj.Queueid,
            //    };

            //    await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));

            //    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        GlobalObjects.ReqList.Remove(reqObj);
            //    }));

            //    switch (modAction)
            //    {
            //        case true:
            //            GlobalObjects.TwitchUsers.FirstOrDefault(o =>
            //                    o.DisplayName.Equals(reqObj.Requester, StringComparison.CurrentCultureIgnoreCase))
            //                ?.UpdateCommandTime(true);
            //            break;

            //        case false:
            //            GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == e.ChatMessage.UserId)
            //                ?.UpdateCommandTime(true);
            //            break;
            //    }

            //    GlobalObjects.QueueUpdateQueueWindow();

            //    string response = modAction
            //        ? $"The request {tmp} requested by @{reqObj.Requester} has been removed."
            //        : Settings.Settings.BotRespRemove;

            //    response = response
            //        .Replace("{song}", tmp)
            //        .Replace("{user}", e.ChatMessage.DisplayName);

            //    if (response.StartsWith("[announce "))
            //    {
            //        await AnnounceInChat(response);
            //    }
            //    else
            //    {
            //        SendChatMessage(e.ChatMessage.Channel, response);
            //    }
            //}

            //// Songlike command (!songlike)
            //else if (Settings.Settings.Player == 0 &&
            //         e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdSonglikeTrigger.ToLower()}" &&
            //         (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) && Settings.Settings.BotCmdSonglike)
            //{
            //    if (string.IsNullOrWhiteSpace(Settings.Settings.SpotifyPlaylistId))
            //    {
            //        SendChatMessage(Settings.Settings.TwChannel,
            //            "No playlist has been specified. Go to Settings -> Spotify and select the playlist you want to use.");
            //        return;
            //    }

            //    try
            //    {
            //        if (await AddToPlaylist(GlobalObjects.CurrentSong.SongId, true)) return;

            //        string response = Settings.Settings.BotRespSongLike;
            //        response = response.Replace("{song}",
            //            $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");
            //        if (response.StartsWith("[announce "))
            //        {
            //            await AnnounceInChat(response);
            //        }
            //        else
            //        {
            //            SendChatMessage(e.ChatMessage.Channel, response);
            //        }
            //    }
            //    catch (Exception exception)
            //    {
            //        Logger.LogStr("SPOTIFY: Error while adding song to playlist");
            //        Logger.LogExc(exception);
            //    }
            //}

            //// Vol command (!vol)
            //else if (Settings.Settings.Player == 0 && e.ChatMessage.Message.ToLower().StartsWith("!vol ") &&
            //         Settings.Settings.BotCmdVol)
            //{
            //    if (Settings.Settings.BotCmdVolIgnoreMod)
            //    {
            //        await SetSpotifyVolume(e.ChatMessage);
            //    }
            //    else if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
            //    {
            //        await SetSpotifyVolume(e.ChatMessage);
            //    }
            //}

            //// Queue command (!queue)
            //else if (e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdQueueTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdQueue)
            //{
            //    string output = "";
            //    int counter = 1;
            //    foreach (RequestObject requestObject in GlobalObjects.QueueTracks.Take(5))
            //    {
            //        output += $"#{counter} {requestObject.Artist} - {requestObject.Title}";
            //        if (requestObject.Requester != "Spotify")
            //            output += $" (@{requestObject.Requester})";
            //        output += " | ";
            //        counter++;
            //    }

            //    output = output.TrimEnd(' ', '|');
            //    // if output exceeds 500 characters, split at the last "|" before 500 characters
            //    SendChatMessage(e.ChatMessage.Channel, output);
            //}

            //// Commands command (!cmd)
            //else if (e.ChatMessage.Message.ToLower() == $"!{Settings.Settings.BotCmdCommandsTrigger.ToLower()}" &&
            //         Settings.Settings.BotCmdCommands)
            //{
            //    IEnumerable<BotCommandInfo> enabledCommands =
            //        BotConfigExtensions.GetAllBotCommands(Settings.Settings.CurrentConfig.BotConfig, true);

            //    List<string> commands =
            //        enabledCommands.Select(cmd =>
            //            cmd.Trigger.StartsWith("!") ? cmd.Trigger : "!" + cmd.Trigger).ToList();
            //    if (Settings.Settings.TwSrCommand)
            //        commands.Insert(0, $"!{Settings.Settings.BotCmdSsrTrigger}");

            //    SendChatMessage(e.ChatMessage.Channel,
            //        $"Active Songify commands: {string.Join(", ", commands)}");
            //}

            //// Play command (!play)
            //else if (e.ChatMessage.Message.ToLower() == "!play" &&
            //         ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
            //          Settings.Settings.BotCmdPlayPause))
            //{
            //    try
            //    {
            //        await SpotifyApiHandler.Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId,
            //            "", null, "");
            //    }
            //    catch
            //    {
            //        // ignored
            //    }
            //}

            //// Pause command (!pause)
            //else if (e.ChatMessage.Message.ToLower() == "!pause" &&
            //         ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) &&
            //          Settings.Settings.BotCmdPlayPause))
            //{
            //    try
            //    {
            //        await SpotifyApiHandler.Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
            //    }
            //    catch
            //    {
            //        // ignored
            //    }
            //}

            //// Vol coammand without arguments (!vol)
            //else if (e.ChatMessage.Message.ToLower() == "!vol" && Settings.Settings.BotCmdVol)
            //{
            //    bool isBroadcasterOrModerator = e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator;

            //    if (Settings.Settings.BotCmdVolIgnoreMod)
            //    {
            //        Client.SendMessage(e.ChatMessage.Channel,
            //            $"Spotify volume is at {(await SpotifyApiHandler.Spotify.GetPlaybackAsync()).Device.VolumePercent}%");
            //    }
            //    else
            //    {
            //        if (isBroadcasterOrModerator)
            //        {
            //            // Always send the message if BotCmdVol is true and BotCmdVolIgnoreMod is false
            //            Client.SendMessage(e.ChatMessage.Channel,
            //                $"Spotify volume is at {(await SpotifyApiHandler.Spotify.GetPlaybackAsync()).Device.VolumePercent}%");
            //        }
            //    }
            //}

            //// Bansong command (!bansong)
            //else if (e.ChatMessage.Message.ToLower() == "!bansong" &&
            //         (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator))
            //{
            //    TrackInfo currentSong = GlobalObjects.CurrentSong;
            //    if (currentSong == null ||
            //        Settings.Settings.SongBlacklist.Any(track => track.TrackId == currentSong.SongId))
            //        return;

            //    List<TrackItem> blacklist = Settings.Settings.SongBlacklist;
            //    blacklist.Add(new TrackItem
            //    {
            //        Artists = currentSong.Artists,
            //        TrackName = currentSong.Title,
            //        TrackId = currentSong.SongId,
            //        TrackUri = $"spotify:track:{currentSong.SongId}",
            //        ReadableName = $"{currentSong.Artists} - {currentSong.Title}"
            //    });
            //    Settings.Settings.SongBlacklist = Settings.Settings.SongBlacklist;

            //    string msg = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            //    {
            //        User = e.ChatMessage.DisplayName,
            //        MaxReq = $"{Settings.Settings.TwSrMaxReq}",
            //        ErrorMsg = null,
            //        MaxLength = $"{Settings.Settings.MaxSongLength}",
            //        Votes = $"{SkipVotes.Count}/{Settings.Settings.BotCmdSkipVoteCount}",
            //        Req = GlobalObjects.Requester,
            //        Cd = Settings.Settings.TwSrCooldown.ToString()
            //    }, "The song {song} has been added to the blocklist.");

            //    SendChatMessage(e.ChatMessage.Channel, msg);

            //    await SpotifyApiHandler.SkipSong();
            //}
        }

        private static async Task<List<int>> GetUserLevels(ChatMessage o)
        {
            List<int> userLevels =
            [
                (int)TwitchUserLevels.Viewer
            ];

            string userId = o.UserId;
            try
            {
                GlobalObjects.subscribers = await TwitchApiHelper.GetAllSubscribersAsync();
                GlobalObjects.moderators = await TwitchApiHelper.GetAllModeratorsAsync();
                GlobalObjects.vips = await TwitchApiHelper.GetAllVipsAsync();
            }
            catch (Exception e)
            {
                // Missing scopes, prompt to reconnect Twitch
                Logger.LogExc(e);
                Logger.LogStr("TWITCH: MISSING SCOPES, PLEASE RE-LINK TWITCH");
                try
                {
                    if (!toastSent)
                        new ToastContentBuilder()
                        .AddText($"Songify")
                        .AddText($"Can't fetch Twitch Users, please re-link Twitch")
                        .AddAttributionText(DateTime.Now.ToString(CultureInfo.CurrentCulture))
                        .Show();
                    toastSent = true;
                }
                catch (Exception exception)
                {
                    Logger.LogExc(exception);
                }
                return userLevels;
            }


            // Check if the user is a moderator
            bool isModerator = GlobalObjects.moderators.Any(m => m.UserId == userId);
            if (isModerator)
            {
                userLevels.Add((int)TwitchUserLevels.Moderator);
            }
            bool isVip = GlobalObjects.vips.Any(v => v.UserId == userId);
            if (isVip)
            {
                userLevels.Add((int)TwitchUserLevels.Vip);
            }
            Subscription subscription = GlobalObjects.subscribers.FirstOrDefault(sub => sub.UserId == userId);
            if (subscription != null)
            {
                userLevels.Add((int)TwitchUserLevels.Subscriber);
                switch (subscription.Tier)
                {
                    case "200":
                        userLevels.Add((int)TwitchUserLevels.SubscriberT2);
                        break;

                    case "300":
                        userLevels.Add((int)TwitchUserLevels.SubscriberT3);
                        break;
                }
            }

            if (userId == Settings.Settings.TwitchUser.Id)
                userLevels.Add((int)TwitchUserLevels.Broadcaster);

            // Get follow status
            (bool? isFollowing, ChannelFollower followInfo) = await GetIsUserFollowing(userId);

            if (isFollowing != null && (bool)isFollowing)
            {
                userLevels.Add((int)TwitchUserLevels.Follower);
            }

            return userLevels;
        }

        /// <summary>
        /// Splits the input on whitespace and tries to extract a YouTube Video ID
        /// from each token. Returns the first found ID, or null if none found.
        /// </summary>
        /// <param name="input">A string that may contain a YouTube URL among other text.</param>
        /// <returns>YouTube video ID, or null if not found.</returns>
        public static string ExtractYouTubeVideoIdFromText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Split on whitespace
            var tokens = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var videoId = ExtractYouTubeVideoId(token);
                if (!string.IsNullOrEmpty(videoId))
                {
                    return videoId;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts a YouTube video ID from a single token (URL string).
        /// For example:
        ///   - https://youtu.be/VIDEOID
        ///   - https://www.youtube.com/watch?v=VIDEOID
        ///   - https://youtube.com/embed/VIDEOID
        ///   - https://youtube.com/shorts/VIDEOID
        /// Returns null if no valid YouTube ID is found.
        /// </summary>
        private static string ExtractYouTubeVideoId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Attempt to parse as a Uri. If it fails, try prefixing with "https://"
            // in case the user provided a URL without scheme.
            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
            {
                if (!Uri.TryCreate("https://" + input, UriKind.Absolute, out uri))
                {
                    return null;
                }
            }

            // Normalize host by removing "www." prefix for easier comparison
            string host = uri.Host.ToLower();
            if (host.StartsWith("www."))
            {
                host = host.Substring(4);
            }

            // Check for common YouTube hosts
            if (host == "youtube.com" || host == "youtu.be" ||
                host == "youtube-nocookie.com" || host == "m.youtube.com")
            {
                // 1) If link is in the form "youtu.be/VIDEOID"
                //    The video ID is in the first path segment
                if (host == "youtu.be")
                {
                    // Path looks like "/VIDEOID", so strip the leading slash
                    return uri.AbsolutePath.TrimStart('/');
                }

                // 2) For youtube.com-like domains, we look for:
                //    - watch?v=VIDEOID
                //    - embed/VIDEOID
                //    - shorts/VIDEOID
                var pathSegments = uri.AbsolutePath.Trim('/').Split('/');

                // Check for query param ?v=VIDEOID
                var query = HttpUtility.ParseQueryString(uri.Query);
                if (query.AllKeys != null && query["v"] != null)
                {
                    return query["v"];
                }

                // Check for "/embed/VIDEOID"
                // PathSegments: [ "embed", "VIDEOID", ... ]
                if (pathSegments.Length >= 2 &&
                    pathSegments[0].Equals("embed", StringComparison.OrdinalIgnoreCase))
                {
                    return pathSegments[1];
                }

                // Check for "/shorts/VIDEOID"
                // PathSegments: [ "shorts", "VIDEOID", ... ]
                if (pathSegments.Length >= 2 &&
                    pathSegments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase))
                {
                    return pathSegments[1];
                }
            }

            // No recognized pattern -> return null
            return null;
        }

        private static async void ClientOnOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Logger.LogStr($"TWITCH: Joined channel {e.Channel}");

            try
            {
                // Validate TwitchApi and AccessToken
                if (TwitchApi == null)
                {
                    Logger.LogStr("TWITCH: TwitchApi is not initialized.");
                }

                if (string.IsNullOrEmpty(Settings.Settings.TwitchAccessToken))
                {
                    Logger.LogStr("TWITCH: TwitchAccessToken is null or empty.");
                }

                // Ensure e.Channel is valid
                if (string.IsNullOrEmpty(e.Channel))
                {
                    throw new ArgumentException(@"Channel name is null or empty.", nameof(e.Channel));
                }


                // Corrected array creation for e.Channel
                GetUsersResponse channels = await TwitchApi.Helix.Users.GetUsersAsync(
                    null, [e.Channel],
                    Settings.Settings.TwitchAccessToken);

                if (channels.Users is not { Length: > 0 }) return;

                _joinedChannelId = channels.Users[0].Id;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Logger.LogStr($"Error joining channel: {exception.Message}");
            }
        }

        private static void ClientOnOnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            Logger.LogStr($"TWITCH: Left channel {e.Channel}");
        }

        private static void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Resets the cooldown for the !ssr command
            _onCooldown = false;
            CooldownStopwatch.Stop();
            CooldownStopwatch.Reset();
            CooldownTimer.Stop();
        }

        private static string CreateCooldownResponse(ChatMessage e)
        {
            string response = Settings.Settings.BotRespCooldown;
            response = response.Replace("{user}", e.DisplayName);
            response = response.Replace("{artist}", "");
            response = response.Replace("{title}", "");
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{errormsg}", "");
            int time = (int)((CooldownTimer.Interval / 1000) - CooldownStopwatch.Elapsed.TotalSeconds);
            response = response.Replace("{cd}", time.ToString());
            return response;
        }

        private static string CreateNoTrackFoundResponse(ChatMessage e)
        {
            string response = Settings.Settings.BotRespNoTrackFound;
            response = response.Replace("{user}", e.DisplayName);
            response = response.Replace("{artist}", "");
            response = response.Replace("{title}", "");
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count}");
            response = response.Replace("{errormsg}", "");
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

        private static string CreateResponse(PlaceholderContext context, string template)
        {
            // Use reflection to get all properties of PlaceholderContext
            PropertyInfo[] properties = typeof(PlaceholderContext).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string placeholder = $"{{{property.Name.ToLower()}}}"; // Placeholder format, e.g., "{user}"
                string value = property.GetValue(context)?.ToString() ?? string.Empty; // Get property value or empty string if null
                template = template.Replace(placeholder, value); // Replace placeholder in the template with value
            }

            if (template.Contains("{{") && template.Contains("}}"))
            {
                if (string.IsNullOrEmpty(context.Req))
                {
                    int start = template.IndexOf("{{", StringComparison.Ordinal);
                    int end = template.IndexOf("}}", start, StringComparison.Ordinal) + 2;

                    if (start >= 0 && end >= start)
                    {
                        template = template.Remove(start, end - start);
                    }
                }
                else
                {
                    template = template.Replace("{{", "").Replace("}}", "");
                }
            }

            template = CleanFormatString(template);
            return template;
        }

        private static string CreateSuccessResponse(FullTrack track, string displayName, string response)
        {
            string artists = "";
            string singleArtist = "";

            //Fix for russia where Spotify is not available
            if (track.HasError() && track.Error.Status == 403)
                return Resources.s_TrackAdded;

            try
            {
                artists = string.Join(", ", track.Artists.Select(o => o.Name).ToList());
                singleArtist = track.Artists.FirstOrDefault()?.Name;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                IoManager.WriteOutput($"{GlobalObjects.RootDirectory}/dev_log.txt", Json.Serialize(track));
            }

            response = response.Replace("{user}", displayName);
            response = response.Replace("{artist}", artists);
            response = response.Replace("{single_artist}", singleArtist);
            response = response.Replace("{title}", track.Name);
            response = response.Replace("{maxreq}", "");
            response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count}");
            response = response.Replace("{pos}", $"{GlobalObjects.ReqList.Count}");
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
            string currentSong = Settings.Settings.Commands.First(c => c.CommandType == CommandType.Song).Response;

            currentSong = currentSong.Format(
                            singleArtist => GlobalObjects.CurrentSong.FullArtists != null ? GlobalObjects.CurrentSong.FullArtists.FirstOrDefault().Name : GlobalObjects.CurrentSong.Artists,
                            artist => GlobalObjects.CurrentSong.Artists,
                            title => GlobalObjects.CurrentSong.Title,
                            extra => "",
                            uri => GlobalObjects.CurrentSong.SongId,
                            url => GlobalObjects.CurrentSong.Url
                    ).Format();
            currentSong = Regex.Replace(currentSong, @"@?\{user\}", "");

            currentSong = currentSong.Trim();

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

        private static async Task<string> GetFullSpotifyUrl(string input)
        {
            using HttpClient httpClient = new();
            HttpRequestMessage request = new(HttpMethod.Get, input);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            return response.RequestMessage.RequestUri != null ? response.RequestMessage.RequestUri.AbsoluteUri : "";
        }

        private static async Task<Tuple<bool?, ChannelFollower>> GetIsUserFollowing(string chatMessageUserId)
        {
            // Using the Twitch API to check if the user is following the channel
            try
            {
                GetChannelFollowersResponse resp = await TwitchApi.Helix.Channels.GetChannelFollowersAsync(
                    _joinedChannelId,
                    chatMessageUserId,
                    20,
                    null,
                    Settings.Settings.TwitchAccessToken);
                return new Tuple<bool?, ChannelFollower>(resp.Data.Length > 0, resp.Data.FirstOrDefault());
            }
            catch (Exception)
            {
                return new Tuple<bool?, ChannelFollower>(null, new ChannelFollower());
            }
        }

        private static int GetMaxRequestsForUserLevel(int userLevel)
        {
            return (TwitchUserLevels)userLevel switch
            {
                TwitchUserLevels.Viewer => Settings.Settings.TwSrMaxReqEveryone,
                TwitchUserLevels.Vip => Settings.Settings.TwSrMaxReqVip,
                TwitchUserLevels.Subscriber => Settings.Settings.TwSrMaxReqSubscriber,
                TwitchUserLevels.Moderator => Settings.Settings.TwSrMaxReqModerator,
                TwitchUserLevels.Broadcaster => 999,
                _ => 0
            };
        }

        private static string GetNextSong()
        {
            int index = 0;
            switch (GlobalObjects.ReqList.Count)
            {
                case 0:
                    {
                        // Get the first song from the Spotify queue
                        RequestObject song = GlobalObjects.QueueTracks.First();
                        return song != null ? $"{song.Artist} - {song.Title}" : "There is no song next up.";
                    }
                case > 0 when GlobalObjects.ReqList[0].Trackid == GlobalObjects.CurrentSong.SongId:
                    {
                        if (GlobalObjects.ReqList.Count <= 1)
                        {
                            return "There is no song next up.";
                        }

                        index = 1;
                        break;
                    }
            }

            return $"{GlobalObjects.ReqList[index].Artist} - {GlobalObjects.ReqList[index].Title} requested by @{GlobalObjects.ReqList[index].Requester}";
        }

        private static List<QueueItem> GetQueueItems(string requester = null)
        {
            // Checks if the song ID is already in the internal queue (Mainwindow reqList)
            List<QueueItem> temp3 = [];
            string currsong = "";
            List<RequestObject> temp = [.. GlobalObjects.ReqList];

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
            const int startIndex = 9;
            int endIndex = response.IndexOf("]", startIndex, StringComparison.Ordinal);

            string colorName = response.Substring(startIndex, endIndex - startIndex).ToLower().Trim();

            AnnouncementColors colors = colorName switch
            {
                "green" => AnnouncementColors.Green,
                "orange" => AnnouncementColors.Orange,
                "blue" => AnnouncementColors.Blue,
                "purple" => AnnouncementColors.Purple,
                "primary" => AnnouncementColors.Primary,
                _ => AnnouncementColors.Purple
            };

            response = response.Replace($"[announce {colorName}]", string.Empty).Trim();
            return new Tuple<string, AnnouncementColors>(item1: response, item2: colors);
        }

        public static async Task<string> GetTrackIdFromInput(string input)
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

            // If the input is a Spotify episode, return an empty string
            if (input.StartsWith("https://open.spotify.com/episode/") || input.Contains("/episode/"))
            {
                return "episode";
            }

            // If the input is a Spotify episode, return an empty string
            if (input.StartsWith("https://open.spotify.com/artist/") || input.Contains("/artist/"))
            {
                return "artist";
            }

            // If the input is a Spotify episode, return an empty string
            if (input.StartsWith("https://open.spotify.com/album/") || input.Contains("/album/"))
            {
                return "album";
            }

            // If the input is a Spotify episode, return an empty string
            if (input.StartsWith("https://open.spotify.com/playlist/") || input.Contains("/playlist/"))
            {
                return "playlist";
            }

            // If the input is a Spotify episode, return an empty string
            if (input.StartsWith("https://open.spotify.com/audiobook/") || input.Contains("/audiobook/"))
            {
                return "audiobook";
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
            //SearchItem searchItem = SpotifyApiHandler.FindTrack(HttpUtility.UrlEncode(input));
            SearchItem searchItem = SpotifyApiHandler.FindTrack(input);
            if (searchItem == null)
            {
                SendChatMessage(Settings.Settings.TwChannel, "An error occurred while searching for the track.");
                return "";
            }
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

        private static bool IsArtistBlacklisted(FullTrack track, ChatMessage e, out string response)
        {
            response = string.Empty;
            if (track?.Artists == null || track.Artists.Count == 0)
            {
                Logger.LogStr("ERROR: No artist was found on the track object.");
                return false;
            }

            try
            {
                foreach (string s in Settings.Settings.ArtistBlacklist.Where(s =>
                             Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
                {
                    response = Settings.Settings.BotRespBlacklist;
                    response = response.Replace("{user}", e.DisplayName);
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

        private static bool IsTrackAlreadyInQueue(FullTrack track, ChatMessage e, out string response)
        {
            response = string.Empty;
            try
            {
                if (IsInQueue(track.Id))
                {

                    Dictionary<string, string> parameters = new()
                    {
                        {"user", e.DisplayName},
                        {"song", $"{string.Join(", ", track.Artists.Select(a => a.Name).ToList())} - {track.Name}"},
                        {"artist", string.Join(", ", track.Artists.Select(a => a.Name).ToList())},
                        {"single_artist", track.Artists.First().Name},
                        {"title", track.Name},
                        {"maxreq", Settings.Settings.TwSrMaxReq.ToString()},
                        {"errormsg", ""}
                    };

                    response = ReplaceParameters(Settings.Settings.BotRespIsInQueue, parameters);
                    return true;

                    response = Settings.Settings.BotRespIsInQueue;
                    response = response.Replace("{user}", e.DisplayName);
                    response = response.Replace("{song}",
                        $"{string.Join(", ", track.Artists.Select(a => a.Name).ToList())} - {track.Name}");
                    response = response.Replace("{artist}", string.Join(", ", track.Artists.Select(a => a.Name).ToList()));
                    response = response.Replace("{single_artist}", track.Artists.First().Name);
                    response = response.Replace("{title}", track.Name);
                    response = response.Replace("{maxreq}", Settings.Settings.TwSrMaxReq.ToString());
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

        private static bool IsTrackExplicit(FullTrack track, ChatMessage e, out string response)
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
                response = response.Replace("{user}", e.DisplayName);
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

        private static bool IsTrackTooLong(FullTrack track, ChatMessage e, out string response)
        {
            response = string.Empty;

            try
            {
                if (track.DurationMs >= TimeSpan.FromMinutes(Settings.Settings.MaxSongLength).TotalMilliseconds)
                {
                    response = Settings.Settings.BotRespLength;
                    response = response.Replace("{user}", e.DisplayName);
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

        private static bool IsTrackUnavailable(FullTrack track, ChatMessage e, out string response)
        {
            response = string.Empty;
            try
            {
                if (track.IsPlayable == null || (bool)track.IsPlayable)
                {
                    return false;
                }

                response = Settings.Settings.BotRespUnavailable;
                response = response.Replace("{user}", e.DisplayName);
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

        private static bool IsUserAtMaxRequests(ChatMessage e, TwitchUser user, out string response)
        {
            response = string.Empty;

            try
            {
                // Check if the maximum queue items have been reached for the user level
                if (MaxQueueItems(e.DisplayName, user.UserLevels.Max()))
                {
                    response = Settings.Settings.BotRespMaxReq;
                    response = response.Replace("{user}", e.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}", $"{GetMaxRequestsForUserLevel(user.UserLevels.Max())}");
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

        public static string ReplaceParameters(string source, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(source) || parameters == null || parameters.Count == 0)
            {
                return source;
            }

            return parameters.Aggregate(source, (current, parameter) => current.Replace($"{{{parameter.Key}}}", parameter.Value));
        }

        private static bool MaxQueueItems(string requester, int userLevel)
        {
            // Checks if the requester already reached max songrequests
            List<RequestObject> temp = GlobalObjects.ReqList.Where(x => x.Requester == requester).ToList();

            int maxreq = (TwitchUserLevels)userLevel switch
            {
                TwitchUserLevels.Broadcaster => 999,
                TwitchUserLevels.Viewer => Settings.Settings.TwSrMaxReqEveryone,
                TwitchUserLevels.Follower => Settings.Settings.TwSrMaxReqFollower,
                TwitchUserLevels.Moderator => Settings.Settings.TwSrMaxReqModerator,
                TwitchUserLevels.Subscriber => Settings.Settings.TwSrMaxReqSubscriber,
                TwitchUserLevels.SubscriberT2 => Settings.Settings.TwSrMaxReqSubscriberT2,
                TwitchUserLevels.SubscriberT3 => Settings.Settings.TwSrMaxReqSubscriberT3,
                TwitchUserLevels.Vip => Settings.Settings.TwSrMaxReqVip,
                _ => throw new ArgumentOutOfRangeException()
            };

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
            Logger.LogStr("TWITCH PUBSUB: Connected");
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
            if (Client is not { IsConnected: true })
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
                List<int> userlevel = GlobalObjects.TwitchUsers.First(o => o.UserId == redeemedUser.Id).UserLevels;
                Logger.LogStr(
                    $"{redeemedUser.DisplayName}s userlevel = {userlevel} ({Enum.GetName(typeof(TwitchUserLevels), userlevel)})");
                string msg;
                if (!userlevel.Contains(Settings.Settings.TwSrUserLevel))
                {
                    msg =
                        $"Sorry, only {Enum.GetName(typeof(TwitchUserLevels), Settings.Settings.TwSrUserLevel)} or higher can request songs.";
                    //Send a Message to the user, that his Userlevel is too low
                    if (Settings.Settings.RefundConditons.Any(i => i == 0) && isManagable)
                    {
                        UpdateRedemptionStatusResponse updateRedemptionStatus =
                            await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                                Settings.Settings.TwitchUser.Id, reward.Id,
                                [e.RewardRedeemed.Redemption.Id],
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
                                [e.RewardRedeemed.Redemption.Id],
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
                if (!Settings.Settings.TwSrUnlimitedSr && MaxQueueItems(redeemedUser.DisplayName, userlevel.Max()))
                {
                    // if the user reached max requests in the queue skip and inform requester
                    string response = Settings.Settings.BotRespMaxReq;
                    response = response.Replace("{user}", redeemedUser.DisplayName);
                    response = response.Replace("{artist}", "");
                    response = response.Replace("{title}", "");
                    response = response.Replace("{maxreq}",
                        $"{(TwitchUserLevels)userlevel.Max()} {GetMaxRequestsForUserLevel(userlevel.Max())}");
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
                                [e.RewardRedeemed.Redemption.Id],
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
                                    [e.RewardRedeemed.Redemption.Id],
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
                                    [e.RewardRedeemed.Redemption.Id],
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

            if (Settings.Settings.TwRewardSkipId.Any(id => id == reward.Id))
            {
                if (_skipCooldown)
                    return;
                await SpotifyApiHandler.SkipSong();

                SendChatMessage(Settings.Settings.TwChannel, "Skipping current song...");
                _skipCooldown = true;
                SkipCooldownTimer.Start();
            }
        }

        public static async Task RunTwitchUserSync()
        {
            if (TwitchApi == null) return;
            try
            {
                // Fetch all chatters and subscribers
                GlobalObjects.chatters = await TwitchApiHelper.GetAllChattersAsync();
                GlobalObjects.subscribers = await TwitchApiHelper.GetAllSubscribersAsync();
                GlobalObjects.moderators = await TwitchApiHelper.GetAllModeratorsAsync();
                GlobalObjects.vips = await TwitchApiHelper.GetAllVipsAsync();

                if (GlobalObjects.chatters == null || GlobalObjects.subscribers == null)
                    return;

                foreach (Chatter chatter in GlobalObjects.chatters)
                {
                    List<int> userLevels =
                    [
                        (int)TwitchUserLevels.Viewer
                    ];
                    // Check if the user is a moderator
                    bool isModerator = GlobalObjects.moderators.Any(m => m.UserId == chatter.UserId);
                    if (isModerator)
                    {
                        userLevels.Add((int)TwitchUserLevels.Moderator);
                    }
                    bool isVip = GlobalObjects.vips.Any(v => v.UserId == chatter.UserId);
                    if (isVip)
                    {
                        userLevels.Add((int)TwitchUserLevels.Vip);
                    }
                    Subscription subsc = GlobalObjects.subscribers.FirstOrDefault(subs => subs.UserId == chatter.UserId);
                    int subtier = 0;
                    if (subsc != null)
                    {
                        userLevels.Add((int)TwitchUserLevels.Subscriber);
                        subtier = int.Parse(subsc.Tier) / 1000;
                        switch (subsc.Tier)
                        {
                            case "2000":
                                userLevels.Add((int)TwitchUserLevels.SubscriberT2);
                                subtier = int.Parse(subsc.Tier) / 1000;
                                break;

                            case "3000":
                                userLevels.Add((int)TwitchUserLevels.SubscriberT3);
                                subtier = int.Parse(subsc.Tier) / 1000;
                                break;
                        }
                    }

                    if (chatter.UserId == Settings.Settings.TwitchUser.Id)
                        userLevels.Add((int)TwitchUserLevels.Broadcaster);

                    // Get follow status
                    (bool? isFollowing, ChannelFollower followInfo) = await GetIsUserFollowing(chatter.UserId);

                    if (isFollowing != null && (bool)isFollowing)
                    {
                        userLevels.Add((int)TwitchUserLevels.Follower);
                    }

                    // Check if the user exists in the global list
                    TwitchUser existingUser = GlobalObjects.TwitchUsers.FirstOrDefault(c => c.UserId == chatter.UserId);

                    if (existingUser != null)
                    {
                        existingUser.Update(
                            chatter.UserLogin,
                            chatter.UserName,
                            existingUser.UserLevels != userLevels ? userLevels : existingUser.UserLevels,
                            isFollowing != null && (bool)isFollowing,
                            subtier,
                            IsUserBlocked(chatter.UserName),
                            followInfo
                        );
                    }
                    else
                    {
                        GlobalObjects.TwitchUsers.Add(new TwitchUser
                        {
                            DisplayName = chatter.UserName,
                            UserId = chatter.UserId,
                            SubTier = subtier,
                            UserName = chatter.UserLogin,
                            IsFollowing = isFollowing,
                            FollowInformation = followInfo,
                            IsSrBlocked = IsUserBlocked(chatter.UserName),
                            UserLevels = userLevels
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions here
                Console.WriteLine(@$"Error in TwitchUserSyncTimer_Tick: {ex.Message}");
            }
        }

        private static void SendChatMessage(string channel, string message)
        {
            if (Client.IsConnected && Client.JoinedChannels.Any(c => c.Channel == channel))
                Client.SendMessage(channel, message);
            else
                Logger.LogStr("DEBUG: Client.IsConnected returned FALSE or Client.JoinedChannels is NULL");
        }

        private static async Task<int?> SetSpotifyVolume(ChatMessage e)
        {
            string[] split = e.Message.Split(' ');
            if (split.Length <= 1) return null;
            if (!int.TryParse(split[1], out int volume)) return null;
            int vol = MathUtils.Clamp(volume, 0, 100);
            ErrorResponse response = await SpotifyApiHandler.Spotify.SetVolumeAsync(vol);
            if (response.HasError())
                return null;
            return vol;

        }

        private static void SkipCooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _skipCooldown = false;
            SkipCooldownTimer.Stop();
        }

        private static void StartCooldown()
        {
            // starts the cooldown on the command
            int interval = Settings.Settings.TwSrCooldown > 0 ? Settings.Settings.TwSrCooldown : 0;
            if (interval == 0)
            {
                _onCooldown = false;
                return;
            }

            _onCooldown = true;
            CooldownTimer.Interval = TimeSpan.FromSeconds(interval).TotalMilliseconds;
            CooldownTimer.Start();
            CooldownStopwatch.Reset();
            CooldownStopwatch.Start();
        }

        private static async void TwitchUserSyncTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await RunTwitchUserSync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"Error in TwitchUserSyncTimer_Tick: {ex.Message}");
            }
        }

        private static async Task UploadToQueue(RequestObject track)
        {
            try
            {
                // upload to the queue
                //WebHelper.UpdateWebQueue(track.Id, artists, track.Name, length, displayName, "0", "i");

                dynamic payload = new
                {
                    uuid = Settings.Settings.Uuid,
                    key = Settings.Settings.AccessKey,
                    queueItem = track
                };

                await WebHelper.QueueRequest(WebHelper.RequestMethod.Post, Json.Serialize(payload));
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static void PrintObjectProperties(object obj)
        {
            if (obj == null)
            {
                Logger.LogStr($"DEBUG: Object is null");
                return;
            }

            Type type = obj.GetType();
            Logger.LogStr($"DEBUG: Object Type: {type.Name}");

            foreach (PropertyInfo prop in type.GetProperties())
            {
                object value = prop.GetValue(obj, null);
                Logger.LogStr($"DEBUG: {prop.Name}: {value}");
            }
        }
    }

    public class TwitchUser : INotifyPropertyChanged
    {
        private string _displayName;
        private ChannelFollower _followInformation;
        private bool? _isFollowing = null;
        private bool _isSrBlocked;
        private DateTime? _lastCommandTime = null;
        private int _subTier;
        private string _userId;
        private List<int> _userLevel;
        private string _userName;

        // Public event required by INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public ChannelFollower FollowInformation
        {
            get => _followInformation;
            set
            {
                if (_followInformation != value)
                {
                    _followInformation = value;
                    OnPropertyChanged(nameof(FollowInformation));
                }
            }
        }

        public bool? IsFollowing
        {
            get => _isFollowing;
            set
            {
                if (_isFollowing != value)
                {
                    _isFollowing = value;
                    OnPropertyChanged(nameof(IsFollowing));
                }
            }
        }

        public bool IsSrAllowed
        {
            get => !IsSrBlocked;  // Invert the existing property
            set => IsSrBlocked = !value;
        }

        public bool IsSrBlocked
        {
            get => _isSrBlocked;
            set
            {
                if (_isSrBlocked != value)
                {
                    _isSrBlocked = value;
                    OnPropertyChanged(nameof(IsSrBlocked));
                }
            }
        }

        public DateTime? LastCommandTime
        {
            get => _lastCommandTime;
            set
            {
                if (_lastCommandTime != value)
                {
                    _lastCommandTime = value;
                    OnPropertyChanged(nameof(LastCommandTime));
                }
            }
        }

        public string ReadableUserLevel => ((TwitchUserLevels)UserLevels.Max()).ToString();

        public int SubTier
        {
            get => _subTier;
            set
            {
                if (_subTier != value)
                {
                    _subTier = value;
                    OnPropertyChanged(nameof(SubTier));
                }
            }
        }

        // For convenience in C# 5+:
        //   protected void OnPropertyChanged([CallerMemberName] string propName = null) =>
        //       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        public string UserId
        {
            get => _userId;
            set
            {
                if (_userId != value)
                {
                    _userId = value;
                    OnPropertyChanged(nameof(UserId));
                }
            }
        }

        public List<int> UserLevels
        {
            get => _userLevel;
            set
            {
                if (_userLevel != value)
                {
                    _userLevel = value;
                    OnPropertyChanged(nameof(UserLevels));
                    // Also raise on "ReadableUserLevel" since it depends on UserLevel
                    OnPropertyChanged(nameof(ReadableUserLevel));
                }
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        public bool IsCooldownExpired(TimeSpan cooldown)
        {
            if (LastCommandTime == null)
                return true;

            return DateTime.Now - LastCommandTime.Value > cooldown;
        }

        public int HighestUserLevel => (UserLevels != null && UserLevels.Any()) ? UserLevels.Max() : 0;

        public string AllUserLevels => string.Join(", ", UserLevels.OrderBy(i => i));

        public void Update(
                    string username,
                    string displayname,
                    List<int> userlevel,
                    bool isFollowing,
                    int subTier = 0,
                    bool isSrBlocked = false,
                    ChannelFollower channelFollower = null)
        {
            // As you set each property, OnPropertyChanged will be raised:
            UserName = username;
            DisplayName = displayname;
            UserLevels = userlevel;
            IsFollowing = isFollowing;
            SubTier = subTier;
            IsSrBlocked = isSrBlocked;
            FollowInformation = channelFollower;
        }

        public void UpdateCommandTime(bool reset = false)
        {
            LastCommandTime = reset ? null : DateTime.Now;
        }

        // Helper method to raise the event
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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