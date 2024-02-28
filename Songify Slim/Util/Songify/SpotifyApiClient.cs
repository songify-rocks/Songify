using Songify_Slim.Util.Settings;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Songify
{
    internal class SpotifyApiClient
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
            if (!(string.IsNullOrEmpty(Settings.Settings.SpotifyAccessToken) || string.IsNullOrEmpty(Settings.Settings.SpotifyRefreshToken)))
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
                  Settings.Settings.SpotifyRefreshToken
                );
            var refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(
                new AuthorizationCodeAuthenticator(ClientId, ClientSecret, new AuthorizationCodeTokenResponse
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

        public static async Task<bool> SkipSong()
        {
            return await Client.Player.SkipNext();
        }

        public static async Task<PrivateUser> GetProfileAsync()
        {
            return await Client.UserProfile.Current();
        }
    }
}
