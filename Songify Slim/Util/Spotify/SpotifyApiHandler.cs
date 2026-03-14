using FuzzySharp;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using static Songify_Slim.Util.General.Enums;
using static Songify_Slim.Util.General.Enums.PlaybackAction;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace Songify_Slim.Util.Spotify
{
    public static class SpotifyApiHandler
    {
        private static EmbedIOAuthServer _server;
        public static SpotifyClient Client;

        private const int SoftLimitPerminute = 60;

        private static readonly Timer AuthTimer = new()
        {
            Interval = 1000 * 60 * 10,
        };

        private static string _cachedPlaylistId;
        private static PlaylistInfo _cachedPlaylistInfo;
        private static DateTime _cachedPlaylistFetchedAt = DateTime.MinValue;
        private static readonly TimeSpan PlaylistCacheTtl = TimeSpan.FromMinutes(10);

        private static readonly SpotifyOEmbedClient OEmbedClient = new();

        private static string _state;
        private static string _codeVerifier;

        private static readonly List<string> SpotifyScopes =
        [
            Scopes.UserReadPlaybackState,
            Scopes.UserReadPrivate,
            Scopes.UserModifyPlaybackState,
            Scopes.PlaylistModifyPublic,
            Scopes.PlaylistModifyPrivate,
            Scopes.PlaylistReadPrivate,
            Scopes.UserLibraryModify,
            Scopes.UserLibraryRead
        ];

        public static void ResetSpotifyAuthState()
        {
            Settings.SpotifyAccessToken = "";
            Settings.SpotifyRefreshToken = "";
            Settings.SpotifyTokenExpiresAt = 0;
            Settings.SpotifyProfile = null;

            GlobalObjects.SpotifyProfile = null;
            Client = null;

            if (AuthTimer.Enabled)
                AuthTimer.Stop();
        }

        public static async Task Auth()
        {
            try
            {
                AuthTimer.Elapsed -= AuthTimer_Elapsed;
                AuthTimer.Elapsed += AuthTimer_Elapsed;

                if (!string.IsNullOrWhiteSpace(Settings.SpotifyRefreshToken))
                {
                    Logger.Info(LogSource.Spotify, "Refreshing tokens");
                    await RefreshTokens();
                    return;
                }
                if (string.IsNullOrEmpty(Settings.ClientId)) return;
                Logger.Info(LogSource.Spotify, "Getting new tokens");
                await StartPkceLogin();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                Settings.SpotifyRefreshToken = string.Empty;
            }
        }

        private static async Task StartPkceLogin()
        {
            try
            {
                if (_server != null)
                {
                    try
                    {
                        await _server.Stop();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, LogSource.Spotify, "Unable to stop auth server", ex);
                    }

                    _server.AuthorizationCodeReceived -= OnAuthorizationCodeReceived;
                    _server.ErrorReceived -= OnAuthError;
                    _server = null;
                }

                _server = new EmbedIOAuthServer(
                    new Uri("http://127.0.0.1:4002/auth"),
                    4002,
                    Assembly.GetExecutingAssembly(),
                    "Songify_Slim.default_site");

                _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
                _server.ErrorReceived += OnAuthError;

                await _server.Start();

                (string verifier, string challenge) = PKCEUtil.GenerateCodes();
                _codeVerifier = verifier;
                _state = Guid.NewGuid().ToString("N");

                LoginRequest request = new(_server.BaseUri, Settings.ClientId, LoginRequest.ResponseType.Code)
                {
                    Scope = SpotifyScopes,
                    State = _state,
                    CodeChallengeMethod = "S256",
                    CodeChallenge = challenge
                };

                Uri uri = request.ToUri();

                Logger.Debug(LogSource.Spotify, $"OAuth state: {_state}");
                Logger.Debug(LogSource.Spotify, $"Auth URL: {uri}");

                try
                {
                    await Task.Delay(300);
                    BrowserUtil.Open(uri);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    Logger.Error(LogSource.Spotify, $"Unable to open browser automatically. Please open manually: {uri}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                throw;
            }
        }

        private static Task OnAuthError(object sender, string error, string receivedState)
        {
            try
            {
                Logger.Error(LogSource.Spotify, "Spotify authorization failed.");
                Logger.Error(LogSource.Spotify, $"OAuth Error: {error}");
                Logger.Error(LogSource.Spotify, $"Received State: {receivedState}");
                Logger.Error(LogSource.Spotify, $"Original State: {_state}");
                Logger.Error(LogSource.Spotify, $"ClientId: {Settings.ClientId}");
                Logger.Error(LogSource.Spotify, $"RedirectUri: {_server?.BaseUri}");
                Logger.Error(LogSource.Spotify, $"Requested scopes: {string.Join(", ", SpotifyScopes)}");
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }

            return Task.CompletedTask;
        }

        private static async void AuthTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (IsTokenExpiringSoon())
                    await RefreshTokens();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private static bool IsTokenExpiringSoon()
        {
            try
            {
                if (Settings.SpotifyTokenExpiresAt <= 0)
                    return true;

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long headroom = (long)TimeSpan.FromMinutes(10).TotalMilliseconds;

                return now + headroom >= Settings.SpotifyTokenExpiresAt;
            }
            catch
            {
                return true;
            }
        }

        private static async Task RefreshTokens()
        {
            if (string.IsNullOrWhiteSpace(Settings.SpotifyRefreshToken))
                throw new InvalidOperationException("No Spotify refresh token available.");

            OAuthClient oauth = new();

            Logger.Info(LogSource.Spotify, "Requesting refreshed PKCE token");

            PKCETokenResponse refreshResponse =
                await oauth.RequestToken(new PKCETokenRefreshRequest(Settings.ClientId, Settings.SpotifyRefreshToken));

            if (string.IsNullOrWhiteSpace(refreshResponse.AccessToken))
                throw new Exception("Spotify returned an empty access token during refresh.");

            Settings.SpotifyAccessToken = refreshResponse.AccessToken;

            if (!string.IsNullOrWhiteSpace(refreshResponse.RefreshToken))
                Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;

            Settings.SpotifyTokenExpiresAt = DateTimeOffset.UtcNow
                .AddSeconds(refreshResponse.ExpiresIn)
                .ToUnixTimeMilliseconds();

            await ApplyAuthenticatedStateAsync(refreshResponse);
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            try
            {
                Logger.Log(LogLevel.Debug, LogSource.Spotify, "Entered OnAuthorizationCodeReceived");

                if (response == null)
                    throw new Exception("AuthorizationCodeResponse was null.");

                if (string.IsNullOrWhiteSpace(response.Code))
                    throw new Exception("Spotify returned an empty authorization code.");

                if (!string.Equals(response.State, _state, StringComparison.Ordinal))
                    throw new Exception("Spotify OAuth state mismatch.");

                OAuthClient oauth = new();

                Logger.Log(LogLevel.Debug, LogSource.Spotify, "Sending PKCE token request");

                PKCETokenResponse tokenResponse = await oauth.RequestToken(
                    new PKCETokenRequest(
                        Settings.ClientId,
                        response.Code,
                        new Uri(_server.BaseUri.ToString()),
                        _codeVerifier
                    )
                );

                if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                    throw new Exception("Spotify returned an empty access token.");

                Settings.SpotifyAccessToken = tokenResponse.AccessToken;
                Settings.SpotifyRefreshToken = tokenResponse.RefreshToken;
                Settings.SpotifyTokenExpiresAt = DateTimeOffset.UtcNow
                    .AddSeconds(tokenResponse.ExpiresIn)
                    .ToUnixTimeMilliseconds();

                await ApplyAuthenticatedStateAsync(tokenResponse);

                Logger.Info(LogSource.Spotify, "Spotify authentication completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            finally
            {
                try
                {
                    if (_server != null)
                    {
                        _server.AuthorizationCodeReceived -= OnAuthorizationCodeReceived;
                        _server.ErrorReceived -= OnAuthError;
                        await _server.Stop();
                        _server = null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }
        }

        private static async Task ApplyAuthenticatedStateAsync(PKCETokenResponse tokenResponse)
        {
            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(new PKCEAuthenticator(Settings.ClientId, tokenResponse));

            Client = new SpotifyClient(config);

            if (!AuthTimer.Enabled)
                AuthTimer.Start();

            Application app = Application.Current;
            if (app == null)
                return;

            await app.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Window_Settings ws)
                            await ws.SetControls();
                    }

                    GlobalObjects.SpotifyProfile = await GetUser();
                    Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;

                    Logger.Info(LogSource.Spotify, $"Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
                    Logger.Info(LogSource.Spotify, $"Account Type: {GlobalObjects.SpotifyProfile.Product}");

                    if (app.MainWindow is MainWindow mw)
                        mw.IconWebSpotify.Foreground = Brushes.GreenYellow;

                    if (GlobalObjects.SpotifyProfile.Product != "premium")
                    {
                        if (!Settings.HideSpotifyPremiumWarning)
                            await ShowPremiumRequiredDialogAsync();

                        if (app.MainWindow is MainWindow mainWindow)
                            mainWindow.IconWebSpotify.Foreground = Brushes.DarkOrange;
                    }

                    ApiCallMeter.ReleaseRateLimit();
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }, DispatcherPriority.Normal);
        }

        public static void LogoutSpotify()
        {
            try
            {
                Settings.SpotifyAccessToken = null;
                Settings.SpotifyRefreshToken = null;
                Settings.SpotifyTokenExpiresAt = 0;
                Settings.SpotifyProfile = null;
                GlobalObjects.SpotifyProfile = null;
                Client = null;

                if (AuthTimer.Enabled)
                    AuthTimer.Stop();

                if (Application.Current?.MainWindow is MainWindow mw)
                    mw.IconWebSpotify.Foreground = Brushes.Gray;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static async Task ShowPremiumRequiredDialogAsync()
        {
            MetroDialogSettings dialogSettings = new()
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
                Settings.HideSpotifyPremiumWarning = true;
            }
        }

        public static async Task<TrackInfo> GetSongInfo()
        {
            var client = Client;
            if (client?.Player == null)
            {
                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 0: client or client.Player is null");
                return null;
            }

            CurrentlyPlayingContext playback = null;
            FullTrack track = null;

            try
            {
                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 1: before API call");

                playback = await ApiCallMeter.RunAsync(
                    "Player.GetCurrentPlayback",
                    () => client.Player.GetCurrentPlayback(new PlayerCurrentPlaybackRequest
                    {
                        Market = "from_token"
                    }),
                    softLimitPerMinute: 60
                );

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 2: API call returned");

                if (playback == null)
                {
                    Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 2a: playback is null");
                    return null;
                }

                if (playback.Item == null)
                {
                    Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 2b: playback.Item is null");
                    return null;
                }

                track = playback.Item as FullTrack;

                if (track == null)
                {
                    Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 2c: playback.Item is not FullTrack");
                    return null;
                }

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 3: track obtained");

                string deviceId = playback.Device?.Id;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 4: before deviceId update");

                if (!string.IsNullOrWhiteSpace(deviceId) && Settings.SpotifyDeviceId != deviceId)
                    Settings.SpotifyDeviceId = deviceId;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 5: deviceId processed");

                string artists = track.Artists != null
                    ? string.Join(", ", track.Artists.Where(a => a != null).Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
                    : string.Empty;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 6: artists parsed");

                List<Image> albums = track.Album?.Images?.ToList();

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 7: album images processed");

                int durationMs = track.DurationMs;
                int progressMs = playback.ProgressMs;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 8: duration/progress read");

                if (progressMs < 0)
                    progressMs = 0;

                if (progressMs > durationMs && durationMs > 0)
                    progressMs = durationMs;

                double totalSeconds = durationMs / 1000.0;
                double currentSeconds = progressMs / 1000.0;

                int percentage = totalSeconds <= 0
                    ? 0
                    : (int)(100 * currentSeconds / totalSeconds);

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 9: percentage calculated");

                string trackId = track.Id;

                string trackUrl = !string.IsNullOrWhiteSpace(trackId)
                    ? "https://open.spotify.com/track/" + trackId
                    : null;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 10: track URL created");

                PlaylistInfo playlistInfo = null;
                string contextUri = playback.Context?.Uri;

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 11: context uri read");

                if (!string.IsNullOrWhiteSpace(contextUri) &&
                    contextUri.StartsWith("spotify:playlist:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = contextUri.Split(':');
                    string playlistId = parts.Length > 0 ? parts[parts.Length - 1] : null;

                    if (!string.IsNullOrWhiteSpace(playlistId))
                    {
                        playlistInfo = new PlaylistInfo
                        {
                            Id = playlistId,
                            Url = $"https://open.spotify.com/playlist/{playlistId}"
                        };
                    }
                }

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 12: playlist info processed");

                Logger.Debug(LogSource.Spotify, "GetSongInfo checkpoint 13: creating TrackInfo");

                return new TrackInfo
                {
                    Artists = artists,
                    Title = track.Name,
                    Albums = albums,
                    SongId = trackId,
                    DurationMs = Math.Max(0, durationMs - progressMs),
                    IsPlaying = playback.IsPlaying,
                    Url = trackUrl,
                    DurationPercentage = percentage,
                    DurationTotal = durationMs,
                    Progress = progressMs,
                    Playlist = playlistInfo,
                    FullArtists = track.Artists?.Where(a => a != null).ToList() ?? new List<SimpleArtist>()
                };
            }
            catch (NullReferenceException ex)
            {
                Logger.Error(LogSource.Spotify, "NullReferenceException in GetSongInfo");
                Logger.Error(LogSource.Spotify, ex.ToString());

                Logger.Error(
                    LogSource.Spotify,
                    "State snapshot: " +
                    "ClientNull=" + (client == null) + ", " +
                    "ClientPlayerNull=" + (client?.Player == null) + ", " +
                    "PlaybackNull=" + (playback == null) + ", " +
                    "PlaybackItemNull=" + (playback?.Item == null) + ", " +
                    "PlaybackDeviceNull=" + (playback?.Device == null) + ", " +
                    "PlaybackContextNull=" + (playback?.Context == null) + ", " +
                    "TrackNull=" + (track == null) + ", " +
                    "TrackAlbumNull=" + (track?.Album == null) + ", " +
                    "TrackArtistsNull=" + (track?.Artists == null)
                );
            }
            catch (APIException apiEx)
            {
                Logger.Error(LogSource.Spotify, "Couldn't fetch song info");
                Logger.Error(LogSource.Spotify, apiEx.ToString());

                object responseBody = apiEx.Response?.Body;
                if (responseBody != null)
                    Logger.Error(LogSource.Spotify, responseBody.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Spotify, "Couldn't fetch song info (unexpected)");
                Logger.Error(LogSource.Spotify, ex.ToString());
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
                        DeviceId = Settings.SpotifyDeviceId
                    }), softLimitPerMinute: SoftLimitPerminute);
                return true;
            }
            catch (APIException ex)
            {
                if (ex.Response == null || (int)ex.Response.StatusCode != 503) return false;
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(1000);
                    try
                    {
                        await ApiCallMeter.RunAsync("Player.AddToQueue", () => Client.Player.AddToQueue(
                            new PlayerAddToQueueRequest(songUri)
                            {
                                DeviceId = Settings.SpotifyDeviceId
                            }), softLimitPerMinute: SoftLimitPerminute);
                        return true;
                    }
                    catch (APIException retryEx)
                    {
                        if (retryEx.Response != null && (int)retryEx.Response.StatusCode != 503)
                            break;
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
                return await ApiCallMeter.RunAsync("Tracks.Get", () => Client.Tracks.Get(id), SoftLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

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
                SearchRequest request = new(SearchRequest.Types.Track, query)
                {
                    Limit = take
                };

                SearchResponse result = await ApiCallMeter.RunAsync(
                    "Search.Item",
                    () => Client.Search.Item(request),
                    SoftLimitPerminute);

                List<FullTrack> tracks = result.Tracks.Items?.Take(take).ToList();
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

                Logger.Trace(LogSource.Spotify, $"Search '{query}' - Found {scored.Count} track candidates:");
                var best = scored.First();

                if (best.Score >= confidenceThreshold) return best.Track;
                FullTrack fallback = tracks.First();
                return fallback;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static List<string> EnsureTrackUris(IEnumerable<string> ids)
        {
            List<string> result = [];

            foreach (string value in ids)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                string v = value.Trim();

                // Already a Spotify URI
                if (v.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(v);
                    continue;
                }

                // Spotify URL
                if (v.Contains("open.spotify.com/track"))
                {
                    try
                    {
                        Uri uri = new(v);

                        string[] segments = uri.AbsolutePath
                            .Split(['/'], StringSplitOptions.RemoveEmptyEntries);

                        if (segments.Length >= 2 && segments[0].Equals("track", StringComparison.OrdinalIgnoreCase))
                        {
                            string id = segments[1];

                            int qIndex = id.IndexOf('?');
                            if (qIndex > 0)
                                id = id.Substring(0, qIndex);

                            result.Add("spotify:track:" + id);
                            continue;
                        }
                    }
                    catch
                    {
                        // ignore malformed URLs
                    }
                }

                // Assume raw ID
                result.Add("spotify:track:" + v);
            }

            return result;
        }

        public static async Task<bool> AddToPlaylist(string trackId)
        {
            if (Client == null)
                return false;

            List<string> uris = EnsureTrackUris([trackId]);

            try
            {
                // No playlist configured -> save to library (new Spotify endpoint: PUT /me/library)
                if (string.IsNullOrEmpty(Settings.SpotifyPlaylistId.PlaylistId) ||
                    Settings.SpotifyPlaylistId.PlaylistId == "-1")
                {
                    bool response = await ApiCallMeter.RunAsync("Library.SaveItems",
                        () => Client.Library.SaveItems(new LibrarySaveItemsRequest(uris)),
                        softLimitPerMinute: SoftLimitPerminute);
                    return !response; // keep your existing semantics: false = success, true = error
                }

                // Make sure cache is filled once (try SpotifyAPI-NET first, fallback to raw if it breaks)
                try
                {
                    SnapshotResponse response = await ApiCallMeter.RunAsync("Playlists.AddPlaylistItems",
                        () => Client.Playlists.AddPlaylistItems(Settings.SpotifyPlaylistId.PlaylistId,
                            new PlaylistAddItemsRequest(uris)), softLimitPerMinute: SoftLimitPerminute);
                    if (response.SnapshotId == Settings.SpotifyPlaylistId.Snapshot) return true;
                    Settings.SpotifyPlaylistId.Snapshot = response.SnapshotId;
                    Settings.SpotifyPlaylistId = Settings.SpotifyPlaylistId;
                    return false;
                }
                catch (Exception cacheEx)
                {
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        "EnsurePlaylistCacheAsync failed, falling back to raw playlist fetch", cacheEx);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Spotify, "Error adding song to playlist", ex);
                return true;
            }

            //try
            //{
            //    // No playlist configured -> save to library
            //    if (string.IsNullOrEmpty(Settings.SpotifyPlaylistId) ||
            //        Settings.SpotifyPlaylistId == "-1")
            //    {
            //        await Client.Library.SaveTracks(
            //            new LibrarySaveTracksRequest(new List<string> { trackId })
            //        );
            //        return false;
            //    }

            //    // Make sure cache is filled once
            //    await EnsurePlaylistCacheAsync();

            //    lock (_playlistLock)
            //    {
            //        if (_playlistTrackIds.Contains(trackId))
            //        {
            //            // Already in playlist
            //            return true;
            //        }
            //    }

            //    // Not in playlist -> add
            //    PlaylistAddItemsRequest request = new(new List<string> { "spotify:track:" + trackId });

            //    await ApiCallMeter.RunAsync("Playlists.AddItems",
            //        () => Client.Playlists.AddItems(Settings.SpotifyPlaylistId, request),
            //        softLimitPerminute);

            //    // Update cache
            //    lock (_playlistLock)
            //    {
            //        _playlistTrackIds.Add(trackId);
            //    }

            //    return false;
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error(LogSource.Spotify, "Error adding song to playlist", ex);
            //    return true;
            //}
            return true;
        }

        public static async Task SkipSong()
        {
            if (Client == null)
                return;
            try
            {
                await ApiCallMeter.RunAsync("Player.SkipNext", () => Client.Player.SkipNext(), SoftLimitPerminute);
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
                    SoftLimitPerminute);
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
                    SoftLimitPerminute);
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
                    DeviceId = Settings.SpotifyDeviceId
                }), SoftLimitPerminute);
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
                    () => Client.Player.GetCurrentPlayback(), SoftLimitPerminute);

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
                                DeviceId = Settings.SpotifyDeviceId
                            }), SoftLimitPerminute);
                        return false;

                    case Play when !isPlaying:
                        await ApiCallMeter.RunAsync("Player.ResumePlayback", () =>
                            Client.Player.ResumePlayback(new PlayerResumePlaybackRequest
                            {
                                DeviceId = Settings.SpotifyDeviceId
                            }), SoftLimitPerminute);
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
                    () => Client.Player.GetCurrentPlayback(), SoftLimitPerminute);
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
                    () => Client.Playlists.GetPlaylistItems(playlistId), SoftLimitPerminute);
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
                    () => Client.Playlists.Get(spotifyPlaylistId), SoftLimitPerminute);
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
                    () => Client.Player.SetVolume(new PlayerVolumeRequest(vol)), SoftLimitPerminute);
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
                SearchRequest request = new(SearchRequest.Types.Artist, search) { Limit = 5 };
                SearchResponse result = await ApiCallMeter.RunAsync("Search.Item", () => Client.Search.Item(request),
                    SoftLimitPerminute);
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
                    () => Client.UserProfile.Current(), SoftLimitPerminute);
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
                    () => Client.Playlists.CurrentUsers(), SoftLimitPerminute);
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
                    () => Client.Player.GetAvailableDevices(), SoftLimitPerminute);
                return x.Devices.FirstOrDefault(d => d.IsActive)?.Name;
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Spotify, "SPOTIFY API: Couldn't get device", e);
            }

            return "No device found";
        }

        public static async Task<PlaylistInfo> GetPlaybackPlaylist()
        {
            if (Client == null)
                return null;

            try
            {
                CurrentlyPlayingContext playback = await ApiCallMeter.RunAsync(
                    "Player.GetCurrentPlayback",
                    () => Client.Player.GetCurrentPlayback(),
                    SoftLimitPerminute);

                // No playlist context -> clear playlist cache and exit
                string contextUri = playback?.Context.Uri;
                if (!string.Equals(playback?.Context.Type, "playlist", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(contextUri))
                {
                    _cachedPlaylistId = null;
                    _cachedPlaylistInfo = null;
                    return null;
                }

                // Extract playlist id from spotify:playlist:<id>
                string playlistId = ExtractLastSegment(contextUri, ':');
                if (string.IsNullOrWhiteSpace(playlistId))
                {
                    _cachedPlaylistId = null;
                    _cachedPlaylistInfo = null;
                    return null;
                }

                // Cache hit
                if (playlistId == _cachedPlaylistId &&
                    _cachedPlaylistInfo != null &&
                    (DateTime.UtcNow - _cachedPlaylistFetchedAt) < PlaylistCacheTtl)
                {
                    return _cachedPlaylistInfo;
                }
                string playlistUrl = $"https://open.spotify.com/playlist/{playlistId}";

                PlaylistInfo info = null;

                // First try: official API
                try
                {
                    FullPlaylist playlist = await ApiCallMeter.RunAsync(
                        "Playlists.Get",
                        () => Client.Playlists.Get(playlistId),
                        SoftLimitPerminute);

                    if (playlist != null)
                    {
                        info = new PlaylistInfo
                        {
                            Id = playlist.Id,
                            Name = playlist.Name,
                            Owner = playlist.Owner?.DisplayName ?? playlist.Owner?.Id ?? "unknown",
                            Url = playlist.ExternalUrls != null && playlist.ExternalUrls.TryGetValue("spotify", out string url)
                                ? url
                                : playlistUrl
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Only fallback if it's likely an access/scopes/404 case.
                    // If you have a specific SpotifyAPI exception type available, narrow this catch.
                    // Otherwise: keep it broad, but don't swallow without fallback.
                    _ = ex; // optionally log
                }

                // Fallback: oEmbed (title + thumbnail only)
                if (info == null)
                {
                    SpotifyOEmbedResponse o = await OEmbedClient.GetAsync(playlistUrl).ConfigureAwait(false);
                    (string Name, string Owner)? embed = await SpotifyEmbedNextData.TryGetPlaylistNameAndOwnerAsync(playlistId);
                    info = new PlaylistInfo
                    {
                        Id = playlistId,
                        Name = embed?.Name ?? o?.Title ?? "unknown",
                        Owner = embed?.Owner ?? "unknown",
                        Url = $"https://open.spotify.com/playlist/{playlistId}",
                        Image = o?.ThumbnailUrl
                    };
                }

                _cachedPlaylistId = playlistId;
                _cachedPlaylistInfo = info;
                _cachedPlaylistFetchedAt = DateTime.UtcNow;

                return info;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private static string ExtractLastSegment(string s, char separator)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            int idx = s.LastIndexOf(separator);
            return (idx >= 0 && idx < s.Length - 1) ? s.Substring(idx + 1) : s;
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
                        DeviceId = Settings.SpotifyDeviceId
                    }), SoftLimitPerminute);
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
                        DeviceId = Settings.SpotifyDeviceId,
                        ContextUri = playlistId.Contains("spotify:playlist") ? playlistId : "spotify:playlist:" + playlistId,
                        PositionMs = 0
                    }), SoftLimitPerminute);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private class ParsedQuery
        {
            public string Raw { get; set; } = "";
            public string TitleCandidate { get; set; }
            public string ArtistCandidate { get; set; }
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
            StringBuilder sb = new();

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
            List<ParsedQuery> list = [];
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
                string[] parts = raw.Split([" - "], StringSplitOptions.RemoveEmptyEntries);
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
            string[] tokens = raw.Split([' '], StringSplitOptions.RemoveEmptyEntries);
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
            string title = track.Name;
            string artists = string.Join(" ", track.Artists.Select(a => a.Name));
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

        public static async Task<List<bool>> CheckSavedTracksAsync(List<string> ids)
        {
            List<string> uris = ids.Select(EnsureTrackUri).ToList();
            try
            {
                List<bool> x = await ApiCallMeter.RunAsync("Library.CheckItems", () => Client.Library.CheckItems(new LibraryCheckItemsRequest(uris)), SoftLimitPerminute);
                return x;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, LogSource.Spotify, "Error checking if IDs are already in Library.", e);
                return null;
            }
        }

        public static string EnsureTrackUri(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Already a URI
            return value.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase)
                ? value
                : $"spotify:track:{value}";
        }
    }
}