using Songify_Slim.Util.Settings;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Text.Encodings.Web;
using System.Threading;
using System.Web.UI.WebControls.Expressions;
using Songify_Slim.Models.YTMD;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Spotify
{
    public static class SpotifyApiClient
    {
        private const string BaseUrl = "https://auth.overcode.tv";
        private static EmbedIOAuthServer _server;
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

            LoginRequest request = new LoginRequest(_server.BaseUri, Settings.Settings.ClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> {
                            Scopes.UserReadPlaybackState , Scopes.UserReadPrivate , Scopes.UserModifyPlaybackState , Scopes.PlaylistModifyPublic , Scopes.PlaylistModifyPrivate , Scopes.PlaylistReadPrivate
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

        private static void AuthTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshTokens();
        }

        private static async void RefreshTokens()
        {
            try
            {
                OAuthClient oauth = new OAuthClient();
                TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
                    new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
                    Settings.Settings.SpotifyRefreshToken
                );
                AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

                Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(Settings.Settings.ClientId, Settings.Settings.ClientSecret, new AuthorizationCodeTokenResponse { AccessToken = Settings.Settings.SpotifyAccessToken, RefreshToken = Settings.Settings.SpotifyRefreshToken }));


                Settings.Settings.SpotifyAccessToken = refreshResponse.AccessToken;
                Settings.Settings.SpotifyRefreshToken = refreshResponse.RefreshToken;

                Client = new SpotifyClient(config);

                if (!AuthTimer.Enabled)
                {
                    AuthTimer.Start();
                }
            }
            catch (Exception e)
            {
                throw; // TODO handle exception
            }
        }

        private static async Task OnAuthorizationCodeReceived(object? sender, AuthorizationCodeResponse response)
        {
            Debug.WriteLine("Got here");
            OAuthClient oauth = new OAuthClient();
            TokenSwapTokenRequest tokenRequest = new TokenSwapTokenRequest(new Uri($"{BaseUrl}/swap?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"), response.Code);
            AuthorizationCodeTokenResponse tokenResponse = await oauth.RequestToken(tokenRequest);
            Debug.WriteLine($"We got an access token from server: {tokenResponse.AccessToken}");

            TokenSwapRefreshRequest refreshRequest = new TokenSwapRefreshRequest(
              new Uri($"{BaseUrl}/refresh?id={Settings.Settings.ClientId}&secret={Settings.Settings.ClientSecret}"),
              tokenResponse.RefreshToken
            );
            AuthorizationCodeRefreshResponse refreshResponse = await oauth.RequestToken(refreshRequest);

            Debug.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");

            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new AuthorizationCodeAuthenticator(Settings.Settings.ClientId, Settings.Settings.ClientSecret, tokenResponse));


            Settings.Settings.SpotifyAccessToken = tokenResponse.AccessToken;
            Settings.Settings.SpotifyRefreshToken = tokenResponse.RefreshToken;


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
                CurrentlyPlayingContext currentPlayingContext = await Client.Player.GetCurrentPlayback();
                return currentPlayingContext;
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
            return await Client?.Player.AddToQueue(new PlayerAddToQueueRequest(v))!;
        }



        public static async Task<SearchResponse> GetArtist(string searchStr)
        {
            if (Client == null) { return null; }
            try
            {
                SearchResponse x = await Client.Search.Item(new SearchRequest(SearchRequest.Types.Artist, searchStr));
                return x;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error getting Artist");
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<FullTrack> GetTrack(string id)
        {
            if (Client == null) { return null; }
            try
            {
                FullTrack x = await Client.Tracks.Get(id);
                return x;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error getting Track");
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task<SearchResponse> FindTrack(string searchQuery)
        {
            if (Client == null) { return null; }
            try
            {
                SearchResponse x = await Client.Search.Item(new SearchRequest(SearchRequest.Types.Artist, UrlEncoder.Default.Encode(searchQuery)));
                return x;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error getting Artist");
                Logger.LogExc(ex);
                return null;
            }
        }


        public static async Task<bool> CheckOrAddTrackToPlaylist(string trackId)
        {
            if (Client == null) { return false; }
            string playlistId = Settings.Settings.SpotifyPlaylistId;

            try
            {
                // Get all playlist items (handle pagination)
                List<PlaylistTrack<IPlayableItem>> allTracks = [];
                string next = null;

                do
                {
                    PlaylistGetItemsRequest request = new PlaylistGetItemsRequest
                    {
                        Limit = 100,
                        Offset = allTracks.Count
                    };

                    Paging<PlaylistTrack<IPlayableItem>> page = await Client.Playlists.GetItems(playlistId, request);
                    if (page.Items != null)
                    {
                        allTracks.AddRange(page.Items);
                    }

                    next = page.Next;
                }
                while (!string.IsNullOrEmpty(next));

                // Check if the track already exists
                if (allTracks.Any(t =>
                        t.Track is FullTrack ft && ft.Id == trackId))
                {
                    return true;
                }

                // Add the track
                PlaylistAddItemsRequest addRequest = new([$"spotify:track:{trackId}"]);
                await Client.Playlists.AddItems(playlistId, addRequest);

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error checking or adding track");
                Logger.LogExc(ex);
                return false;
            }
        }


        public static async Task<bool> SkipSong()
        {
            if (Client == null) { return false; }

            try
            {
                bool response = await Client.Player.SkipNext();
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error skipping song");
                Logger.LogExc(ex);
                return false;
            }
        }

        public static async Task<QueueResponse> GetQueueInfo()
        {
            if (Client == null) { return null; }
            try
            {
                QueueResponse queue = await Client.Player.GetQueue();
                return queue;
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error getting queue info");
                Logger.LogExc(ex);
                return null;
            }
        }

        public static async Task SkipPrevious()
        {
            if (Client == null) { return; }
            try
            {
                PlayerSkipPreviousRequest options = new PlayerSkipPreviousRequest
                {
                    DeviceId = Settings.Settings.SpotifyDeviceId
                };
                await Client.Player.SkipPrevious(options);
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error skipping previous song");
                Logger.LogExc(ex);
            }
        }

        public static async Task PlayFromStart()
        {
            if (Client == null) { return; }
            try
            {
                PlayerSeekToRequest request = new PlayerSeekToRequest(0)
                {
                    DeviceId = Settings.Settings.SpotifyDeviceId
                };
                await Client.Player.SeekTo(request);
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error seeking to start");
                Logger.LogExc(ex);
            }
        }

        public static async Task<bool> PlayPause()
        {
            if (Client == null) { return false; }

            CurrentlyPlayingContext playback = await Client.Player.GetCurrentPlayback();

            try
            {
                if (playback.IsPlaying == true)
                {
                    PlayerPausePlaybackRequest pauseRequest = new PlayerPausePlaybackRequest
                    {
                        DeviceId = Settings.Settings.SpotifyDeviceId
                    };
                    await Client.Player.PausePlayback(pauseRequest);
                }
                else
                {
                    PlayerResumePlaybackRequest resumeRequest = new PlayerResumePlaybackRequest
                    {
                        DeviceId = Settings.Settings.SpotifyDeviceId,
                        // You can optionally set context_uri, uris, offset, position_ms if needed
                    };
                    await Client.Player.ResumePlayback(resumeRequest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogStr("SPOTIFY: Error playing/pausing");
                Logger.LogExc(ex);
                return false;
            }

            return playback.IsPlaying;
        }

    }
}
