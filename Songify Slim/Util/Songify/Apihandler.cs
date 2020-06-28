using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using Songify_Slim.Models;


namespace Songify_Slim
{
    // This class handles everything regarding Spotify-API integration
    public static class APIHandler
    {
        public static TokenSwapWebAPIFactory webApiFactory = null;
        public static SpotifyWebAPI spotify;
        public static Token lastToken;
        public static bool authenticated;
        public static bool authed = false;
        public static Timer authRefresh = new Timer
        {
            // Interval for refreshing Spotify-Auth
            Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds
        };



        // Spotify Authentication flow with the webserver
        private static TokenSwapAuth auth;

        public static async void DoAuthAsync()
        {
            if (Settings.UseOwnApp)
            {
                auth = new TokenSwapAuth(
                    exchangeServerUri: "https://songify.rocks/auth/auth.php?id=" + Settings.ClientID + "&secret=" + Settings.ClientSecret,
                    serverUri: "http://localhost:4002/auth",
                    scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState
                );
                Console.WriteLine("Own ID");
            }
            else
            {
                auth = new TokenSwapAuth(
                    exchangeServerUri: "https://songify.rocks/auth/_index.php",
                    serverUri: "http://localhost:4002/auth",
                    scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState
                );
                Console.WriteLine("Songify ID");
            }

            try
            {

                // Execute the authentication flow and subscribe the timer elapsed event
                authRefresh.Elapsed += AuthRefresh_Elapsed;

                // If Refresh and Accesstoken are present, just refresh the auth
                if (!string.IsNullOrEmpty(Settings.RefreshToken) && !string.IsNullOrEmpty(Settings.AccessToken))
                {
                    authed = true;
                    spotify = new SpotifyWebAPI()
                    {
                        TokenType = (await auth.RefreshAuthAsync(Settings.RefreshToken)).TokenType,
                        AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken
                    };
                    spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
                }
                else
                {
                    authed = false;
                }

                // if the auth was successfull save the new tokens and 
                auth.AuthReceived += async (sender, response) =>
                {

                    if (authed)
                        return;

                    lastToken = await auth.ExchangeCodeAsync(response.Code);
                    // Save tokens
                    Settings.RefreshToken = lastToken.RefreshToken;
                    Settings.AccessToken = lastToken.AccessToken;
                    // create ne Spotify object
                    spotify = new SpotifyWebAPI()
                    {
                        TokenType = lastToken.TokenType,
                        AccessToken = lastToken.AccessToken
                    };
                    authenticated = true;
                    auth.Stop();
                    authRefresh.Start();
                    await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                     {
                         foreach (Window window in System.Windows.Application.Current.Windows)
                         {
                             if (window.GetType() != typeof(Window_Settings)) continue;
                             ((Window_Settings)window).SetControls();
                         }
                     }));
                };

                // autmatically refreshes the token after it expires
                auth.OnAccessTokenExpired += async (sender, e) =>
                {
                    spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
                    Settings.RefreshToken = lastToken.RefreshToken;
                    Settings.AccessToken = spotify.AccessToken;
                };

                auth.Start();

                if (authed)
                {
                    authRefresh.Start();
                    return;
                }
                auth.OpenBrowser();

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
                spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
                Settings.AccessToken = spotify.AccessToken;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static TrackInfo GetSongInfo()
        {
            // returns the trackinfo of the current playback (used in the fetch timer) 
            PlaybackContext context = spotify.GetPlayback();
            if (context.Error != null)
            {
                Logger.LogStr(context.Error.Status + " | " + context.Error.Message);
            }

            if (context.Item != null)
            {
                string artists = "";

                for (int i = 0; i < context.Item.Artists.Count; i++)
                {
                    if (i != context.Item.Artists.Count - 1)
                        artists += context.Item.Artists[i].Name + ", ";
                    else
                        artists += context.Item.Artists[i].Name;
                }

                if (context.Device != null)
                    Settings.SpotifyDeviceID = context.Device.Id;

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + context.Device.Id);

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

            return new TrackInfo() { Artists = "", Title = "" };
        }

        public static SearchItem GetArtist(string searchStr)
        {
            // returns Artist matching the search string
            return spotify.SearchItems(searchStr, SearchType.Artist, 1);
        }

        public static ErrorResponse AddToQ(string SongURI)
        {
            // Tries to add a song to the current playback queue
            ErrorResponse error = spotify.AddToQueue(SongURI, Settings.SpotifyDeviceID);
            return error;
        }

        public static FullTrack GetTrack(string id)
        {
            // Returns a Track-Object matching the song id
            return spotify.GetTrack(id);
        }

        public static SearchItem FindTrack(string searchQuery)
        {
            // Returns a Track-Object matching a search query (artist - title). It only returns the first match which is found
            return spotify.SearchItems(searchQuery, SearchType.Track, 1);
        }

        public static bool GetPlaybackState()
        {
            // Returns a bool wether the playbackstate is playing or not (used for custom pause text)
            return spotify.GetPlayback().IsPlaying;
        }
    }
}
