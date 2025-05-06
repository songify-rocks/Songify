using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using Timer = System.Timers.Timer;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Songify_Slim.Util.Settings;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Spotify
{
    // This class handles everything regarding Spotify-API integration
    public static class SpotifyApiHandler
    {

        private const string BaseUrl = "https://auth.overcode.tv";
        private static EmbedIOAuthServer? _server;
        public static SpotifyClient? Client;

        private static readonly Timer AuthTimer = new()
        {
            Interval = 1000 * 60 * 30,
        };

        public static async Task Auth()
        {
            AuthTimer.Elapsed += AuthTimer_Elapsed;
            if (!(string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken) ||
                  string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken)))
            {
                Debug.WriteLine("Refreshing Tokens");
                await RefreshTokens();
                return;
            }

            Debug.WriteLine("Getting new tokens");
            _server = new EmbedIOAuthServer(new Uri($"http://{Settings.Settings.SpotifyRedirectUri}:4002/auth"), 4002, Assembly.GetExecutingAssembly(), "Songify_Slim.default_site");
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
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }
        }

        private static async void AuthTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
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
            OAuthClient oauth = new OAuthClient();
            TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
                new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                Settings.Settings.SpotifyRefreshToken
            );
            AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault().WithAuthenticator(
                new AuthorizationCodeAuthenticator(Settings.Settings.ClientId, Settings.Settings.ClientSecret,
                    new AuthorizationCodeTokenResponse
                    {
                        AccessToken = Settings.Settings.SpotifyAccessToken,
                        RefreshToken = Settings.Settings.SpotifyRefreshToken
                    }));

            Settings.Settings.SpotifyAccessToken = refreshResponse.AccessToken;
            Settings.Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;

            Client = new SpotifyClient(config);
            if (!AuthTimer.Enabled)
            {
                AuthTimer.Start();
            }

            // We are authenticated!
            if (Application.Current.MainWindow == null) return;
            ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                Brushes.GreenYellow;
            ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                PackIconBootstrapIconsKind.CheckCircleFill;
        }

        private static async Task OnAuthorizationCodeReceived(object? sender, AuthorizationCodeResponse response)
        {
            Debug.WriteLine("Got here");
            OAuthClient oauth = new OAuthClient();
            TokenSwapTokenRequest tokenRequest = new TokenSwapTokenRequest(
                new Uri($"{BaseUrl}/swap?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                response.Code);
            AuthorizationCodeTokenResponse tokenResponse = await oauth.RequestToken(tokenRequest);
            Debug.WriteLine($"We got an access token from server: {tokenResponse.AccessToken}");

            TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
                new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                tokenResponse.RefreshToken
            );
            AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

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
                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                            PackIconBootstrapIconsKind.CheckCircleFill;
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
            if (Client == null)
                return null;
            try
            {
                CurrentlyPlaying playback =
                    await Client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                if (playback == null)
                    return null;
                if (playback.Item is not FullTrack track)
                    return null;

                string artists = string.Join(", ", track.Artists.Select(a => a.Name));

                CurrentlyPlayingContext currentPlayback = await Client.Player.GetCurrentPlayback();
                if (Settings.Settings.SpotifyDeviceId != currentPlayback.Device.Id)
                {
                    Settings.Settings.SpotifyDeviceId = currentPlayback.Device.Id;
                }

                List<Image> albums = track.Album.Images;
                double totalSeconds = track.DurationMs / 1000.0;
                double currentSeconds = (double)(playback.ProgressMs / 1000.0);
                double percentage = totalSeconds == 0 ? 0 : (100 * currentSeconds / totalSeconds);

                PlaylistInfo playlistInfo = null;
                if (playback.Context is not { Type: "playlist" })
                    return new TrackInfo
                    {
                        Artists = artists,
                        Title = track.Name,
                        Albums = albums.ToList(),
                        SongId = track.Id,
                        DurationMs = (int)(track.DurationMs - playback.ProgressMs),
                        IsPlaying = playback.IsPlaying,
                        Url = "https://open.spotify.com/track/" + track.Id,
                        DurationPercentage = (int)percentage,
                        DurationTotal = track.DurationMs,
                        Progress = (int)playback.ProgressMs,
                        Playlist = playlistInfo,
                        FullArtists = track.Artists.ToList()
                    };
                string[] uriParts = playback.Context.Uri.Split(':');
                if (uriParts is not { Length: 3 })
                    return new TrackInfo
                    {
                        Artists = artists,
                        Title = track.Name,
                        Albums = albums.ToList(),
                        SongId = track.Id,
                        DurationMs = (int)(track.DurationMs - playback.ProgressMs),
                        IsPlaying = playback.IsPlaying,
                        Url = "https://open.spotify.com/track/" + track.Id,
                        DurationPercentage = (int)percentage,
                        DurationTotal = track.DurationMs,
                        Progress = (int)playback.ProgressMs,
                        Playlist = playlistInfo,
                        FullArtists = track.Artists.ToList()
                    };
                string playlistId = uriParts[2];
                FullPlaylist playlist = await Client.Playlists.Get(playlistId);
                playlistInfo = new PlaylistInfo
                {
                    Name = playlist.Name,
                    Id = playlist.Id,
                    Owner = playlist.Owner.DisplayName,
                    Url = playlist.Uri,
                    Image = playlist.Images.FirstOrDefault()?.Url
                };

                return new TrackInfo
                {
                    Artists = artists,
                    Title = track.Name,
                    Albums = albums.ToList(),
                    SongId = track.Id,
                    DurationMs = (int)(track.DurationMs - playback.ProgressMs),
                    IsPlaying = playback.IsPlaying,
                    Url = "https://open.spotify.com/track/" + track.Id,
                    DurationPercentage = (int)percentage,
                    DurationTotal = track.DurationMs,
                    Progress = (int)playback.ProgressMs,
                    Playlist = playlistInfo,
                    FullArtists = track.Artists.ToList()
                };
            }
            catch (Exception ex)
            {
                Logger.LogStr("ERROR: Couldn't fetch song info");
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
                await Client.Player.AddToQueue(new PlayerAddToQueueRequest(songUri)
                {
                    DeviceId = Settings.Settings.SpotifyDeviceId
                });
                return true;
            }
            catch (APIException ex)
            {
                if ((int)ex.Response.StatusCode == 503)
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
                            if ((int)retryEx.Response.StatusCode != 503)
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
                return await Client.Tracks.Get(id);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<FullTrack> FindTrack(string query)
        {
            if (Client == null)
                return null;
            try
            {
                SearchRequest request = new SearchRequest(SearchRequest.Types.Track, query) { Limit = 1 };
                SearchResponse result = await Client.Search.Item(request);

                return result.Tracks is { Items.Count: > 0 } ? result.Tracks.Items[0] : null;
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
                if (string.IsNullOrEmpty(Settings.Settings.SpotifyPlaylistId) ||
                    Settings.Settings.SpotifyPlaylistId == "-1")
                {
                    await Client.Library.SaveTracks(new LibrarySaveTracksRequest(new List<string> { trackId }));
                    return false;
                }

                Paging<PlaylistTrack<IPlayableItem>> tracks =
                    await Client.Playlists.GetItems(Settings.Settings.SpotifyPlaylistId);

                while (tracks.Items != null)
                {
                    foreach (PlaylistTrack<IPlayableItem> item in tracks.Items)
                    {
                        // item is PlaylistTrack<IPlayableItem>
                        if (item.Track is FullTrack fullTrack && fullTrack.Id == trackId)
                        {
                            return true;
                        }
                    }

                    if (tracks.Next == null)
                        break;

                    tracks = await Client.NextPage(tracks);
                }



                PlaylistAddItemsRequest request =
                    new PlaylistAddItemsRequest(new List<string> { "spotify:track:" + trackId });
                await Client.Playlists.AddItems(Settings.Settings.SpotifyPlaylistId, request);
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
                await Client.Player.SkipNext();
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
                return await Client.Player.GetQueue();
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
                await Client.Player.SkipPrevious();
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
                await Client.Player.SeekTo(new PlayerSeekToRequest(0)
                {
                    DeviceId = Settings.Settings.SpotifyDeviceId
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static async Task<bool> PlayPause(PlaybackAction action = PlaybackAction.Toggle)
        {
            if (Client == null)
                return false;
            try
            {
                CurrentlyPlayingContext playback = await Client.Player.GetCurrentPlayback();

                bool isPlaying = playback is { IsPlaying: true };

                if (action == PlaybackAction.Toggle)
                {
                    action = isPlaying ? PlaybackAction.Pause : PlaybackAction.Play;
                }

                switch (action)
                {
                    case PlaybackAction.Pause when isPlaying:
                        await Client.Player.PausePlayback(new PlayerPausePlaybackRequest
                        {
                            DeviceId = Settings.Settings.SpotifyDeviceId
                        });
                        return false;
                    case PlaybackAction.Play when !isPlaying:
                        await Client.Player.ResumePlayback(new PlayerResumePlaybackRequest
                        {
                            DeviceId = Settings.Settings.SpotifyDeviceId
                        });
                        return true;
                    case PlaybackAction.Toggle:
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
                return await Client.Player.GetCurrentPlayback();
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
                List<bool> response = await Client.Library.CheckTracks(new LibraryCheckTracksRequest(tracks));
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        //private static PlaylistInfo _playlistInfo;
        //public static SpotifyWebAPI Spotify;
        //private static Token _lastToken;
        //public static bool Authed;

        //private static readonly Timer AuthRefresh = new()
        //{
        //    // Interval for refreshing Spotify-Auth
        //    Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds
        //};

        //// Spotify Authentication flow with the webserver
        //private static TokenSwapAuth _auth;

        //public static async Task DoAuthAsync(bool altUrl = false)
        //{
        //    string url = altUrl ? GlobalObjects.AltAuthUrl : GlobalObjects.AuthUrl;
        //    Debug.WriteLine(url);

        //    string uriType = Settings.Settings.SpotifyRedirectUri switch
        //    {
        //        "localhost" => "name",
        //        "127.0.0.1" => "ip",
        //        _ => "name"
        //    };

        //    Debug.WriteLine($"{url}/auth/auth3.php?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}&uri_type={uriType}");

        //    _auth = new TokenSwapAuth(
        //        $"{url}/auth/auth3.php?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}&uri_type={uriType}",
        //        $"http://{Settings.Settings.SpotifyRedirectUri}:4002/auth",
        //        Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
        //        Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
        //    );

        //    //if (Settings.Settings.UseOwnApp)
        //    //{
        //    //    _auth = new TokenSwapAuth(
        //    //        $"{url}/auth/auth.php?id=" + Settings.Settings.ClientId +
        //    //        "&secret=" + Settings.Settings.ClientSecret,
        //    //        "http://localhost:4002/auth",
        //    //        Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
        //    //        Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
        //    //    );

        //    //}
        //    //else
        //    //{
        //    //    _auth = new TokenSwapAuth(
        //    //        $"{url}/auth/_index.php",
        //    //        "http://localhost:4002/auth",
        //    //        Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
        //    //        Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
        //    //    );
        //    //}

        //    try
        //    {
        //        // Execute the authentication flow and subscribe the timer elapsed event
        //        AuthRefresh.Elapsed += AuthRefresh_Elapsed;

        //        // If Refresh and Access-token are present, just refresh the auth
        //        if (!string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken) &&
        //            !string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken))
        //        {
        //            Authed = true;
        //            Token token = await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken);
        //            if (token == null)
        //                return;
        //            Spotify = new SpotifyWebAPI
        //            {
        //                TokenType = token.TokenType,
        //                AccessToken = token.AccessToken
        //            };
        //            Spotify.AccessToken = token.AccessToken;
        //            if (Application.Current.MainWindow != null)
        //            {
        //                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
        //                    Brushes.GreenYellow;
        //                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
        //                    PackIconBootstrapIconsKind.CheckCircleFill;
        //            }
        //        }
        //        else
        //        {
        //            Authed = false;
        //        }

        //        // if the auth was successful save the new tokens and
        //        _auth.AuthReceived += static async (sender, response) =>
        //        {
        //            if (Authed)
        //                return;

        //            _lastToken = await _auth.ExchangeCodeAsync(response.Code);
        //            if (_lastToken == null)
        //                return;
        //            try
        //            {
        //                // Save tokens
        //                Settings.Settings.SpotifyRefreshToken = _lastToken.RefreshToken;
        //                Settings.Settings.SpotifyAccessToken = _lastToken.AccessToken;
        //                // create ne Spotify object
        //                Spotify = new SpotifyWebAPI
        //                {
        //                    TokenType = _lastToken.TokenType,
        //                    AccessToken = _lastToken.AccessToken
        //                };
        //                _auth.Stop();
        //                Authed = true;
        //                AuthRefresh.Start();
        //                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
        //                    new Action(async void () =>
        //                    {
        //                        foreach (Window window in Application.Current.Windows)
        //                        {
        //                            if (window.GetType() == typeof(Window_Settings))
        //                                await ((Window_Settings)window).SetControls();
        //                        }

        //                        if (Application.Current.MainWindow == null) return;
        //                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
        //                            Brushes.GreenYellow;
        //                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
        //                            PackIconBootstrapIconsKind.CheckCircleFill;
        //                        GlobalObjects.SpotifyProfile = await Spotify.GetPrivateProfileAsync();
        //                        Settings.Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;
        //                        Logger.LogStr(
        //                            $"SPOTIFY: Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
        //                        Logger.LogStr($"SPOTIFY: Account Type: {GlobalObjects.SpotifyProfile.Product}");

        //                        if (GlobalObjects.SpotifyProfile.Product == "premium") return;

        //                        if (!Settings.Settings.HideSpotifyPremiumWarning)
        //                            await ShowPremiumRequiredDialogAsync();

        //                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
        //                            Brushes.DarkOrange;

        //                    }));
        //            }
        //            catch (Exception e)
        //            {
        //                Logger.LogStr("Error while saving Spotify tokens");
        //                Logger.LogExc(e);
        //            }
        //        };

        //        // automatically refreshes the token after it expires
        //        _auth.OnAccessTokenExpired += async (sender, e) =>
        //        {
        //            Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken))
        //                .AccessToken;
        //            Settings.Settings.SpotifyRefreshToken = _lastToken.RefreshToken;
        //            Settings.Settings.SpotifyAccessToken = Spotify.AccessToken;
        //        };

        //        _auth.Start();

        //        if (Authed)
        //        {
        //            AuthRefresh.Start();
        //            if (Application.Current.MainWindow == null) return;
        //            ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
        //                Brushes.GreenYellow;
        //            ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
        //                PackIconBootstrapIconsKind.CheckCircleFill;
        //            PrivateProfile x = await Spotify.GetPrivateProfileAsync();

        //            Logger.LogStr($"SPOTIFY: Connected Account: {x.DisplayName}");
        //            Logger.LogStr($"SPOTIFY: Account Type: {x.Product}");

        //            if (x.Product == "premium") return;
        //            ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
        //                Brushes.DarkOrange;
        //            return;
        //        }

        //        _auth.OpenBrowser();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExc(ex);
        //    }
        //}

        //public static async Task ShowPremiumRequiredDialogAsync()
        //{
        //    MetroDialogSettings dialogSettings = new MetroDialogSettings
        //    {
        //        AffirmativeButtonText = "OK",
        //        NegativeButtonText = "Don't Show Again",
        //        AnimateShow = true,
        //        AnimateHide = true,
        //    };

        //    // You need a reference to the dialog host (usually the main window)
        //    MetroWindow mainWindow = (Application.Current.MainWindow as MetroWindow);
        //    if (mainWindow == null)
        //        return;

        //    MessageDialogResult result = await mainWindow.ShowMessageAsync(
        //        "Spotify Premium required",
        //        "Spotify Premium is required to perform song requests. Songify was unable to verify your Spotify Premium status.",
        //        MessageDialogStyle.AffirmativeAndNegative,
        //        dialogSettings);

        //    if (result == MessageDialogResult.Negative)
        //    {
        //        Settings.Settings.HideSpotifyPremiumWarning = true;
        //    }
        //}


        //private static async void AuthRefresh_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        // When the timer elapses the tokens will get refreshed
        //        Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).AccessToken;
        //        Settings.Settings.SpotifyAccessToken = Spotify.AccessToken;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogExc(ex);
        //    }
        //}

        //public static async Task<TrackInfo> GetSongInfo()
        //{
        //    // returns the trackinfo of the current playback (used in the fetch timer)

        //    PlaybackContext context;
        //    try
        //    {
        //        context = await Spotify.GetPlaybackAsync();
        //    }
        //    catch (Exception ex)
        //    {

        //        Logger.LogStr("SPOTIFY API: Couldn't fetch Song info");
        //        return null;
        //    }

        //    if (context.Error != null)
        //        Logger.LogStr("SPOTIFY API: " + context.Error.Status + " | " + context.Error.Message);

        //    if (context.Item == null) return null;

        //    string artists = "";

        //    for (int i = 0; i < context.Item.Artists.Count; i++)
        //        if (i != context.Item.Artists.Count - 1)
        //            artists += context.Item.Artists[i].Name + ", ";
        //        else
        //            artists += context.Item.Artists[i].Name;

        //    if (context.Device != null)
        //    {
        //        if (Settings.Settings.SpotifyDeviceId != context.Device.Id)
        //            Settings.Settings.SpotifyDeviceId = context.Device.Id;
        //    }

        //    List<Image> albums = context.Item.Album.Images;
        //    double totalSeconds = TimeSpan.FromMilliseconds(context.Item.DurationMs).TotalSeconds;
        //    double currentDuration = TimeSpan.FromMilliseconds(context.ProgressMs).TotalSeconds;
        //    double percentage = 100 / totalSeconds * currentDuration;
        //    try
        //    {
        //        if (context.Context is { Type: "playlist" })
        //        {
        //            if (GlobalObjects.CurrentSong == null || GlobalObjects.CurrentSong.SongId != context.Item.Id)
        //            {
        //                FullPlaylist playlist = await Spotify.GetPlaylistAsync(context.Context.Uri.Split(':')[2]);
        //                if (playlist != null || !GlobalObjects.IsObjectDefault(playlist))
        //                {
        //                    if (playlist is { Id: not null })
        //                        _playlistInfo = new PlaylistInfo
        //                        {
        //                            Name = playlist.Name,
        //                            Id = playlist.Id,
        //                            Owner = playlist.Owner.DisplayName,
        //                            Url = playlist.Uri,
        //                            Image = playlist.Images[0].Url
        //                        };
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        // ignored because it's not important if the playlist info can't be fetched
        //        _playlistInfo = null;
        //    }

        //    return new TrackInfo
        //    {
        //        Artists = artists,
        //        Title = context.Item.Name,
        //        Albums = albums,
        //        SongId = context.Item.Id,
        //        DurationMs = (int)context.Item.DurationMs - context.ProgressMs,
        //        IsPlaying = context.IsPlaying,
        //        Url = "https://open.spotify.com/track/" + context.Item.Id,
        //        DurationPercentage = (int)percentage,
        //        DurationTotal = (int)context.Item.DurationMs,
        //        Progress = context.ProgressMs,
        //        Playlist = _playlistInfo,
        //        FullArtists = context.Item.Artists
        //    };
        //}

        //public static SearchItem GetArtist(string searchStr)
        //{
        //    try
        //    {
        //        // returns Artist matching the search string
        //        return Spotify.SearchItems(searchStr, SearchType.Artist, 10);
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        //public static ErrorResponse AddToQ(string songUri)
        //{
        //    try
        //    {
        //        // Tries to add a song to the current playback queue
        //        ErrorResponse error = Spotify.AddToQueue(songUri, Settings.Settings.SpotifyDeviceId);

        //        // If the error message is "503 | Service unavailable" wait a second and retry for a total of 5 times.
        //        if (!error.HasError()) return error;
        //        if (error.Error.Status != 503) return error;
        //        for (int i = 0; i < 5; i++)
        //        {
        //            Thread.Sleep(1000);
        //            error = Spotify.AddToQueue(songUri, Settings.Settings.SpotifyDeviceId);
        //            if (!error.HasError())
        //                break;
        //        }

        //        return error;
        //    }
        //    catch (Exception e)
        //    {
        //        if (e.Message == "Input string was not in a correct format.")
        //            return null;
        //        Logger.LogExc(e);
        //        return null;
        //    }
        //}

        //public static async Task<FullTrack> GetTrack(string id)
        //{
        //    try
        //    {
        //        FullTrack x = await Spotify.GetTrackAsync(id, "");
        //        //Debug.WriteLine(Json.Serialize(x));
        //        return x;
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.LogExc(e);
        //        return null;
        //    }
        //}

        //public static SearchItem FindTrack(string searchQuery)
        //{
        //    // Returns a Track-Object matching a search query (artist - title). It only returns the first match which is found
        //    try
        //    {
        //        string newQuery = UrlEncoder.Default.Encode(searchQuery);
        //        //newQuery = newQuery.Replace("%20", "+");
        //        Debug.WriteLine(searchQuery);
        //        Debug.WriteLine(newQuery);
        //        SearchItem search = Spotify.SearchItems(newQuery, SearchType.Track, 1);
        //        return search;
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.LogExc(e);
        //        return null;
        //    }
        //}

        //public static async Task<bool> AddToPlaylist(string trackId)
        //{
        //    if (Settings.Settings.SpotifyPlaylistId == null || Settings.Settings.SpotifyPlaylistId == "-1")
        //    {
        //        await Spotify.SaveTracksAsync([trackId]);
        //    }
        //    else
        //    {
        //        try
        //        {
        //            Paging<PlaylistTrack> tracks =
        //                await Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId);

        //            while (tracks is { Items: not null })
        //            {
        //                if (tracks.Items.Any(t => t.Track.Id == trackId))
        //                {
        //                    return true;
        //                }

        //                if (!tracks.HasNextPage())
        //                {
        //                    break;  // Exit if no more pages
        //                }

        //                tracks = await Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100, tracks.Offset + tracks.Limit);
        //            }

        //            ErrorResponse x = await Spotify.AddPlaylistTrackAsync(Settings.Settings.SpotifyPlaylistId,
        //                $"spotify:track:{trackId}");
        //            return x == null || x.HasError();
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogStr("Error adding song to playlist");
        //            Logger.LogExc(ex);
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //public static async Task<ErrorResponse> SkipSong()
        //{
        //    try
        //    {
        //        return await Spotify.SkipPlaybackToNextAsync();
        //    }
        //    catch (Exception)
        //    {
        //        //ignored
        //        return null;
        //    }
        //}

        //public static async Task<SimpleQueue> GetQueueInfo()
        //{
        //    return await Spotify.GetQueueAsync();
        //}

        //public static async Task SkipPrevious()
        //{
        //    try
        //    {
        //        await Spotify.SkipPlaybackToPreviousAsync(Settings.Settings.SpotifyDeviceId);
        //    }
        //    catch (Exception)
        //    {
        //        //ignored
        //    }
        //}

        //public static async Task PlayFromStart()
        //{
        //    try
        //    {
        //        await Spotify.SeekPlaybackAsync(0, Settings.Settings.SpotifyDeviceId);
        //    }
        //    catch (Exception)
        //    {
        //        //ignored
        //    }
        //}

        //public static async Task<bool> PlayPause()
        //{
        //    PlaybackContext playback = await Spotify.GetPlaybackAsync();

        //    try
        //    {
        //        if (playback.IsPlaying)
        //            await Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
        //        else
        //            await Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, null, 0);
        //    }
        //    catch (Exception)
        //    {
        //        //ignored
        //    }
        //    return !playback.IsPlaying;
        //}

        public static async Task<Paging<PlaylistTrack<IPlayableItem>>> GetPlaylistTracks(string playlistId)
        {
            if (Client == null)
                return null;
            try
            {
                Paging<PlaylistTrack<IPlayableItem>> tracks = await Client.Playlists.GetItems(playlistId);
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
                FullPlaylist playlist = await Client.Playlists.Get(spotifyPlaylistId);
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
                return await Client.Player.SetVolume(new PlayerVolumeRequest(vol));
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return false;
            }
        }

        public class TrackScore
        {
            public string TrackName { get; set; }
            public string ArtistName { get; set; }
            public int Score { get; set; }
        }

        public static async Task<List<FullArtist>> GetArtist(string search)
        {
            if (Client == null)
                return null;
            try
            {
                SearchRequest request = new(SearchRequest.Types.Artist, search) { Limit = 1 };
                SearchResponse result = await Client.Search.Item(request);
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
                PrivateUser user = await Client.UserProfile.Current();
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
                Paging<FullPlaylist> playlists = await Client.Playlists.CurrentUsers();
                return playlists;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }
    }
}