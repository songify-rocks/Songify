using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Newtonsoft.Json.Linq;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FuzzySharp;
using static Songify_Slim.Util.General.Enums;
using static Songify_Slim.Util.General.Enums.PlaybackAction;
using static System.Net.WebRequestMethods;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace Songify_Slim.Util.Spotify
{
    // This class handles everything regarding Spotify-API integration
    public static class SpotifyApiHandler
    {
        private const string BaseUrl = "https://auth.overcode.tv";
        private static EmbedIOAuthServer _server;
        public static SpotifyClient Client;
        private static int softLimitPerminute = 60;

        private static readonly Timer AuthTimer = new()
        {
            Interval = 1000 * 60 * 30,
        };

        // ---- caching state (in SpotifyApiHandler) ----
        private static CurrentlyPlayingContext _cachedPlayback;

        private static DateTime _cachedPlaybackAt = DateTime.MinValue;
        private static readonly TimeSpan PlaybackCacheTtl = TimeSpan.FromSeconds(5);

        private static string _cachedPlaylistId;
        private static PlaylistInfo _cachedPlaylistInfo;
        private static DateTime _cachedPlaylistFetchedAt = DateTime.MinValue;
        private static readonly TimeSpan PlaylistCacheTtl = TimeSpan.FromMinutes(10);

        private static readonly HashSet<string> _playlistTrackIds = new();
        private static bool _playlistCacheInitialized = false;
        private static readonly object _playlistLock = new();

        public static async Task Auth()
        {
            AuthTimer.Elapsed += AuthTimer_Elapsed;
            if (!string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken))
            {
                Logger.LogStr("SPOTIFY: Refreshing Tokens");
                await RefreshTokens();
                return;
            }

            Logger.LogStr("SPOTIFY: Getting new tokens");
            _server = new EmbedIOAuthServer(new Uri($"http://{Settings.Settings.SpotifyRedirectUri}:4002/auth"), 4002,
                Assembly.GetExecutingAssembly(), "Songify_Slim.default_site");
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            LoginRequest request =
                new(_server.BaseUri, Settings.Settings.ClientId, LoginRequest.ResponseType.Code)
                {
                    Scope = new List<string>
                    {
                        Scopes.UserReadPlaybackState,
                        Scopes.UserReadPrivate,
                        Scopes.UserModifyPlaybackState,
                        Scopes.PlaylistModifyPublic,
                        Scopes.PlaylistModifyPrivate,
                        Scopes.PlaylistReadPrivate,
                        Scopes.UserLibraryModify,
                        Scopes.UserLibraryRead
                    }
                };
            Uri uri = request.ToUri();

            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine(@"Unable to open URL, manually open: {0}", uri);
            }
        }

        private static async void AuthTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await RefreshTokens();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private static async Task RefreshTokens()
        {
            // 1) Get new tokens (background thread is fine)
            OAuthClient oauth = new OAuthClient();
            TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
                new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                Settings.Settings.SpotifyRefreshToken
            );
            AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

            // Avoid logging secrets in production:
            Debug.WriteLine("We got a refreshed access token from server.");

            // 2) Update settings with new tokens
            Settings.Settings.SpotifyAccessToken = refreshResponse.AccessToken;
            if (!string.IsNullOrEmpty(refreshResponse.RefreshToken))
                Settings.Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;

            // 3) Build client using the *updated* tokens
            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault().WithAuthenticator(
                new AuthorizationCodeAuthenticator(
                    Settings.Settings.ClientId,
                    Settings.Settings.ClientSecret,
                    new AuthorizationCodeTokenResponse
                    {
                        AccessToken = Settings.Settings.SpotifyAccessToken,
                        RefreshToken = Settings.Settings.SpotifyRefreshToken
                    }
                )
            );

            Client = new SpotifyClient(config);

            // 4) Marshal any UI/DispatcherTimer work to the UI thread
            Application app = Application.Current;
            if (app != null)
            {
                await app.Dispatcher.InvokeAsync(async () =>
                {
                    // If AuthTimer is a DispatcherTimer, start it on its owning Dispatcher (the UI thread)
                    if (!AuthTimer.Enabled) // or .Enabled if it's a different timer type
                        AuthTimer.Start();
                    GlobalObjects.SpotifyProfile = await GetUser();
                    Settings.Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;
                    Logger.LogStr(
                        $"SPOTIFY: Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
                    Logger.LogStr($"SPOTIFY: Account Type: {GlobalObjects.SpotifyProfile.Product}");

                    if (app.MainWindow is MainWindow mw)
                    {
                        mw.IconWebSpotify.Foreground = Brushes.GreenYellow;
                        //mw.IconWebSpotify.Kind = PackIconBoxIconsKind.LogosSpotify;
                    }

                    await EnsurePlaylistCacheAsync(null, true);
                });
            }
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            OAuthClient oauth = new OAuthClient();
            TokenSwapTokenRequest tokenRequest = new TokenSwapTokenRequest(
                new Uri($"{BaseUrl}/swap?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                response.Code);
            AuthorizationCodeTokenResponse tokenResponse = await oauth.RequestToken(tokenRequest);
            Logger.LogStr(
                $"SPOTIFY: We got an access token from server: {tokenResponse.AccessToken.Substring(0, 6)}...");

            TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
                new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                tokenResponse.RefreshToken
            );
            AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

            Logger.LogStr(
                $"SPOTIFY: We got a new refreshed access token from server: {refreshResponse.AccessToken.Substring(0, 6)}...");

            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator(Settings.Settings.ClientId,
                    Settings.Settings.ClientSecret, tokenResponse));

            Settings.Settings.SpotifyAccessToken = tokenResponse.AccessToken;
            Settings.Settings.SpotifyRefreshToken = tokenResponse.RefreshToken;

            Client = new SpotifyClient(config);
            if (!AuthTimer.Enabled)
            {
                AuthTimer.Start();
            }

            // We are authenticated!
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(async void () =>
                {
                    try
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() == typeof(Window_Settings))
                                await ((Window_Settings)window).SetControls();
                        }

                        if (Application.Current.MainWindow == null) return;
                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                            Brushes.GreenYellow;
                        //((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                        //    PackIconBoxIconsKind.LogosSpotify;
                        GlobalObjects.SpotifyProfile = await GetUser();
                        Settings.Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;
                        Logger.LogStr(
                            $"SPOTIFY: Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
                        Logger.LogStr($"SPOTIFY: Account Type: {GlobalObjects.SpotifyProfile.Product}");

                        if (GlobalObjects.SpotifyProfile.Product == "premium") return;

                        if (!Settings.Settings.HideSpotifyPremiumWarning)
                            await ShowPremiumRequiredDialogAsync();

                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                            Brushes.DarkOrange;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                    }
                }));

            await EnsurePlaylistCacheAsync(null, true);
        }

        public static async Task ShowPremiumRequiredDialogAsync()
        {
            MetroDialogSettings dialogSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Don't Show Again",
                AnimateShow = true,
                AnimateHide = true,
            };

            // You need a reference to the dialog host (usually the main window)
            MetroWindow mainWindow = (Application.Current.MainWindow as MetroWindow);
            if (mainWindow == null)
                return;

            MessageDialogResult result = await mainWindow.ShowMessageAsync(
                "Spotify Premium required",
                "Spotify Premium is required to perform song requests. Songify was unable to verify your Spotify Premium status.",
                MessageDialogStyle.AffirmativeAndNegative,
                dialogSettings);

            if (result == MessageDialogResult.Negative)
            {
                Settings.Settings.HideSpotifyPremiumWarning = true;
            }
        }

        public static async Task<TrackInfo> GetSongInfo()
        {
            if (Client == null) return null;

            try
            {
                // 1) Track/progress (1/sec ok)
                CurrentlyPlaying playback = await ApiCallMeter.RunAsync(
                    "Player.GetCurrentlyPlaying",
                    () => Client.Player.GetCurrentlyPlaying(
                        new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track)
                        {
                            Market = "from_token"
                        }),
                    softLimitPerMinute: 60
                );

                if (playback == null || playback.Item is not FullTrack track || playback.ProgressMs == null)
                    return null;

                // 2) Playback state / device (cache 5s)
                CurrentlyPlayingContext currentPlayback;
                if ((DateTime.UtcNow - _cachedPlaybackAt) > PlaybackCacheTtl || _cachedPlayback == null)
                {
                    try
                    {
                        currentPlayback = await ApiCallMeter.RunAsync(
                            "Player.GetCurrentPlayback",
                            () => Client.Player.GetCurrentPlayback(new PlayerCurrentPlaybackRequest
                            { Market = "from_token" }),
                            softLimitPerMinute: 12 // ~ once per 5s
                        );
                    }
                    catch (APIException apiEx) when ((int)apiEx.Response.StatusCode is 404 or 204)
                    {
                        currentPlayback = null;
                    }

                    _cachedPlayback = currentPlayback;
                    _cachedPlaybackAt = DateTime.UtcNow;
                }
                else
                {
                    currentPlayback = _cachedPlayback;
                }

                if (currentPlayback?.Device?.Id != null &&
                    Settings.Settings.SpotifyDeviceId != currentPlayback.Device.Id)
                {
                    Settings.Settings.SpotifyDeviceId = currentPlayback.Device.Id;
                }

                // 3) Artists / progress math
                string artists = string.Join(", ", track.Artists.Select(a => a.Name));
                List<Image> albums = track.Album?.Images ?? [];
                double totalSeconds = track.DurationMs / 1000.0;
                double currentSeconds = (double)playback.ProgressMs / 1000.0;
                int percentage = totalSeconds == 0 ? 0 : (int)(100 * currentSeconds / totalSeconds);

                // 4) Playlist context (only when ID changes; cache 10 min)
                PlaylistInfo playlistInfo = null;

                return new TrackInfo
                {
                    Artists = artists,
                    Title = track.Name,
                    Albums = albums.ToList(),
                    SongId = track.Id,
                    DurationMs = (int)(track.DurationMs - playback.ProgressMs),
                    IsPlaying = playback.IsPlaying,
                    Url = "https://open.spotify.com/track/" + track.Id,
                    DurationPercentage = percentage,
                    DurationTotal = track.DurationMs,
                    Progress = (int)playback.ProgressMs,
                    Playlist = playlistInfo,
                    FullArtists = track.Artists.ToList()
                };
            }
            catch (APIException apiEx)
            {
                Logger.LogStr("ERROR: Couldn't fetch song info");
                Logger.LogStr($"Spotify API error {(int)apiEx.Response.StatusCode}: {apiEx.Message}");
                if (!string.IsNullOrEmpty((string)(apiEx.Response?.Body)))
                    Logger.LogStr((string)apiEx.Response.Body);
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Couldn't fetch song info (unexpected)");
                Logger.LogExc(ex);
            }

            return null;
        }

        public static async Task<bool> AddToQueue(string songUri)
        {
            if (Client == null)
                return false;
            try
            {
                await ApiCallMeter.RunAsync("Player.AddToQueue", () => Client.Player.AddToQueue(
                    new PlayerAddToQueueRequest(songUri)
                    {
                        DeviceId = Settings.Settings.SpotifyDeviceId
                    }), softLimitPerMinute: softLimitPerminute);
                return true;
            }
            catch (APIException ex)
            {
                if (ex.Response != null && (int)ex.Response.StatusCode == 503)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        await Task.Delay(1000);
                        try
                        {
                            await Client.Player.AddToQueue(new PlayerAddToQueueRequest(songUri)
                            {
                                DeviceId = Settings.Settings.SpotifyDeviceId
                            });
                            return true;
                        }
                        catch (APIException retryEx)
                        {
                            if (retryEx.Response != null && (int)retryEx.Response.StatusCode != 503)
                                break;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return false;
            }
        }

        public static async Task<FullTrack> GetTrack(string id)
        {
            if (Client == null)
                return null;
            try
            {
                return await ApiCallMeter.RunAsync("Tracks.Get", () => Client.Tracks.Get(id), softLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        //public static async Task<FullTrack> FindTrack(string query)
        //{
        //    if (Client == null)
        //        return null;
        //    try
        //    {
        //        SearchRequest request = new(SearchRequest.Types.Track, query) { Limit = 1 };
        //        SearchResponse result = await ApiCallMeter.RunAsync("Search.Item", () => Client.Search.Item(request),
        //            softLimitPerminute);

        //        return result.Tracks is { Items.Count: > 0 } ? result.Tracks.Items[0] : null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExc(ex);
        //        return null;
        //    }
        //}

        public static async Task<FullTrack> FindTrack(
            string query,
            int take = 10,
            int confidenceThreshold = 60)
        {
            if (Client == null)
                return null;

            if (string.IsNullOrWhiteSpace(query))
                return null;

            try
            {
                SearchRequest request = new SearchRequest(SearchRequest.Types.Track, query)
                {
                    Limit = take
                };

                SearchResponse result = await ApiCallMeter.RunAsync(
                    "Search.Item",
                    () => Client.Search.Item(request),
                    softLimitPerminute);

                List<FullTrack> tracks = result.Tracks?.Items?.Take(take).ToList();
                if (tracks == null || tracks.Count == 0)
                    return null;

                List<ParsedQuery> interpretations = GenerateInterpretations(query);

                var scored = tracks
                    .Select(t => new
                    {
                        Track = t,
                        Score = ScoreTrack(t, interpretations)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                // ---- LOGGING ----
                Logger.LogStr($"Search '{query}' - Found {scored.Count} track candidates:");
                int rank = 1;
                foreach (var item in scored)
                {
                    string artists = string.Join(", ", item.Track.Artists.Select(a => a.Name));

                    Logger.LogStr($"  #{rank}: {item.Track.Name} - {artists} | Score: {item.Score}");

                    // Log each interpretation's score
                    foreach (ParsedQuery pq in interpretations)
                    {
                        int interpScore = ScoreTrackForInterpretation(item.Track, pq);

                        Logger.LogStr($"      -> [{pq.SourceHint}] " +
                                       $"Title='{pq.TitleCandidate}' Artist='{pq.ArtistCandidate}' " +
                                       $"Score={interpScore}");
                    }

                    rank++;
                }

                // -------------------

                var best = scored.First();

                // Optional: inspect these logs to tune threshold later
                // Logger.LogInfo($"Search '{query}' best match: \"{best.Track.Name}\" " +
                //                $"by {string.Join(", ", best.Track.Artists.Select(a => a.Name))} (Score {best.Score})");

                if (best.Score < confidenceThreshold)
                {
                    // Fallback: if our fuzzy match is weak, use Spotify's first result (what you currently do)
                    FullTrack fallback = tracks.First();
                    // Logger.LogInfo($"Score {best.Score} < {confidenceThreshold}, falling back to first result \"{fallback.Name}\"");
                    return fallback;
                }

                return best.Track;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<bool> AddToPlaylist(string trackId)
        {
            if (Client == null)
                return false;

            try
            {
                // No playlist configured -> save to library
                if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                    Settings.Settings.SpotifyPlaylistId == "-1")
                {
                    await Client.Library.SaveTracks(
                        new LibrarySaveTracksRequest(new List<string> { trackId })
                    );
                    return false;
                }

                // Make sure cache is filled once
                await EnsurePlaylistCacheAsync();

                lock (_playlistLock)
                {
                    if (_playlistTrackIds.Contains(trackId))
                    {
                        // Already in playlist
                        return true;
                    }
                }

                // Not in playlist -> add
                PlaylistAddItemsRequest request = new(new List<string> { "spotify:track:" + trackId });

                await ApiCallMeter.RunAsync("Playlists.AddItems",
                    () => Client.Playlists.AddItems(Settings.Settings.SpotifyPlaylistId, request),
                    softLimitPerminute);

                // Update cache
                lock (_playlistLock)
                {
                    _playlistTrackIds.Add(trackId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogStr("Error adding song to playlist");
                Logger.LogExc(ex);
                return true;
            }
        }

        public static async Task SkipSong()
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.SkipNext", () => Client.Player.SkipNext(), softLimitPerminute);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static async Task<QueueResponse> GetQueueInfo()
        {
            if (Client == null)
                return null;
            try
            {
                return await ApiCallMeter.RunAsync("Player.GetQueue", () => Client.Player.GetQueue(),
                    softLimitPerminute);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task SkipPrevious()
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.SkipPrevius", () => Client.Player.SkipPrevious(),
                    softLimitPerminute);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static async Task PlayFromStart()
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.SeekTo", () => Client.Player.SeekTo(new PlayerSeekToRequest(0)
                {
                    DeviceId = Settings.Settings.SpotifyDeviceId
                }), softLimitPerminute);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static async Task<bool> PlayPause(PlaybackAction action = Toggle)
        {
            if (Client == null)
                return false;
            try
            {
                CurrentlyPlayingContext playback = await ApiCallMeter.RunAsync("Player.GetCurrentPlayback",
                    () => Client.Player.GetCurrentPlayback(), softLimitPerminute);

                bool isPlaying = playback is { IsPlaying: true };

                if (action == Toggle)
                {
                    action = isPlaying ? Pause : Play;
                }

                switch (action)
                {
                    case Pause when isPlaying:
                        await ApiCallMeter.RunAsync("Player.PausePlayback", () =>
                            Client.Player.PausePlayback(new PlayerPausePlaybackRequest
                            {
                                DeviceId = Settings.Settings.SpotifyDeviceId
                            }), softLimitPerminute);
                        return false;

                    case Play when !isPlaying:
                        await ApiCallMeter.RunAsync("Player.ResumePlayback", () =>
                            Client.Player.ResumePlayback(new PlayerResumePlaybackRequest
                            {
                                DeviceId = Settings.Settings.SpotifyDeviceId
                            }), softLimitPerminute);
                        return true;
                    // ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis
                    case Toggle:
                        // Heuristically unreachable but included for completeness
                        break;

                    default:
                        return isPlaying;
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return false;
            }

            return false;
        }

        public static async Task<CurrentlyPlayingContext> GetPlayback()
        {
            if (Client == null)
                return null;
            try
            {
                return await ApiCallMeter.RunAsync("Player.GetCurrentPlayback",
                    () => Client.Player.GetCurrentPlayback(), softLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<List<bool>> CheckLibrary(List<string> tracks)
        {
            if (Client == null)
                return null;
            try
            {
                List<bool> response = await ApiCallMeter.RunAsync("Library.GetTracks",
                    () => Client.Library.CheckTracks(new LibraryCheckTracksRequest(tracks)), softLimitPerminute);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<Paging<PlaylistTrack<IPlayableItem>>> GetPlaylistTracks(string playlistId)
        {
            if (Client == null)
                return null;
            try
            {
                Paging<PlaylistTrack<IPlayableItem>> tracks = await ApiCallMeter.RunAsync("Playlists.GetItems",
                    () => Client.Playlists.GetItems(playlistId), softLimitPerminute);
                return tracks;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<FullPlaylist> GetPlaylist(string spotifyPlaylistId)
        {
            if (Client == null)
                return null;
            try
            {
                FullPlaylist playlist = await ApiCallMeter.RunAsync("Playlists.Get",
                    () => Client.Playlists.Get(spotifyPlaylistId), softLimitPerminute);
                return playlist;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<bool> SetVolume(int vol)
        {
            if (Client == null)
                return false;
            try
            {
                return await ApiCallMeter.RunAsync("Player.SetVolune",
                    () => Client.Player.SetVolume(new PlayerVolumeRequest(vol)), softLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return false;
            }
        }

        public static async Task<List<FullArtist>> GetArtist(string search)
        {
            if (Client == null)
                return null;
            try
            {
                SearchRequest request = new(SearchRequest.Types.Artist, search) { Limit = 1 };
                SearchResponse result = await ApiCallMeter.RunAsync("Search.Item", () => Client.Search.Item(request),
                    softLimitPerminute);
                return result.Artists.Items;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<PrivateUser> GetUser()
        {
            if (Client == null)
                return null;
            try
            {
                PrivateUser user = await ApiCallMeter.RunAsync("UserProfile.Current",
                    () => Client.UserProfile.Current(), softLimitPerminute);
                return user;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<Paging<FullPlaylist>> GetUserPlaylists()
        {
            if (Client == null)
                return null;
            try
            {
                Paging<FullPlaylist> playlists = await ApiCallMeter.RunAsync("Playlists.CurrentUsers",
                    () => Client.Playlists.CurrentUsers(), softLimitPerminute);
                return playlists;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<string> GetDeviceNameForId(string spotifyDeviceId)
        {
            if (Client == null)
                return null;
            try
            {
                DeviceResponse x = await ApiCallMeter.RunAsync("Player.GetAvailableDevices",
                    () => Client.Player.GetAvailableDevices(), softLimitPerminute);
                return x.Devices.FirstOrDefault(d => d.IsActive)?.Name;
            }
            catch (Exception e)
            {
                Logger.LogStr("SPOTIFY API: Couldn't get device");
            }

            return "No device found";
        }

        private static (int? status, string message) TryExtractSpotifyError(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return (null, null);
            try
            {
                JObject jo = JObject.Parse(body);
                JToken err = jo["error"];
                if (err == null) return (null, null);

                int? status = err["status"]?.Type switch
                {
                    JTokenType.Integer => (int?)err["status"],
                    JTokenType.String => int.TryParse((string)err["status"], out int n) ? n : (int?)null,
                    _ => null
                };
                string message = (string)(err["message"] ?? jo["error_description"]) ?? "";
                return (status, message);
            }
            catch
            {
                // Body wasn’t JSON; ignore
                return (null, null);
            }
        }

        public static async Task<PlaylistInfo> GetPlaybackPlaylist()
        {
            if (Client == null)
                return null;
            try
            {
                // Get current playback
                CurrentlyPlayingContext playback = await ApiCallMeter.RunAsync("Player.GetCurrentPlayback",
                    () => Client.Player.GetCurrentPlayback(), softLimitPerminute);
                if (playback?.Context?.Type != "playlist" || playback.Context.Uri == null)
                {
                    _cachedPlaylistId = null;
                    _cachedPlaylistInfo = null;
                    return null;
                }

                string playlistId = playback.Context.Uri.Split(':').Last();
                if (playlistId == _cachedPlaylistId &&
                    (DateTime.UtcNow - _cachedPlaylistFetchedAt) < PlaylistCacheTtl &&
                    _cachedPlaylistInfo != null)
                {
                    return _cachedPlaylistInfo; // Cache hit
                }

                FullPlaylist playlist = await ApiCallMeter.RunAsync("Playlists.Get",
                    () => Client.Playlists.Get(playlistId), softLimitPerminute);
                if (playlist == null)
                {
                    _cachedPlaylistId = null;
                    _cachedPlaylistInfo = null;
                    return null;
                }

                _cachedPlaylistId = playlistId;
                _cachedPlaylistInfo = new PlaylistInfo
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Owner = playlist.Owner?.DisplayName ?? playlist.Owner?.Id ?? "unknown",
                    Url = playlist.ExternalUrls?["spotify"]
                };
                _cachedPlaylistFetchedAt = DateTime.UtcNow;
                return _cachedPlaylistInfo;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public static async Task SetShuffle(bool b)
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.SetShuffle", () => Client.Player.SetShuffle(
                    new PlayerShuffleRequest(b)
                    {
                        DeviceId = Settings.Settings.SpotifyDeviceId
                    }), softLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static async Task PlayFromPlaylist(string playlistId)
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.ResumePlayback", () => Client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        DeviceId = Settings.Settings.SpotifyDeviceId,
                        ContextUri = playlistId.Contains("spotify:playlist") ? playlistId : "spotify:playlist:" + playlistId,
                        PositionMs = 0
                    }), softLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private class ParsedQuery
        {
            public string Raw { get; set; } = "";
            public string? TitleCandidate { get; set; }
            public string? ArtistCandidate { get; set; }
            public string SourceHint { get; set; } = "";
        }

        /// <summary>
        /// Unicode-aware normalization: lowercases, strips accents, removes punctuation.
        /// Keeps letters from all languages and digits.
        /// </summary>
        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Strip diacritics (é -> e, ö -> o, etc.)
            string formD = input.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char ch in formD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            string noAccents = sb.ToString().Normalize(NormalizationForm.FormC);

            // Lowercase
            noAccents = noAccents.ToLowerInvariant();

            // Keep only letters (any language), digits, whitespace
            noAccents = Regex.Replace(noAccents, @"[^\p{L}\p{Nd}\s]", " ");

            // Collapse whitespace
            noAccents = Regex.Replace(noAccents, @"\s+", " ").Trim();

            return noAccents;
        }

        private static int Similarity(string a, string b)
        {
            a = Normalize(a);
            b = Normalize(b);

            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0;

            // 0–100
            return Fuzz.TokenSetRatio(a, b);
        }

        /// <summary>
        /// Generate multiple interpretations so "by" in titles doesn't break us.
        /// </summary>
        private static List<ParsedQuery> GenerateInterpretations(string query)
        {
            List<ParsedQuery> list = new List<ParsedQuery>();
            string raw = query.Trim();

            // 1) No structure assumption
            list.Add(new ParsedQuery
            {
                Raw = raw,
                TitleCandidate = null,
                ArtistCandidate = null,
                SourceHint = "none"
            });

            string qLower = raw.ToLowerInvariant();

            // 2) Artist - Title pattern
            if (qLower.Contains(" - "))
            {
                string[] parts = raw.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    list.Add(new ParsedQuery
                    {
                        Raw = raw,
                        ArtistCandidate = parts[0],
                        TitleCandidate = string.Join(" - ", parts.Skip(1)),
                        SourceHint = "dash"
                    });
                }
            }

            // 3) Title by Artist pattern – only as a *candidate*
            int byIndex = qLower.LastIndexOf(" by ", StringComparison.Ordinal);
            if (byIndex > 0)
            {
                string title = raw.Substring(0, byIndex);
                string artist = raw.Substring(byIndex + 4);

                list.Add(new ParsedQuery
                {
                    Raw = raw,
                    TitleCandidate = title,
                    ArtistCandidate = artist,
                    SourceHint = "by"
                });
            }

            // 4) Heuristic: first word = artist, rest = title
            string[] tokens = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2)
            {
                // first word as artist
                string firstArtist = tokens[0];
                string firstTitle = string.Join(" ", tokens.Skip(1));

                list.Add(new ParsedQuery
                {
                    Raw = raw,
                    ArtistCandidate = firstArtist,
                    TitleCandidate = firstTitle,
                    SourceHint = "first-word-artist"
                });

                // last word as artist (handles "live forever headhunterz")
                string lastArtist = tokens[tokens.Length - 1];
                string lastTitle = string.Join(" ", tokens.Take(tokens.Length - 1));

                list.Add(new ParsedQuery
                {
                    Raw = raw,
                    ArtistCandidate = lastArtist,
                    TitleCandidate = lastTitle,
                    SourceHint = "last-word-artist"
                });
            }

            return list;
        }

        private static int ScoreTrackForInterpretation(FullTrack track, ParsedQuery pq)
        {
            string title = track.Name ?? "";
            string artists = string.Join(" ", track.Artists?.Select(a => a.Name ?? "") ?? Array.Empty<string>());
            string full = $"{title} {artists}";

            int titleScore = 0;
            int artistScore = 0;
            int fullScore = Similarity(pq.Raw, full);

            if (!string.IsNullOrWhiteSpace(pq.TitleCandidate))
                titleScore = Similarity(pq.TitleCandidate!, title);

            if (!string.IsNullOrWhiteSpace(pq.ArtistCandidate))
                artistScore = Similarity(pq.ArtistCandidate!, artists);

            double score;

            if (pq.TitleCandidate != null && pq.ArtistCandidate != null)
            {
                // We think we know both artist & title
                score = 0.5 * titleScore + 0.3 * artistScore + 0.2 * fullScore;
            }
            else
            {
                // No strong structure → rely more on full query
                score = 0.7 * fullScore + 0.3 * Math.Max(titleScore, artistScore);
            }

            return (int)Math.Round(score);
        }

        private static int ScoreTrack(FullTrack track, List<ParsedQuery> interpretations)
        {
            int best = 0;

            foreach (ParsedQuery pq in interpretations)
            {
                int s = ScoreTrackForInterpretation(track, pq);
                if (s > best)
                    best = s;
            }

            return best;
        }

        public static async Task EnsurePlaylistCacheAsync(Window_Settings windowSettings = null, bool force = false)
        {
            if (Client == null)
                return;

            if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                Settings.Settings.SpotifyPlaylistId == "-1")
                return;

            // Fast path: already initialized
            if (_playlistCacheInitialized && !force)
                return;

            lock (_playlistLock)
            {
                if (_playlistCacheInitialized && !force)
                    return;
                _playlistCacheInitialized = true; // mark as initialized; actual fill happens below
            }

            if (windowSettings != null)
            {
                windowSettings.TbGridLoading.Text = "Caching Playlist: 0";
                windowSettings.GridLoading.Visibility = Visibility.Visible;
            }

            Stopwatch sw = new();

            Logger.LogStr("SPOTIFY: Started caching playlist");
            sw.Reset();
            sw.Start();
            try
            {
                _playlistTrackIds.Clear();

                // Basic pagination – adapt to your SpotifyAPI-NET overloads
                Paging<PlaylistTrack<IPlayableItem>> page = await ApiCallMeter.RunAsync("Playlists.GetItems",
                    () => Client.Playlists.GetItems(Settings.Settings.SpotifyPlaylistId),
                    softLimitPerminute);

                int counter = 0;

                while (page?.Items is { Count: > 0 })
                {
                    foreach (PlaylistTrack<IPlayableItem> item in page.Items)
                    {
                        if (item.Track is FullTrack fullTrack && !string.IsNullOrEmpty(fullTrack.Id))
                            _playlistTrackIds.Add(fullTrack.Id);

                        counter++;
                        if (windowSettings == null) continue;
                        if (counter % 25 == 0) // update UI every 25 items
                        {
                            await windowSettings.Dispatcher.InvokeAsync(() =>
                            {
                                windowSettings.TbGridLoading.Text =
                                    $"Caching Playlist: {_playlistTrackIds.Count}";
                            }, DispatcherPriority.Background);
                        }
                    }

                    if (page.Next == null)
                        break;

                    page = await ApiCallMeter.RunAsync("Client.NextPage", () => Client.NextPage(page), softLimitPerminute);
                }

            }
            catch (Exception ex)
            {
                Logger.LogStr("Error initializing playlist cache");
                Logger.LogExc(ex);
                // If you want, you can set _playlistCacheInitialized = false here to retry later
            }
            finally
            {
                sw.Stop();
                Logger.LogStr($"SPOTIFY: Finished caching playlist ({_playlistTrackIds.Count} tracks in {sw.Elapsed.Seconds}s)");
                if (windowSettings != null)
                {
                    windowSettings.TbGridLoading.Text = "Getting things ready";
                    windowSettings.GridLoading.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}