using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Songify_Slim.Util.Spotify.SpotifyAPI.Web;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Songify_Slim.Views;
using Timer = System.Timers.Timer;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;

namespace Songify_Slim.Util.Spotify
{
    // This class handles everything regarding Spotify-API integration
    public static class SpotifyApiHandler
    {
        private static PlaylistInfo _playlistInfo;
        public static SpotifyWebAPI Spotify;
        private static Token _lastToken;
        public static bool Authed;

        private static readonly Timer AuthRefresh = new()
        {
            // Interval for refreshing Spotify-Auth
            Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds
        };

        // Spotify Authentication flow with the webserver
        private static TokenSwapAuth _auth;

        public static async Task DoAuthAsync(bool altUrl = false)
        {
            string url = altUrl ? GlobalObjects.AltAuthUrl : GlobalObjects.AuthUrl;
            Debug.WriteLine(url);

            string uriType = Settings.Settings.SpotifyRedirectUri switch
            {
                "localhost" => "name",
                "127.0.0.1" => "ip",
                _ => "name"
            };

            Debug.WriteLine($"{url}/auth/auth3.php?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}&uri_type={uriType}");

            _auth = new TokenSwapAuth(
                $"{url}/auth/auth3.php?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}&uri_type={uriType}",
                $"http://{Settings.Settings.SpotifyRedirectUri}:4002/auth",
                Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
                Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
            );

            //if (Settings.Settings.UseOwnApp)
            //{
            //    _auth = new TokenSwapAuth(
            //        $"{url}/auth/auth.php?id=" + Settings.Settings.ClientId +
            //        "&secret=" + Settings.Settings.ClientSecret,
            //        "http://localhost:4002/auth",
            //        Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
            //        Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
            //    );

            //}
            //else
            //{
            //    _auth = new TokenSwapAuth(
            //        $"{url}/auth/_index.php",
            //        "http://localhost:4002/auth",
            //        Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
            //        Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate | Scope.UserLibraryModify | Scope.UserLibraryRead
            //    );
            //}

            try
            {
                // Execute the authentication flow and subscribe the timer elapsed event
                AuthRefresh.Elapsed += AuthRefresh_Elapsed;

                // If Refresh and Access-token are present, just refresh the auth
                if (!string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken) &&
                    !string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken))
                {
                    Authed = true;
                    Token token = await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken);
                    if (token == null)
                        return;
                    Spotify = new SpotifyWebAPI
                    {
                        TokenType = token.TokenType,
                        AccessToken = token.AccessToken
                    };
                    Spotify.AccessToken = token.AccessToken;
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
                _auth.AuthReceived += static async (sender, response) =>
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
                            new Action(async void () =>
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
                                GlobalObjects.SpotifyProfile = await Spotify.GetPrivateProfileAsync();
                                Settings.Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;
                                Logger.LogStr(
                                    $"SPOTIFY: Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
                                Logger.LogStr($"SPOTIFY: Account Type: {GlobalObjects.SpotifyProfile.Product}");

                                if (GlobalObjects.SpotifyProfile.Product == "premium") return;

                                if (!Settings.Settings.HideSpotifyPremiumWarning)
                                    await ShowPremiumRequiredDialogAsync();

                                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                                    Brushes.DarkOrange;

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
                    Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken))
                        .AccessToken;
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
                    PrivateProfile x = await Spotify.GetPrivateProfileAsync();

                    Logger.LogStr($"SPOTIFY: Connected Account: {x.DisplayName}");
                    Logger.LogStr($"SPOTIFY: Account Type: {x.Product}");

                    if (x.Product == "premium") return;
                    ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                        Brushes.DarkOrange;
                    return;
                }

                _auth.OpenBrowser();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
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

        public static async Task<TrackInfo> GetSongInfo()
        {
            // returns the trackinfo of the current playback (used in the fetch timer)

            PlaybackContext context;
            try
            {
                context = await Spotify.GetPlaybackAsync();
            }
            catch (Exception ex)
            {

                Logger.LogStr("SPOTIFY API: Couldn't fetch Song info");
                return null;
            }

            if (context.Error != null)
                Logger.LogStr("SPOTIFY API: " + context.Error.Status + " | " + context.Error.Message);

            if (context.Item == null) return null;

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

            List<Image> albums = context.Item.Album.Images;
            double totalSeconds = TimeSpan.FromMilliseconds(context.Item.DurationMs).TotalSeconds;
            double currentDuration = TimeSpan.FromMilliseconds(context.ProgressMs).TotalSeconds;
            double percentage = 100 / totalSeconds * currentDuration;
            try
            {
                if (context.Context is { Type: "playlist" })
                {
                    if (GlobalObjects.CurrentSong == null || GlobalObjects.CurrentSong.SongId != context.Item.Id)
                    {
                        FullPlaylist playlist = await Spotify.GetPlaylistAsync(context.Context.Uri.Split(':')[2]);
                        if (playlist != null || !GlobalObjects.IsObjectDefault(playlist))
                        {
                            if (playlist is { Id: not null })
                                _playlistInfo = new PlaylistInfo
                                {
                                    Name = playlist.Name,
                                    Id = playlist.Id,
                                    Owner = playlist.Owner.DisplayName,
                                    Url = playlist.Uri,
                                    Image = playlist.Images[0].Url
                                };
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored because it's not important if the playlist info can't be fetched
                _playlistInfo = null;
            }

            return new TrackInfo
            {
                Artists = artists,
                Title = context.Item.Name,
                Albums = albums,
                SongId = context.Item.Id,
                DurationMs = (int)context.Item.DurationMs - context.ProgressMs,
                IsPlaying = context.IsPlaying,
                Url = "https://open.spotify.com/track/" + context.Item.Id,
                DurationPercentage = (int)percentage,
                DurationTotal = (int)context.Item.DurationMs,
                Progress = context.ProgressMs,
                Playlist = _playlistInfo,
                FullArtists = context.Item.Artists
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
                if (e.Message == "Input string was not in a correct format.")
                    return null;
                Logger.LogExc(e);
                return null;
            }
        }

        public static async Task<FullTrack> GetTrack(string id)
        {
            try
            {
                FullTrack x = await Spotify.GetTrackAsync(id, "");
                //Debug.WriteLine(Json.Serialize(x));
                return x;
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
                string newQuery = UrlEncoder.Default.Encode(searchQuery);
                //newQuery = newQuery.Replace("%20", "+");
                Debug.WriteLine(searchQuery);
                Debug.WriteLine(newQuery);
                SearchItem search = Spotify.SearchItems(newQuery, SearchType.Track, 1);
                return search;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }
        }

        public static async Task<bool> AddToPlaylist(string trackId)
        {
            if (Settings.Settings.SpotifyPlaylistId == null || Settings.Settings.SpotifyPlaylistId == "-1")
            {
                await Spotify.SaveTracksAsync([trackId]);
            }
            else
            {
                try
                {
                    Paging<PlaylistTrack> tracks =
                        await Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId);

                    while (tracks is { Items: not null })
                    {
                        if (tracks.Items.Any(t => t.Track.Id == trackId))
                        {
                            return true;
                        }

                        if (!tracks.HasNextPage())
                        {
                            break;  // Exit if no more pages
                        }

                        tracks = await Spotify.GetPlaylistTracksAsync(Settings.Settings.SpotifyPlaylistId, "", 100, tracks.Offset + tracks.Limit);
                    }

                    ErrorResponse x = await Spotify.AddPlaylistTrackAsync(Settings.Settings.SpotifyPlaylistId,
                        $"spotify:track:{trackId}");
                    return x == null || x.HasError();
                }
                catch (Exception ex)
                {
                    Logger.LogStr("Error adding song to playlist");
                    Logger.LogExc(ex);
                    return true;
                }
            }
            return false;
        }

        public static async Task<ErrorResponse> SkipSong()
        {
            try
            {
                return await Spotify.SkipPlaybackToNextAsync();
            }
            catch (Exception)
            {
                //ignored
                return null;
            }
        }

        public static async Task<SimpleQueue> GetQueueInfo()
        {
            return await Spotify.GetQueueAsync();
        }

        public static async Task SkipPrevious()
        {
            try
            {
                await Spotify.SkipPlaybackToPreviousAsync(Settings.Settings.SpotifyDeviceId);
            }
            catch (Exception)
            {
                //ignored
            }
        }

        public static async Task PlayFromStart()
        {
            try
            {
                await Spotify.SeekPlaybackAsync(0, Settings.Settings.SpotifyDeviceId);
            }
            catch (Exception)
            {
                //ignored
            }
        }

        public static async Task<bool> PlayPause()
        {
            PlaybackContext playback = await Spotify.GetPlaybackAsync();

            try
            {
                if (playback.IsPlaying)
                    await Spotify.PausePlaybackAsync(Settings.Settings.SpotifyDeviceId);
                else
                    await Spotify.ResumePlaybackAsync(Settings.Settings.SpotifyDeviceId, "", null, null, 0);
            }
            catch (Exception)
            {
                //ignored
            }
            return !playback.IsPlaying;
        }
    }

    public class TrackScore
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public int Score { get; set; }
    }
}