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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Unosquare.Swan;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Forms.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace Songify_Slim.Views
{
    public partial class MainWindow
    {
        #region Variables
        private WindowConsole _consoleWindow;
        public NotifyIcon NotifyIcon = new();
        public string SongArtist, SongTitle;
        public string CurrSong = "", CurrSongTwitch = "";
        private readonly ContextMenu _contextMenu = new();
        private bool _firstRun = true;
        private bool _forceClose;
        private string _currentId;
        private string _prevSong;
        private CancellationTokenSource _sCts;
        private string _selectedSource;
        private string _songPath, _coverPath, _root, _coverTemp;
        private string _temp = "";
        private Timer _timerFetcher = new();
        private readonly WebClient _webClient = new();
        public SongFetcher Sf = new();
        private static readonly Timer _timer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);

        #endregion Variables

        private static async void TelemetryTask(object sender, ElapsedEventArgs e)
        {
            await SendTelemetry();
        }

        private static async Task SendTelemetry()
        {
            try
            {
                dynamic telemetryPayload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey,
                    tst = DateTime.Now.ToUnixEpochDate(),
                    twitch_id = Settings.TwitchUser == null ? "" : Settings.TwitchUser.Id,
                    twitch_name = Settings.TwitchUser == null ? "" : Settings.TwitchUser.DisplayName,
                    vs = GlobalObjects.AppVersion,
                    playertype = GlobalObjects.GetReadablePlayer(),
                };

                await WebHelper.TelemetryRequest(WebHelper.RequestMethod.Post, Json.Serialize(telemetryPayload));

            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public MainWindow()
        {  // start minimized in systray (hide)
            InitializeComponent();
            if (Settings.Systray) MinimizeToSysTray();
            _webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            _timer.Elapsed += TelemetryTask;
            _timer.Start();
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
                WebHelper.UploadSong(currSong, coverUrl);
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

        private static string CleanFormatString(string currSong)
        {
            RegexOptions options = RegexOptions.None;
            Regex regex = new("[ ]{2,}", options);
            currSong = regex.Replace(currSong, " ");
            currSong = currSong.Trim();

            // Add trailing spaces for better scroll
            if (Settings.AppendSpaces)
                currSong = currSong.PadRight(currSong.Length + Settings.SpaceCount);

            return currSong;
        }

        private static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            // This method checks if a window of type <T> is already opened in the current application context and returns true or false
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().Any()
                : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        private void AddSourcesToSourceBox()
        {
            string[] sourceBoxItems =
            {
                PlayerType.SpotifyWeb,
                PlayerType.SpotifyLegacy,
                PlayerType.Deezer,
                PlayerType.FooBar2000,
                PlayerType.Vlc,
                PlayerType.Youtube
            };
            cbx_Source.ItemsSource = sourceBoxItems;
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            // Opens the 'About'-Window
            AboutWindow aW = new() { Top = Top, Left = Left };
            aW.ShowDialog();
        }

        private void BtnDiscord_Click(object sender, RoutedEventArgs e)
        {
            // Opens Discord-Invite Link in Standard-Browser
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnFAQ_Click(object sender, RoutedEventArgs e)
        {
            Process.Start($"{GlobalObjects.BaseUrl}/faq.html");
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/songify-rocks/Songify/issues");
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            // Opens the History in either Window or Browser
            MenuItem item = (MenuItem)sender;
            if (item.Tag.ToString().Contains("Window"))
            {
                if (!IsWindowOpen<HistoryWindow>())
                {
                    // Opens the 'History'-Window
                    HistoryWindow hW = new() { Top = Top, Left = Left };
                    hW.ShowDialog();
                }
            }
            // Opens the Queue in the Browser
            else if (item.Tag.ToString().Contains("Browser"))
            {
                Process.Start($"{GlobalObjects.BaseUrl}/history.php?id=" + Settings.Uuid);
            }
        }

        private void BtnPaypal_Click(object sender, RoutedEventArgs e)
        {
            // links to the projects patreon page (the button name is old because I used to use paypal)
            Process.Start("https://ko-fi.com/overcodetv");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Opens the 'Settings'-Window
            Window_Settings sW = new() { Top = Top, Left = Left };
            sW.ShowDialog();
        }

        private void BtnTwitch_Click(object sender, RoutedEventArgs e)
        {
            // Tries to connect to the twitch service given the credentials in the settings or disconnects
            MenuItem item = (MenuItem)sender;
            switch (item.Tag.ToString())
            {
                // Connects
                case "Connect":
                    TwitchHandler.BotConnect();
                    TwitchHandler.MainConnect();
                    break;
                // Disconnects
                case "Disconnect":
                    TwitchHandler.ForceDisconnect = true;
                    TwitchHandler.Client.Disconnect();
                    break;
            }
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
                if (msgResult != MessageDialogResult.Affirmative) return;
                Settings.Upload = true;
                Process.Start("https://widget.songify.rocks/" + Settings.Uuid);
            }
        }

        private void Cbx_Source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                // This prevents that the selected is always 0 (initialize components)
                return;

            _selectedSource = cbx_Source.SelectedValue.ToString();

            Settings.Player = cbx_Source.SelectedIndex;

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

        private async void DownloadCover(string cover)
        {
            try
            {
                if (cover == null)
                {
                    // create Empty png file
                    Bitmap bmp = new(640, 640);
                    Graphics g = Graphics.FromImage(bmp);

                    g.Clear(Color.Transparent);
                    g.Flush();
                    bmp.Save(_coverPath, ImageFormat.Png);
                }

                else
                {
                    Uri uri = new(cover);
                    // Downloads the album cover to the filesystem
                    await _webClient.DownloadFileTaskAsync(uri, _coverTemp);
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private async void FetchSpotifyWeb()
        {
            TrackInfo info = await Sf.FetchSpotifyWeb();
            if (info == null) return;

            if (!info.IsPlaying)
            {
                if (Settings.CustomPauseTextEnabled)
                    WriteSong("", "", "");
                return;
            }

            string albumUrl = null;

            if (info.Albums.Count != 0) albumUrl = info.Albums[0].Url;
            if (GlobalObjects.SkipList.Find(o => o.Trackid == info.SongId) != null)
            {
                GlobalObjects.SkipList.Remove(GlobalObjects.SkipList.Find(o => o.Trackid == info.SongId));
                await ApiHandler.SkipSong();
            }

            WriteSong(info.Artists, info.Title, "", albumUrl, false, info.SongId, info.Url);
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

            _timerFetcher = new Timer();
            _timerFetcher.Elapsed += OnTimedEvent;
            _timerFetcher.Interval = ms;
            _timerFetcher.Enabled = true;
        }

        private async Task GetCurrentSongAsync()
        {
            SongInfo songInfo;
            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:

                    #region Spotify

                    // Fetching the song thats currently playing on spotify
                    // and updating the output on success
                    songInfo = await Sf.FetchDesktopPlayer("Spotify");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);

                    break;

                #endregion Spotify

                case PlayerType.Youtube:

                    #region YouTube

                    // Fetching the song thats currently playing on youtube
                    // and updating the output on success
                    _temp = Sf.FetchBrowser("YouTube");
                    if (string.IsNullOrWhiteSpace(_temp))
                    {
                        if (!string.IsNullOrWhiteSpace(_prevSong)) WriteSong(_prevSong, "", "", null, true);

                        break;
                    }
                    if (_temp.Contains(" - "))
                    {
                        List<string> x = _temp.Split(new[] { " - " }, StringSplitOptions.None).ToList();
                        string brArtists = x[0];
                        x.Remove(x[0]);
                        string brTitle = string.Join(" - ", x);
                        WriteSong(brArtists, brTitle, "", null, _firstRun);

                        break;
                    }

                    WriteSong("", _temp, "", null, _firstRun);

                    break;

                #endregion YouTube


                case PlayerType.Vlc:

                    #region VLC

                    songInfo = await Sf.FetchDesktopPlayer("vlc");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);
                    break;

                #endregion VLC

                case PlayerType.FooBar2000:

                    #region foobar2000

                    songInfo = await Sf.FetchDesktopPlayer("foobar2000");
                    if (songInfo != null)
                        WriteSong(songInfo.Artist, songInfo.Title, songInfo.Extra, null, _firstRun);

                    break;

                #endregion foobar2000

                case PlayerType.Deezer:

                    #region Deezer

                    _temp = Sf.FetchBrowser("Deezer");
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_forceClose)
                return;
            if (!Settings.Systray)
            {
                NotifyIcon.Visible = false;
                NotifyIcon?.Dispose();
                NotifyIcon = null;
                e.Cancel = false;
            }
            else
            {
                e.Cancel = !_forceClose;
                MinimizeToSysTray();
            }
        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //If the user presses alt + F12 run Crash() method.
            //if (e.Key == Key.F12)
            //{
            //    Crash();
            //}
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            Settings.PosX = Left;
            Settings.PosY = Top;
        }

        private async void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Settings.Directory) && MessageBox.Show($"The directory \"{Settings.Directory}\" doesn't exist.\nThe output directory has been set to \"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\".", "Directory doesn't exist", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Settings.Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            }

            SetIconColors();

            Settings.MsgLoggingEnabled = false;

            // Add sources to combobox
            AddSourcesToSourceBox();

            // Create systray menu and icon and show it
            CreateSystrayIcon();

            // set the current theme
            ThemeHandler.ApplyTheme();

            // get the software version from assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            GlobalObjects.AppVersion = fvi.FileVersion;

            // check for update
            CheckForUpdates();

            // set the cbx index to the correct source
            cbx_Source.SelectedIndex = Settings.Player;
            _selectedSource = cbx_Source.SelectedValue.ToString();
            cbx_Source.SelectionChanged += Cbx_Source_SelectionChanged;

            // text in the bottom right
            if (GlobalObjects.IsBeta)
                LblCopyright.Content = $"Songify v1.5.2.beta_2 Copyright ©";
            else
                LblCopyright.Content = $"Songify v{GlobalObjects.AppVersion} Copyright ©";

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                if (string.IsNullOrEmpty(Settings.SpotifyAccessToken) && string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                    TxtblockLiveoutput.Text = Properties.Resources.mw_LiveOutputLinkSpotify;
                else
                    await ApiHandler.DoAuthAsync();

                img_cover.Visibility = Visibility.Visible;
            }
            else
            {
                img_cover.Visibility = Visibility.Hidden;
            }
            if (Settings.AutoStartWebServer) GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);
            if (Settings.OpenQueueOnStartup) OpenQueue();
            if (Settings.TwAutoConnect)
            {
                TwitchHandler.MainConnect();
                TwitchHandler.BotConnect();
            }
            if (Settings.AutoClearQueue)
            {
                GlobalObjects.ReqList.Clear();
                dynamic payload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey
                };
                await WebHelper.QueueRequest(WebHelper.RequestMethod.Clear, Json.Serialize(payload));
                //WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }


            if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
                await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Main);
            if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
                await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Bot);
            await SendTelemetry();
            Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
            // automatically start fetching songs
            SetFetchTimer();
            if (!Settings.UpdateRequired) return;
            List<int> userLevels = new();
            for (int i = 0; i <= Settings.TwSrUserLevel; i++)
            {
                userLevels.Add(i);
            }
            if (Settings.UserLevelsCommand.Count == 0) Settings.UserLevelsCommand = userLevels;
            if (Settings.UserLevelsReward.Count == 0) Settings.UserLevelsReward = userLevels;

            OpenPatchNotes();
            Settings.UpdateRequired = false;
        }

        private void CheckForUpdates()
        {
            AutoUpdater.Mandatory = false;
            AutoUpdater.UpdateMode = Mode.Normal;
            AutoUpdater.AppTitle = "Songify";
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.ReportErrors = false;
            AutoUpdater.Start(Settings.BetaUpdates
                ? $"{GlobalObjects.BaseUrl}/update-beta.xml"
                : $"{GlobalObjects.BaseUrl}/update.xml");
        }

        private void CreateSystrayIcon()
        {
            _contextMenu.MenuItems.AddRange(new[]
            {
                new System.Windows.Forms.MenuItem("Twitch", new[]
                {
                    new System.Windows.Forms.MenuItem("Connect", (sender1, args1) =>
                    {
                        TwitchHandler.BotConnect();
                        TwitchHandler.MainConnect();
                    }),
                    new System.Windows.Forms.MenuItem("Disconnect", (sender1, args1) => { TwitchHandler.Client.Disconnect(); })
                }),
                new System.Windows.Forms.MenuItem("Show", (sender1, args1) =>
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Show();
                        WindowState = WindowState.Normal;
                    }));
                }),
                new System.Windows.Forms.MenuItem("Exit", (sender1, args1) =>
                {
                    _forceClose = true;
                    Close();
                })
            });
            NotifyIcon.Icon = Properties.Resources.songify;
            NotifyIcon.ContextMenu = _contextMenu;
            NotifyIcon.Visible = true;
            NotifyIcon.DoubleClick += (sender1, args1) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                }));
            };
            NotifyIcon.Text = @"Songify";
        }

        private void SetIconColors()
        {
            IconTwitchAPI.Foreground = Brushes.IndianRed;
            IconTwitchBot.Foreground = Brushes.IndianRed;
            IconTwitchPubSub.Foreground = Brushes.IndianRed;
            IconWebServer.Foreground = Brushes.Gray;
            IconWebSpotify.Foreground = Brushes.IndianRed;
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            Settings.UpdateRequired = true;
            Application.Current.Shutdown();
        }

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            // if the window state changes to minimize check run MinimizeToSysTray()
            //if (WindowState != WindowState.Minimized) return;
            //if (Settings.Systray) MinimizeToSysTray();
            switch (WindowState)
            {
                case WindowState.Normal:
                    break;
                case WindowState.Minimized:
                    if (Settings.Systray)
                    {
                        MinimizeToSysTray();
                    }
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void Mi_Blacklist_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Blacklist Window
            if (!IsWindowOpen<Window_Blacklist>())
            {
                Window_Blacklist wB = new() { Top = Top, Left = Left };
                wB.Show();
            }
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            _forceClose = true;
            Application.Current.Shutdown();
        }

        private void Mi_Queue_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Queue Window
            MenuItem item = (MenuItem)sender;
            if (item.Tag.ToString().Contains("Window"))
            {
                OpenQueue();
            }
            // Opens the Queue in the Browser
            else if (item.Header.ToString().Contains("Browser"))
            {
                Process.Start($"{GlobalObjects.BaseUrl}/queue.php?id=" + Settings.Uuid);
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
                //GlobalObjects.ReqList.Clear();
                //WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
                GlobalObjects.ReqList.Clear();
                dynamic payload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey
                };
                await WebHelper.QueueRequest(WebHelper.RequestMethod.Clear, Json.Serialize(payload));
            }
        }

        private void mi_TW_BotResponses_Click(object sender, RoutedEventArgs e)
        {
            WindowBotresponse wBr = new();
            wBr.Show();
        }

        private void MinimizeToSysTray()
        {
            // if the setting is set, hide window
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Hide));
            NotifyIcon.ShowBalloonTip(5000, @"Songify", @"Songify is running in the background", ToolTipIcon.Info);
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _timerFetcher.Enabled = false;
            _timerFetcher.Elapsed -= OnTimedEvent;
            _sCts = new CancellationTokenSource();

            await img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                img_cover.Visibility = _selectedSource == PlayerType.SpotifyWeb && Settings.DownloadCover ? Visibility.Visible : Visibility.Collapsed;
            }));
            try
            {
                _sCts.CancelAfter(3500);
                _timerFetcher.Enabled = false;
                await GetCurrentSongAsync();
                _timerFetcher.Enabled = true;
                // when the timer 'ticks' this code gets executed
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogExc(ex);
            }
            finally
            {
                _sCts.Dispose();
                _timerFetcher.Enabled = true;
                _timerFetcher.Elapsed += OnTimedEvent;
            }
        }

        private void OpenQueue()
        {
            if (IsWindowOpen<WindowQueue>()) return;
            WindowQueue wQ = new() { Top = Top, Left = Left };
            wQ.Show();
        }

        private void BtnPatchNotes_Click(object sender, RoutedEventArgs e)
        {
            // Check if the patch notes window is already open, if not open it, else switch to it
            OpenPatchNotes();
        }

        private static void OpenPatchNotes()
        {
            if (IsWindowOpen<WindowPatchnotes>())
            {
                WindowPatchnotes wPn = Application.Current.Windows.OfType<WindowPatchnotes>().First();
                wPn.Focus();
                wPn.Activate();
            }
            else
            {
                WindowPatchnotes wPn = new()
                {
                    Owner = (Application.Current.MainWindow),
                };
                wPn.Show();
                wPn.Activate();
            }
        }

        private void SetFetchTimer()
        {
            _ = GetCurrentSongAsync();

            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:
                case PlayerType.Vlc:
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
                    FetchTimer(Settings.UseOwnApp ? 1000 : 20000);
                    break;
            }
        }

        private void mi_TwitchAPI_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(TwitchHandler.TwitchAccount.Main);
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            if (_consoleWindow == null) return;
            if (GlobalObjects.DetachConsole) return;
            _consoleWindow.Left = Left + Width;
            _consoleWindow.Top = Top;
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            const int tries = 5;
            if (_coverPath != "" && _coverTemp != "")
            {
                try
                {
                    for (int i = 0; i < tries; i++)
                    {
                        if (IsFileLocked(new FileInfo(_coverPath)))
                        {
                            Thread.Sleep(1000);
                            if (i != tries) continue;
                            return;
                        }

                        break;
                    }
                    File.Delete(_coverPath);
                }
                catch (Exception)
                {
                    //Debug.WriteLine(exception);
                }

                try
                {
                    File.Move(_coverTemp, _coverPath);
                }
                catch (Exception)
                {
                    //Debug.WriteLine(exception);
                }

            }

            img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(SetCoverImage));
        }

        private async void SetCoverImage()
        {
            const int numberOfRetries = 5;
            const int delayOnRetry = 1000;

            for (int i = 1; i < numberOfRetries; i++)
                try
                {
                    try
                    {
                        BitmapImage image = new();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        image.UriSource = new Uri(_coverPath);
                        image.EndInit();
                        img_cover.Source = image;
                    }
                    catch (Exception)
                    {
                        //Debug.WriteLine(ex);
                        await Task.Delay(1000);
                        continue;
                    }

                    Logger.LogStr("COVER: Set succesfully");
                    break;
                }
                catch (Exception) when (i <= numberOfRetries)
                {
                    Thread.Sleep(delayOnRetry);
                }
        }

        private void WriteOutput(string songPath, string currSong)
        {
            try
            {
                string interpretedText = InterpretEscapeCharacters(currSong);
                File.WriteAllText(songPath, interpretedText);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }
        public static string InterpretEscapeCharacters(string input)
        {
            if(input == null)
                return null;
            string replacedInput = input
                .Replace(@"\t", "\t")
                .Replace(@"\n", Environment.NewLine)
                .Replace(@"\r", "\r");

            // Split the replaced input into lines
            string[] lines = replacedInput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // Trim the leading spaces from each line
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimStart(' ');
            }

            // Join the lines back into a single string
            string result = string.Join(Environment.NewLine, lines);

            return result;
        }
        private void WriteSong(string rArtist, string rTitle, string rExtra, string rCover = null,
                    bool forceUpdate = false, string rTrackId = null, string rTrackUrl = null)
        {
            RequestObject rq = null;
            _currentId = rTrackId;

            //if(rTrackUrl != null)
            //    Console.WriteLine(rTrackUrl);

            if (rArtist.Contains("Various Artists, "))
            {
                rArtist = rArtist.Replace("Various Artists, ", "");
                rArtist = rArtist.Trim();
            }

            // get the songPath which is default the directory where the exe is, else get the user set directory
            _root = string.IsNullOrEmpty(Settings.Directory)
                ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                : Settings.Directory;

            _songPath = _root + "/Songify.txt";
            _coverTemp = _root + "/tmp.png";
            _coverPath = _root + "/cover.png";

            if (_firstRun)
                WriteOutput(_songPath, "");

            // if all those are empty we expect the player to be paused
            if (string.IsNullOrEmpty(rArtist) && string.IsNullOrEmpty(rTitle) && string.IsNullOrEmpty(rExtra))
            {
                // read the text file
                if (!File.Exists(_songPath)) File.Create(_songPath).Close();

                WriteOutput(_songPath, Settings.CustomPauseText);

                if (Settings.SplitOutput) WriteSplitOutput(Settings.CustomPauseText, rTitle, rExtra);

                DownloadCover(null);

                TxtblockLiveoutput.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new Action(() => { TxtblockLiveoutput.Text = Settings.CustomPauseText; }));
                return;
            }

            // get the output string
            CurrSong = Settings.OutputString;
            CurrSongTwitch = Settings.BotRespSong;
            // Replace {artist}, {title} and {extra} in the output string with values from rArtist, rTitle and rExtra

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                // this only is used for Spotify because here the artist and title are split
                // replace parameters with actual info
                CurrSong = CurrSong.Format(
                    artist => rArtist,
                    title => rTitle,
                    extra => rExtra,
                    uri => rTrackId,
                    url => rTrackUrl
                ).Format();

                CurrSongTwitch = CurrSongTwitch.Format(
                    artist => rArtist,
                    title => rTitle,
                    extra => rExtra,
                    uri => rTrackId,
                    url => rTrackUrl
                ).Format();

                if (GlobalObjects.ReqList.Count > 0)
                {
                    rq = GlobalObjects.ReqList.FirstOrDefault(x => x.Trackid == _currentId);
                    if (rq != null)
                    {
                        CurrSong = CurrSong.Replace("{{", "");
                        CurrSong = CurrSong.Replace("}}", "");
                        CurrSong = CurrSong.Replace("{req}", rq.Requester);

                        CurrSongTwitch = CurrSongTwitch.Replace("{{", "");
                        CurrSongTwitch = CurrSongTwitch.Replace("}}", "");
                        CurrSongTwitch = CurrSongTwitch.Replace("{req}", rq.Requester);
                        GlobalObjects.Requester = rq.Requester;

                    }
                    else
                    {
                        int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                        int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                        start = CurrSongTwitch.IndexOf("{{", StringComparison.Ordinal);
                        end = CurrSongTwitch.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSongTwitch = CurrSongTwitch.Remove(start, end - start);
                        GlobalObjects.Requester = "";
                    }
                }
                else
                {
                    try
                    {
                        int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                        int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                        start = CurrSongTwitch.IndexOf("{{", StringComparison.Ordinal);
                        end = CurrSongTwitch.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSongTwitch = CurrSongTwitch.Remove(start, end - start);
                        GlobalObjects.Requester = "";
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

                // get the first occurrence of "}" to get the separator from the custom output ({artist} - {title})
                // and replace it
                //int pFrom = CurrSong.IndexOf("}", StringComparison.Ordinal);
                //string result = CurrSong.Substring(pFrom + 2, 1);
                //CurrSong = CurrSong.Replace(result, "");

                // artist is set to be artist and title in this case, {title} and {extra} are empty strings
                CurrSong = CurrSong.Format(
                    artist => rArtist,
                    title => rTitle,
                    extra => rExtra,
                    uri => rTrackId,
                    url => rTrackUrl
                ).Format();
                CurrSongTwitch = CurrSongTwitch.Format(
                    artist => rArtist,
                    title => rTitle,
                    extra => rExtra,
                    uri => rTrackId,
                    url => rTrackUrl
                ).Format();
                CurrSongTwitch = CurrSongTwitch.Trim();
                CurrSong = CurrSong.Trim();
                // Remove trailing "-" from the output string
                if (CurrSong.EndsWith("-")) CurrSong = CurrSong.Remove(CurrSong.Length - 1);

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
            SongTitle = rTitle;
            SongArtist = rArtist;

            // read the text file
            if (!File.Exists(_songPath))
            {
                try
                {
                    File.Create(_songPath).Close();
                }
                catch (Exception e)
                {
                    Logger.LogExc(e);
                    return;
                }
            }

            //if (new FileInfo(_songPath).Length == 0) File.WriteAllText(_songPath, CurrSong);
            string temp = File.ReadAllText(_songPath);

            // if the text file is different to _currSong (fetched song) or update is forced
            if (temp.Trim() != CurrSong.Trim() || forceUpdate || _firstRun)
            {
                if (temp.Trim() != CurrSong.Trim())
                    // Clear the SkipVotes list in TwitchHandler Class
                    TwitchHandler.ResetVotes();

                // write song to the text file
                try
                {
                    WriteOutput(_songPath, CurrSong);
                }
                catch (Exception)
                {
                    Logger.LogStr($"File {_songPath} couldn't be accessed.");
                }

                if (Settings.SplitOutput) WriteSplitOutput(rArtist, rTitle, rExtra, rq?.Requester);

                // if upload is enabled
                if (Settings.Upload) UploadSong(CurrSong.Trim().Replace(@"\n", " - ").Replace("  ", " "), rCover);

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

                    XElement elem = new("Song", CurrSong.Trim());
                    elem.Add(new XAttribute("Time", unixTimestamp));
                    XElement x = doc.Descendants("d_" + DateTime.Now.ToString("dd.MM.yyyy")).FirstOrDefault();
                    XNode lastNode = x.LastNode;
                    if (lastNode != null)
                    {
                        if (CurrSong.Trim() != ((XElement)lastNode).Value)
                            x?.Add(elem);
                    }
                    else
                    {
                        x?.Add(elem);
                    }
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
                        WebHelper.UploadHistory(CurrSong.Trim().Replace(@"\n", " - ").Replace("  ", " "), unixTimestamp);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                        // Writing to the statusstrip label
                        LblStatus.Dispatcher.Invoke(
                            DispatcherPriority.Normal,
                            new Action(() => { LblStatus.Content = "Error uploading history"; }));
                    }
                }

                // Update Song Queue, Track has been played. All parameters are optional except track id, playedd and o. o has to be the value "u"
                //if (rTrackId != null) WebHelper.UpdateWebQueue(rTrackId, "", "", "", "", "1", "u");

                // Send Message to Twitch if checked
                if (Settings.AnnounceInChat)
                {
                    TwitchHandler.SendCurrSong();
                }


                //Save Album Cover
                if (Settings.DownloadCover) DownloadCover(rCover);

            }

            // write song to the output label
            TxtblockLiveoutput.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action(() => { TxtblockLiveoutput.Text = CurrSong.Trim().Replace(@"\n", " - ").Replace("  ", " "); }));
        }
        private void WriteSplitOutput(string artist, string title, string extra, string requester = "")
        {
            // Writes the output to 2 different text files

            if (!File.Exists(_root + "/Artist.txt"))
                File.Create(_root + "/Artist.txt").Close();

            if (!File.Exists(_root + "/Title.txt"))
                File.Create(_root + "/Title.txt").Close();

            if (!File.Exists(_root + "/Requester.txt"))
                File.Create(_root + "/Requester.txt").Close();

            WriteOutput(_root + "/Artist.txt", artist);
            WriteOutput(_root + "/Title.txt", title + extra);
            WriteOutput(_root + "/Requester.txt", requester);
        }

        private void BtnLogFolderClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Logger.LogDirectoryPath);
        }

        private void BtnMenuViewConsole_Click(object sender, RoutedEventArgs e)
        {
            _consoleWindow ??= new WindowConsole
            {
                Left = Left + Width,
                Top = Top,
                Owner = this
            };
            if (!_consoleWindow.IsLoaded)
                _consoleWindow = new WindowConsole
                {
                    Left = Left + Width,
                    Top = Top,
                    Owner = this
                };
            if (_consoleWindow.IsVisible)
                _consoleWindow.Hide();
            else
                _consoleWindow.Show();
        }

        private async void Mi_TwitchCheckOnlineStatus_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
            mi_TwitchCheckOnlineStatus.Header = $"{Properties.Resources.mw_menu_Twitch_CheckOnlineStatus} ({(Settings.IsLive ? "Live" : "Offline")})";
            LblStatus.Content = Settings.IsLive ? "Stream is Up!" : "Stream is offline.";
            Logger.LogStr($"TWITCH: Stream is {(Settings.IsLive ? "Live" : "Offline")}");
        }

        private void BtnWebServerUrl_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalObjects.WebServer.Run)
                Process.Start($"http://localhost:{Settings.WebServerPort}");
        }
    }
}