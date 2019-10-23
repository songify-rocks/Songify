using System;
using System.Collections.Generic;
using System.Windows;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;


namespace Songify_Slim
{
    public static class APIHandler
    {
        public static TokenSwapWebAPIFactory webApiFactory;
        public static SpotifyWebAPI spotify;
        public static Token lastToken;
        public static bool authenticated;
        public static bool authed = false;

        private static TokenSwapAuth auth = new TokenSwapAuth(
            exchangeServerUri: "https://songify.bloemacher.com/auth/index.php",
            serverUri: "http://localhost:4002/auth",
            scope: Scope.UserReadPlaybackState | Scope.UserReadPrivate
        );

        public static async System.Threading.Tasks.Task DoAuthAsync()
        {
            if (!string.IsNullOrEmpty(Settings.RefreshToken))
            {
                authed = true;
                spotify = new SpotifyWebAPI()
                {
                    TokenType = (await auth.RefreshAuthAsync(Settings.RefreshToken)).TokenType,
                    AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken
                };
            }
            else
            {
                authed = false;
            }

            auth.AuthReceived += async (sender, response) =>
            {
                if (authed)
                    return;

                lastToken = await auth.ExchangeCodeAsync(response.Code);

                Settings.RefreshToken = lastToken.RefreshToken;

                spotify = new SpotifyWebAPI()
                {
                    TokenType = lastToken.TokenType,
                    AccessToken = lastToken.AccessToken
                };

                authenticated = true;
                auth.Stop();

                await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                 {
                     foreach (Window window in System.Windows.Application.Current.Windows)
                     {
                         if (window.GetType() != typeof(SettingsWindow)) continue;
                         ((SettingsWindow)window).SetControls();

                     }
                 }));


            };

            auth.OnAccessTokenExpired += async (sender, e) => spotify.AccessToken = (await auth.RefreshAuthAsync(Settings.RefreshToken)).AccessToken;
            auth.Start();
            if (authed)
                return;
            auth.OpenBrowser();
        }

        public static TrackInfo GetSongInfo()
        {
            PlaybackContext context = spotify.GetPlayingTrack();
            if (context.Item != null)
            {
                string artists = "";

                for (var i = 0; i < context.Item.Artists.Count; i++)
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
