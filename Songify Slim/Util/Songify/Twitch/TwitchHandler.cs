using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Uwp.Notifications;
using Songify_Slim.Models;
using Songify_Slim.Models.Pear;
using Songify_Slim.Models.Placeholders;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Properties;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Pear;
using Songify_Slim.Util.Songify.TwitchOAuth;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Youtube.YTMYHCH.YtmDesktopApi;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using Swan;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Security.Authentication.OnlineId;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateRedemptionStatus;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Channels.SendChatMessage;
using TwitchLib.Api.Helix.Models.Chat;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets.Extensions;
using Windows.UI.Text.Core;
using TwitchLib.EventSub.Core.Models.Chat;
using Scopes = Songify_Slim.Util.Songify.TwitchOAuth.Scopes;
using Song = Songify_Slim.Util.Youtube.YTMYHCH.Song;
using Timer = System.Timers.Timer;

namespace Songify_Slim.Util.Songify.Twitch;

// This class handles everything regarding twitch.tv
public static class TwitchHandler
{
    private static bool _onCooldown;
    private static bool _skipCooldown;
    private static bool _syncTimerHooked;
    private static bool _toastSent;
    private static CancellationTokenSource _cts;
    private static IHost _host;
    private static int _syncRunning;
    private static readonly DispatcherTimer TwitchUserSyncTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private static readonly List<string> SkipVotes = [];
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static readonly Stopwatch CooldownStopwatch = new();
    private static readonly Timer CooldownTimer = new() { Interval = TimeSpan.FromSeconds(Settings.TwSrCooldown < 1 ? 0 : Settings.TwSrCooldown).TotalMilliseconds };
    private static readonly Timer SkipCooldownTimer = new() { Interval = TimeSpan.FromSeconds(5).TotalMilliseconds };
    private static string _currentState;
    private static string _userId;
    private static TwitchAPI _twitchApiBot;
    public const string ClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";
    public static bool ForceDisconnect;
    public static TwitchAPI TwitchApi;
    public static ValidateAccessTokenResponse BotTokenCheck;
    public static ValidateAccessTokenResponse TokenCheck;

    public static async void AddSong(string trackId, TwitchRequestUser e, Enums.SongRequestSource source, TwitchUser user, RewardInfo reward = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                await SendChatMessage("No song found.");
                await CheckAndRefund(source, reward, Enums.RefundCondition.NoSongFound, e);
                return;
            }

            switch (trackId)
            {
                case "shortened":
                case "episode":
                case "artist":
                case "album":
                case "playlist":
                case "audiobook":
                    string message = trackId switch
                    {
                        "shortened" => "Spotify short links are not supported. Please type in the full title or get the Spotify URI (starts with \"spotify:track:\")",
                        "episode" => "Episodes are not supported. Please specify a track!",
                        "artist" => "Artist links are not supported. Please specify a track!",
                        "album" => "Album links are not supported. Please specify a track!",
                        "playlist" => "Playlist links are not supported. Please specify a track!",
                        "audiobook" => "Audiobook links are not supported. Please specify a track!",
                        _ => "Unsupported track type."
                    };

                    await SendChatMessage(message);
                    await CheckAndRefund(source, reward, Enums.RefundCondition.NoSongFound, e);
                    return;
            }

            FullTrack track = await SpotifyApiHandler.GetTrack(trackId);

            if (Settings.LimitSrToPlaylist &&
                !string.IsNullOrEmpty(Settings.SpotifySongLimitPlaylist))
            {
                Tuple<bool, string> result = await IsInAllowedPlaylist(trackId);
                if (!result.Item1)
                {
                    await SendChatMessage(result.Item2);
                    return;
                }
            }

            (bool isBlacklisted, string response) = await IsSongBlacklisted(trackId);
            if (isBlacklisted)
            {
                response = ReplaceParameters(response, new Dictionary<string, string>
                {
                    { "user", e.DisplayName },
                    { "req", "" },
                    { "artist}", string.Join(", ", track.Artists.Select(a => a.Name).ToList()) },
                    { "single_artist", track.Artists.First().Name },
                    { "errormsg", "" },
                    { "maxlength", Settings.MaxSongLength.ToString() },
                    { "maxreq", "" },
                    { "song", $"{string.Join(", ", track.Artists.Select(a => a.Name).ToList())} - {track.Name}" },
                    { "playlist_name", "" },
                    { "playlist_url", "" },
                    { "votes", "" },
                    { "cd", "" },
                    { "url", "" },
                    { "queue", "" },
                    { "commands", "" },
                    { "userlevel", "" },
                    { "ttp", "" },
                });
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.SongBlocked, e);
                return;
            }

            if (track == null)
            {
                await SendChatMessage(CreateNoTrackFoundResponse(e));
                await CheckAndRefund(source, reward, Enums.RefundCondition.NoSongFound, e);
                return;
            }

            if (IsTrackExplicit(track, e, out response))
            {
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.TrackIsEplicit, e);
                return;
            }

            if (IsTrackUnavailable(track, e, out response))
            {
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.SongUnavailable, e);
                return;
            }

            if (IsArtistBlacklisted(track, e, out response))
            {
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.ArtistBlocked, e);
                return;
            }

            if (IsTrackTooLong(track, e, out response))
            {
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.SongTooLong, e);
                return;
            }

            if (IsTrackAlreadyInQueue(track, e, out response))
            {
                await SendChatMessage(response);
                await CheckAndRefund(source, reward, Enums.RefundCondition.SongAlreadyInQueue, e);
                return;
            }

            bool unlimitedSr = false;
            if (user.UserLevels is { Count: > 0 } && user.UserLevels.All(i => i != 7))
            {
                switch (source)
                {
                    case Enums.SongRequestSource.Reward:
                        if (Settings.UnlimitedSrUserlevelsReward != null &&
                            Settings.UnlimitedSrUserlevelsReward.Count > 0)
                        {
                            unlimitedSr = Settings.UnlimitedSrUserlevelsReward.Intersect(user.UserLevels).Any();
                        }
                        break;

                    case Enums.SongRequestSource.Command:
                        if (Settings.UnlimitedSrUserlevelsCommand != null &&
                            Settings.UnlimitedSrUserlevelsCommand.Count > 0)
                        {
                            unlimitedSr = Settings.UnlimitedSrUserlevelsCommand.Intersect(user.UserLevels).Any();
                        }
                        break;

                    case Enums.SongRequestSource.Websocket:
                    default:
                        //ignored
                        break;
                }
            }

            if (!unlimitedSr && !e.IsBroadcaster)
                if (IsUserAtMaxRequests(e, user, out response))
                {
                    await SendChatMessage(response);
                    await CheckAndRefund(source, reward, Enums.RefundCondition.QueueLimitReached, e);
                    return;
                }

            if (!Settings.AddSrtoPlaylistOnly)
                await SpotifyApiHandler.AddToQueue("spotify:track:" + trackId);

            if (Settings.AddSrToPlaylist)
                await SpotifyApiHandler.AddToPlaylist(track.Id);

            string successResponse = Settings.Commands.First(cmd => cmd.Name == "Song Request").Response;
            response = CreateSuccessResponse(track, e.DisplayName, successResponse);
            TwitchCommand cmd = Settings.Commands.First(cmd => cmd.Name == "Song Request");

            // this will take the first 4 artists and join their names with ", "
            string artists = string.Join(", ", track.Artists
                .Take(4)
                .Select(a => a.Name));

            string length = FormattedTime(track.DurationMs);

            // Get the Requester Twitch User Object from the api
            GetUsersResponse x = await TwitchApi.Helix.Users.GetUsersAsync([e.UserId], null, Settings.TwitchAccessToken);

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

            if (Settings.Commands.First(command => command.Name == "Song Request").Response.Contains("{ttp}"))
            {
                try
                {
                    if (GlobalObjects.QueueTracks.Count > 0)
                    {
                        string timeToPlay = await GetEstimatedTimeToPlay(track.Id);

                        response = response.Replace("{ttp}", timeToPlay);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            response = response.Replace("{ttp}", "");
            SendOrAnnounceMessage(response, cmd);
            await CheckAndRefund(source, reward, Enums.RefundCondition.AlwaysRefund, e);
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    public static async Task<string> AddSongFromWebsocket(string trackId, string requester = "")
    {
        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                {
                    string validationError = ValidateTrackId(trackId);
                    if (validationError != null)
                        return validationError;

                    (bool valid, FullTrack track, string message) = await TryGetValidTrack(trackId);
                    if (!valid)
                        return message;

                    await SpotifyApiHandler.AddToQueue("spotify:track:" + trackId);

                    if (Settings.AddSrToPlaylist)
                        await SpotifyApiHandler.AddToPlaylist(track.Id);

                    RequestObject o = BuildRequestObject(track, requester);
                    await UploadToQueue(o);

                    return $"{string.Join(", ", track.Artists.Take(2).Select(a => a.Name))} - {track.Name} has been added to the queue.";
                }

            case Enums.PlayerType.Pear:
                {
                    //string videoId = ExtractYouTubeVideoIdFromText(trackId);
                    string videoId = trackId;

                    if (GlobalObjects.ReqList.Any(r => r.Trackid == videoId))
                    {
                        return "That song is already in the queue ";
                    }

                    string title = await WebTitleFetcher.GetWebsiteTitleAsync($"https://www.youtube.com/watch?v={videoId}");
                    string thumbnail = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

                    RequestObject req = new()
                    {
                        Uuid = Settings.Uuid,
                        Trackid = videoId,
                        PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                        Artist = "",
                        Title = title,
                        Length = "",
                        Requester = requester,
                        Played = 0,
                        Albumcover = thumbnail
                    };
                    GlobalObjects.ReqList.Add(req);

                    bool ok = await PearApi.EnqueueAsync(req.Trackid, Enums.InsertPosition.InsertAfterCurrentVideo);
                    if (ok)
                    {
                        await WaitForSongInQueueAsync(videoId, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(150));
                        await EnsureOrderAsync();
                    }
                    else
                        return "That song is already in the queue ";

                    return "Song queued successfully.";
                }
            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
                return "This player type does not support song requests via the API. Please use Spotify Web or YTM Desktop.";

            default:
                return "No player selected. Go to Settings -> Player and select a player.";
        }
    }

    public static void ApiConnect(Enums.TwitchAccount account)
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
                case Enums.TwitchAccount.Main:
                    // Don't actually print the user token on screen or to the console.
                    // Here you should save it where the application can access it whenever it wants to, such as in appdata.
                    Settings.TwitchAccessToken = token;
                    break;

                case Enums.TwitchAccount.Bot:
                    // Don't actually print the user token on screen or to the console.
                    // Here you should save it where the application can access it whenever it wants to, such as in appdata.
                    Settings.TwitchBotToken = token;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(account), account, null);
            }
            await InitializeApi(account);
            if (string.IsNullOrEmpty(Settings.TwChannel))
                Settings.TwChannel = Settings.TwitchUser.Login;
            bool shownInSettings = false;
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(Window_Settings)) continue;
                    await ((Window_Settings)window).ShowMessageAsync(Resources.msgbx_BotAccount,
                        Resources.msgbx_UseAsBotAccount.Replace("{account}",
                            account == Enums.TwitchAccount.Main
                                ? Settings.TwitchUser.DisplayName
                                : Settings.TwitchBotUser.DisplayName),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Resources.msgbx_Yes,
                            NegativeButtonText = Resources.msgbx_No,
                            DefaultButtonFocus = MessageDialogResult.Affirmative
                        }).ContinueWith(x =>
                    {
                        if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                        Settings.TwOAuth =
                            $"oauth:{(account == Enums.TwitchAccount.Main ? Settings.TwitchAccessToken : Settings.TwitchBotToken)}";
                        Settings.TwAcc = account == Enums.TwitchAccount.Main
                            ? Settings.TwitchUser.Login
                            : Settings.TwitchBotUser.Login;

                        Settings.TwitchChatAccount = new TwitchChatAccount
                        {
                            Id = Settings.TwitchUser.Id,
                            Name = Settings.TwitchUser.Login,
                            Token = Settings.TwOAuth
                        };

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
                            account == Enums.TwitchAccount.Main
                                ? Settings.TwitchUser.DisplayName
                                : Settings.TwitchBotUser.DisplayName),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Resources.msgbx_Yes,
                            NegativeButtonText = Resources.msgbx_No,
                            DefaultButtonFocus = MessageDialogResult.Affirmative
                        }).ContinueWith(x =>
                    {
                        if (x.Result != MessageDialogResult.Affirmative) return Task.CompletedTask;
                        Settings.TwOAuth =
                            $"oauth:{(account == Enums.TwitchAccount.Main ? Settings.TwitchAccessToken : Settings.TwitchBotToken)}";
                        Settings.TwAcc = account == Enums.TwitchAccount.Main
                            ? Settings.TwitchUser.Login
                            : Settings.TwitchBotUser.Login;

                        Settings.TwitchChatAccount = new TwitchChatAccount
                        {
                            Id = Settings.TwitchUser.Id,
                            Name = Settings.TwitchUser.Login,
                            Token = Settings.TwOAuth
                        };

                        return Task.CompletedTask;
                    });
                }
                ForceDisconnect = true;
                ConnectTwitchChatClient();
                dynamic telemetryPayload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey,
                    tst = DateTime.Now.ToUnixEpochDate(),
                    twitch_id = Settings.TwitchUser == null ? "" : Settings.TwitchUser.Id,
                    twitch_name = Settings.TwitchUser == null
                        ? ""
                        : Settings.TwitchUser.DisplayName,
                    vs = GlobalObjects.AppVersion,
                    playertype = GlobalObjects.GetReadablePlayer(),
                };
                string json = Json.Serialize(telemetryPayload);
                await SongifyApi.PostTelemetryAsync(json);
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
            if (TwitchApi == null) return false;
            if (Settings.TwitchUser == null) return false;
            if (string.IsNullOrEmpty(Settings.TwitchAccessToken)) return false;
            GetStreamsResponse x = await TwitchApi.Helix.Streams.GetStreamsAsync(null, 20, null, null,
                [Settings.TwitchUser.Id], null, Settings.TwitchAccessToken);
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

    public static void ConnectTwitchChatClient()
    {
        try
        {
            // Pick chat account safely
            TwitchChatAccount acct;

            if (Settings.TwitchUser != null && Settings.TwAcc == Settings.TwitchUser.Login)
            {
                acct = new TwitchChatAccount
                {
                    Id = Settings.TwitchUser.Id,
                    Name = Settings.TwitchUser.Login,
                    Token = Settings.TwOAuth
                };
            }
            else if (Settings.TwitchBotUser != null)
            {
                acct = new TwitchChatAccount
                {
                    Id = Settings.TwitchBotUser.Id,
                    Name = Settings.TwitchBotUser.Login,
                    Token = Settings.TwOAuth
                };
            }
            else
            {
                // neither matches / is present
                Logger.Warning(LogSource.Twitch, "No valid chat account (user/bot) available.");
                return;
            }

            Settings.TwitchChatAccount = acct;

            // Validate required fields BEFORE using them
            if (string.IsNullOrWhiteSpace(Settings.TwAcc) ||
                string.IsNullOrWhiteSpace(Settings.TwOAuth) ||
                string.IsNullOrWhiteSpace(Settings.TwChannel))
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow mw)
                            mw.LblStatus.Content = "Please fill in Twitch credentials.";
                    }
                });
                return;
            }

            // Ensure timers exist before wiring events
            if (CooldownTimer != null) CooldownTimer.Elapsed += CooldownTimer_Elapsed;
            if (SkipCooldownTimer != null) SkipCooldownTimer.Elapsed += SkipCooldownTimer_Elapsed;

            // Ensure commands list isn’t null
            InitializeCommands(Settings.Commands);
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch, $"Couldn't connect to Twitch.", ex);
        }
    }

    public static void DisposeTwitchUserSyncTimer()
    {
        if (!_syncTimerHooked) return;
        TwitchUserSyncTimer.Stop();
        TwitchUserSyncTimer.Tick -= TwitchUserSyncTimer_Tick; // <-- unsubscribe
        _syncTimerHooked = false;
    }

    public static async Task EnsureOrderAsync()
    {
        await Task.Delay(250); // tiny settle delay

        // queue snapshot (once!)
        List<Song> queue = await PearApi.GetQueueAsync();
        if (queue.Count == 0) return;

        int currentIdx = queue.FindIndex(q => q.IsCurrent);
        if (currentIdx < 0) currentIdx = 0;
        string currentId = queue[currentIdx].Id;

        // pending snapshot (distinct, exclude what's playing)
        List<RequestObject> pending = GlobalObjects.ReqList
            .Where(r => r.Played == 0 && r.Trackid != currentId)
            .GroupBy(r => r.Trackid)
            .Select(g => g.First())
            .ToList();

        // also make sure each pending item is still in the queue
        pending = pending.Where(p => queue.Any(q => q.Id == p.Trackid)).ToList();
        if (pending.Count == 0) return;

        // reorder using one local list; search only after current
        for (int i = 0; i < pending.Count; i++)
        {
            string id = pending[i].Trackid;
            int desired = currentIdx + 1 + i;

            int current = FindIndexAfter(queue, id, currentIdx);
            if (current == -1 || current == desired) continue;

            await PearApi.MoveQueueItemAsync(current, desired);

            MoveLocal(queue, current, desired);
        }

        await GlobalObjects.QueueUpdateQueueWindow();
    }

    public static async void ExecuteChatCommand(ChannelChatMessage msg)
    {
        try
        {
            if (GlobalObjects.TwitchUsers.Count == 0)
            {
                await RunTwitchUserSync();
            }

            TwitchUser existingUser = GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == msg.ChatterUserId);
            List<int> userLevels = await GetUserLevels(msg.ChatterUserId, msg.BroadcasterUserId, msg.IsModerator, msg.IsVip, msg.IsSubscriber, msg.IsBroadcaster);
            int subtier = int.Parse(GlobalObjects.Subscribers.Where(s => s.UserId == msg.ChatterUserId)
                // Helix tier is a string like "1000"/"2000"/"3000"
                .OrderByDescending(s => int.Parse(s.Tier))
                .FirstOrDefault()?.Tier ?? "0") / 1000;

            if (existingUser == null)
            {
                Tuple<bool?, ChannelFollower> isUserFollowing = await GetIsUserFollowing(msg.ChatterUserId, Settings.TwitchUser.Id);
                existingUser = new TwitchUser
                {
                    DisplayName = msg.ChatterUserName,
                    FollowInformation = isUserFollowing.Item1 == true ? isUserFollowing.Item2 : null,
                    IsFollowing = isUserFollowing.Item1,
                    IsSrBlocked = IsUserBlocked(msg.ChatterUserName),
                    LastCommandTime = null,
                    SubTier = int.Parse(GlobalObjects.Subscribers.Where(s => s.UserId == msg.ChatterUserId)
                        // Helix tier is a string like "1000"/"2000"/"3000"
                        .OrderByDescending(s => int.Parse(s.Tier))
                        .FirstOrDefault()?.Tier ?? "0") / 1000,
                    UserId = msg.ChatterUserId,
                    UserLevels = userLevels,
                    UserName = msg.ChatterUserLogin
                };
                Logger.Warning(LogSource.Twitch, $"User {msg.ChatterUserName} ({msg.ChatterUserId}) not found. Adding manually.");
                GlobalObjects.TwitchUsers.Add(existingUser);
            }
            else
            {
                existingUser.Update(msg.ChatterUserLogin, msg.ChatterUserName, userLevels, existingUser.IsFollowing != null && (bool)existingUser.IsFollowing, subtier);
            }

            bool executed = TwitchCommandHandler.TryExecuteCommand(msg, new TwitchCommandParams
            {
                Subtier = subtier,
                ExistingUser = existingUser,
                UserLevels = userLevels
            });

            if (executed)
                Logger.Info(LogSource.Twitch,
                    $"Command \"{msg.Message.Text.Split(' ')[0]}\" by {msg.ChatterUserName}: Executed successfully.");
            else
            {
                Logger.Warning(LogSource.Twitch,
                    $"Command \"{msg.Message.Text.Split(' ')[0]}\" by {msg.ChatterUserName}: Not executed (not found or disabled).");
            }
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
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
        string[] tokens = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        return tokens.Select(ExtractYouTubeVideoId).FirstOrDefault(videoId => !string.IsNullOrEmpty(videoId));
    }

    public static async Task<string> GetTrackIdFromInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

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
        FullTrack searchItem = await SpotifyApiHandler.FindTrack(input);
        if (searchItem == null)
        {
            await SendChatMessage("An error occurred while searching for the track.");
            return "";
        }

        if (searchItem.Restrictions is not { Count: > 0 }) return searchItem.Id;
        await SendChatMessage(string.Join(", ", searchItem.Restrictions.Select(kv => $"{kv.Key}: {kv.Value}")));
        return "";

        // if a track was found convert the object to FullTrack (easier use than searchItem)
    }

    public static async Task HandleBitsSongRequest(string userId, string userName,
        string userInput, string channel)
    {
        TwitchUser existingUser = GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == userId);

        int subtier = int.Parse(GlobalObjects.Subscribers.Where(s => s.UserId == userId)
              // Helix tier is a string like "1000"/"2000"/"3000"
              .OrderByDescending(s => int.Parse(s.Tier))
              .FirstOrDefault()?.Tier ?? "0") / 1000;

        if (existingUser == null)
        {
            Tuple<bool?, ChannelFollower> isUserFollowing = await GetIsUserFollowing(userId, Settings.TwitchUser.Id);
            existingUser = new TwitchUser
            {
                DisplayName = userName,
                FollowInformation = isUserFollowing.Item1 == true ? isUserFollowing.Item2 : null,
                IsFollowing = isUserFollowing.Item1,
                IsSrBlocked = IsUserBlocked(userName),
                LastCommandTime = null,
                SubTier = subtier,
                UserId = userId,
                UserLevels = null,
                UserName = userName
            };
            Logger.Warning(LogSource.Twitch, $"User {userName} ({userId}) not found. Added manually");
            GlobalObjects.TwitchUsers.Add(existingUser);
        }

        // Do nothing if the user is blocked, don't even reply
        if (IsUserBlocked(userName))
        {
            return;
        }

        if (SpotifyApiHandler.Client == null)
        {
            await SendChatMessage("It seems that Spotify is not connected right now.");
            return;
        }

        TwitchRequestUser user = new()
        {
            Channel = channel,
            DisplayName = userName,
            UserId = userId,
            Message = userInput,
            IsBroadcaster = false
        };

        AddSong(await GetTrackIdFromInput(userInput), user, Enums.SongRequestSource.Reward, existingUser);
    }

    public static async Task HandleChannelPointSongRequst(bool isBroadcaster, string userId, string userName,
        string userInput, string channel, string rewardId, string redemptionId)
    {
        if (GlobalObjects.TwitchUsers.Count == 0)
        {
            await RunTwitchUserSync();
        }

        TwitchUser existingUser = GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == userId);
        List<int> userLevels = await GetUserLevels(userId, Settings.TwitchUser.Id);
        int subtier = int.Parse(GlobalObjects.Subscribers.Where(s => s.UserId == userId)
            // Helix tier is a string like "1000"/"2000"/"3000"
            .OrderByDescending(s => int.Parse(s.Tier))
            .FirstOrDefault()?.Tier ?? "0") / 1000;

        if (existingUser == null)
        {
            Tuple<bool?, ChannelFollower> isUserFollowing = await GetIsUserFollowing(userId, Settings.TwitchUser.Id);
            existingUser = new TwitchUser
            {
                DisplayName = userName,
                FollowInformation = isUserFollowing.Item1 == true ? isUserFollowing.Item2 : null,
                IsFollowing = isUserFollowing.Item1,
                IsSrBlocked = IsUserBlocked(userName),
                LastCommandTime = null,
                SubTier = subtier,
                UserId = userId,
                UserLevels = userLevels,
                UserName = userName
            };
            Logger.Warning(LogSource.Twitch, $"User {userName} ({userId}) not found. Adding manually.");
            GlobalObjects.TwitchUsers.Add(existingUser);
        }
        // Check if the user level is lower than broadcaster or not allowed to request songs

        if (!IsUserAllowed(Settings.UserLevelsReward, new TwitchCommandParams
        {
            ExistingUser = existingUser,
            UserLevels = existingUser?.UserLevels
        }, isBroadcaster, null, userId))
        {
            // Send a message to the user that their user level is too low to request songs
            string response = Settings.BotRespUserlevelTooLowCommand;
            response = response.Replace("{user}", userName);

            string userLevelNames = Settings.UserLevelsReward.Any()
                ? string.Join(", ", Settings.UserLevelsReward
                    .Select(level => Enum.GetName(typeof(Enums.TwitchUserLevels), level)))
                : "None";

            response = response.Replace("{userlevel}", userLevelNames);

            // Send a message to the user that their user level is too low to request songs
            await SendChatMessage(response);

            if (Settings.RefundConditons.Any(c => c == Enums.RefundCondition.UserLevelTooLow))
            {
                await RefundChannelPoints(rewardId, redemptionId, new TwitchRequestUser(channel, userName, userId, userInput, isBroadcaster), Enums.RefundCondition.UserLevelTooLow);
            }
            return;
        }

        // Do nothing if the user is blocked, don't even reply
        if (IsUserBlocked(userName))
        {
            if (Settings.RefundConditons.Any(c => c == Enums.RefundCondition.UserBlocked))
            {
                await RefundChannelPoints(rewardId, redemptionId, new TwitchRequestUser(channel, userName, userId, userInput, isBroadcaster), Enums.RefundCondition.UserBlocked);
            }
            return;
        }

        TwitchRequestUser user = new()
        {
            Channel = channel,
            DisplayName = userName,
            UserId = userId,
            Message = userInput,
            IsBroadcaster = isBroadcaster
        };

        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                if (SpotifyApiHandler.Client == null)
                {
                    await SendChatMessage("It seems that Spotify is not connected right now.");
                    return;
                }

                AddSong(await GetTrackIdFromInput(userInput), user, Enums.SongRequestSource.Reward, existingUser, new RewardInfo
                {
                    RewardId = rewardId,
                    RedemptionId = redemptionId,
                    Channel = channel
                });
                break;

            case Enums.PlayerType.Pear:
                string videoId = ExtractYouTubeVideoIdFromText(userInput);

                if (string.IsNullOrEmpty(videoId))
                {
                    string messageWithoutTrigger = userInput;
                    PearSearch sr = await PearApi.SearchAsync(messageWithoutTrigger);
                    if (sr == null) return;

                    if (GlobalObjects.ReqList.All(r => r.Trackid != sr.VideoId))
                    {
                        RequestObject req = new()
                        {
                            Uuid = Settings.Uuid,
                            Trackid = sr.VideoId,
                            PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                            Artist = string.Join(", ", sr.Artists),
                            Title = sr.Title,
                            Length = sr.Duration,
                            Requester = userName,
                            Played = 0,
                            Albumcover = sr.ThumbnailUrl
                        };
                        GlobalObjects.ReqList.Add(req);

                        bool ok = await PearApi.EnqueueAsync(req.Trackid, Enums.InsertPosition.InsertAfterCurrentVideo);
                        if (ok)
                        {
                            // Your success response logic
                            await SendChatMessage($"Queued: {req.Artist} - {req.Title}");
                            // wait until YT queue actually contains the item
                            int? pos = await WaitForSongInQueueAsync(sr.VideoId,
                                TimeSpan.FromSeconds(3),
                                TimeSpan.FromMilliseconds(150));
                            if (pos == null)
                            {
                                // fallback: skip reorder now; try again later (timer/next enqueue)
                                return;
                            }
                            await EnsureOrderAsync();
                        }
                        else
                        {
                            await SendChatMessage("That song is already in the queue ");
                        }
                    }
                    else
                    {
                        await SendChatMessage("That song is already in the queue ");
                    }
                }
                else
                {
                    if (GlobalObjects.ReqList.Any(r => r.Trackid == videoId))
                    {
                        await SendChatMessage("That song is already in the queue ");
                        return;
                    }

                    string title = await WebTitleFetcher.GetWebsiteTitleAsync($"https://www.youtube.com/watch?v={videoId}");
                    string thumbnail = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

                    RequestObject req = new()
                    {
                        Uuid = Settings.Uuid,
                        Trackid = videoId,
                        PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                        Artist = "",
                        Title = title,
                        Length = "",
                        Requester = userName,
                        Played = 0,
                        Albumcover = thumbnail
                    };
                    GlobalObjects.ReqList.Add(req);

                    bool ok = await PearApi.EnqueueAsync(req.Trackid, Enums.InsertPosition.InsertAfterCurrentVideo);
                    if (ok)
                    {
                        await SendChatMessage($"Queued: {title}");
                        // wait until YT queue actually contains the item
                        int? pos = await WaitForSongInQueueAsync(videoId,
                            TimeSpan.FromSeconds(3),
                            TimeSpan.FromMilliseconds(150));
                        if (pos == null)
                        {
                            // fallback: skip reorder now; try again later (timer/next enqueue)
                            return;
                        }
                        await EnsureOrderAsync();
                    }
                    else
                        await SendChatMessage("That song is already in the queue ");
                }

                break;

            case Enums.PlayerType.WindowsPlayback:
            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
            default:
                await SendChatMessage("No player selected. Go to Settings -> Player and select a player.");
                return;
        }
    }

    public static async Task<bool> HandleSkipReward()
    {
        if (GlobalObjects.CurrentSong.IsSongrequest() && Settings.SkipOnlyNonSrSongs)
            return true;
        // Skip song
        if (_skipCooldown)
            return true;

        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                await SpotifyApiHandler.SkipSong();
                break;

            case Enums.PlayerType.Pear:
                await PearApi.SkipAsync();
                break;

            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
                break;

            case Enums.PlayerType.WindowsPlayback:
            default:
                throw new ArgumentOutOfRangeException();
        }

        await SendChatMessage("Skipping current song...");
        _skipCooldown = true;
        SkipCooldownTimer.Start();
        return false;
    }

    public static async Task InitializeApi(Enums.TwitchAccount twitchAccount)
    {
        GetUsersResponse users;
        User user;
        switch (twitchAccount)
        {
            #region Main

            case Enums.TwitchAccount.Main:
                TwitchApi = new TwitchAPI
                {
                    Settings =
                    {
                        ClientId = ClientId,
                        AccessToken = Settings.TwitchAccessToken
                    }
                };

                try
                {
                    TokenCheck = await TwitchApi.Auth.ValidateAccessTokenAsync(Settings.TwitchAccessToken);
                }
                catch (HttpRequestException ex)
                {
                    Logger.Warning(
                        LogSource.Twitch,
                        "HTTP error occurred during Twitch token validation.",
                        ex
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        LogSource.Twitch,
                        "Unexpected exception during Twitch token validation.",
                        ex
                    );
                }

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
                                PackIconBoxIconsKind.LogosTwitch;
                            ((MainWindow)window).mi_TwitchAPI.IsEnabled = false;
                            MessageDialogResult msgResult = await ((MainWindow)window).ShowMessageAsync(
                                "Twitch Account Issues",
                                "Your Twitch Account token has expired. Please login again with Twitch",
                                MessageDialogStyle.AffirmativeAndNegative,
                                new MetroDialogSettings
                                { AffirmativeButtonText = "Login (Main)", NegativeButtonText = "Cancel" });
                            if (msgResult == MessageDialogResult.Negative) return;
                            ApiConnect(Enums.TwitchAccount.Main);
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
                        { AffirmativeButtonText = "Login (Main)", NegativeButtonText = "Cancel" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        Settings.TwitchUser = null;
                        Settings.TwitchAccessToken = "";
                        TwitchApi = null;
                        ApiConnect(Enums.TwitchAccount.Main);
                        return;
                    }
                }

                Settings.TwitchAccessTokenExpiryDate = tokenExpiryDate;

                GlobalObjects.TwitchUserTokenExpired = false;
                _userId = TokenCheck.UserId;

                users = await TwitchApi.Helix.Users.GetUsersAsync([_userId], null,
                    Settings.TwitchAccessToken);

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
                        //((MainWindow)window).IconTwitchAPI.Kind = PackIconBoxIconsKind.LogosTwitch;
                        ((MainWindow)window).mi_TwitchAPI.IsEnabled = false;

                        Logger.Info(LogSource.Twitch, $"Logged into Twitch API ({user.DisplayName})");
                    }
                });

                Settings.TwitchUser = user;
                Settings.TwitchChannelId = user.Id;
                Settings.TwChannel = user.Login;

                // Get User Color:
                GetUserChatColorResponse chatColorResponse = await TwitchApi.Helix.Chat.GetUserChatColorAsync([user.Id], Settings.TwitchAccessToken);
                Settings.TwitchUserColor = chatColorResponse.Data.Any() ? chatColorResponse.Data[0].Color : "#f8953c";

                ConfigHandler.WriteAllConfig(Settings.Export());
                //StreamUpTimer.Tick += StreamUpTimer_Tick;
                //StreamUpTimer.Start();

                InitTwitchUserSyncTimer();
                await RunTwitchUserSync();

                break;

            #endregion Main

            #region Bot

            case Enums.TwitchAccount.Bot:
                _twitchApiBot = new TwitchAPI
                {
                    Settings =
                    {
                        ClientId = ClientId,
                        AccessToken = Settings.TwitchBotToken
                    }
                };

                try
                {
                    BotTokenCheck = await _twitchApiBot.Auth.ValidateAccessTokenAsync(Settings.TwitchBotToken);
                }
                catch (HttpRequestException ex)
                {
                    Logger.Warning(
                        LogSource.Twitch,
                        "HTTP error occurred during Twitch token validation.",
                        ex
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        LogSource.Twitch,
                        "Unexpected exception during Twitch token validation.",
                        ex
                    );
                }

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
                            ApiConnect(Enums.TwitchAccount.Bot);
                        }
                    });
                    return;
                }

                DateTime botTokenExpiryDate = DateTime.Now.AddSeconds(BotTokenCheck.ExpiresIn);

                Settings.BotAccessTokenExpiryDate = botTokenExpiryDate;

                GlobalObjects.TwitchBotTokenExpired = false;

                _userId = BotTokenCheck.UserId;

                users = await _twitchApiBot.Helix.Users.GetUsersAsync([_userId], null,
                    Settings.TwitchBotToken);

                user = users.Users.FirstOrDefault();
                if (user == null)
                    return;
                Settings.TwitchBotUser = user;
                break;

            #endregion Bot

            default:
                throw new ArgumentOutOfRangeException(nameof(twitchAccount), twitchAccount, null);
        }

        // fire-and-forget is fine; the method itself is concurrency-safe
        _ = TwitchApi != null ? StartOrRestartAsync() : StopAsync();
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
                Enums.CommandType.SongRequest => HandleSongRequestCommand,
                Enums.CommandType.Next => HandleNextCommand,
                Enums.CommandType.Play => HandlePlayCommand,
                Enums.CommandType.Pause => HandlePauseCommand,
                Enums.CommandType.Position => HandlePositionCommand,
                Enums.CommandType.Queue => HandleQueueCommand,
                Enums.CommandType.Remove => HandleRemoveCommand,
                Enums.CommandType.Skip => HandleSkipCommand,
                Enums.CommandType.Voteskip => HandleVoteSkipCommand,
                Enums.CommandType.Song => HandleSongCommand,
                Enums.CommandType.Songlike => HandleSongLikeCommand,
                Enums.CommandType.Volume => HandleVolumeCommand,
                Enums.CommandType.Commands => HandleCommandsCommand,
                Enums.CommandType.BanSong => HandleBanSongCommand,
                _ => null
            };

            if (handler == null) continue;
            // Register Custom CommandProperties
            // Add Custom Property to the command

            switch (command.CommandType)
            {
                case Enums.CommandType.SongRequest:
                case Enums.CommandType.Next:
                case Enums.CommandType.Play:
                case Enums.CommandType.Pause:
                case Enums.CommandType.Position:
                case Enums.CommandType.Queue:
                case Enums.CommandType.Remove:
                case Enums.CommandType.Skip:
                    break;

                case Enums.CommandType.Voteskip:
                    if (command.CustomProperties?.ContainsKey("SkipCount") == false)
                    {
                        command.CustomProperties["SkipCount"] = Settings.BotCmdSkipVoteCount;
                    }
                    break;

                case Enums.CommandType.Song:
                case Enums.CommandType.Songlike:
                    break;

                case Enums.CommandType.Volume:
                    if (command.CustomProperties?.ContainsKey("VolumeSetResponse") == false)
                    {
                        command.CustomProperties["VolumeSetResponse"] = "Volume set to {vol}%";
                    }
                    break;

                case Enums.CommandType.Commands:
                case Enums.CommandType.BanSong:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            TwitchCommandHandler.RegisterCommand(command, handler);
        }
    }

    #region CommandHandlers

    private static async Task HandleBanSongCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        try
        {
            TrackInfo currentSong = GlobalObjects.CurrentSong;
            if (currentSong == null ||
                Settings.SongBlacklist.Any(track => track.TrackId == currentSong.SongId))
                return;

            List<TrackItem> blacklist = Settings.SongBlacklist;
            blacklist.Add(new TrackItem
            {
                Artists = currentSong.Artists,
                TrackName = currentSong.Title,
                TrackId = currentSong.SongId,
                TrackUri = $"spotify:track:{currentSong.SongId}",
                ReadableName = $"{currentSong.Artists} - {currentSong.Title}"
            });
            Settings.SongBlacklist = Settings.SongBlacklist;

            string response = cmd.Response;
            response = response.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");

            SendOrAnnounceMessage(response, cmd);

            await SpotifyApiHandler.SkipSong();
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch, "Error while banning song", ex);
        }
    }

    private static async Task HandleCommandsCommand(ChannelChatMessage message, TwitchCommand cmd,
        TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        List<TwitchCommand> x = Settings.Commands.Where(c => c.IsEnabled).ToList();

        List<string> cmds =
            x.Select(twitchCommand =>
                twitchCommand.Trigger.StartsWith("!")
                    ? twitchCommand.Trigger
                    : "!" + twitchCommand.Trigger).ToList();

        string response = cmd.Response;
        response = response.Replace("{user}", message.ChatterUserName);
        response = response.Replace("{commands}", string.Join(", ", cmds));

        if (cmd.IsAnnouncement)
            await AnnounceChatMessage(response, cmd.AnnouncementColor);
        else
            await SendChatMessage(response);
    }

    private static async Task HandleNextCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;

        string response = cmd.Response;
        response = response.Replace("{user}", message.ChatterUserName);

        //if (GlobalObjects.ReqList.Count == 0)
        //    return;
        response = response.Replace("{song}", GetNextSong());
        SendOrAnnounceMessage(response, cmd);
    }

    private static async Task HandlePauseCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        try
        {
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    await SpotifyApiHandler.PlayPause(Enums.PlaybackAction.Pause);

                    break;

                case Enums.PlayerType.Pear:
                    await YtmDesktopApi.PauseAsync();
                    break;

                case Enums.PlayerType.WindowsPlayback:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch
        {
            // ignored
        }
    }

    private static async Task HandlePlayCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        try
        {
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    await SpotifyApiHandler.PlayPause(Enums.PlaybackAction.Play);
                    break;

                case Enums.PlayerType.Pear:
                    await YtmDesktopApi.PlayAsync();
                    break;

                case Enums.PlayerType.WindowsPlayback:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch
        {
            // ignored
        }
    }

    private static async Task HandlePositionCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;

        List<QueueItem> queueItems = GetQueueItems(message.ChatterUserName);
        if (queueItems == null || queueItems.Count == 0)
        {
            await SendChatMessage($"@{message.ChatterUserName} you have no songs in the current queue.");
            return;
        }

        if (cmd.Response == null || !cmd.Response.Contains("{songs}") || !cmd.Response.Contains("{/songs}"))
            return;

        string response = cmd.Response;
        bool showTimeToPlay = response.Contains("{ttp}");
        string[] split = response.Split(["{songs}", "{/songs}"], StringSplitOptions.None);

        string before = split[0].Replace("{user}", message.ChatterUserName);
        string betweenTemplate = split[1].Replace("{user}", message.ChatterUserName);
        string after = split[2].Replace("{user}", message.ChatterUserName);

        List<string> songDetails = [];

        foreach (QueueItem item in queueItems)
        {
            string timeToPlay = showTimeToPlay ? await GetEstimatedTimeToPlay(item.Id) : "";
            string entry = betweenTemplate
                .Replace("{pos}", $"#{item.Position}")
                .Replace("{song}", item.Title)
                .Replace("{ttp}", timeToPlay);

            songDetails.Add(entry);
        }

        string between = string.Join(" | ", songDetails);
        string finalResponse = before + between + after;

        SendOrAnnounceMessage(finalResponse, cmd);
    }

    private static async Task HandleQueueCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
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
        response = response.Replace("{user}", message.ChatterUserName);

        SendOrAnnounceMessage(response, cmd);
    }

    private static async Task HandleRemoveCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        bool modAction = false;
        RequestObject reqObj;

        string[] words = message.Message.Text.Split(' ');
        if (GlobalObjects.Moderators.Any(o => o.UserId == message.ChatterUserId) || message.IsBroadcaster)
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
                reqObj = GlobalObjects.ReqList.LastOrDefault(o => o.Requester.Equals(message.ChatterUserName, StringComparison.InvariantCultureIgnoreCase));
            }
        }
        else
        {
            // Remove the user's own last request
            reqObj = GlobalObjects.ReqList.LastOrDefault(o => o.Requester.Equals(message.ChatterUserName, StringComparison.InvariantCultureIgnoreCase));
        }

        if (reqObj == null) return;

        string tmp = $"{reqObj.Artist} - {reqObj.Title}";
        GlobalObjects.SkipList.Add(reqObj);

        dynamic payload = new { uuid = Settings.Uuid, key = Settings.AccessKey, queueid = reqObj.Queueid, };

        await SongifyApi.PatchQueueAsync(Json.Serialize(payload));

        await Application.Current.Dispatcher.BeginInvoke(new Action(() => { GlobalObjects.ReqList.Remove(reqObj); }));

        switch (modAction)
        {
            case true:
                GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName.Equals(reqObj.Requester, StringComparison.CurrentCultureIgnoreCase))
                    ?.UpdateCommandTime(true);
                break;

            case false:
                GlobalObjects.TwitchUsers.FirstOrDefault(o => o.UserId == message.ChatterUserId)
                    ?.UpdateCommandTime(true);
                break;
        }

        await GlobalObjects.QueueUpdateQueueWindow();

        string response = modAction
            ? $"The request {tmp} requested by @{reqObj.Requester} has been removed."
            : cmd.Response;

        response = CreateResponse(new PlaceholderContext()
        {
            User = message.ChatterUserName,
            Artist = reqObj.Artist,
            SingleArtist = reqObj.Artist.Split(',').First(),
            Title = reqObj.Title,
            MaxReq = null,
            ErrorMsg = null,
            MaxLength = null,
            Votes = null,
            Song = $"{reqObj.Artist} - {reqObj.Title}",
            Req = reqObj.Requester,
            Url = null,
            PlaylistName = null,
            PlaylistUrl = null,
            Cd = null
        }, response);

        response = response.Replace("{song}", tmp)
            .Replace("{user}", message.ChatterUserName);

        SendOrAnnounceMessage(response, cmd);
    }

    private static async Task HandleSkipCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;

        if (_skipCooldown)
            return;

        int count = 0;
        string name = "";

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            count = GlobalObjects.ReqList.Count;
            if (count <= 0) return;
            RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
            if (firstRequest == null || firstRequest.Trackid != GlobalObjects.CurrentSong.SongId) return;
            name = firstRequest.Requester;
            GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName == name)
                ?.UpdateCommandTime(true);
        });

        if (count > 0 && name.Equals(message.ChatterUserName, StringComparison.CurrentCultureIgnoreCase))
        {
            if (cmdParams.UserLevels.All(ul => ul != -1))
            {
                cmdParams.UserLevels.Add(-1);
            }
        }

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
            User = message.ChatterUserName,
            Artist = null,
            SingleArtist = null,
            Title = null,
            MaxReq = $"{Settings.TwSrMaxReq}",
            ErrorMsg = null,
            MaxLength = $"{Settings.MaxSongLength}",
            Votes = $"{SkipVotes.Count}/{Settings.BotCmdSkipVoteCount}",
            Song = null,
            Req = GlobalObjects.Requester,
            Url = null,
            PlaylistName = null,
            PlaylistUrl = null,
            Cd = Settings.TwSrCooldown.ToString(),
        }, cmd.Response);

        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                await SpotifyApiHandler.SkipSong();
                break;

            case Enums.PlayerType.Pear:
                await PearApi.SkipAsync();
                break;

            case Enums.PlayerType.WindowsPlayback:
            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
                //case PlayerType.YtmDesktop:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        SendOrAnnounceMessage(response, cmd);

        _skipCooldown = true;
        SkipCooldownTimer.Start();
    }

    private static async Task HandleSongCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        try
        {
            if (!await PreCheckCommandAsync(cmd, cmdParams, message))
                return;

            if (_skipCooldown)
                return;

            string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            {
                User = message.ChatterUserName,
                SingleArtist = GlobalObjects.CurrentSong.FullArtists != null
                    ? GlobalObjects.CurrentSong.FullArtists.First().Name
                    : GlobalObjects.CurrentSong.Artists,
                MaxReq = $"{Settings.TwSrMaxReq}",
                ErrorMsg = null,
                MaxLength = $"{Settings.MaxSongLength}",
                Votes = $"{SkipVotes.Count}/{Settings.BotCmdSkipVoteCount}",
                Req = GlobalObjects.Requester,
                Cd = Settings.TwSrCooldown.ToString()
            }, cmd.Response);

            if (response.Contains("{single_artist}"))
                response = response.Replace("{single_artist}", GlobalObjects.CurrentSong.FullArtists != null
                    ? GlobalObjects.CurrentSong.FullArtists.First().Name
                    : GlobalObjects.CurrentSong.Artists);

            SendOrAnnounceMessage(response, cmd);
        }
        catch
        {
            Logger.Error(LogSource.Twitch, "Error sending song info.");
        }
    }

    private static async Task HandleSongLikeCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;
        int count = 0;
        string name = "";

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            count = GlobalObjects.ReqList.Count;
            if (count <= 0) return;
            RequestObject firstRequest = GlobalObjects.ReqList.FirstOrDefault();
            if (firstRequest == null || firstRequest.Trackid != GlobalObjects.CurrentSong.SongId) return;
            name = firstRequest.Requester;
            GlobalObjects.TwitchUsers.FirstOrDefault(o => o.DisplayName == name)
                ?.UpdateCommandTime(true);
        });

        if (count > 0 && name.Equals(message.ChatterUserName, StringComparison.CurrentCultureIgnoreCase))
        {
            if (cmdParams.UserLevels.All(ul => ul != -1))
            {
                cmdParams.UserLevels.Add(-1);
            }
        }

        if (string.IsNullOrWhiteSpace(Settings.SpotifyPlaylistId))
        {
            await SendChatMessage("No playlist has been specified. Go to Settings -> Spotify and select the playlist you want to use.");
            return;
        }

        try
        {
            if (await SpotifyApiHandler.AddToPlaylist(GlobalObjects.CurrentSong.SongId))
            {
                await SendChatMessage($"The Song \"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}\" is already in the playlist.");
                return;
            }

            string response = cmd.Response;
            response = response.Replace("{song}", $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}");

            SendOrAnnounceMessage(response, cmd);
        }
        catch (Exception exception)
        {
            Logger.Error(LogSource.Spotify, "Error while adding song to playlist");
            Logger.LogExc(exception);
        }
    }

    private static async Task HandleSongRequestCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        //PrintObjectProperties(cmdParams.ExistingUser);

        if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster, cmd, message.ChatterUserId))
        {
            string response = Settings.BotRespUserlevelTooLowCommand;
            response = response.Replace("{user}", message.ChatterUserName);

            List<string> allowedUserLevels = Settings.Commands
                .First(c => c.CommandType == Enums.CommandType.SongRequest)
                .AllowedUserLevels
                .Where(level => Enum.IsDefined(typeof(Enums.TwitchUserLevels), level)) // Ensure valid enums
                .Select(level => ((Enums.TwitchUserLevels)level).ToString()) // Convert to name
                .ToList();

            // Join the list into a single string
            string allowedUserLevelsString = allowedUserLevels.Any()
                ? string.Join(", ", allowedUserLevels)
                : "None";

            response = response.Replace("{userlevel}", allowedUserLevelsString);

            await SendChatMessage(response);
            return;
        }

        try
        {
            if (!CheckLiveStatus())
            {
                if (Settings.ChatLiveStatus) await SendChatMessage("The stream is not live right now.");
                return;
            }
        }
        catch (Exception)
        {
            Logger.Error(LogSource.Twitch, "Error sending chat message \"The stream is not live right now.\"");
        }

        if (message.Message.Text.Split(' ').Length <= 1)
        {
            await SendChatMessage("No query provided.");
            return;
        }

        // Do nothing if the user is blocked, don't even reply
        if (IsUserBlocked(message.ChatterUserName))
        {
            return;
        }

        TimeSpan cooldown = TimeSpan.FromSeconds(Settings.TwSrPerUserCooldown); // Set your cooldown time here
        if (!cmdParams.ExistingUser.IsCooldownExpired(cooldown))
        {
            // Inform user about the cooldown
            if (cmdParams.ExistingUser.LastCommandTime == null) return;
            TimeSpan remaining = cooldown - (DateTime.Now - cmdParams.ExistingUser.LastCommandTime.Value);
            Logger.Info(LogSource.Twitch, $"{cmdParams.ExistingUser.DisplayName} is on cooldown. ({remaining.Seconds} more seconds)");
            // if remaining is more than 1 minute format to mm:ss, else to ss
            string time = remaining.Minutes >= 1
                ? $"{remaining.Minutes} minute{(remaining.Minutes > 1 ? "s" : "")} {remaining.Seconds} seconds"
                : $"{remaining.Seconds} seconds";

            string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
            {
                User = message.ChatterUserName,
                MaxReq = $"{Settings.TwSrMaxReq}",
                ErrorMsg = null,
                MaxLength = $"{Settings.MaxSongLength}",
                Votes = $"{SkipVotes.Count}/{Settings.BotCmdSkipVoteCount}",
                Req = GlobalObjects.Requester,
                Cd = time
            }, Settings.BotRespUserCooldown);
            await SendChatMessage(response);
            return;
        }

        // if onCooldown skips
        if (_onCooldown)
        {
            await SendChatMessage(CreateCooldownResponse(message));
            return;
        }

        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                await HandleSpotifyRequest(message, cmdParams, cmd);
                break;
            //case PlayerType.YtmDesktop:
            case Enums.PlayerType.Pear:
                await HandleYtmRequest(message, cmdParams, cmd);
                break;

            case Enums.PlayerType.WindowsPlayback:
            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
            default:
                await SendChatMessage("No player selected. Go to Settings -> Player and select a player.");
                return;
        }

        // start the command cooldown
        StartCooldown();
        cmdParams.ExistingUser.UpdateCommandTime();
    }

    private static async Task HandleVolumeCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;

        if (message.Message.Text.Split(' ').Length > 1)
        {
            // Volume Set
            cmd.CustomProperties.TryGetValue("VolumeSetResponse", out object volSetResponse);
            string response = (string)volSetResponse ?? "";

            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster, cmd, message.ChatterUserId)) return;
            int? vol = Settings.Player switch
            {
                Enums.PlayerType.Spotify => await SetSpotifyVolume(message.Message.Text),
                Enums.PlayerType.Pear => await SetPearVolume(message.Message.Text),
                _ => 0
            };

            if (vol == null)
            {
                await SendChatMessage("Error setting volume.");
                return;
            }
            response = response.Replace("{vol}", vol.ToString());
            response = response.Replace("{user}", message.ChatterUserName);

            SendOrAnnounceMessage(response, cmd);
        }
        else
        {
            string response = cmd.Response;
            // Volume Get
            if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster, cmd, message.ChatterUserId)) return;
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    CurrentlyPlayingContext spotifyPlaybackAsync = await SpotifyApiHandler.GetPlayback();
                    response = response.Replace("{vol}", spotifyPlaybackAsync?.Device.VolumePercent.ToString());
                    break;

                case Enums.PlayerType.Pear:
                    int pearVolume = await PearApi.GetVolumeAsync();
                    response = response.Replace("{vol}", $"{pearVolume}");

                    break;

                case Enums.PlayerType.WindowsPlayback:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            response = response.Replace("{user}", message.ChatterUserName);

            SendOrAnnounceMessage(response, cmd);
        }
    }

    private static async Task HandleVoteSkipCommand(ChannelChatMessage message, TwitchCommand cmd, TwitchCommandParams cmdParams)
    {
        if (!await PreCheckCommandAsync(cmd, cmdParams, message))
            return;

        if (_skipCooldown) return;
        //Start a skip vote, add the user to SkipVotes, if at least 5 users voted, skip the song
        if (SkipVotes.Any(o => o == message.ChatterUserName)) return;
        SkipVotes.Add(message.ChatterUserName);

        string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
        {
            User = message.ChatterUserName,
            MaxReq = $"{Settings.TwSrMaxReq}",
            ErrorMsg = null,
            MaxLength = $"{Settings.MaxSongLength}",
            Votes = $"{SkipVotes.Count}/{Settings.BotCmdSkipVoteCount}",
            Req = GlobalObjects.Requester,
            Cd = Settings.TwSrCooldown.ToString()
        }, cmd.Response);

        SendOrAnnounceMessage(response, cmd);

        if (SkipVotes.Count < Settings.BotCmdSkipVoteCount) return;
        switch (Settings.Player)
        {
            case Enums.PlayerType.Spotify:
                await SpotifyApiHandler.SkipSong();
                break;

            case Enums.PlayerType.Pear:
                await PearApi.SkipAsync();
                break;

            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
            case Enums.PlayerType.WindowsPlayback:
            default:
                throw new ArgumentOutOfRangeException();
        }

        await SendChatMessage("Skipping song by vote...");

        SkipVotes.Clear();
        _skipCooldown = true;
        SkipCooldownTimer.Start();
    }

    private static async Task HandleSpotifyRequest(ChannelChatMessage message, TwitchCommandParams cmdParams, TwitchCommand cmd)
    {
        if (SpotifyApiHandler.Client == null)
        {
            await SendChatMessage("It seems that Spotify is not connected right now.");
            return;
        }

        string msg = message.Message.Text.Contains(' ')
            ? message.Message.Text.Substring(message.Message.Text.IndexOf(' ') + 1)
            : string.Empty;

        string trackId = await GetTrackIdFromInput(msg.Trim());

        AddSong(trackId, TwitchRequestUser.FromChatmessage(message), Enums.SongRequestSource.Command, cmdParams.ExistingUser);
    }

    private static async Task HandleYtmRequest(ChannelChatMessage message, TwitchCommandParams cmdParams, TwitchCommand cmd)
    {
        switch (Settings.Player)
        {
            case Enums.PlayerType.Pear:
                {
                    RequestObject req = null;
                    bool addedToYtQueue = false;

                    string videoId = ExtractYouTubeVideoIdFromText(message.Message.Text);

                    if (string.IsNullOrEmpty(videoId))
                    {
                        // Search by text
                        string messageWithoutTrigger = Regex.Replace(
                            message.Message.Text,
                            $"!{cmd.Trigger}",
                            "",
                            RegexOptions.IgnoreCase
                        ).Trim();

                        PearSearch sr = await PearApi.SearchAsync(messageWithoutTrigger);
                        if (sr == null) return;
                        req = new RequestObject
                        {
                            Uuid = Settings.Uuid,
                            Trackid = sr.VideoId,
                            PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                            Artist = string.Join(", ", sr.Artists.Where(a => a != "Song")),
                            Title = sr.Title,
                            Length = sr.Duration, // assumed "hh:mm:ss" style string
                            Requester = message.ChatterUserName,
                            Played = 0,
                            Albumcover = sr.ThumbnailUrl
                        };

                        // Check if already in our internal queue
                        if (GlobalObjects.ReqList.Any(r => r.Trackid == sr.VideoId))
                        {
                            string resp = CreateResponse(new()
                            {
                                User = message.ChatterUserName,
                                Artist = string.Join(", ", sr.Artists.Where(a => a != "Song")),
                                SingleArtist = sr.Artists.First(),
                                Title = sr.Title,
                                MaxReq = $"{Settings.TwSrMaxReq}",
                                ErrorMsg = null,
                                MaxLength = $"{Settings.MaxSongLength}",
                                Song = $"{sr.Artists.First()} - {sr.Title}",
                                Req = message.ChatterUserName,
                            }, Settings.BotRespIsInQueue);
                            SendOrAnnounceMessage(resp, cmd);
                            return;
                        }

                        // Try to add to YT queue first
                        addedToYtQueue = await YtmDesktopApi.AddToQueueAsync(
                            req.Trackid,
                            Enums.InsertPosition.InsertAfterCurrentVideo
                        );

                        if (!addedToYtQueue)
                        {
                            await SendChatMessage("Error adding song to queue");
                            return;
                        }

                        // Only add to our internal queue if YT accepted it
                        GlobalObjects.ReqList.Add(req);

                        // wait until YT queue actually contains the item
                        int? pos = await WaitForSongInQueueAsync(
                            sr.VideoId,
                            TimeSpan.FromSeconds(3),
                            TimeSpan.FromMilliseconds(150)
                        );

                        if (pos == null)
                        {
                            // fallback: skip reorder now; try again later (timer/next enqueue)
                            return;
                        }
                    }
                    else
                    {
                        // Direct videoId path
                        string title = await WebTitleFetcher.GetWebsiteTitleAsync(
                            $"https://www.youtube.com/watch?v={videoId}"
                        );
                        string thumbnail = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

                        req = new RequestObject
                        {
                            Uuid = Settings.Uuid,
                            Trackid = videoId,
                            PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                            Artist = "",
                            Title = title,
                            Length = "", // we might not know yet
                            Requester = message.ChatterUserName,
                            Played = 0,
                            Albumcover = thumbnail
                        };

                        // Already in our own queue?
                        if (GlobalObjects.ReqList.Any(r => r.Trackid == videoId))
                        {
                            string song = $"{req.Artist} - {req.Title}";
                            song = song.StartsWith(" - ")
                                ? song.Substring(3)
                                : song;
                            string resp = CreateResponse(new()
                            {
                                User = message.ChatterUserName,
                                Artist = string.Join(", ", req.Artist),
                                SingleArtist = req.Artist,
                                Title = req.Title,
                                MaxReq = $"{Settings.TwSrMaxReq}",
                                ErrorMsg = null,
                                MaxLength = $"{Settings.MaxSongLength}",
                                Song = song,
                                Req = message.ChatterUserName,
                            }, Settings.BotRespIsInQueue);
                            SendOrAnnounceMessage(resp, cmd);
                            return;
                        }

                        // Try to add to YT queue first
                        addedToYtQueue = await PearApi.EnqueueAsync(
                            req.Trackid,
                            Enums.InsertPosition.InsertAfterCurrentVideo
                        );

                        if (!addedToYtQueue)
                        {
                            await SendChatMessage("That song is already in the queue");
                            return;
                        }

                        // Only add if it really made it into the player queue
                        GlobalObjects.ReqList.Add(req);
                    }

                    await EnsureOrderAsync();

                    // If nothing was enqueued successfully, stop here
                    if (req == null || !addedToYtQueue)
                        return;

                    // -------- Generic success response below --------

                    // Safely get duration in ms
                    int durationMs = 0;
                    if (!string.IsNullOrWhiteSpace(req.Length) &&
                        TimeSpan.TryParse(req.Length, out TimeSpan ts))
                    {
                        durationMs = (int)ts.TotalMilliseconds;
                    }

                    // Build a pseudo-Spotify track object for the generic pipeline
                    FullTrack track = new()
                    {
                        Album = null, // not needed for now
                        Artists = [new() { Name = req.Artist }],
                        AvailableMarkets = null,
                        DiscNumber = 0,
                        DurationMs = durationMs,
                        Explicit = false,
                        ExternalIds = null,
                        ExternalUrls = null,
                        Href = $"https://youtu.be/{req.Trackid}",
                        Id = req.Trackid,
                        IsPlayable = true,
                        Name = req.Title,
                        Type = ItemType.Track,
                        Uri = $"https://youtu.be/{req.Trackid}",
                    };

                    string successResponse = Settings.Commands
                        .First(command => command.Name == "Song Request")
                        .Response;

                    string response = CreateSuccessResponse(track, message.ChatterUserName, successResponse);
                    if (response.StartsWith("- "))
                    {
                        response = response.Substring(2);
                    }

                    // Take first 4 artists
                    string artists = string.Join(", ",
                        track.Artists
                            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                            .Take(4)
                            .Select(a => a.Name)
                    );

                    string length = FormattedTime(track.DurationMs);

                    // Get the Requester Twitch User Object from the api
                    GetUsersResponse x = await TwitchApi.Helix.Users.GetUsersAsync(
                        [message.ChatterUserId],
                        null,
                        Settings.TwitchAccessToken
                    );

                    SimpleTwitchUser requestUser = null;
                    if (x.Users.Length > 0)
                    {
                        requestUser = x.Users[0].ToSimpleUser();
                    }

                    // This RequestObject is for your *internal* Songify queue (stats/widget/etc)
                    RequestObject o = new()
                    {
                        Trackid = track.Id,
                        PlayerType = nameof(Enums.RequestPlayerType.Youtube),
                        Artist = artists,
                        Title = track.Name,
                        Length = length,
                        Requester = message.ChatterUserName,
                        FullRequester = requestUser,
                        Played = 0,
                        Albumcover = req.Albumcover, // use the YT thumbnail we already have
                    };

                    await UploadToQueue(o);

                    if (successResponse.Contains("{ttp}"))
                    {
                        try
                        {
                            if (GlobalObjects.QueueTracks.Count > 0)
                            {
                                string timeToPlay = await GetEstimatedTimeToPlay(track.Id);
                                response = response.Replace("{ttp}", timeToPlay);
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }

                    // If {ttp} was not available or something failed, just strip it
                    response = response.Replace("{ttp}", "");

                    SendOrAnnounceMessage(response, cmd);

                    await CheckAndRefund(
                        Enums.SongRequestSource.Command,
                        null,
                        Enums.RefundCondition.AlwaysRefund,
                        new TwitchRequestUser
                        {
                            Channel = message.BroadcasterUserLogin,
                            DisplayName = message.ChatterUserName,
                            IsBroadcaster = message.IsBroadcaster,
                            Message = message.Message.Text,
                            UserId = message.ChatterUserId
                        }
                    );

                    break;
                }

            case Enums.PlayerType.Spotify:
            case Enums.PlayerType.WindowsPlayback:
            case Enums.PlayerType.FooBar2000:
            case Enums.PlayerType.Vlc:
            case Enums.PlayerType.BrowserCompanion:
            default:
                return;
        }
    }

    #endregion CommandHandlers

    public static void InitTwitchUserSyncTimer()
    {
        if (_syncTimerHooked) return;               // <-- prevents duplicates
        TwitchUserSyncTimer.Tick += TwitchUserSyncTimer_Tick;
        _syncTimerHooked = true;
        TwitchUserSyncTimer.Start();
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

    public static string ReplaceParameters(string source, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(source) || parameters == null || parameters.Count == 0)
        {
            return source;
        }

        return parameters.Aggregate(source, (current, parameter) => current.Replace($"{{{parameter.Key}}}", parameter.Value));
    }

    public static void ResetTwitchSetting(Enums.TwitchAccount account)
    {
        try
        {
            switch (account)
            {
                case Enums.TwitchAccount.Main:
                    // Clear persisted main account credentials/state
                    Settings.TwitchAccessToken = "";
                    Settings.TwitchUser = null;
                    Settings.TwitchChannelId = "";
                    Settings.TwChannel = "";
                    Settings.TwitchAccessTokenExpiryDate = DateTime.MinValue;

                    // Clear runtime objects
                    TokenCheck = null;
                    TwitchApi = null;
                    break;

                case Enums.TwitchAccount.Bot:
                    // Clear persisted bot account credentials/state
                    Settings.TwitchBotToken = "";
                    Settings.TwitchBotUser = null;
                    Settings.TwAcc = "";
                    Settings.TwOAuth = "";
                    Settings.BotAccessTokenExpiryDate = DateTime.MinValue;

                    // Clear runtime objects
                    BotTokenCheck = null;
                    _twitchApiBot = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(account), account, null);
            }

            // Clear joined channel and stop EventSub host (depends on main token)
            _ = StopAsync();
            DisposeTwitchUserSyncTimer();

            // Update UI hints
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is not MainWindow mw) continue;

                    // API icon to red when main reset; bot icon to red when bot reset
                    if (account == Enums.TwitchAccount.Main)
                    {
                        mw.IconTwitchAPI.Foreground = Brushes.IndianRed;
                        mw.mi_TwitchAPI.IsEnabled = false;
                    }
                    else
                    {
                        mw.IconTwitchBot.Foreground = Brushes.IndianRed;
                    }

                    // Reflect chat disconnect availability
                    mw.mi_TwitchConnect.IsEnabled = true;
                    mw.mi_TwitchDisconnect.IsEnabled = false;
                    mw.LblStatus.Content = "Twitch credentials cleared.";
                }
            });

            Logger.Info(LogSource.Twitch, $"Cleared {(account == Enums.TwitchAccount.Main ? "main" : "bot")} account credentials.");
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch, "Error clearing Twitch settings", ex);
        }
    }

    private static async Task<bool> PreCheckCommandAsync(
        TwitchCommand cmd,
        TwitchCommandParams cmdParams,
        ChannelChatMessage message)
    {
        // 1. User permission check
        if (!IsUserAllowed(cmd.AllowedUserLevels, cmdParams, message.IsBroadcaster, cmd, message.ChatterUserId))
            return false;

        // 2. Live-status check
        try
        {
            if (!CheckLiveStatus())
            {
                if (Settings.ChatLiveStatus)
                    await SendChatMessage("The stream is not live right now.");

                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch,
                "Error sending chat message \"The stream is not live right now.\"",
                ex);
            return false;
        }

        return true; // All checks passed
    }

    public static void ResetVotes()
    {
        SkipVotes.Clear();
    }

    public static async Task RunTwitchUserSync()
    {
        TwitchUserSyncTimer.Stop();

        try
        {
            if (Settings.TwitchUser == null)
                return;

            if (string.IsNullOrEmpty(Settings.TwitchAccessToken))
                return;

            if (TwitchApi == null)
                return;

            Debug.WriteLine("TWITCH FETCHING USERS");

            // Fetch all chatters and subscribers
            GlobalObjects.Chatters = await TwitchApiHelper.GetAllChattersAsync();
            Debug.WriteLine("CHATTERS DONE");

            GlobalObjects.Subscribers = await TwitchApiHelper.GetAllSubscribersAsync();
            Debug.WriteLine("SUBS DONE");

            GlobalObjects.Moderators = await TwitchApiHelper.GetAllModeratorsAsync();
            Debug.WriteLine("MODS DONE");

            GlobalObjects.Vips = await TwitchApiHelper.GetAllVipsAsync();
            Debug.WriteLine("VIPS DONE");

            if (GlobalObjects.Chatters == null || GlobalObjects.Subscribers == null)
                return;

            string broadcasterId = Settings.TwitchUser.Id;

            // --- Pre-index lists for O(1) lookups ---
            HashSet<string> modIds = new(
                (GlobalObjects.Moderators ?? Enumerable.Empty<Moderator>())
                .Select(m => m.UserId)
            );

            HashSet<string> vipIds = new(
                (GlobalObjects.Vips ?? Enumerable.Empty<ChannelVIPsResponseModel>())
                .Select(v => v.UserId)
            );

            // For each user, keep highest tier sub (if multiple exist)
            Dictionary<string, Subscription> subsByUser = (GlobalObjects.Subscribers ?? Enumerable.Empty<Subscription>())
                .GroupBy(s => s.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(s => ParseTierSafe(s.Tier)).FirstOrDefault()
                );

            // Existing users indexed by userId
            Dictionary<string, TwitchUser> existingById = GlobalObjects.TwitchUsers
                .ToDictionary(u => u.UserId, u => u);

            // De-dupe chatters (DistinctBy alternative)
            HashSet<string> seenChatters = [];

            foreach (Chatter chatter in GlobalObjects.Chatters)
            {
                if (chatter == null || string.IsNullOrEmpty(chatter.UserId))
                    continue;

                // Skip duplicates
                if (!seenChatters.Add(chatter.UserId))
                    continue;

                List<int> userLevels = [(int)Enums.TwitchUserLevels.Viewer];

                if (modIds.Contains(chatter.UserId))
                    userLevels.Add((int)Enums.TwitchUserLevels.Moderator);

                if (vipIds.Contains(chatter.UserId))
                    userLevels.Add((int)Enums.TwitchUserLevels.Vip);

                int subtier = 0;

                if (subsByUser.TryGetValue(chatter.UserId, out Subscription subsc) && subsc != null)
                {
                    userLevels.Add((int)Enums.TwitchUserLevels.Subscriber);

                    int tierValue = ParseTierSafe(subsc.Tier);
                    subtier = tierValue / 1000;

                    switch (subsc.Tier)
                    {
                        case "2000":
                            userLevels.Add((int)Enums.TwitchUserLevels.SubscriberT2);
                            break;

                        case "3000":
                            userLevels.Add((int)Enums.TwitchUserLevels.SubscriberT3);
                            break;
                    }
                }

                if (chatter.UserId == broadcasterId)
                    userLevels.Add((int)Enums.TwitchUserLevels.Broadcaster);

                // Follow status (NOTE: this is still per-user; consider batching/caching)
                (bool? isFollowingNullable, ChannelFollower followInfo) =
                    await GetIsUserFollowing(chatter.UserId, broadcasterId);

                bool isFollowing = isFollowingNullable == true;

                if (isFollowing)
                    userLevels.Add((int)Enums.TwitchUserLevels.Follower);

                bool isBlocked = IsUserBlocked(chatter.UserName);

                // Update existing or add new
                if (existingById.TryGetValue(chatter.UserId, out TwitchUser existingUser))
                {
                    // Only replace levels if they actually changed (content compare)
                    List<int> levelsToApply =
                        existingUser.UserLevels != null &&
                        existingUser.UserLevels.SequenceEqual(userLevels)
                            ? existingUser.UserLevels
                            : userLevels;

                    existingUser.Update(
                        chatter.UserLogin,
                        chatter.UserName,
                        levelsToApply,
                        isFollowing,
                        subtier,
                        isBlocked,
                        followInfo
                    );
                }
                else
                {
                    TwitchUser newUser = new()
                    {
                        DisplayName = chatter.UserName,
                        UserId = chatter.UserId,
                        UserName = chatter.UserLogin,
                        SubTier = subtier,
                        IsFollowing = isFollowing,
                        FollowInformation = followInfo,
                        IsSrBlocked = isBlocked,
                        UserLevels = userLevels
                    };

                    GlobalObjects.TwitchUsers.Add(newUser);
                    existingById[chatter.UserId] = newUser; // keep dict in sync
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(@$"Error in TwitchUserSyncTimer_Tick: {ex.Message}");
        }
        finally
        {
            TwitchUserSyncTimer.Start();
        }
    }

    // Safe parse helper
    private static int ParseTierSafe(string tier)
    {
        if (string.IsNullOrEmpty(tier))
            return 0;

        int parsed;
        return int.TryParse(tier, out parsed) ? parsed : 0;
    }

    public static async void SendCurrSong()
    {
        try
        {
            try
            {
                if (!CheckLiveStatus())
                {
                    if (Settings.ChatLiveStatus)
                        await SendChatMessage("The stream is not live right now.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch,
                    "Error sending chat message \"The stream is not live right now.\"",
                    ex);
            }

            string msg = GetCurrentSong();
            msg = Regex.Replace(msg, @"(@)?\{user\}", "");
            msg = msg.Replace("{song}",
                $"{GlobalObjects.CurrentSong.Artists} {(GlobalObjects.CurrentSong.Title != "" ? " - " + GlobalObjects.CurrentSong.Title : "")}");
            msg = msg.Replace("{artist}", $"{GlobalObjects.CurrentSong.Artists}");
            msg = msg.Replace("{single_artist}", GlobalObjects.CurrentSong.FullArtists != null ? $"{GlobalObjects.CurrentSong.FullArtists.FirstOrDefault()?.Name}" : GlobalObjects.CurrentSong.Artists);
            msg = msg.Replace("{title}", $"{GlobalObjects.CurrentSong.Title}");
            msg = msg.Replace(@"\n", " - ").Replace("  ", " ");

            TwitchCommand cmd = Settings.Commands.Find(c => c.CommandType == Enums.CommandType.Song);
            if (cmd.IsAnnouncement)
                await AnnounceChatMessage(msg, cmd.AnnouncementColor);
            else
                await SendChatMessage(msg);
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }
    }

    public static async Task SetTwitchSrRewardsEnabledState(bool isOn)
    {
        if (TwitchApi == null)
            return;
        try
        {
            foreach (string rewardId in Settings.TwRewardId)
            {
                await TwitchApi.Helix.ChannelPoints.UpdateCustomRewardAsync(Settings.TwitchUser.Id, rewardId,
                    new UpdateCustomRewardRequest
                    {
                        IsPaused = !isOn,
                    }, Settings.TwitchAccessToken);
            }
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }
    }

    public static async Task StartOrRestartAsync()
    {
        await Lock.WaitAsync();
        try
        {
            // If there's an old host, stop & dispose it first
            if (_host is not null)
            {
                try
                {
                    _cts?.Cancel();
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                }
                catch { /* swallow or log */ }
                finally
                {
                    _host.Dispose();
                    _host = null;

                    _cts?.Dispose();
                    _cts = null;
                }
            }

            // Create and start a fresh host (non-blocking)
            _cts = new CancellationTokenSource();
            _host = CreateHostBuilder([]).Build();
            await _host.StartAsync(_cts.Token); // runs hosted services in background
        }
        finally
        {
            Lock.Release();
        }
    }

    public static async Task StopAsync()
    {
        await Lock.WaitAsync();
        try
        {
            if (_host is null) return;

            try
            {
                _cts?.Cancel();
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                _host.Dispose();
                _host = null;

                _cts?.Dispose();
                _cts = null;
            }
        }
        finally
        {
            Lock.Release();
        }
    }

    public static async Task UpdateRewardCost(string rewardId, int result)
    {
        if (TwitchApi == null)
            return;
        try
        {
            await TwitchApi.Helix.ChannelPoints.UpdateCustomRewardAsync(Settings.TwitchUser.Id, rewardId,
                new UpdateCustomRewardRequest
                {
                    Cost = result,
                }, Settings.TwitchAccessToken);
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }
    }

    private static async Task AnnounceChatMessage(string msg, Enums.AnnouncementColor color)
    {
        AnnouncementColors announcementColors = color switch
        {
            Enums.AnnouncementColor.Blue => AnnouncementColors.Blue,
            Enums.AnnouncementColor.Green => AnnouncementColors.Green,
            Enums.AnnouncementColor.Orange => AnnouncementColors.Orange,
            Enums.AnnouncementColor.Purple => AnnouncementColors.Purple,
            Enums.AnnouncementColor.Primary => AnnouncementColors.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
        try
        {
            if (BotTokenCheck != null)
            {
                await _twitchApiBot.Helix.Chat.SendChatAnnouncementAsync(Settings.TwitchUser.Id,
                    Settings.TwitchBotUser.Id, msg, announcementColors,
                    Settings.TwitchBotToken);
                return;
            }

            if (TokenCheck != null)
            {
                await TwitchApi.Helix.Chat.SendChatAnnouncementAsync(Settings.TwitchUser.Id,
                    Settings.TwitchUser.Id, msg, announcementColors,
                    Settings.TwitchAccessToken);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch, "Could not send announcement. Has the bot been created through the app?", ex);
        }

        await SendChatMessage($"{msg}");
    }

    private static RequestObject BuildRequestObject(FullTrack track, string requester = "")
    {
        string artists = string.Join(", ", track.Artists.Take(4).Select(a => a.Name));
        string length = FormattedTime(track.DurationMs);

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

    private static async Task CheckAndRefund(Enums.SongRequestSource source, RewardInfo reward,
        Enums.RefundCondition condition, TwitchRequestUser twitchRequestUser)
    {
        if (source != Enums.SongRequestSource.Reward || reward == null) return;
        if (Settings.RefundConditons.Any(c => c == condition))
        {
            await RefundChannelPoints(reward.RewardId, reward.RedemptionId, twitchRequestUser, condition);
        }
    }

    private static async Task<Tuple<bool, FullPlaylist>> CheckIsSongAllowed(string trackId,
        string spotifySongLimitPlaylist)
    {
        FullPlaylist playlist = await SpotifyApiHandler.GetPlaylist(spotifySongLimitPlaylist);
        Paging<PlaylistTrack<IPlayableItem>> tracks = await SpotifyApiHandler.GetPlaylistTracks(spotifySongLimitPlaylist);
        while (tracks is { Items: not null })
        {
            // Check if any track matches the given ID
            if (tracks.Items.Any(t => t.Track.Type == ItemType.Track && ((FullTrack)t.Track).Id == trackId))
            {
                return new Tuple<bool, FullPlaylist>(true, playlist);
            }

            // Check if there are more pages, if not, exit the loop
            if (tracks.Next == null)
            {
                break;
            }

            // Fetch the next page of tracks
            //tracks = await SpotifyApiHandler.Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100, tracks.Offset + tracks.Limit);
        }

        return new Tuple<bool, FullPlaylist>(false, playlist);
    }

    private static bool CheckLiveStatus()
    {
        if (Settings.IsLive)
        {
            Logger.Info(LogSource.Twitch, "Stream is live.");
            return true;
        }
        if (!Settings.BotOnlyWorkWhenLive)
            return true;

        Logger.Info(LogSource.Twitch, "Stream is down.");
        return false;
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

    private static void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        // Resets the cooldown for the !ssr command
        _onCooldown = false;
        CooldownStopwatch.Stop();
        CooldownStopwatch.Reset();
        CooldownTimer.Stop();
    }

    private static string CreateCooldownResponse(ChannelChatMessage e)
    {
        string response = Settings.BotRespCooldown;
        response = response.Replace("{user}", e.ChatterUserName);
        response = response.Replace("{artist}", "");
        response = response.Replace("{title}", "");
        response = response.Replace("{maxreq}", "");
        response = response.Replace("{errormsg}", "");
        int time = (int)((CooldownTimer.Interval / 1000) - CooldownStopwatch.Elapsed.TotalSeconds);
        response = response.Replace("{cd}", time.ToString());
        return response;
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddTwitchLibEventSubWebsockets();
                services.AddHostedService<WebsocketHostedService>();
            });

    private static string CreateNoTrackFoundResponse(TwitchRequestUser e)
    {
        string response = Settings.BotRespNoTrackFound;
        response = response.Replace("{user}", e.DisplayName);
        response = response.Replace("{artist}", "");
        response = response.Replace("{title}", "");
        response = response.Replace("{maxreq}", "");
        response = response.Replace("{position}", $"{GlobalObjects.ReqList.Count}");
        response = response.Replace("{errormsg}", "");
        return response;
    }

    private static string CreateResponse(PlaceholderContext context, string template)
    {
        // Use reflection to get all properties of PlaceholderContext
        PropertyInfo[] properties = typeof(PlaceholderContext).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            string placeholder = $"{{{property.Name.ToLower()}}}"; // Placeholder format, e.g., "{user}"

            if (placeholder == "{singleartist}")
                placeholder = "{single_artist}";

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
        if (track.Restrictions is { Count: > 0 })
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
        if (host is "youtube.com" or "youtu.be" or "youtube-nocookie.com" or "m.youtube.com" or "music.youtube.com")
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
            string[] pathSegments = uri.AbsolutePath.Trim('/').Split('/');

            // Check for query param ?v=VIDEOID
            NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
            if (query["v"] != null)
            {
                return query["v"];
            }

            switch (pathSegments.Length)
            {
                // Check for "/embed/VIDEOID"
                // PathSegments: [ "embed", "VIDEOID", ... ]
                case >= 2 when
                    pathSegments[0].Equals("embed", StringComparison.OrdinalIgnoreCase):
                case >= 2 when
                    pathSegments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase):
                    return pathSegments[1];
                    // Check for "/shorts/VIDEOID"
                    // PathSegments: [ "shorts", "VIDEOID", ... ]
            }
        }

        // No recognized pattern -> return null
        return null;
    }

    /// <summary>Return first index of id strictly after startPos.</summary>
    private static int FindIndexAfter(List<Song> q, string id, int startPos)
    {
        for (int i = startPos + 1; i < q.Count; i++)
            if (q[i].Id == id) return i;
        return -1;
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
        string currentSong = Settings.Commands.First(c => c.CommandType == Enums.CommandType.Song).Response;

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

    private static string GetCurrentSongTitle()
    {
        string song = "";
        Application.Current.Dispatcher.Invoke(() =>
        {
            MainWindow mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            if (mainWindow != null)
            {
                song = $"{mainWindow.SongArtist} - {mainWindow.SongTitle}";
            }
        });
        return song;
    }

    private static async Task<string> GetEstimatedTimeToPlay(string trackId)
    {
        try
        {
            ObservableCollection<RequestObject> queue = GlobalObjects.QueueTracks;
            int index = -1;

            // Find the index of the track
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Trackid != trackId) continue;
                index = i;
                break;
            }

            if (index == -1)
                return "";

            TimeSpan total = TimeSpan.Zero;

            for (int i = 0; i <= index; i++)
            {
                RequestObject track = queue[i];

                if (i == 0)
                {
                    // Always treat index 0 as the currently playing track
                    TrackInfo currentInfo = await SpotifyApiHandler.GetSongInfo();
                    if (currentInfo == null) continue;
                    if (currentInfo.SongId != GlobalObjects.CurrentSong.SongId)
                        continue;

                    int timeLeft = Math.Max(0, currentInfo.DurationTotal - currentInfo.Progress);
                    total += TimeSpan.FromMilliseconds(timeLeft);
                }
                else
                {
                    total += ParseLength(track.Length);
                }
            }

            return $"{(int)total.TotalMinutes}m {total.Seconds}s";
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Twitch, $"Error calculating TTP", ex);
            return "";
        }
    }

    private static async Task<string> GetFullSpotifyUrl(string input)
    {
        using HttpClient httpClient = new();
        HttpRequestMessage request = new(HttpMethod.Get, input);
        HttpResponseMessage response = await httpClient.SendAsync(request);
        return response.RequestMessage.RequestUri != null ? response.RequestMessage.RequestUri.AbsoluteUri : "";
    }

    private static async Task<Tuple<bool?, ChannelFollower>> GetIsUserFollowing(string chatMessageUserId, string broadcasterUserId)
    {
        // Using the Twitch API to check if the user is following the channel
        try
        {
            GetChannelFollowersResponse resp = await TwitchApi.Helix.Channels.GetChannelFollowersAsync(
                broadcasterUserId,
                chatMessageUserId,
                20,
                null,
                Settings.TwitchAccessToken);
            return new Tuple<bool?, ChannelFollower>(resp.Data.Length > 0, resp.Data.FirstOrDefault());
        }
        catch (Exception)
        {
            return new Tuple<bool?, ChannelFollower>(null, new ChannelFollower());
        }
    }

    private static int GetMaxRequestsForUserLevels(List<int> userLevels)
    {
        return userLevels
            .Select(level => (Enums.TwitchUserLevels)level)
            .Select(userLevel => userLevel switch
            {
                Enums.TwitchUserLevels.Viewer => Settings.TwSrMaxReqEveryone,
                Enums.TwitchUserLevels.Follower => Settings.TwSrMaxReqFollower,
                Enums.TwitchUserLevels.Subscriber => Settings.TwSrMaxReqSubscriber,
                Enums.TwitchUserLevels.SubscriberT2 => Settings.TwSrMaxReqSubscriberT2,
                Enums.TwitchUserLevels.SubscriberT3 => Settings.TwSrMaxReqSubscriberT3,
                Enums.TwitchUserLevels.Vip => Settings.TwSrMaxReqVip,
                Enums.TwitchUserLevels.Moderator => Settings.TwSrMaxReqModerator,
                Enums.TwitchUserLevels.Broadcaster => 999,
                _ => 0
            })
            .Max();
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
        // Copy the current queue
        List<RequestObject> requestList = new(GlobalObjects.ReqList);

        // Get currently playing song title
        string currentSong = GetCurrentSongTitle();

        // Filter by requester if specified
        if (!string.IsNullOrEmpty(requester))
        {
            return requestList
                .Where(r => r.Requester == requester && r.Trackid != GlobalObjects.CurrentSong.SongId)
                .Select((r, _) => new QueueItem
                {
                    Id = r.Trackid,
                    Position = requestList.IndexOf(r),
                    Title = $"{r.Artist} - {r.Title}",
                    Requester = r.Requester
                })
                .ToList();
        }

        switch (requestList.Count)
        {
            // Return null if the queue is empty
            case 0:
                return null;
            // Special case: only one item in the list, but it's not the currently playing one
            case 1:
                {
                    RequestObject onlyRequest = requestList[0];
                    string onlySong = $"{onlyRequest.Artist} - {onlyRequest.Title}";

                    if (onlySong != currentSong)
                    {
                        return
                        [
                            new QueueItem
                        {
                            Id = onlyRequest.Trackid,
                            Title = onlySong,
                            Requester = onlyRequest.Requester
                        }
                        ];
                    }

                    return null;
                }
            default:
                {
                    // Default: return second item in queue (if any)
                    RequestObject nextRequest = requestList[1];

                    return
                    [
                        new QueueItem
                    {
                        Id = nextRequest.Trackid,
                        Title = $"{nextRequest.Artist} - {nextRequest.Title}",
                        Requester = nextRequest.Requester
                    }
                    ];
                }
        }
    }

    private static async Task<List<int>> GetUserLevels(
        string userId,
        string broadcasterId,
        bool isModerator = false,
        bool isVip = false,
        bool isSubscriber = false,
        bool isBroadcaster = false)
    {
        List<int> userLevels = [(int)Enums.TwitchUserLevels.Viewer];

        // Mod
        if (isModerator || GlobalObjects.Moderators.Any(m => m.UserId == userId))
            userLevels.Add((int)Enums.TwitchUserLevels.Moderator);

        // VIP
        if (isVip || GlobalObjects.Vips.Any(m => m.UserId == userId))
            userLevels.Add((int)Enums.TwitchUserLevels.Vip);

        // Subscriber
        if (isSubscriber || GlobalObjects.Subscribers.Any(s => s.UserId == userId))
        {
            userLevels.Add((int)Enums.TwitchUserLevels.Subscriber);

            Subscription sub = GlobalObjects.Subscribers
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => int.Parse(s.Tier))
                .FirstOrDefault();

            if (sub?.Tier == "2000")
                userLevels.Add((int)Enums.TwitchUserLevels.SubscriberT2);
            else if (sub?.Tier == "3000")
                userLevels.Add((int)Enums.TwitchUserLevels.SubscriberT3);
        }

        // Broadcaster (you can still override with true if msg reports it)
        if (isBroadcaster || userId == broadcasterId)
            userLevels.Add((int)Enums.TwitchUserLevels.Broadcaster);

        // Follower check
        (bool? isFollowing, _) = await GetIsUserFollowing(userId, broadcasterId);
        if (isFollowing == true)
            userLevels.Add((int)Enums.TwitchUserLevels.Follower);

        return userLevels;
    }

    private static bool IsArtistBlacklisted(FullTrack track, TwitchRequestUser e, out string response)
    {
        response = string.Empty;
        if (track?.Artists == null || track.Artists.Count == 0)
        {
            Logger.Warning(LogSource.Songrequest, "No artist was found on the track object.");
            return false;
        }

        try
        {
            foreach (string s in Settings.ArtistBlacklist.Where(s =>
                         Array.IndexOf(track.Artists.Select(x => x.Name).ToArray(), s) != -1))
            {
                response = Settings.BotRespBlacklist;
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
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Artist Blacklist", ex);
        }

        return false;
    }

    private static async Task<Tuple<bool, string>> IsInAllowedPlaylist(string trackId)
    {
        string response = string.Empty;
        Tuple<bool, FullPlaylist> isAllowedSong =
            await CheckIsSongAllowed(trackId, Settings.SpotifySongLimitPlaylist);
        if (!isAllowedSong.Item1)
        {
            response = Settings.BotRespPlaylist;
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

    private static Task<(bool IsBlacklisted, string Response)> IsSongBlacklisted(string trackId)
    {
        try
        {
            if (Settings.SongBlacklist != null &&
                Settings.SongBlacklist.Any(s => s.TrackId == trackId))
            {
                string response = Settings.BotRespBlacklistSong;
                return Task.FromResult((true, response));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Song Blacklist", ex);
        }

        return Task.FromResult((false, string.Empty));
    }

    private static bool IsTrackAlreadyInQueue(FullTrack track, TwitchRequestUser e, out string response)
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
                    {"maxreq", Settings.TwSrMaxReq.ToString()},
                    {"errormsg", ""}
                };

                response = ReplaceParameters(Settings.BotRespIsInQueue, parameters);
                return true;

                //response = Settings.Settings.BotRespIsInQueue;
                //response = response.Replace("{user}", e.DisplayName);
                //response = response.Replace("{song}",
                //    $"{string.Join(", ", track.Artists.Select(a => a.Name).ToList())} - {track.Name}");
                //response = response.Replace("{artist}", string.Join(", ", track.Artists.Select(a => a.Name).ToList()));
                //response = response.Replace("{single_artist}", track.Artists.First().Name);
                //response = response.Replace("{title}", track.Name);
                //response = response.Replace("{maxreq}", Settings.Settings.TwSrMaxReq.ToString());
                //response = response.Replace("{errormsg}", "");
                //response = CleanFormatString(response);
                //return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Track Already In Queue", ex);
        }

        return false;
    }

    private static bool IsTrackExplicit(FullTrack track, TwitchRequestUser e, out string response)
    {
        response = string.Empty;
        if (!Settings.BlockAllExplicitSongs)
            return false;
        try
        {
            if (!track.Explicit)
            {
                return false;
            }

            response = Settings.BotRespTrackExplicit;
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
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Track Unavailable", ex);
        }

        return false;
    }

    private static bool IsTrackTooLong(FullTrack track, TwitchRequestUser e, out string response)
    {
        response = string.Empty;

        try
        {
            if (track.DurationMs >= TimeSpan.FromMinutes(Settings.MaxSongLength).TotalMilliseconds)
            {
                response = Settings.BotRespLength;
                response = response.Replace("{user}", e.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", "");
                response = response.Replace("{errormsg}", "");
                response = response.Replace("{maxlength}", Settings.MaxSongLength.ToString());
                response = CleanFormatString(response);

                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Track Too Long", ex);
        }

        return false;
    }

    private static bool IsTrackUnavailable(FullTrack track, TwitchRequestUser e, out string response)
    {
        response = string.Empty;

        if (track.AvailableMarkets.Any(s => s == Settings.SpotifyProfile.Country))
            return false;

        try
        {
            if (track.IsPlayable)
            {
                return false;
            }

            response = Settings.BotRespUnavailable;
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
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking Track Unavailable", ex);
        }

        return false;
    }

    private static bool IsUserAllowed(List<int> allowedUserLevels, TwitchCommandParams cmdParams, bool messageIsBroadcaster, TwitchCommand cmd, string chatterId)
    {
        if (messageIsBroadcaster)
            return true;

        if (cmd != null && cmd.AllowedUsers.Any(u => u.Id == chatterId))
        {
            Logger.Info(LogSource.Twitch, $"User {cmd.AllowedUsers.First(u => u.Id == chatterId).DisplayName} is explicitly allowed to use !{cmd.Trigger}");
            return true;
        }

        return allowedUserLevels.Count != 0 && allowedUserLevels.Intersect(cmdParams.UserLevels).Any();
    }

    private static bool IsUserAtMaxRequests(TwitchRequestUser e, TwitchUser user, out string response)
    {
        response = string.Empty;
        try
        {
            // Check if the maximum queue items have been reached for the user level
            if (MaxQueueItems(e.DisplayName, user.UserLevels))
            {
                response = Settings.BotRespMaxReq;
                response = response.Replace("{user}", e.DisplayName);
                response = response.Replace("{artist}", "");
                response = response.Replace("{title}", "");
                response = response.Replace("{maxreq}", $"{GetMaxRequestsForUserLevels(user.UserLevels)}");
                response = response.Replace("{errormsg}", "");
                response = CleanFormatString(response);
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Songrequest, "ERROR: Issue checking User At Max Requests", ex);
        }

        return false;
    }

    private static bool IsUserBlocked(string displayName)
    {
        // checks if one of the artist in the requested song is on the blacklist
        return Settings.UserBlacklist.Any(s =>
            s.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));
    }

    private static bool MaxQueueItems(string requester, List<int> userLevels)
    {
        // Get all current requests from this user (case-insensitive)
        List<RequestObject> userRequests = GlobalObjects.ReqList
            .Where(x => x.Requester.Equals(requester, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Get the highest max request limit based on all user levels
        int maxAllowed = GetMaxRequestsForUserLevels(userLevels);

        return userRequests.Count >= maxAllowed;
    }

    /// <summary>Apply the move to local list to keep positions in sync.</summary>
    private static void MoveLocal<T>(List<T> list, int from, int to)
    {
        if (from == to) return;
        T item = list[from];
        list.RemoveAt(from);
        if (to > list.Count) to = list.Count;
        list.Insert(to, item);
    }

    private static async Task RefundChannelPoints(string rewardId, string redemptionId,
        TwitchRequestUser twitchRequestUser, Enums.RefundCondition condition)
    {
        try
        {
            if (TwitchApi != null)
            {
                GetCustomRewardsResponse resp = await TwitchApi.Helix.ChannelPoints.GetCustomRewardAsync(
                    broadcasterId: Settings.TwitchUser.Id,
                    rewardIds: [rewardId],
                    onlyManageableRewards: true,
                    accessToken: Settings.TwitchAccessToken
                );

                if (resp.Data == null || !resp.Data.Any())
                {
                    Logger.Warning(LogSource.Twitch, "TWITCH API: Cannot refund because the reward is not created through Songify.");
                    return;
                }

                //refund redemption
                UpdateRedemptionStatusResponse updateRedemptionStatus = await TwitchApi.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                    Settings.TwitchUser.Id, rewardId,
                    [redemptionId],
                    new UpdateCustomRewardRedemptionStatusRequest
                    { Status = CustomRewardRedemptionStatus.CANCELED });
                if (updateRedemptionStatus.Data[0].Status == CustomRewardRedemptionStatus.CANCELED)
                {
                    if (string.IsNullOrEmpty(Settings.BotRespRefund))
                        return;
                    string response = CreateResponse(new PlaceholderContext(GlobalObjects.CurrentSong)
                    {
                        User = twitchRequestUser.DisplayName,
                        MaxReq = $"{Settings.TwSrMaxReq}",
                        ErrorMsg = null,
                        MaxLength = $"{Settings.MaxSongLength}",
                        Votes = $"{SkipVotes.Count}/{Settings.BotCmdSkipVoteCount}",
                        Req = GlobalObjects.Requester,
                        Reason = GlobalObjects.GetRefundConditionLabel(condition)
                    }, Settings.BotRespRefund);
                    //await SendChatMessage(BroadcasterUserLogin, "Refunded channel points :)");
                    await SendChatMessage(response);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(LogSource.Twitch, "TWITCH: Failed to refund channel points", e);
        }
    }

    private static async Task SendChatMessage(string message)
    {
        try
        {
            SendChatMessageResponse chatResponse = await TwitchApi.Helix.Chat.SendChatMessage(Settings.TwitchUser.Id,
                Settings.TwitchChatAccount.Id, message,
                accessToken: Settings.TwitchChatAccount.Token.Replace("oauth:", ""));
            ChatMessageInfo msgInfo = chatResponse.Data[0];
            if (msgInfo.IsSent)
                Logger.Info(LogSource.Twitch, $"Twitch Chat: Sent: {message}");
            else
                Logger.Error(LogSource.Twitch, $"Twitch Chat: Failed to send ({msgInfo.DropReason})");
        }
        catch (Exception e)
        {
            Logger.Error(LogSource.Twitch, "Error sending chat message", e);
        }
    }

    private static async void SendOrAnnounceMessage(string message, TwitchCommand cmd)
    {
        try
        {
            if (cmd.IsAnnouncement)
                await AnnounceChatMessage(message, cmd.AnnouncementColor);
            else
                await SendChatMessage(message);
        }
        catch (Exception e)
        {
            Logger.Error(LogSource.Twitch, "Failed to send chat or announcement", e);
        }
    }

    private static async Task<int?> SetSpotifyVolume(string msg)
    {
        string[] split = msg.Split(' ');
        if (split.Length <= 1) return null;
        if (!int.TryParse(split[1], out int volume)) return null;
        int vol = MathUtils.Clamp(volume, 0, 100);
        bool response = await SpotifyApiHandler.SetVolume(vol);
        if (!response)
            return null;
        return vol;
    }

    private static async Task<int?> SetPearVolume(string msg)
    {
        string[] split = msg.Split(' ');
        if (split.Length <= 1) return null;
        if (!int.TryParse(split[1], out int volume)) return null;
        int vol = MathUtils.Clamp(volume, 0, 100);
        ApiOk response = await PearApi.SetVolumeAsncy(vol);
        if (!response.Ok)
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
        int interval = Settings.TwSrCooldown > 0 ? Settings.TwSrCooldown : 0;
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

    private static async Task<(bool valid, FullTrack track, string message)> TryGetValidTrack(string trackId)
    {
        FullTrack track = await SpotifyApiHandler.GetTrack(trackId);

        (bool isBlacklisted, string response) = await IsSongBlacklisted(trackId);
        if (isBlacklisted)
        {
            response = ReplaceParameters(response, new Dictionary<string, string>
            {
                { "user", "" },
                { "req", "" },
                { "artist}", string.Join(", ", track.Artists.Select(a => a.Name).ToList()) },
                { "single_artist", track.Artists.First().Name },
                { "errormsg", "" },
                { "maxlength", Settings.MaxSongLength.ToString() },
                { "maxreq", "" },
                { "song", $"{string.Join(", ", track.Artists.Select(a => a.Name).ToList())} - {track.Name}" },
                { "playlist_name", "" },
                { "playlist_url", "" },
                { "votes", "" },
                { "cd", "" },
                { "url", "" },
                { "queue", "" },
                { "commands", "" },
                { "userlevel", "" },
                { "ttp", "" },
            });
            return (false, null, response);
        }

        if (track == null)
            return (false, null, "No song found.");

        if (IsTrackExplicit(track, null, out string msg)) return (false, null, msg);
        if (IsTrackUnavailable(track, null, out msg)) return (false, null, msg);
        if (IsArtistBlacklisted(track, null, out msg)) return (false, null, msg);
        if (IsTrackTooLong(track, null, out msg)) return (false, null, msg);
        if (IsTrackAlreadyInQueue(track, null, out msg)) return (false, null, msg);

        return (true, track, null);
    }

    private static async void TwitchUserSyncTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            if (Interlocked.Exchange(ref _syncRunning, 1) == 1) return; // already running
            try
            {
                TwitchUserSyncTimer.Stop(); // pause ticks during work
                await RunTwitchUserSync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"Error in Tick: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _syncRunning, 0);
                TwitchUserSyncTimer.Start();
            }
        }
        catch (Exception exc)
        {
            Logger.LogExc(exc);
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
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                queueItem = track
            };

            await QueueService.AddRequestAsync(Json.Serialize(payload));
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
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

    private static async Task<int?> WaitForSongInQueueAsync(string videoId, TimeSpan timeout, TimeSpan pollInterval)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            List<Song> queue = await PearApi.GetQueueAsync(); // returns List<Song> with Pos
            Song hit = queue.Find(q => q.Id == videoId);
            if (hit != null) return hit.Pos;

            await Task.Delay(pollInterval);
        }
        return null; // timed out
    }
}

public class RewardInfo
{
    public string Channel { get; set; }
    public string RedemptionId { get; set; }
    public string RewardId { get; set; }
}

public class TwitchRequestUser
{
    public TwitchRequestUser()
    { }

    public TwitchRequestUser(string channel, string displayName, string userId, string message, bool isBroadcaster)
    {
        Channel = channel;
        DisplayName = displayName;
        UserId = userId;
        Message = message;
        IsBroadcaster = isBroadcaster;
    }

    public string Channel { get; set; }
    public string DisplayName { get; set; }
    public bool IsBroadcaster { get; set; }
    public string Message { get; set; }
    public string UserId { get; set; }

    public static TwitchRequestUser FromChatmessage(ChannelChatMessage msg)
    {
        return new TwitchRequestUser
        {
            Channel = msg.BroadcasterUserLogin,
            DisplayName = msg.ChatterUserName,
            UserId = msg.ChatterUserId,
            Message = msg.Message.Text,
            IsBroadcaster = msg.IsBroadcaster
        };
    }
}

public class TwitchUser : INotifyPropertyChanged
{
    private string _displayName;
    private ChannelFollower _followInformation;
    private bool? _isFollowing;
    private bool _isSrBlocked;
    private DateTime? _lastCommandTime;
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
            if (_displayName == value) return;
            _displayName = value;
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public ChannelFollower FollowInformation
    {
        get => _followInformation;
        set
        {
            if (_followInformation == value) return;
            _followInformation = value;
            OnPropertyChanged(nameof(FollowInformation));
        }
    }

    public int HighestUserLevel => (UserLevels != null && UserLevels.Any()) ? UserLevels.Max() : 0;

    public bool? IsFollowing
    {
        get => _isFollowing;
        set
        {
            if (_isFollowing == value) return;
            _isFollowing = value;
            OnPropertyChanged(nameof(IsFollowing));
        }
    }

    public bool IsSrBlocked
    {
        get => _isSrBlocked;
        set
        {
            if (_isSrBlocked == value) return;
            _isSrBlocked = value;
            OnPropertyChanged(nameof(IsSrBlocked));
        }
    }

    public DateTime? LastCommandTime
    {
        get => _lastCommandTime;
        set
        {
            if (_lastCommandTime == value) return;
            _lastCommandTime = value;
            OnPropertyChanged(nameof(LastCommandTime));
        }
    }

    //public string ReadableUserLevel => ((TwitchUserLevels)UserLevels.Max()).ToString();
    public string ReadableUserLevel => string.Join(", ", UserLevels.Select(level => ((Enums.TwitchUserLevels)level).ToString()));

    public int SubTier
    {
        get => _subTier;
        set
        {
            if (_subTier == value) return;
            _subTier = value;
            OnPropertyChanged(nameof(SubTier));
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
            if (_userId == value) return;
            _userId = value;
            OnPropertyChanged(nameof(UserId));
        }
    }

    public List<int> UserLevels
    {
        get => _userLevel;
        set
        {
            if (_userLevel == value) return;
            _userLevel = value;
            OnPropertyChanged(nameof(UserLevels));
            // Also raise on "ReadableUserLevel" since it depends on UserLevels
            OnPropertyChanged(nameof(ReadableUserLevel));
        }
    }

    public string UserName
    {
        get => _userName;
        set
        {
            if (_userName == value) return;
            _userName = value;
            OnPropertyChanged(nameof(UserName));
        }
    }

    public bool IsCooldownExpired(TimeSpan cooldown)
    {
        if (LastCommandTime == null)
            return true;

        return DateTime.Now - LastCommandTime.Value > cooldown;
    }

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
    public string Id { get; set; }
    public int Position { get; set; }
    public string Requester { get; set; }
    public string Title { get; set; }
}