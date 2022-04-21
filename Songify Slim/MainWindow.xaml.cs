using AutoUpdaterDotNET;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Timer = System.Threading.Timer;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace Songify_Slim
{
    public partial class MainWindow
    {
        #region Variables

        private static string _version;
        private bool _appActive;
        public string CurrSong;
        public string _artist, _title;
        public NotifyIcon _notifyIcon = new NotifyIcon();
        public readonly List<RequestObject> ReqList = new List<RequestObject>();
        private string _songPath, _coverPath, _root, _coverTemp;
        private readonly BackgroundWorker _workerTelemetry = new BackgroundWorker();
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private readonly TimeSpan _periodTimeSpan = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _startTimeSpan = TimeSpan.Zero;
        private bool _firstRun = true;
        private bool _forceClose;
        private string _prevSong;
        private string _selectedSource;
        private string _temp = "";
        private Timer _timer;
        private System.Timers.Timer _timerFetcher = new System.Timers.Timer();
        private string _prevId, _currentId;
        private readonly System.Timers.Timer _songTimer = new System.Timers.Timer();
        private CancellationTokenSource s_cts;

        #endregion Variables

        public MainWindow()
        {
            InitializeComponent();
            Left = Settings.PosX;
            Top = Settings.PosY;
            _songTimer.Elapsed += SongTimer_Elapsed;
            // Backgroundworker for telemetry, and methods
            _workerTelemetry.DoWork += Worker_Telemetry_DoWork;
        }

        private static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            // This method checks if a window of type <T> is already opened in the current application context and returns true or false
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().Any()
                : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        public static void RegisterInStartup(bool isChecked)
        {
            // Adding the RegKey for Songify in startup (autostart with windows)
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                true);
            if (isChecked)
                registryKey?.SetValue("Songify",
                    Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException());
            else
                registryKey?.DeleteValue("Songify");

            Settings.Autostart = isChecked;
        }

        public void UploadSong(string currSong, string coverUrl = null)
        {
            if (currSong == null)
                return;
            try
            {
                // extras are UUID and Songinfo
                string extras = Settings.Uuid +
                                "&song=" + HttpUtility.UrlEncode(currSong.Trim().Replace("\"", ""), Encoding.UTF8) +
                                "&cover=" + HttpUtility.UrlEncode(coverUrl, Encoding.UTF8);
                string url = "https://songify.rocks/song.php?id=" + extras;
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if (myHttpWebResponse.StatusCode != HttpStatusCode.OK)
                    Logger.LogStr("MAIN: Upload Song:" + myHttpWebResponse.StatusCode);

                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                // if error occurs write text to the status asynchronous
                LblStatus.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
            }
        }

        private void Worker_Telemetry_DoWork(object sender, DoWorkEventArgs e)
        {
            // Backgroundworker is asynchronous
            // sending a webrequest that parses parameters to php code
            // it sends the UUID (randomly generated on first launch), unix timestamp, version number and if the app is active
            try
            {
                int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                string extras = Settings.Uuid + "&tst=" + unixTimestamp + "&v=" + _version + "&a=" + _appActive;
                string url = "https://songify.rocks/songifydata.php/?id=" + extras;
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                // Writing to the statusstrip label
                LblStatus.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
            }
        }

        private void AddSourcesToSourceBox()
        {
            string[] sourceBoxItems =
            {
                PlayerType.SpotifyWeb, PlayerType.SpotifyLegacy,
                PlayerType.Deezer, PlayerType.FooBar2000,/* PlayerType.Nightbot,*/ PlayerType.VLC, PlayerType.Youtube
            };
            cbx_Source.ItemsSource = sourceBoxItems;
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            // Opens the 'About'-Window
            AboutWindow aW = new AboutWindow { Top = Top, Left = Left };
            aW.ShowDialog();
        }

        private void BtnDiscord_Click(object sender, RoutedEventArgs e)
        {
            // Opens Discord-Invite Link in Standard-Browser
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnFAQ_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://songify.rocks/faq.html");
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/songify-rocks/Songify/issues");
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            // Opens the History in either Window or Browser
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Tag.ToString().Contains("Window"))
            {
                if (!IsWindowOpen<HistoryWindow>())
                {
                    // Opens the 'History'-Window
                    HistoryWindow hW = new HistoryWindow { Top = Top, Left = Left };
                    hW.ShowDialog();
                }
            }
            // Opens the Queue in the Browser
            else if (item.Tag.ToString().Contains("Browser"))
            {
                Process.Start("https://songify.rocks/history.php?id=" + Settings.Uuid);
            }
        }

        private void BtnPaypal_Click(object sender, RoutedEventArgs e)
        {
            // links to the projects patreon page (the button name is old because I used to use paypal)
            Process.Start("https://www.patreon.com/Songify");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Opens the 'Settings'-Window
            Window_Settings sW = new Window_Settings { Top = Top, Left = Left };
            sW.ShowDialog();
        }

        private void BtnTwitch_Click(object sender, RoutedEventArgs e)
        {
            // Tries to connect to the twitch service given the credentials in the settings or disconnects
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Tag.ToString().Equals("Connect"))
                // Connects
                TwitchHandler.BotConnect();
            else if (item.Tag.ToString().Equals("Disconnect"))
                // Disconnects
                TwitchHandler.Client.Disconnect();
        }

        private async void BtnWidget_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Upload)
                Process.Start("https://widget.songify.rocks/" + Settings.Uuid);
            else
            {
                // After user confirmation sends a command to the webserver which clears the queue
                MessageDialogResult msgResult = await this.ShowMessageAsync("",
                    "The widget only works if \"Upload Song Info\" is enabled. You can find this option under Settings -> Output.\n\n\nDo you want to activate it now?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    Settings.Upload = true;
                    Process.Start("https://widget.songify.rocks/" + Settings.Uuid);
                }
            }
        }

        private void Cbx_Source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                // This prevents that the selected is always 0 (initialize components)
                return;

            _selectedSource = cbx_Source.SelectedValue.ToString();

            Settings.Source = cbx_Source.SelectedIndex;

            // Dpending on which source is chosen, it starts the timer that fetches the song info
            SetFetchTimer();

            if (_selectedSource == PlayerType.SpotifyWeb)
                img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (_selectedSource == PlayerType.SpotifyWeb && Settings.DownloadCover)
                        img_cover.Visibility = Visibility.Visible;
                    else
                        img_cover.Visibility = Visibility.Collapsed;
                }));
        }

        private string CleanFormatString(string currSong)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            currSong = regex.Replace(currSong, " ");
            currSong = currSong.Trim();

            // Add trailing spaces for better scroll
            if (Settings.AppendSpaces)
                for (int i = 0; i < Settings.SpaceCount; i++)
                    currSong += " ";

            return currSong;
        }

        private void DownloadCover(string cover)
        {
            try
            {
                if (cover == null)
                {
                    // create Empty png file
                    Bitmap bmp = new Bitmap(640, 640);
                    Graphics g = Graphics.FromImage(bmp);

                    g.Clear(Color.Transparent);
                    g.Flush();
                    bmp.Save(_coverPath, ImageFormat.Png);
                }

                else
                {
                    const int numberOfRetries = 5;
                    const int delayOnRetry = 1000;

                    for (int i = 1; i < numberOfRetries; i++)
                        try
                        {
                            Uri uri = new Uri(cover);
                            // Downloads the album cover to the filesystem
                            WebClient webClient = new WebClient();
                            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                            webClient.DownloadFileAsync(uri, _coverTemp);
                            break;
                        }
                        catch (Exception) when (i <= numberOfRetries)
                        {
                            Thread.Sleep(delayOnRetry);
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (_coverPath != "" && _coverTemp != "")
            {
                File.Delete(_coverPath);
                File.Move(_coverTemp, _coverPath);
            }


            img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    int numberOfRetries = 5;
                    int delayOnRetry = 1000;

                    for (int i = 1; i < numberOfRetries; i++)
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            image.UriSource = new Uri(_coverPath);
                            image.EndInit();
                            img_cover.Source = image;
                            break;
                        }
                        catch (Exception) when (i <= numberOfRetries)
                        {
                            Thread.Sleep(delayOnRetry);
                        }
                }));
        }

        private void FetchSpotifyWeb()
        {
            SongFetcher sf = new SongFetcher();
            TrackInfo info = sf.FetchSpotifyWeb();
            if (info != null)
            {
                if (!info.isPlaying)
                {
                    if (Settings.CustomPauseTextEnabled)
                        WriteSong("", "", "");
                    return;
                }

                string albumUrl = null;

                if (info.albums.Count != 0) albumUrl = info.albums[0].Url;

                if (info.DurationMS > 2000)
                {
                    if (!_songTimer.Enabled)
                        _songTimer.Enabled = true;
                    _songTimer.Stop();
                    _songTimer.Interval = info.DurationMS + 400;
                    _songTimer.Start();
                }

                WriteSong(info.Artists, info.Title, "", albumUrl, false, info.SongID, info.url);
            }
            else
            {
                if (!_songTimer.Enabled)
                    _songTimer.Enabled = true;
                _songTimer.Stop();
                _songTimer.Interval = 1000;
                _songTimer.Start();
            }
        }

        private void FetchTimer(int ms)
        {
            // Check if the timer is running, if yes stop it and start new with the ms giving in the parameter
            try
            {
                _timerFetcher.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }

            _timerFetcher = new System.Timers.Timer();
            _timerFetcher.Elapsed += OnTimedEvent;
            _timerFetcher.Interval = ms;
            _timerFetcher.Enabled = true;
        }

        private async Task GetCurrentSongAsync()
        {
            SongFetcher sf = new SongFetcher();
            SongInfo songInfo;
            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:

                    #region Spotify

                    // Fetching the song thats currently playing on spotify
                    // and updating the output on success
                    songInfo = await sf.FetchDesktopPlayer("Spotify");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);

                    break;

                #endregion Spotify

                case PlayerType.Youtube:

                    #region YouTube

                    // Fetching the song thats currently playing on youtube
                    // and updating the output on success
                    _temp = sf.FetchBrowser("YouTube");
                    if (string.IsNullOrWhiteSpace(_temp))
                    {
                        if (!string.IsNullOrWhiteSpace(_prevSong)) WriteSong(_prevSong, "", "", null, true);

                        break;
                    }
                    if (_temp.Contains(" - "))
                    {
                        List<string> x = _temp.Split(new[] { " - " }, StringSplitOptions.None).ToList();
                        string br_artists = x[0];
                        x.Remove(x[0]);
                        string br_title = string.Join(" - ", x);
                        WriteSong(br_artists, br_title, "", null, _firstRun);

                        break;
                    }

                    WriteSong("", _temp, "", null, _firstRun);

                    break;

                #endregion YouTube


                case PlayerType.VLC:

                    #region VLC

                    songInfo = await sf.FetchDesktopPlayer("vlc");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);
                    break;

                #endregion VLC

                case PlayerType.FooBar2000:

                    #region foobar2000

                    songInfo = await sf.FetchDesktopPlayer("foobar2000");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);

                    break;

                #endregion foobar2000

                case PlayerType.Deezer:

                    #region Deezer

                    _temp = sf.FetchBrowser("Deezer");
                    if (string.IsNullOrWhiteSpace(_temp))
                    {
                        if (!string.IsNullOrWhiteSpace(_prevSong)) WriteSong(_prevSong, "", "", null, _firstRun);

                        break;
                    }

                    WriteSong(_temp, "", "", null, _firstRun);
                    break;

                #endregion Deezer

                case PlayerType.SpotifyWeb:

                    #region Spotify API

                    FetchSpotifyWeb();
                    break;

                    #endregion Spotify API
            }
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            // If Systray is enabled [X] minimizes to systray
            if (!Settings.Systray)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = !_forceClose;
                MinimizeToSysTray();
            }
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            Settings.PosX = Left;
            Settings.PosY = Top;

            // write config file on closing
            //ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            // send inactive
            SendTelemetry(false);
            // remove systray icon
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.LogStr("##### Unhandled Exception #####");
            Logger.LogStr("MyHandler caught : " + e.Message);
            Logger.LogStr("Runtime terminating: {0}" + args.IsTerminating);
            Logger.LogStr("###############################");
            Logger.LogExc(e);
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log"))
                File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log");

            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/Debug.log"))
                File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/Debug.log");

            if (Settings.AutoClearQueue)
            {
                ReqList.Clear();
                WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }

            Settings.MsgLoggingEnabled = false;

            // Load Config file if one exists
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml"))
                ConfigHandler.LoadConfig(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");

            // Add sources to combobox
            AddSourcesToSourceBox();

            // Create systray menu and icon and show it
            _contextMenu.MenuItems.AddRange(new[] {
                new MenuItem("Twitch", new[] {
                    new MenuItem("Connect", (sender1, args1) => {
                        TwitchHandler.BotConnect();
                    }),
                    new MenuItem("Disconnect", (sender1, args1) => {
                        TwitchHandler.Client.Disconnect();
                    })
                }),
                new MenuItem("Show", (sender1, args1) => {
                    Show();
                    WindowState = WindowState.Normal;
                }),
                new MenuItem("Exit", (sender1, args1) => {
                    _forceClose = true;
                    Close();
                })
                });

            _notifyIcon.Icon = Properties.Resources.songify;
            _notifyIcon.ContextMenu = _contextMenu;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (sender1, args1) =>
            {
                Show();
                WindowState = WindowState.Normal;
            };
            _notifyIcon.Text = @"Songify";

            // set the current theme
            ThemeHandler.ApplyTheme();

            // start minimized in systray (hide)
            if (Settings.Systray) MinimizeToSysTray();

            // get the software version from assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            _version = fvi.FileVersion;

            // generate UUID if not exists, expand the window and show the telemetrydisclaimer
            if (Settings.Uuid == "")
            {
                Width = 588 + 200;
                Height = 247.881 + 200;
                Settings.Uuid = Guid.NewGuid().ToString();

                TelemetryDisclaimer();
            }
            else
            {
                // start the timer that sends telemetry every 5 Minutes
                TelemetryTimer();
            }

            // check for update
            AutoUpdater.Mandatory = true;
            AutoUpdater.UpdateMode = Mode.ForcedDownload;
            AutoUpdater.AppTitle = "Songify";
            AutoUpdater.RunUpdateAsAdmin = false;

            AutoUpdater.Start("https://songify.rocks/update.xml");

            // set the cbx index to the correct source
            cbx_Source.SelectedIndex = Settings.Source;
            _selectedSource = cbx_Source.SelectedValue.ToString();
            cbx_Source.SelectionChanged += Cbx_Source_SelectionChanged;

            // text in the bottom right
            LblCopyright.Content =
                "Songify v" + _version.Substring(0, 5) + " Copyright ©";

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                if (string.IsNullOrEmpty(Settings.AccessToken) && string.IsNullOrEmpty(Settings.RefreshToken))
                    TxtblockLiveoutput.Text = "Please link your Spotify account\nSettings -> Spotify";
                else
                    ApiHandler.DoAuthAsync();

                img_cover.Visibility = Visibility.Visible;
            }
            else
            {
                img_cover.Visibility = Visibility.Hidden;
            }

            if (Settings.TwAutoConnect) TwitchHandler.BotConnect();
            // automatically start fetching songs
            SetFetchTimer();
        }

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            // if the window state changes to minimize check run MinimizeToSysTray()
            if (WindowState != WindowState.Minimized) return;
            if (Settings.Systray) MinimizeToSysTray();
        }

        private void Mi_Blacklist_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Blacklist Window
            if (!IsWindowOpen<Window_Blacklist>())
            {
                Window_Blacklist wB = new Window_Blacklist { Top = Top, Left = Left };
                wB.Show();
            }
        }

        private void Mi_Queue_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Queue Window
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Tag.ToString().Contains("Window"))
            {
                if (!IsWindowOpen<Window_Queue>())
                {
                    Window_Queue wQ = new Window_Queue { Top = Top, Left = Left };
                    wQ.Show();
                }
            }
            // Opens the Queue in the Browser
            else if (item.Header.ToString().Contains("Browser"))
            {
                Process.Start("https://songify.rocks/queue.php?id=" + Settings.Uuid);
            }
        }

        private async void Mi_QueueClear_Click(object sender, RoutedEventArgs e)
        {
            // After user confirmation sends a command to the webserver which clears the queue
            MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                "Do you really want to clear the queue?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (msgResult == MessageDialogResult.Affirmative)
            {
                ReqList.Clear();
                WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }
        }

        private void MinimizeToSysTray()
        {
            // if the setting is set, hide window
            Hide();
            _notifyIcon.ShowBalloonTip(5000, @"Songify", @"Songify is running in the background", ToolTipIcon.Info);
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _timerFetcher.Enabled = false;
            s_cts = new CancellationTokenSource();

            await img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                img_cover.Visibility = _selectedSource == PlayerType.SpotifyWeb && Settings.DownloadCover ? Visibility.Visible : Visibility.Collapsed;
            }));
            try
            {
                s_cts.CancelAfter(3500);

                await GetCurrentSongAsync();            // when the timer 'ticks' this code gets executed
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                s_cts.Dispose();
                _timerFetcher.Enabled = true;
            }
        }

        private void SendTelemetry(bool active)
        {
            // send telemetry data once
            _appActive = active;
            if (!_workerTelemetry.IsBusy)
                _workerTelemetry.RunWorkerAsync();
        }

        private void SetFetchTimer()
        {
            _ = GetCurrentSongAsync();

            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:
                case PlayerType.VLC:
                case PlayerType.FooBar2000:
                    FetchTimer(1000);
                    break;

                case PlayerType.Youtube:
                case PlayerType.Deezer:
                    // Browser User-Set Poll Rate (seconds) * 1000 for milliseconds
                    FetchTimer(Settings.ChromeFetchRate * 1000);
                    break;

                //case PlayerType.Nightbot:
                //    // Nightbot
                //    FetchTimer(3000);
                //    break;

                case PlayerType.SpotifyWeb:
                    // Prevent Rate Limiting
                    FetchTimer(Settings.UseOwnApp ? 2000 : 20000);
                    break;
            }
        }

        private void SongTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FetchSpotifyWeb();
        }

        private async void TelemetryDisclaimer()
        {
            SendTelemetry(true);
            // show messagebox with the Telemetry disclaimer
            MessageDialogResult result = await this.ShowMessageAsync("Anonymous Data",
                FindResource("data_colletion") as string
                , MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Accept",
                    NegativeButtonText = "Decline",
                    DefaultButtonFocus = MessageDialogResult.Affirmative
                });
            if (result == MessageDialogResult.Affirmative)
            {
                // if accepted save to settings, restore window size
                Settings.Telemetry = true;
                Width = 588;
                MinHeight = 285;
                Height = 285;
            }
            else
            {
                // if accepted save to settings, restore window size
                Settings.Telemetry = false;
                Width = 588;
                MinHeight = 285;
                Height = 285;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TelemetryTimer()
        {
            // call SendTelemetry every 5 minutes
            _timer = new Timer(e =>
            {
                if (Settings.Telemetry)
                    SendTelemetry(true);
                else
                    _timer.Dispose();
            }, null, _startTimeSpan, _periodTimeSpan);
        }

        private void WriteSong(string _artist, string _title, string _extra, string cover = null,
            bool forceUpdate = false, string _trackId = null, string _trackUrl = null)
        {
            _currentId = _trackId;

            if (_artist.Contains("Various Artists, "))
            {
                _artist = _artist.Replace("Various Artists, ", "");
                _artist = _artist.Trim();
            }

            // get the songPath which is default the directory where the exe is, else get the user set directory
            _root = string.IsNullOrEmpty(Settings.Directory)
                ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                : Settings.Directory;

            _songPath = _root + "/Songify.txt";
            _coverTemp = _root + "/tmp.png";
            _coverPath = _root + "/cover.png";

            if (_firstRun)
                File.WriteAllText(_songPath, "");

            // if all those are empty we expect the player to be paused
            if (string.IsNullOrEmpty(_artist) && string.IsNullOrEmpty(_title) && string.IsNullOrEmpty(_extra))
            {
                // read the text file
                if (!File.Exists(_songPath)) File.Create(_songPath).Close();

                File.WriteAllText(_songPath, Settings.CustomPauseText);

                if (Settings.SplitOutput) WriteSplitOutput(Settings.CustomPauseText, _title, _extra);

                DownloadCover(null);

                TxtblockLiveoutput.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new Action(() => { TxtblockLiveoutput.Text = Settings.CustomPauseText; }));
                return;
            }

            // get the output string
            CurrSong = Settings.OutputString;

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                // this only is used for Spotify because here the artist and title are split
                // replace parameters with actual info
                CurrSong = CurrSong.Format(
                    artist => _artist,
                    title => _title,
                    extra => _extra,
                    uri => _trackId
                ).Format();

                if (ReqList.Count > 0)
                {
                    RequestObject rq = ReqList.Find(x => x.TrackID == _currentId);
                    if (rq != null)
                    {
                        CurrSong = CurrSong.Replace("{{", "");
                        CurrSong = CurrSong.Replace("}}", "");
                        CurrSong = CurrSong.Replace("{req}", rq.Requester);
                    }
                    else
                    {
                        int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                        int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                    }
                }
                else
                {
                    try
                    {
                        int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                        int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                    }
                }
            }
            else
            {
                // used for Youtube and Nightbot
                // replace parameters with actual info

                // get the first occurance of "}" to get the seperator from the custom output ({artist} - {title})
                // and replace it
                //int pFrom = CurrSong.IndexOf("}", StringComparison.Ordinal);
                //string result = CurrSong.Substring(pFrom + 2, 1);
                //CurrSong = CurrSong.Replace(result, "");

                // artist is set to be artist and title in this case, {title} and {extra} are empty strings
                CurrSong = CurrSong.Format(
                    artist => _artist,
                    title => _title,
                    extra => _extra,
                    uri => _trackId
                ).Format();

                if(CurrSong.EndsWith(" - "))
                    CurrSong = CurrSong.Remove(CurrSong.Length - 3);

                if (CurrSong.StartsWith(" - "))
                    CurrSong = CurrSong.Remove(0, 2);

                try
                {
                    int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                    int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                    if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }

            // Cleanup the string (remove double spaces, trim and add trailing spaces for scroll)
            CurrSong = CleanFormatString(CurrSong);
            this._title = _title;
            this._artist = _artist;

            // read the text file
            if (!File.Exists(_songPath))
            {
                File.Create(_songPath).Close();
                try
                {
                    File.WriteAllText(_songPath, CurrSong);
                }
                catch (Exception)
                {
                    Logger.LogStr($"File {_songPath} couldn't be accessed.");
                }
            }

            //if (new FileInfo(_songPath).Length == 0) File.WriteAllText(_songPath, CurrSong);
            string temp = "";
            temp = File.ReadAllText(_songPath);

            // if the text file is different to _currSong (fetched song) or update is forced
            if (temp.Trim() != CurrSong.Trim() || forceUpdate || _firstRun)
            {
                // write song to the text file
                try
                {
                    File.WriteAllText(_songPath, CurrSong);
                }
                catch (Exception)
                {
                    Logger.LogStr($"File {_songPath} couldn't be accessed.");
                }

                try
                {
                    ReqList.Remove(ReqList.Find(x => x.TrackID == _prevId));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(Window_Queue))
                                continue;
                            //(qw as Window_Queue).dgv_Queue.ItemsSource.
                            (window as Window_Queue)?.dgv_Queue.Items.Refresh();
                        }
                    });
                }
                catch (Exception)
                {
                    // ignored
                }

                if (Settings.SplitOutput) WriteSplitOutput(_artist, _title, _extra);

                // if upload is enabled
                if (Settings.Upload) UploadSong(CurrSong.Trim(), cover);

                if (_firstRun)
                {
                    _prevSong = CurrSong.Trim();
                    _firstRun = false;
                }
                else
                {
                    if (_prevSong == CurrSong.Trim())
                        return;
                }

                //Write History
                if (Settings.SaveHistory && !string.IsNullOrEmpty(CurrSong.Trim()) &&
                    CurrSong.Trim() != Settings.CustomPauseText)
                {
                    _prevSong = CurrSong.Trim();

                    int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                    //save the history file
                    string historyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" +
                                         "history.shr";
                    XDocument doc;
                    if (!File.Exists(historyPath))
                    {
                        doc = new XDocument(new XElement("History",
                            new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy"))));
                        doc.Save(historyPath);
                    }

                    doc = XDocument.Load(historyPath);
                    if (!doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).Any())
                        doc.Descendants("History").FirstOrDefault()
                            ?.Add(new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy")));

                    XElement elem = new XElement("Song", CurrSong.Trim());
                    elem.Add(new XAttribute("Time", unixTimestamp));
                    XElement x = doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).FirstOrDefault();
                    x?.Add(elem);
                    doc.Save(historyPath);
                }

                //Upload History
                if (Settings.UploadHistory && !string.IsNullOrEmpty(CurrSong.Trim()) &&
                    CurrSong.Trim() != Settings.CustomPauseText)
                {
                    _prevSong = CurrSong.Trim();

                    int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                    // Upload Song
                    try
                    {
                        string extras = Settings.Uuid + "&tst=" + unixTimestamp + "&song=" +
                                        HttpUtility.UrlEncode(CurrSong.Trim(), Encoding.UTF8);
                        string url = "https://songify.rocks/song_history.php/?id=" + extras;
                        // Create a new 'HttpWebRequest' object to the mentioned URL.
                        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                        myHttpWebRequest.UserAgent = Settings.Webua;

                        // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                        if (myHttpWebResponse.StatusCode != HttpStatusCode.OK)
                            Logger.LogStr("MAIN: Upload Song:" + myHttpWebResponse.StatusCode);

                        myHttpWebResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                        // Writing to the statusstrip label
                        LblStatus.Dispatcher.Invoke(
                            DispatcherPriority.Normal,
                            new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
                    }
                }

                // Update Song Queue, Track has been player. All parameters are optional except track id, playerd and o. o has to be the value "u"
                if (_trackId != null) WebHelper.UpdateWebQueue(_trackId, "", "", "", "", "1", "u");

                // Send Message to Twitch if checked
                if (Settings.AnnounceInChat && TwitchHandler.Client.IsConnected)
                    TwitchHandler.SendCurrSong("Now playing: " + CurrSong.Trim());

                _prevId = _currentId;

                //Save Album Cover
                if (Settings.DownloadCover) DownloadCover(cover);



                if (File.Exists(_coverPath) && new FileInfo(_coverPath).Length > 0)
                    img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            image.UriSource = new Uri(_coverPath);
                            image.EndInit();
                            img_cover.Source = image;
                        }));
            }

            // write song to the output label
            TxtblockLiveoutput.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action(() => { TxtblockLiveoutput.Text = CurrSong.Trim(); }));
        }

        private void WriteSplitOutput(string artist, string title, string extra)
        {
            // Writes the output to 2 different text files

            if (!File.Exists(_root + "/Artist.txt"))
                File.Create(_root + "/Artist.txt").Close();

            if (!File.Exists(_root + "/Title.txt"))
                File.Create(_root + "/Title.txt").Close();

            File.WriteAllText(_root + "/Artist.txt", artist);
            File.WriteAllText(_root + "/Title.txt", title + extra);
        }

        private void mi_TW_BotResponses_Click(object sender, RoutedEventArgs e)
        {
            Window_Botresponse wBr = new Window_Botresponse();
            wBr.Show();
        }


    }
}