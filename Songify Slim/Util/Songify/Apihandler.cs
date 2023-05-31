using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding Spotify-API integration
    public static class ApiHandler
    {
        public static SpotifyWebAPI Spotify;
        private static Token _lastToken;
        public static bool Authed;

        private static readonly Timer AuthRefresh = new Timer
        {
            // Interval for refreshing Spotify-Auth
            Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds
        };

        // Spotify Authentication flow with the webserver
        private static TokenSwapAuth _auth;

        public static async void DoAuthAsync()
        {
            if (Settings.Settings.UseOwnApp)
            {
                _auth = new TokenSwapAuth(
                    $"{GlobalObjects.BaseUrl}/auth/auth.php?id=" + Settings.Settings.ClientId +
                    "&secret=" + Settings.Settings.ClientSecret,
                    "http://localhost:4002/auth",
                    Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState | Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate
                );
            }
            else
            {
                _auth = new TokenSwapAuth(
                    $"{GlobalObjects.BaseUrl}/auth/_index.php",
                    "http://localhost:4002/auth",
                    Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState | Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate
                );
            }
            try
            {
                // Execute the authentication flow and subscribe the timer elapsed event
                AuthRefresh.Elapsed += AuthRefresh_Elapsed;

                // If Refresh and Access-token are present, just refresh the auth
                if (!string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken) &&
                    !string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken))
                {
                    Authed = true;
                    Spotify = new SpotifyWebAPI
                    {
                        TokenType = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).TokenType,
                        AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).AccessToken
                    };
                    Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).AccessToken;
                    if (Application.Current.MainWindow != null)
                    {
                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                            Brushes.GreenYellow;
                        ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                            PackIconBootstrapIconsKind.CheckCircleFill;
                    }
                }
                else
                {
                    Authed = false;
                }

                // if the auth was successful save the new tokens and
                _auth.AuthReceived += async (sender, response) =>
                {
                    if (Authed)
                        return;

                    _lastToken = await _auth.ExchangeCodeAsync(response.Code);
                    if (_lastToken == null)
                        return;
                    try
                    {
                        // Save tokens
                        Settings.Settings.SpotifyRefreshToken = _lastToken.RefreshToken;
                        Settings.Settings.SpotifyAccessToken = _lastToken.AccessToken;
                        // create ne Spotify object
                        Spotify = new SpotifyWebAPI
                        {
                            TokenType = _lastToken.TokenType,
                            AccessToken = _lastToken.AccessToken
                        };
                        _auth.Stop();
                        Authed = true;
                        AuthRefresh.Start();
                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                foreach (Window window in Application.Current.Windows)
                                {
                                    if (window.GetType() == typeof(Window_Settings))
                                        ((Window_Settings)window).SetControls();
                                }

                                if (Application.Current.MainWindow == null) return;
                                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                                    Brushes.GreenYellow;
                                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                                    PackIconBootstrapIconsKind.CheckCircleFill;
                            }));

                    }
                    catch (Exception e)
                    {
                        Logger.LogStr("Error while saving Spotify tokens");
                        Logger.LogExc(e);
                    }

                };

                // automatically refreshes the token after it expires
                _auth.OnAccessTokenExpired += async (sender, e) =>
                {
                    Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).AccessToken;
                    Settings.Settings.SpotifyRefreshToken = _lastToken.RefreshToken;
                    Settings.Settings.SpotifyAccessToken = Spotify.AccessToken;
                };

                _auth.Start();

                if (Authed)
                {
                    AuthRefresh.Start();
                    if (Application.Current.MainWindow == null) return;
                    ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                        Brushes.GreenYellow;
                    ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Kind =
                        PackIconBootstrapIconsKind.CheckCircleFill;
                    return;
                }

                _auth.OpenBrowser();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private static async void AuthRefresh_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // When the timer elapses the tokens will get refreshed
                Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken)).AccessToken;
                Settings.Settings.SpotifyAccessToken = Spotify.AccessToken;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static TrackInfo GetSongInfo()
        {
            // returns the trackinfo of the current playback (used in the fetch timer)

            PlaybackContext context;
            try
            {
                context = Spotify.GetPlayback();
            }
            catch (Exception)
            {
                Logger.LogStr("SPOTIFY API: Couldn't fetch Song info");
                return new TrackInfo { Artists = "", Title = "" };
            }

            if (context.Error != null)
                Logger.LogStr("SPOTIFY API: " + context.Error.Status + " | " + context.Error.Message);

            if (context.Item == null) return new TrackInfo { Artists = "", Title = "" };


            string artists = "";

            for (int i = 0; i < context.Item.Artists.Count; i++)
                if (i != context.Item.Artists.Count - 1)
                    artists += context.Item.Artists[i].Name + ", ";
                else
                    artists += context.Item.Artists[i].Name;

            if (context.Device != null)
            {
                if (Settings.Settings.SpotifyDeviceId != context.Device.Id)
                    Settings.Settings.SpotifyDeviceId = context.Device.Id;
            }

            var albums = context.Item.Album.Images;
            double totalSeconds = TimeSpan.FromMilliseconds(context.Item.DurationMs).TotalSeconds;
            double currentDuration = TimeSpan.FromMilliseconds(context.ProgressMs).TotalSeconds;
            double percentage = 100 / totalSeconds * currentDuration;
            PlaylistInfo playlistInfo = null;
            try
            {
                if (context.Context != null && context.Context.Type == "playlist")
                {
                    var playlist = Spotify.GetPlaylist(context.Context.Uri.Split(':')[2]);
                    playlistInfo = new PlaylistInfo
                    {
                        Name = playlist.Name,
                        Id = playlist.Id,
                        Owner = playlist.Owner.DisplayName,
                        Url = playlist.Uri,
                        Image = playlist.Images[0].Url
                    };
                }
            }
            catch (Exception)
            {
                Logger.LogStr("SPOTIFY API: Couldn't fetch Playlist info, missing scope maybe?");
            }

            return new TrackInfo
            {
                Artists = artists,
                Title = context.Item.Name,
                Albums = albums,
                SongId = context.Item.Id,
                DurationMs = context.Item.DurationMs - context.ProgressMs,
                IsPlaying = context.IsPlaying,
                Url = "https://open.spotify.com/track/" + context.Item.Id,
                DurationPercentage = (int)percentage,
                DurationTotal = context.Item.DurationMs,
                Progress = context.ProgressMs,
                Playlist = playlistInfo
            };
        }

        public static SearchItem GetArtist(string searchStr)
        {
            try
            {
                // returns Artist matching the search string
                return Spotify.SearchItems(searchStr, SearchType.Artist, 10);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ErrorResponse AddToQ(string songUri)
        {
            try
            {
                // Tries to add a song to the current playback queue
                ErrorResponse error = Spotify.AddToQueue(songUri, Settings.Settings.SpotifyDeviceId);

                // If the error message is "503 | Service unavailable" wait a second and retry for a total of 5 times.
                if (!error.HasError()) return error;
                if (error.Error.Status != 503) return error;
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(1000);
                    error = Spotify.AddToQueue(songUri, Settings.Settings.SpotifyDeviceId);
                    if (!error.HasError())
                        break;
                }
                return error;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }

        }

        public static FullTrack GetTrack(string id)
        {
            try
            {
                var x = Spotify.GetPrivateProfile().Country;
                // Returns a Track-Object matching the song id
                return Spotify.GetTrack(id, x);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }

        }

        public static SearchItem FindTrack(string searchQuery)
        {
            // Returns a Track-Object matching a search query (artist - title). It only returns the first match which is found
            try
            {
                return Spotify.SearchItems(searchQuery, SearchType.Track, 1);

            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }
        }

        public static async Task<ErrorResponse> SkipSong()
        {
            try
            {
                return await Spotify.SkipPlaybackToNextAsync();

            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }
        }
    }
}