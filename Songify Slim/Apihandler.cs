using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;


namespace Songify_Slim
{
    public static class APIHandler
    {
        public static TokenSwapWebAPIFactory webApiFactory = null;
        public static SpotifyWebAPI spotify;
        public static Token lastToken;
        public static bool authenticated;
        public static bool authed = false;
        public static Timer authRefresh = new Timer {
            Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds
        };
        

        private static TokenSwapAuth auth = new TokenSwapAuth(
            exchangeServerUri: "https://songify.bloemacher.com/auth/index.php",
            serverUri: "http://localhost:4002/auth",
            scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate
        );

        public static async void DoAuthAsync()
        {
            authRefresh.Elapsed += AuthRefresh_Elapsed;

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

            auth.AuthReceived += async (sender, response) =>
            {
                Console.WriteLine(DateTime.Now.ToShortTimeString() + " Auth Received");

                if (authed)
                    return;

                lastToken = await auth.ExchangeCodeAsync(response.Code);

                Settings.RefreshToken = lastToken.RefreshToken;
                Settings.AccessToken = lastToken.AccessToken;

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
                         if (window.GetType() != typeof(SettingsWindow)) continue;
                         ((SettingsWindow)window).SetControls();
                     }
                 }));
            };

            auth.OnAccessTokenExpired += async (sender, e) =>
            {
                spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
                Settings.RefreshToken = lastToken.RefreshToken;
                Settings.AccessToken = spotify.AccessToken;
                Console.WriteLine(DateTime.Now.ToShortTimeString() + " Auth Refreshed");
            };

            auth.Start();

            if (authed)
            {
                authRefresh.Start();
                return;
            }
            auth.OpenBrowser();
        }

        private static async void AuthRefresh_Elapsed(object sender, ElapsedEventArgs e)
        {
            spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
            Settings.AccessToken = spotify.AccessToken;
            Console.WriteLine(DateTime.Now.ToShortTimeString() + " Auth Refreshed (timer)");
        }

        public static TrackInfo GetSongInfo()
        {
            PlaybackContext context = spotify.GetPlayingTrack();
            if (!context.IsPlaying)
            {
                return null;
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

                List<Image> albums = context.Item.Album.Images;

                return new TrackInfo() { Artists = artists, Title = context.Item.Name, albums = albums };
            }

            return new TrackInfo() { Artists = "", Title = "" };
        }
    }

    public class TrackInfo
    {
        public string Artists { get; set; }

        public string Title { get; set; }

        public List<Image> albums { get; set; }
    }
}
