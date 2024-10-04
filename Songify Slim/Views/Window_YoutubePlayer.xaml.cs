using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Youtube;
using TwitchLib.Api.Helix.Models.Soundtrack;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_YoutubePlayer.xaml
    /// </summary>
    public partial class Window_YoutubePlayer
    {
        private string _currentSongId = "";
        private string _currentRequestId = "";
        private string currentSongRequester = "";

        public Window_YoutubePlayer()
        {
            InitializeComponent();
            //CefSettings settings = new CefSettings();
            //settings.CefCommandLineArgs.Add("disable-web-security", "true");
            //settings.CefCommandLineArgs.Add("allow-file-access-from-files", "true");
            //Cef.Initialize(settings);

            DgvPlaylist.ItemsSource = GlobalObjects.YoutubeRequests;

            // Adjust the path to where your HTML file is located
            string htmlFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "YouTubePlayer.html");
            browser.Address = new Uri(htmlFilePath).AbsoluteUri;

            // Handle messages from JavaScript
            browser.JavascriptMessageReceived += OnBrowserJavascriptMessageReceived;
        }

        private void OnBrowserJavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
        {
            string jsonMessage = e.Message.ToString();
            BaseMessage baseMessage = JsonConvert.DeserializeObject<BaseMessage>(jsonMessage);

            switch (baseMessage.Type)
            {
                case "VideoPlaying":
                    VideoPlayingMessage playingMessage = JsonConvert.DeserializeObject<VideoPlayingMessage>(jsonMessage);
                    HandleVideoPlayingMessage(playingMessage);
                    break;

                case "VideoEnded":
                    VideoEndedMessage endedMessage = JsonConvert.DeserializeObject<VideoEndedMessage>(jsonMessage);
                    HandleVideoEndedMessage(endedMessage);
                    break;

                case "PlaylistEnded":
                    HandlePlaylistEndedMessage();
                    break;

                default:
                    Debug.WriteLine($"Unknown message type: {baseMessage.Type}");
                    break;
            }
            UpdateDatagrid();
        }

        private void HandleVideoEndedMessage(VideoEndedMessage message)
        {
            string videoId = message.VideoId;
            bool isPlaylist = message.IsPlaylist;
            
            if (string.IsNullOrEmpty(message.Title))
                return;

            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"VideoEnded: Video ID: {videoId}, IsPlaylist: {isPlaylist}");
            });

            if (GlobalObjects.YoutubeRequests.Count > 0)
            {
                string nextVideoId = GetNextVideoId();
                QueueNextVideo(nextVideoId);
            }
            else if (!isPlaylist)
            {
                QueueNextPlaylist("");
            }
            else
            {
                // Let the playlist continue
            }
            UpdateDatagrid();
        }

        private void HandleVideoPlayingMessage(VideoPlayingMessage message)
        {
            string videoId = message.VideoId;
            string videoTitle = message.Title;
            bool isPlaylist = message.IsPlaylist;

            if (GlobalObjects.CurrentSong != null && _currentSongId == videoId && (!string.IsNullOrEmpty(GlobalObjects.CurrentSong.Artists) || !string.IsNullOrEmpty(GlobalObjects.CurrentSong.Title)))
                return;

            TrackInfo trackinfo = null;

            if (message.State == Enums.YoutubePlayerState.Paused)
            {
                trackinfo = Settings.CustomPauseTextEnabled
                    ? new TrackInfo { Artists = Settings.CustomPauseText, Title = "" }
                    : new TrackInfo { Artists = "", Title = "" };
            }

            string artist = videoTitle;
            string title = "";
            string extra = "";

            if (videoTitle.Contains(" - "))
            {
                string[] parts = videoTitle.Split(new string[] { " - " }, StringSplitOptions.None);
                artist = parts[0].Trim();
                title = parts[1].Trim();
            }

            trackinfo = new TrackInfo { Artists = artist, Title = title };

            string output = Settings.OutputString;

            output = output.Format(
                artist => trackinfo.Artists ?? "",
                single_artist => trackinfo.Artists ?? "",
                title => trackinfo.Title ?? "",
                extra => "",
                uri => trackinfo.SongId ?? "",
                url => trackinfo.Url ?? ""
            ).Format();


            if (!string.IsNullOrEmpty(currentSongRequester))
            {

                output = output.Replace("{{", "");
                output = output.Replace("}}", "");
                output = output.Replace("{req}", currentSongRequester);
                GlobalObjects.Requester = currentSongRequester;
            }
            else
            {
                try
                {
                    int start = output.IndexOf("{{", StringComparison.Ordinal);
                    int end = output.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) output = output.Remove(start, end - start);
                    GlobalObjects.Requester = "";
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }

            output = output.Trim();

            GlobalObjects.CurrentSong = trackinfo;

            IOManager.WriteOutput($"{GlobalObjects.RootDirectory}/songify.txt", output.Trim());

            if (Settings.SplitOutput)
            {
                IOManager.WriteSplitOutput(trackinfo?.Artists, trackinfo?.Title, "");
            }

            if (Settings.Upload)
                try
                {
                    WebHelper.UploadSong(output.Trim().Replace(@"\n", " - ").Replace("  ", " "));
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                    // if error occurs write text to the status asynchronous
                    Application.Current.MainWindow?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        (((MainWindow)Application.Current.MainWindow)!).LblStatus.Content = "Error uploading Song information";
                    }));
                }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                main?.img_cover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    main.SetTextPreview(output);
                }));
                this.Title = "Now Playing: " + videoTitle;
                Debug.WriteLine($"VideoPlaying: {videoTitle}, Video ID: {videoId}, IsPlaylist: {isPlaylist}");
            });
            _currentSongId = videoId;
        }

        private void HandlePlaylistEndedMessage()
        {
            // Same as before
        }

        private string GetNextVideoId()
        {
            YoutubeRequest nextVideo = GlobalObjects.YoutubeRequests.FirstOrDefault();
            if (nextVideo != null)
            {
                currentSongRequester = nextVideo.Requester;
                _currentRequestId = nextVideo.VideoId;
                return nextVideo.VideoId;
            }
            else
            {
                currentSongRequester = "";
                _currentRequestId = "";
                return "";
            }
        }

        private void QueueNextVideo(string videoid = "")
        {
            // JavaScript code to load, mute, and play the video
            string script = $@"
                            player.loadVideoById('{videoid}');
                            player.mute();
                            player.playVideo();
                            player.unMute();";


            // Execute the JavaScript code in the browser
            browser.ExecuteScriptAsync(script);

            if (string.IsNullOrEmpty(videoid)) return;
            YoutubeRequest x = GlobalObjects.YoutubeRequests.FirstOrDefault(o => o.VideoId == videoid);
            if (x == null) return;
            Debug.WriteLine($"Deleting {x.Title}");
            GlobalObjects.YoutubeRequests.Remove(x);
        }

        private void QueueNextPlaylist(string playlistId)
        {
            currentSongRequester = "";
            _currentRequestId = "";

            playlistId = "PLNE3b80YbdkmpG-ZZ2O-DOxIZyMuIT9_4";
            string script = $@"
                            player.loadPlaylist({{
                                listType: 'playlist',
                                list: '{playlistId}',
                            }});
                            player.setShuffle(true);
                            player.playVideo();
                        ";
            browser.ExecuteScriptAsync(script);
        }

        private void Button_Play_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxYoutubeUrl.Text))
            {
                string nextVideoId = GetNextVideoId();
                QueueNextVideo(nextVideoId);
                //QueueNextPlaylist("");
                return;
            }
            // Get the video ID from the youtube URL and queue it using QueuenExtVideo()
            // https://www.youtube.com/watch?v=U3iqyuVjaX0&pp=ygUKbW9uc3RlcmNhdA%3D%3D
            string videoId = TextBoxYoutubeUrl.Text.Split(new string[] { "v=" }, StringSplitOptions.None)[1];
            if (videoId.Contains("&"))
                videoId = videoId.Split('&')[0];
            QueueNextVideo(videoId);

        }

        public void UpdateDatagrid()
        {
            // invoke the dispatcher to update the datagrid on the UI thread
            Dispatcher.Invoke(() =>
            {
                DgvPlaylist.ItemsSource = null;
                DgvPlaylist.ItemsSource = GlobalObjects.YoutubeRequests;
            });
        }
    }
}
