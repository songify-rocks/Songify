using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Timer = System.Timers.Timer;
using System.Linq;
using System.Text.Encodings.Web;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Songify_Slim.Util.Settings;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding Spotify-API integration
    public static class SpotifyApiHandler
    {
        private const string BaseUrl = "https://auth.overcode.tv";
        private const string ClientId = "6b08bd81685e4115b2c615e18499fe6c";
        private const string ClientSecret = "561d947c50324d8783b4e1601b5ceafd";
        private static EmbedIOAuthServer? _server;
        public static SpotifyClient? Client;
        private static readonly System.Timers.Timer AuthTimer = new()
        {
            Interval = 1000 * 60 * 30,
        };

        public static async Task Auth()
        {
            AuthTimer.Elapsed += AuthTimer_Elapsed;
            if (!(string.IsNullOrEmpty(Globals.spotifyCredentials!.AccessToken) || string.IsNullOrEmpty(Globals.spotifyCredentials.RefreshToken)))
            {
                Debug.WriteLine("Refreshing Tokens");
                RefreshTokens();
                return;
            }
            Debug.WriteLine("Getting new tokens");
            _server = new EmbedIOAuthServer(new Uri("http://localhost:4002/auth"), 4002);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> {
                            Scopes.UserReadPlaybackState , Scopes.UserReadPrivate , Scopes.UserModifyPlaybackState , Scopes.PlaylistModifyPublic , Scopes.PlaylistModifyPrivate , Scopes.PlaylistReadPrivate
                }
            };
            var uri = request.ToUri();
            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }
        }

        private static void AuthTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshTokens();
        }

        private static async void RefreshTokens()
        {
            var oauth = new OAuthClient();
            var refreshRequest = new TokenSwapRefreshRequest(
                  new Uri($"{BaseUrl}/refresh?id={ClientId}&secret={ClientSecret}"),
                  Globals.spotifyCredentials!.RefreshToken
                );
            var refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(ClientId, ClientSecret, new AuthorizationCodeTokenResponse { AccessToken = Globals.spotifyCredentials.AccessToken, RefreshToken = Globals.spotifyCredentials.RefreshToken }));

            Globals.spotifyCredentials = new SpotifyCredentials
            {
                AccessToken = refreshResponse.AccessToken,
                RefreshToken = Globals.spotifyCredentials.RefreshToken,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                DeviceId = "",
            };

            await ConfigHandler.SaveSettings(Globals.spotifyCredentials);

            Client = new SpotifyClient(config);
            if (!AuthTimer.Enabled)
            {
                AuthTimer.Start();
            }
        }

        private static async Task OnAuthorizationCodeReceived(object? sender, AuthorizationCodeResponse response)
        {
            Debug.WriteLine("Got here");
            var oauth = new OAuthClient();
            var tokenRequest = new TokenSwapTokenRequest(new Uri($"{BaseUrl}/swap?id={ClientId}&secret={ClientSecret}"), response.Code);
            var tokenResponse = await oauth.RequestToken(tokenRequest);
            Debug.WriteLine($"We got an access token from server: {tokenResponse.AccessToken}");

            var refreshRequest = new TokenSwapRefreshRequest(
              new Uri($"{BaseUrl}/refresh?id={ClientId}&secret={ClientSecret}"),
              tokenResponse.RefreshToken
            );
            var refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(ClientId, ClientSecret, tokenResponse));

            Globals.spotifyCredentials = new SpotifyCredentials
            {
                AuthorizationCode = response.Code,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                DeviceId = "",
            };

            await ConfigHandler.SaveSettings(Globals.spotifyCredentials);

            Client = new SpotifyClient(config);
            if (!AuthTimer.Enabled)
            {
                AuthTimer.Start();
            }
        }

        public static async Task<CurrentlyPlayingContext?> GetCurrentSongAsync()
        {
            if (Client == null) { return null; }
            try
            {
                return await Client.Player.GetCurrentPlayback();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<QueueResponse?> GetPlaybackQueue()
        {
            if (Client == null) { return null; }
            try
            {
                return await Client.Player.GetQueue();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<bool> AddToQueue(string v)
        {
            return await Client.Player.AddToQueue(new PlayerAddToQueueRequest(v));
        }
    }
}
}