using System;
using System.Collections.Generic;
using System.Timers;
using System.Web.UI;
using System.Windows;
using Songify_Slim.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding Spotify-API integration
    public static class ApiHandler
    {
        public static SpotifyWebAPI Spotify;
        public static Token LastToken;
        public static bool Authenticated;
        public static bool Authed;
        public static Timer AuthRefresh = new Timer
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
                    exchangeServerUri: "https://songify.rocks/auth/auth.php?id=" + Settings.Settings.ClientId + "&secret=" + Settings.Settings.ClientSecret,
                    serverUri: "http://localhost:4002/auth",
                    scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState
                );
                Console.WriteLine(@"Own ID");
            }
            else
            {
                _auth = new TokenSwapAuth(
                    exchangeServerUri: "https://songify.rocks/auth/_index.php",
                    serverUri: "http://localhost:4002/auth",
                    scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState
                );
                Console.WriteLine(@"Songify ID");
            }

            try
            {

                // Execute the authentication flow and subscribe the timer elapsed event
                AuthRefresh.Elapsed += AuthRefresh_Elapsed;

                // If Refresh and Access-token are present, just refresh the auth
                if (!string.IsNullOrEmpty(Settings.Settings.RefreshToken) && !string.IsNullOrEmpty(Settings.Settings.AccessToken))
                {
                    Authed = true;
                    Spotify = new SpotifyWebAPI()
                    {
                        TokenType = (await _auth.RefreshAuthAsync(Settings.Settings.RefreshToken)).TokenType,
                        AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.RefreshToken)).AccessToken
                    };
                    Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.RefreshToken)).AccessToken;
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

                    LastToken = await _auth.ExchangeCodeAsync(response.Code);
                    // Save tokens
                    Settings.Settings.RefreshToken = LastToken.RefreshToken;
                    Settings.Settings.AccessToken = LastToken.AccessToken;
                    // create ne Spotify object
                    Spotify = new SpotifyWebAPI()
                    {
                        TokenType = LastToken.TokenType,
                        AccessToken = LastToken.AccessToken
                    };
                    Authenticated = true;
                    _auth.Stop();
                    AuthRefresh.Start();
                    await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                     {
                         foreach (Window window in Application.Current.Windows)
                         {
                             if (window.GetType() != typeof(Window_Settings)) continue;
                             ((Window_Settings)window).SetControls();
                         }
                     }));
                };

                // automatically refreshes the token after it expires
                _auth.OnAccessTokenExpired += async (sender, e) =>
                {
                    Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.RefreshToken)).AccessToken;
                    Settings.Settings.RefreshToken = LastToken.RefreshToken;
                    Settings.Settings.AccessToken = Spotify.AccessToken;
                };

                _auth.Start();

                if (Authed)
                {
                    AuthRefresh.Start();
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
                Spotify.AccessToken = (await _auth.RefreshAuthAsync(Settings.Settings.RefreshToken)).AccessToken;
                Settings.Settings.AccessToken = Spotify.AccessToken;
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
                Logger.LogStr("Couldn't fetch Song info");
                return new TrackInfo() { Artists = "", Title = "" };
            }

            if (context.Error != null)
            {
                Logger.LogStr(context.Error.Status + " | " + context.Error.Message);
            }

            if (context.Item == null) return new TrackInfo() {Artists = "", Title = ""};
            
            
            string artists = "";

            for (int i = 0; i < context.Item.Artists.Count; i++)
            {
                if (i != context.Item.Artists.Count - 1)
                    artists += context.Item.Artists[i].Name + ", ";
                else
                    artists += context.Item.Artists[i].Name;
            }

            if (context.Device != null)
                Settings.Settings.SpotifyDeviceId = context.Device.Id;

            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + context.Device.Id);

            List<Image> albums = context.Item.Album.Images;

            return new TrackInfo()
            {
                Artists = artists,
                Title = context.Item.Name,
                albums = albums,
                SongID = context.Item.Id,
                DurationMS = context.Item.DurationMs - context.ProgressMs,
                isPlaying = context.IsPlaying
            };

        }

        public static SearchItem GetArtist(string searchStr)
        {
            // returns Artist matching the search string
            return Spotify.SearchItems(searchStr, SearchType.Artist, 1);
        }

        public static ErrorResponse AddToQ(string songUri)
        {
            // Tries to add a song to the current playback queue
            ErrorResponse error = Spotify.AddToQueue(songUri, Settings.Settings.SpotifyDeviceId);
            return error;
        }

        public static FullTrack GetTrack(string id)
        {
            // Returns a Track-Object matching the song id
            return Spotify.GetTrack(id);
        }

        public static SearchItem FindTrack(string searchQuery)
        {
            // Returns a Track-Object matching a search query (artist - title). It only returns the first match which is found
            return Spotify.SearchItems(searchQuery, SearchType.Track, 1);
        }

        public static bool GetPlaybackState()
        {
            // Returns a bool wether the playbackstate is playing or not (used for custom pause text)
            return Spotify.GetPlayback().IsPlaying;
        }
    }
}
