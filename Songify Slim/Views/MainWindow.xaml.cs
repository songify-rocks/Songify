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
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using AutoUpdaterDotNET;
using MahApps.Metro.Controls.Dialogs;
using MdXaml;
using Microsoft.Win32;
using Octokit;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using static ICSharpCode.AvalonEdit.Document.TextDocumentWeakEventManager;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using ContextMenu = System.Windows.Forms.ContextMenu;
using FileMode = System.IO.FileMode;
using FontFamily = System.Windows.Media.FontFamily;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace Songify_Slim.Views
{
    public partial class MainWindow
    {
        #region Variables
        private FileSystemWatcher _watcher;
        private Window_Console secondaryWindow;
        bool updated;
        public NotifyIcon notifyIcon = new NotifyIcon();
        public string _artist, _title;
        public string CurrSong, CurrSongTwitch;
        private static string _version;
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private bool _firstRun = true;
        private bool _forceClose;
        private string _prevId, _currentId;
        private string _prevSong;
        private CancellationTokenSource _sCts;
        private string _selectedSource;
        private string _songPath, _coverPath, _root, _coverTemp;
        private string _temp = "";
        private System.Timers.Timer _timerFetcher = new System.Timers.Timer();
        private readonly WebClient _webClient = new WebClient();
        public SongFetcher sf = new SongFetcher();
        #endregion Variables

        public MainWindow()
        {  // start minimized in systray (hide)
            InitializeComponent();
            if (Settings.Systray) MinimizeToSysTray();

            _webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;


            if (Settings.UpdateRequired)
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("SongifyInfo"));
                Task<IReadOnlyList<Release>> releases = client.Repository.Release.GetAll("songify-rocks", "Songify");
                Release release = releases.Result[0];
                string markdownTxt = releases.Result[0].Body.Split(new[] { "Checksum" }, StringSplitOptions.None)[0];
                Markdown engine = new Markdown();
                FlowDocument document = engine.Transform(markdownTxt);
                document.FontFamily = new FontFamily("Sogeo UI");
                document.LineHeight = 30;
                document.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                document.FontSize = 16;
                rtbPatchnotes.Document = document;
                tbVersion.Text = $"Songify Update {release.TagName}";
                grdUpdate.Visibility = Visibility.Visible;
                Settings.UpdateRequired = false;
            }
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
            Regex regex = new Regex("[ ]{2,}", options);
            currSong = regex.Replace(currSong, " ");
            currSong = currSong.Trim();

            // Add trailing spaces for better scroll
            if (Settings.AppendSpaces)
                for (int i = 0; i < Settings.SpaceCount; i++)
                    currSong += " ";

            return currSong;
        }

        private static void Crash()
        {
            throw new NotImplementedException();
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
                PlayerType.SpotifyWeb, PlayerType.SpotifyLegacy,
                PlayerType.Deezer, PlayerType.FooBar2000, PlayerType.VLC, PlayerType.Youtube
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
            Process.Start($"{GlobalObjects._baseUrl}/faq.html");
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
                Process.Start($"{GlobalObjects._baseUrl}/history.php?id=" + Settings.Uuid);
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
            Window_Settings sW = new Window_Settings { Top = Top, Left = Left };
            sW.ShowDialog();
        }

        private void BtnTwitch_Click(object sender, RoutedEventArgs e)
        {
            // Tries to connect to the twitch service given the credentials in the settings or disconnects
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            switch (item.Tag.ToString())
            {
                // Connects
                case "Connect":
                    TwitchHandler.BotConnect();
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
                    Bitmap bmp = new Bitmap(640, 640);
                    Graphics g = Graphics.FromImage(bmp);

                    g.Clear(Color.Transparent);
                    g.Flush();
                    bmp.Save(_coverPath, ImageFormat.Png);
                }

                else
                {
                    Uri uri = new Uri(cover);
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
            TrackInfo info = sf.FetchSpotifyWeb();
            if (info == null) return;

            if (!info.isPlaying)
            {
                if (Settings.CustomPauseTextEnabled)
                    WriteSong("", "", "");
                return;
            }

            string albumUrl = null;

            if (info.albums.Count != 0) albumUrl = info.albums[0].Url;
            if (GlobalObjects.SkipList.Find(o => o.TrackID == info.SongID) != null)
            {
                GlobalObjects.SkipList.Remove(GlobalObjects.SkipList.Find(o => o.TrackID == info.SongID));
                await ApiHandler.SkipSong();
            }

            WriteSong(info.Artists, info.Title, "", albumUrl, false, info.SongID, info.url);
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
                        string brArtists = x[0];
                        x.Remove(x[0]);
                        string brTitle = string.Join(" - ", x);
                        WriteSong(brArtists, brTitle, "", null, _firstRun);

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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            // If Systray is enabled [X] minimizes to systray
            if (!Settings.Systray)
            {
                notifyIcon.Visible = false;
                notifyIcon?.Dispose();
                notifyIcon = null;
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

            IconTwitchAPI.Foreground = Brushes.IndianRed;
            IconTwitchBot.Foreground = Brushes.IndianRed;
            IconTwitchPubSub.Foreground = Brushes.IndianRed;
            IconWebServer.Foreground = Brushes.IndianRed;
            IconWebSpotify.Foreground = Brushes.IndianRed;

            if (Settings.AutoClearQueue)
            {
                GlobalObjects.ReqList.Clear();
                WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }

            Settings.MsgLoggingEnabled = false;

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
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Show();
                        WindowState = WindowState.Normal;
                    }));
                }),
                new MenuItem("Exit", (sender1, args1) => {
                    _forceClose = true;
                    Close();
                })
                });

            notifyIcon.Icon = Properties.Resources.songify;
            notifyIcon.ContextMenu = _contextMenu;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += (sender1, args1) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                }));
            };
            notifyIcon.Text = @"Songify";

            // set the current theme
            ThemeHandler.ApplyTheme();

            // get the software version from assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            _version = fvi.FileVersion;

            // generate UUID if not exists
            if (Settings.Uuid == "")
            {
                Settings.Uuid = Guid.NewGuid().ToString();
                Settings.Telemetry = false;
            }



            // check for update
            AutoUpdater.Mandatory = false;
            AutoUpdater.UpdateMode = Mode.Normal;
            AutoUpdater.AppTitle = "Songify";
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            //AutoUpdater.ReportErrors = true;
            AutoUpdater.Start(Settings.BetaUpdates
                ? $"{GlobalObjects._baseUrl}/update-beta.xml"
                : $"{GlobalObjects._baseUrl}/update.xml");

            // set the cbx index to the correct source
            cbx_Source.SelectedIndex = Settings.Player;
            _selectedSource = cbx_Source.SelectedValue.ToString();
            cbx_Source.SelectionChanged += Cbx_Source_SelectionChanged;

            // text in the bottom right
            LblCopyright.Content = $"Songify v{_version} Copyright ©";

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                if (string.IsNullOrEmpty(Settings.SpotifyAccessToken) && string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                    TxtblockLiveoutput.Text = Properties.Resources.mw_LiveOutputLinkSpotify;
                else
                    ApiHandler.DoAuthAsync();

                img_cover.Visibility = Visibility.Visible;
            }
            else
            {
                img_cover.Visibility = Visibility.Hidden;
            }
            if (Settings.AutoStartWebServer) GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);
            if (Settings.OpenQueueOnStartup) OpenQueue();
            if (Settings.TwAutoConnect) TwitchHandler.BotConnect();
            // automatically start fetching songs
            SetFetchTimer();
            if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
                await TwitchHandler.InitializeApi();
            WebHelper.SendTelemetry();
            await TwitchHandler.CheckStreamIsUp();
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
            switch (this.WindowState)
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
                Window_Blacklist wB = new Window_Blacklist { Top = Top, Left = Left };
                wB.Show();
            }
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Mi_Queue_Click(object sender, RoutedEventArgs e)
        {
            // Opens the Queue Window
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Tag.ToString().Contains("Window"))
            {
                OpenQueue();
            }
            // Opens the Queue in the Browser
            else if (item.Header.ToString().Contains("Browser"))
            {
                Process.Start($"{GlobalObjects._baseUrl}/queue.php?id=" + Settings.Uuid);
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
                GlobalObjects.ReqList.Clear();
                WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }
        }

        private void mi_TW_BotResponses_Click(object sender, RoutedEventArgs e)
        {
            Window_Botresponse wBr = new Window_Botresponse();
            wBr.Show();
        }

        private void MinimizeToSysTray()
        {
            // if the setting is set, hide window
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Hide));
            notifyIcon.ShowBalloonTip(5000, @"Songify", @"Songify is running in the background", ToolTipIcon.Info);
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _timerFetcher.Enabled = false;
            _sCts = new CancellationTokenSource();

            await img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                img_cover.Visibility = _selectedSource == PlayerType.SpotifyWeb && Settings.DownloadCover ? Visibility.Visible : Visibility.Collapsed;
            }));
            try
            {
                _sCts.CancelAfter(3500);

                await GetCurrentSongAsync();            // when the timer 'ticks' this code gets executed
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                _sCts.Dispose();
                _timerFetcher.Enabled = true;
            }
        }

        private void OpenQueue()
        {
            if (IsWindowOpen<Window_Queue>()) return;
            Window_Queue wQ = new Window_Queue { Top = Top, Left = Left };
            wQ.Show();
        }

        private void BtnPatchNotes_Click(object sender, RoutedEventArgs e)
        {
            // Check if the patch notes window is already open, if not open it, else switch to it
            if (IsWindowOpen<Window_Patchnotes>())
            {
                Window_Patchnotes wPN = Application.Current.Windows.OfType<Window_Patchnotes>().First();
                wPN.Focus();
            }
            else
            {
                Window_Patchnotes wPN = new Window_Patchnotes();
                wPN.Show();
            }
        }

        private void btnUpdateOK_Click(object sender, RoutedEventArgs e)
        {
            grdUpdate.Visibility = Visibility.Collapsed;
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
                    FetchTimer(Settings.UseOwnApp ? 1000 : 20000);
                    break;
            }
        }

        private void mi_TwitchAPI_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.APIConnect();
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
            if (secondaryWindow == null) return;
            if (GlobalObjects.DetachConsole) return;
            secondaryWindow.Left = this.Left + this.Width;
            secondaryWindow.Top = this.Top;
        }

        private void MetroWindow_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void MetroWindow_LostFocus(object sender, RoutedEventArgs e)
        {

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
                        else
                        {
                            break;
                        }
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
                new Action(async () =>
                {
                    const int numberOfRetries = 5;
                    const int delayOnRetry = 1000;

                    for (int i = 1; i < numberOfRetries; i++)
                        try
                        {
                            try
                            {
                                BitmapImage image = new BitmapImage();
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
                            Logger.LogStr($"COVER: Set succesfully");
                            break;
                        }
                        catch (Exception) when (i <= numberOfRetries)
                        {
                            Thread.Sleep(delayOnRetry);
                        }
                }));
        }

        private void WriteOutput(string songPath, string currSong)
        {
            try
            {
                File.WriteAllText(songPath, currSong);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        private void WriteSong(string rArtist, string rTitle, string rExtra, string rCover = null,
                    bool forceUpdate = false, string rTrackId = null, string rTrackUrl = null)
        {
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
            CurrSongTwitch = Settings.OutputString2;
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
                    RequestObject rq = GlobalObjects.ReqList.Find(x => x.TrackID == _currentId);
                    if (rq != null)
                    {
                        CurrSong = CurrSong.Replace("{{", "");
                        CurrSong = CurrSong.Replace("}}", "");
                        CurrSong = CurrSong.Replace("{req}", rq.Requester);

                        CurrSongTwitch = CurrSongTwitch.Replace("{{", "");
                        CurrSongTwitch = CurrSongTwitch.Replace("}}", "");
                        CurrSongTwitch = CurrSongTwitch.Replace("{req}", rq.Requester);
                    }
                    else
                    {
                        int start = CurrSong.IndexOf("{{", StringComparison.Ordinal);
                        int end = CurrSong.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSong = CurrSong.Remove(start, end - start);
                        start = CurrSongTwitch.IndexOf("{{", StringComparison.Ordinal);
                        end = CurrSongTwitch.LastIndexOf("}}", StringComparison.Ordinal) + 2;
                        if (start >= 0) CurrSongTwitch = CurrSongTwitch.Remove(start, end - start);
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
            _title = rTitle;
            _artist = rArtist;

            // read the text file
            if (!File.Exists(_songPath))
            {
                File.Create(_songPath).Close();
                try
                {
                    WriteOutput(_songPath, CurrSong);
                }
                catch (Exception)
                {
                    Logger.LogStr($"File {_songPath} couldn't be accessed.");
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

                try
                {
                    GlobalObjects.ReqList.Remove(GlobalObjects.ReqList.Find(x => x.TrackID == _prevId));
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

                if (Settings.SplitOutput) WriteSplitOutput(rArtist, rTitle, rExtra);

                // if upload is enabled
                if (Settings.Upload) UploadSong(CurrSong.Trim(), rCover);

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
                        WebHelper.UploadHistory(CurrSong.Trim(), unixTimestamp);
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

                // Update Song Queue, Track has been played. All parameters are optional except track id, playedd and o. o has to be the value "u"
                if (rTrackId != null) WebHelper.UpdateWebQueue(rTrackId, "", "", "", "", "1", "u");

                // Send Message to Twitch if checked
                if (TwitchHandler.Client != null && Settings.AnnounceInChat && TwitchHandler.Client.IsConnected)
                {
                    if (Settings.BotOnlyWorkWhenLive)
                    {
                        if (Settings.IsLive)
                            TwitchHandler.SendCurrSong("Now playing: " + CurrSong.Trim());
                    }
                    else
                    {
                        TwitchHandler.SendCurrSong("Now playing: " + CurrSong.Trim());
                    }
                }


                _prevId = _currentId;

                //Save Album Cover
                if (Settings.DownloadCover) DownloadCover(rCover);

                //if (File.Exists(_coverPath) && new FileInfo(_coverPath).Length > 0)
                //    img_cover.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                //        new Action(() =>
                //        {
                //            BitmapImage image = new BitmapImage();
                //            image.BeginInit();
                //            image.CacheOption = BitmapCacheOption.OnLoad;
                //            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                //            image.UriSource = new Uri(_coverPath);
                //            image.EndInit();
                //            img_cover.Source = image;
                //        }));
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

            WriteOutput(_root + "/Artist.txt", artist);
            WriteOutput(_root + "/Title.txt", title + extra);
        }

        private void BtnLogFolderClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Logger.LogDirectoryPath);
        }

        private void BtnMenuViewConsole_Click(object sender, RoutedEventArgs e)
        {
            if (secondaryWindow == null)
                secondaryWindow = new Window_Console
                {
                    Left = this.Left + this.Width,
                    Top = this.Top,
                    Owner = this
                };
            if (!secondaryWindow.IsLoaded)
                secondaryWindow = new Window_Console
                {
                    Left = this.Left + this.Width,
                    Top = this.Top,
                    Owner = this
                };
            if (secondaryWindow.IsVisible)
                secondaryWindow.Hide();
            else
                secondaryWindow.Show();
        }

        private async void Mi_TwitchCheckOnlineStatus_OnClick(object sender, RoutedEventArgs e)
        {
            bool live = await TwitchHandler.CheckStreamIsUp();
            Settings.IsLive = live;
            mi_TwitchCheckOnlineStatus.Header = $"{Properties.Resources.mw_menu_Twitch_CheckOnlineStatus} ({(live ? "Live" : "Offline")})";
            LblStatus.Content = live ? "Stream is Up!" : "Stream is offline.";
            Logger.LogStr($"TWITCH: Stream is {(live ? "Live" : "Offline")}");
        }
    }
}