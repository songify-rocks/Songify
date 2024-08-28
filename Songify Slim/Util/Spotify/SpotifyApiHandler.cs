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
using Songify_Slim.Util.Spotify.SpotifyAPI.Web;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Timer = System.Timers.Timer;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Unosquare.Swan.Formatters;
using System.Linq;
using System.Text.Encodings.Web;
using System.Web.Util;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Songify_Slim.Util.Songify
{
    // This class handles everything regarding Spotify-API integration
    public static class SpotifyApiHandler
    {
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
            if (Settings.Settings.UseOwnApp)
            {
                _auth = new TokenSwapAuth(
                    $"{url}/auth/auth.php?id=" + Settings.Settings.ClientId +
                    "&secret=" + Settings.Settings.ClientSecret,
                    "http://localhost:4002/auth",
                    Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
                    Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate
                );
            }
            else
            {
                _auth = new TokenSwapAuth(
                    $"{url}/auth/_index.php",
                    "http://localhost:4002/auth",
                    Scope.UserReadPlaybackState | Scope.UserReadPrivate | Scope.UserModifyPlaybackState |
                    Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate | Scope.PlaylistReadPrivate
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
                    Token token = await _auth.RefreshAuthAsync(Settings.Settings.SpotifyRefreshToken);
                    if(token == null)
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
                            new Action(async () =>
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
                                GlobalObjects.SpotifyProfile = await Spotify.GetPrivateProfileAsync();
                                Settings.Settings.SpotifyProfile = GlobalObjects.SpotifyProfile;
                                Logger.LogStr(
                                    $"SPOTIFY: Connected Account: {GlobalObjects.SpotifyProfile.DisplayName}");
                                Logger.LogStr($"SPOTIFY: Account Type: {GlobalObjects.SpotifyProfile.Product}");
                                if (GlobalObjects.SpotifyProfile.Product == "premium") return;
                                ((MainWindow)Application.Current.MainWindow).IconWebSpotify.Foreground =
                                    Brushes.DarkOrange;
                                MessageBox.Show(
                                    "Spotify Premium is required to perform song requests. This is a limitation by Spotify, not by us.",
                                    "Spotify Premium required", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            PlaylistInfo playlistInfo = null;
            try
            {
                if (context.Context is { Type: "playlist" })
                {
                    if (GlobalObjects.CurrentSong == null || GlobalObjects.CurrentSong.SongId != context.Item.Id)
                    {
                        FullPlaylist playlist = Spotify.GetPlaylist(context.Context.Uri.Split(':')[2]);
                        if (playlist != null)
                        {
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
                }
            }
            catch (Exception ex)
            {
                // ignored because it's not important if the playlist info can't be fetched
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
                Playlist = playlistInfo,
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
                Debug.WriteLine(Json.Serialize(x));
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
                Debug.WriteLine(searchQuery);
                Debug.WriteLine(newQuery);
                SearchItem search = Spotify.SearchItems(newQuery, SearchType.Track, 1);
                return search;

                foreach (FullTrack track in search.Tracks.Items)
                {
                    Debug.WriteLine(string.Join(", ", track.Artists.Select(a => a.Name).ToList()) + " - " + track.Name);
                }

                List<TrackScore> trackScores = (from track in search.Tracks.Items
                                                let combined = string.Join(" ", track.Artists.Select(a => a.Name)) + " " + track.Name
                                                let score = LevenshteinDistance(searchQuery, combined)
                                                select new TrackScore
                                                {
                                                    TrackName = track.Name,
                                                    ArtistName = string.Join(", ", track.Artists.Select(a => a.Name)),
                                                    Score = score
                                                }).ToList();

                // To find the best match from the list
                TrackScore bestMatch = trackScores.OrderBy(ts => ts.Score).FirstOrDefault();

                if (bestMatch == null)
                    return search;

                // Find the FullTrack object for the best match
                FullTrack bestFullTrack = search.Tracks.Items.FirstOrDefault(track => track.Name == bestMatch.TrackName && string.Join(", ", track.Artists.Select(a => a.Name)) == bestMatch.ArtistName);

                return new SearchItem()
                {
                    Error = search.Error,
                    Tracks = new Paging<FullTrack>()
                    {
                        Items = new List<FullTrack> { bestFullTrack }
                    }
                };
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }
        }


        private static int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            int sourceLength = source.Length;
            int targetLength = target.Length;

            int[,] distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetLength; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
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

        public static async Task<SimpleQueue> GetQueueInfo()
        {
            return await Spotify.GetQueueAsync();
        }
    }

    public class TrackScore
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public int Score { get; set; }
    }
}