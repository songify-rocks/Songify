using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private static EmbedIOAuthServer? _server;
        public static SpotifyClient? _client;
        private static Timer authTimer = new()
        {
            Interval = 1000 * 60 * 30,
        };

        public static async Task Auth()
        {
            authTimer.Elapsed += AuthTimer_Elapsed;
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

            var request = new LoginRequest(_server.BaseUri, Settings.Settings.ClientId, LoginRequest.ResponseType.Code)
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
        private static void AuthTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            RefreshTokens();
        }

        private async static void RefreshTokens()
        {
            var oauth = new OAuthClient();
            var refreshRequest = new TokenSwapRefreshRequest(
                  new Uri($"http://localhost:5544/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                  Settings.Settings.SpotifyRefreshToken
                );
            var refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(Settings.Settings.ClientId, Settings.Settings.ClientSecret, new AuthorizationCodeTokenResponse { AccessToken = Settings.Settings.SpotifyAccessToken, RefreshToken = Settings.Settings.SpotifyRefreshToken }));

            Settings.Settings.SpotifyAccessToken = refreshResponse.AccessToken;
            Settings.Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;
            Settings.Settings.SpotifyDeviceId = "";

            _client = new SpotifyClient(config);
            if (!authTimer.Enabled)
            {
                authTimer.Start();
            }
        }

        private static async Task OnAuthorizationCodeReceived(object? sender, AuthorizationCodeResponse response)
        {
            Debug.WriteLine("Got here");
            var oauth = new OAuthClient();
            var tokenRequest = new TokenSwapTokenRequest(new Uri($"http://localhost:5544/swap?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"), response.Code);
            var tokenResponse = await oauth.RequestToken(tokenRequest);
            Debug.WriteLine($"We got an access token from server: {tokenResponse.AccessToken}");

            var refreshRequest = new TokenSwapRefreshRequest(
              new Uri($"http://localhost:5544/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
              tokenResponse.RefreshToken
            );
            var refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(Settings.Settings.ClientId, Settings.Settings.ClientSecret, tokenResponse));

            Settings.Settings.SpotifyAccessToken = refreshResponse.AccessToken;
            Settings.Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;
            Settings.Settings.SpotifyDeviceId = "";

            _client = new SpotifyClient(config);
            if (!authTimer.Enabled)
            {
                authTimer.Start();
            }
        }

        public static async Task<CurrentlyPlayingContext?> GetCurrentSongAsync()
        {
            if (_client == null) { return null; }
            try
            {
                return await _client.Player.GetCurrentPlayback();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<FullTrack?> GetTrackById(string id)
        {
            if (_client == null) { return null; }
            try
            {
                return await _client.Tracks.Get(id);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<FullPlaylist?> GetPlaylistAsync(string id)
        {
            if (_client == null) { return null; }
            try
            {
                FullPlaylist playlist = await _client.Playlists.Get(id, new PlaylistGetRequest(PlaylistGetRequest.AdditionalTypes.Track));
                return playlist;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<SearchResponse?> SearchForItem(SearchRequest.Types type, string search)
        {
            if (_client == null) { return null; }
            try
            {
                return await _client.Search.Item(new SearchRequest(type, search));
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return null;
        }

        public static async Task<bool> AddToQueue(string songUri)
        {
            if (_client == null) { return false; }
            try
            {
                bool addSuccess = await _client.Player.AddToQueue(new PlayerAddToQueueRequest(songUri));
                return addSuccess;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return false;
        }

        public static async Task<bool> SkipCurrentSong()
        {
            if (_client == null) { return false; }
            try
            {
                bool skipSuccess = await _client.Player.SkipNext();
                return skipSuccess;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return false;
        }

        public static async Task AddSongToPlaylist(string playlistId, string uri)
        {
            if (_client == null) { return; }
            try
            {
                SnapshotResponse x = await _client.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(new List<string> { uri }));
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static async Task<List<FullTrack>?> GetCurrentQueue()
        {
            if (_client == null) { return null; }
            try
            {
                QueueResponse x = await _client.Player.GetQueue();
                if (x == null) { return null; }
                List<FullTrack> tracks = new();
                foreach (IPlayableItem item in x.Queue)
                    if (item.Type == ItemType.Track)
                        tracks.Add((FullTrack)item);

                return tracks;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return null;
        }

        public static async Task<bool> ResumePlayback()
        {
            if (_client == null) { return false; }
            try
            {
               return await _client.Player.ResumePlayback();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return false;
        }

        public static async Task<bool> PausePlayback()
        {
            if (_client == null) { return false; }
            try
            {
                return await _client.Player.PausePlayback();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
            return false;
        }
    }
}