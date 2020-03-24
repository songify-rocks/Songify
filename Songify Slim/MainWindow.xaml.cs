using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace Songify_Slim
{
    public static class PlayerType
    {
        public const string SpotifyLegacy = "Spotify Legacy";
        public const string Youtube = "Youtube (Chrome)";
        public const string Nightbot = "Nightbot";
        public const string VLC = "VLC";
        public const string FooBar2000 = "foobar2000";
        public const string Deezer = "Deezer (Chrome)";
        public const string SpotifyWeb = "Spotify API";
    }

    public class RequestObject
    {
        public string TrackID { get; set; }
        public string Artists { get; set; }
        public string Title { get; set; }
        public string Length { get; set; }
        public string Requester { get; set; }

    }

    public partial class MainWindow
    {
        #region Variables
        public string songPath, coverPath, root;
        public static string Version;
        public bool AppActive;
        public NotifyIcon NotifyIcon = new NotifyIcon();
        public bool UpdateError;
        public BackgroundWorker WorkerTelemetry = new BackgroundWorker();
        public BackgroundWorker WorkerUpdate = new BackgroundWorker();
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private readonly MenuItem _menuItem1 = new MenuItem();
        private readonly MenuItem _menuItem2 = new MenuItem();
        public string CurrSong;
        private readonly TimeSpan _periodTimeSpan = TimeSpan.FromMinutes(5);
        private string _selectedSource = Settings.Source;
        private readonly TimeSpan _startTimeSpan = TimeSpan.Zero;
        private string _temp = "";
        private System.Threading.Timer _timer;
        private System.Timers.Timer _timerFetcher = new System.Timers.Timer();
        private bool _forceClose;
        bool _firstRun = true;
        string _prevSong;
        public List<RequestObject> ReqList = new List<RequestObject>();

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.Left = Settings.PosX;
            this.Top = Settings.PosY;
            // Backgroundworker for telemetry, and methods
            WorkerTelemetry.DoWork += Worker_Telemetry_DoWork;

            // Backgroundworker for updates, and methods
            WorkerUpdate.DoWork += Worker_Update_DoWork;
            WorkerUpdate.RunWorkerCompleted += Worker_Update_RunWorkerCompleted;
        }

        public static void RegisterInStartup(bool isChecked)
        {
            // Adding the RegKey for Songify in startup (autostart with windows)
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                true);
            if (isChecked)
            {
                registryKey?.SetValue("Songify", Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException());
            }
            else
            {
                registryKey?.DeleteValue("Songify");
            }

            Settings.Autostart = isChecked;
        }

        public void Worker_Telemetry_DoWork(object sender, DoWorkEventArgs e)
        {
            // Backgroundworker is asynchronous
            // sending a webrequest that parses parameters to php code
            // it sends the UUID (randomly generated on first launch), unix timestamp, version number and if the app is active
            try
            {
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string extras = Settings.Uuid + "&tst=" + unixTimestamp + "&v=" + Version + "&a=" + AppActive;
                string url = "http://songify.bloemacher.com/songifydata.php/?id=" + extras;
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
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
            }
        }

        public void Worker_Update_DoWork(object sender, DoWorkEventArgs e)
        {
            // Checking for updates, calling the Updater class with the current version
            try
            {
                Updater.CheckForUpdates(new Version(Version));
                UpdateError = false;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                UpdateError = true;
            }
        }

        public void Worker_Update_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Writing to the statusstrip label when the updater encountered an error
            if (UpdateError)
                LblStatus.Content = "Unable to check for new version.";
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            // Opens the 'About'-Window
            AboutWindow aW = new AboutWindow { Top = this.Top, Left = this.Left };
            aW.ShowDialog();
        }

        private void BtnDiscord_Click(object sender, RoutedEventArgs e)
        {
            // Opens Discord-Invite Link in Standard-Browser
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Opens the 'Settings'-Window
            SettingsWindow sW = new SettingsWindow { Top = this.Top, Left = this.Left };
            sW.ShowDialog();
        }

        private void Cbx_Source_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Sets the source (Spotify, Youtube, Nightbot)
            if (!IsLoaded)
            {
                // This prevents that the selected is always 0 (initialize components)
                return;
            }

            _selectedSource = cbx_Source.SelectedValue.ToString();

            Settings.Source = _selectedSource;

            // Dpending on which source is chosen, it starts the timer that fetches the song info
            SetFetchTimer();

            if (_selectedSource == PlayerType.SpotifyWeb)

            {
                SongFetcher sf = new SongFetcher();
                string[] currentlyPlaying = sf.FetchSpotifyWeb();
                if (currentlyPlaying != null)
                {
                    WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2], currentlyPlaying[3], true, currentlyPlaying[4]);
                    try
                    {
                        ReqList.Remove(ReqList.Find(x => x.TrackID == currentlyPlaying[4]));

                        System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            foreach (Window window in System.Windows.Application.Current.Windows)
                            {
                                if (window.GetType() != typeof(Window_Queue))
                                    continue;
                                //(qw as Window_Queue).dgv_Queue.ItemsSource.
                                (window as Window_Queue).dgv_Queue.Items.Refresh();
                            }
                        }));

                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private void SetFetchTimer()
        {
            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:
                case PlayerType.VLC:
                case PlayerType.FooBar2000:
                case PlayerType.SpotifyWeb:
                    // Spotify, VLC or foobar2000
                    FetchTimer(1000);
                    break;

                case PlayerType.Youtube:
                case PlayerType.Deezer:
                    // Browser User-Set Poll Rate (seconds) * 1000 for milliseconds
                    FetchTimer(Settings.ChromeFetchRate * 1000);
                    break;

                case PlayerType.Nightbot:
                    // Nightbot
                    FetchTimer(3000);
                    break;
            }
        }

        private void FetchTimer(int ms)
        {
            // Check if the timer is running, if yes stop it and start new with the ms givin in the parameter
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

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            img_cover.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                if (_selectedSource == PlayerType.SpotifyWeb && Settings.DownloadCover)
                {
                    img_cover.Visibility = Visibility.Visible;
                }
                else
                {
                    img_cover.Visibility = Visibility.Collapsed;
                }
            }));


            // when the timer 'ticks' this code gets executed
            GetCurrentSong();
        }

        private void GetCurrentSong()
        {
            SongFetcher sf = new SongFetcher();
            string[] currentlyPlaying;
            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:

                    #region Spotify
                    // Fetching the song thats currently playing on spotify
                    // and updating the output on success
                    currentlyPlaying = sf.FetchDesktopPlayer("Spotify");
                    if (currentlyPlaying != null)
                    {
                        WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2]);
                    }
                    break;

                #endregion Spotify

                case PlayerType.Youtube:

                    #region YouTube
                    // Fetching the song thats currently playing on youtube
                    // and updating the output on success
                    _temp = sf.FetchBrowser("YouTube");
                    if (string.IsNullOrWhiteSpace(_temp))
                    {
                        if (!string.IsNullOrWhiteSpace(_prevSong))
                        {
                            WriteSong(_prevSong, "", "");
                        }
                        break;
                    }
                    WriteSong(_temp, "", "");

                    break;

                #endregion

                case PlayerType.Nightbot:

                    #region Nightbot
                    // Fetching the currently playing song on NB Song Request
                    // and updating the output on success
                    _temp = sf.FetchNightBot();
                    if (String.IsNullOrWhiteSpace(_temp))
                    {
                        if (!String.IsNullOrWhiteSpace(_prevSong))
                        {
                            WriteSong(_prevSong, "", "");
                        }
                        break;
                    }
                    WriteSong(_temp, "", "");

                    break;

                #endregion Nightbot

                case PlayerType.VLC:

                    #region VLC
                    currentlyPlaying = sf.FetchDesktopPlayer("vlc");
                    if (currentlyPlaying != null)
                    {
                        WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2]);
                    }
                    break;

                #endregion VLC

                case PlayerType.FooBar2000:

                    #region foobar2000
                    currentlyPlaying = sf.FetchDesktopPlayer("foobar2000");
                    if (currentlyPlaying != null)
                    {
                        WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2]);
                    }
                    break;

                #endregion foobar2000

                case PlayerType.Deezer:

                    #region Deezer
                    _temp = sf.FetchBrowser("Deezer");
                    if (string.IsNullOrWhiteSpace(_temp))
                    {
                        if (!string.IsNullOrWhiteSpace(_prevSong))
                        {
                            WriteSong(_prevSong, "", "");
                        }
                        break;
                    }
                    WriteSong(_temp, "", "");
                    break;

                #endregion Deezer

                case PlayerType.SpotifyWeb:

                    #region Spotify API
                    currentlyPlaying = sf.FetchSpotifyWeb();
                    if (currentlyPlaying != null)
                    {
                        WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2], currentlyPlaying[3], false, currentlyPlaying[4]);
                    }
                    break;

                    #endregion
            }
        }

        private void MenuItem1Click(object sender, EventArgs e)
        {
            // Click on "Exit" in the Systray
            _forceClose = true;
            Close();
        }

        private void MenuItem2Click(object sender, EventArgs e)
        {
            // Click on "Show" in the Systray
            Show();
            WindowState = WindowState.Normal;
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
            ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            // send inactive
            SendTelemetry(false);
            // remove systray icon
            NotifyIcon.Visible = false;
            NotifyIcon.Dispose();
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log"))
            {
                File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log");
            }

            if (Settings.AutoClearQueue)
            {
                ReqList.Clear();
                WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
            }

            Settings.MsgLoggingEnabled = false;
            // Load Config file if one exists
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml"))
            {
                ConfigHandler.LoadConfig(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            }

            // Add sources to combobox
            AddSourcesToSourceBox();

            // Create systray menu and icon and show it
            _menuItem1.Text = @"Exit";
            _menuItem1.Click += MenuItem1Click;
            _menuItem2.Text = @"Show";
            _menuItem2.Click += MenuItem2Click;

            _contextMenu.MenuItems.AddRange(new[] { _menuItem2, _menuItem1 });

            NotifyIcon.Icon = Properties.Resources.songify;
            NotifyIcon.ContextMenu = _contextMenu;
            NotifyIcon.Visible = true;
            NotifyIcon.DoubleClick += MenuItem2Click;
            NotifyIcon.Text = @"Songify";

            // set the current theme
            ThemeHandler.ApplyTheme();

            // start minimized in systray (hide)
            if (WindowState == WindowState.Minimized) MinimizeToSysTray();

            // get the software version from assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;

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
            // WorkerUpdate.RunWorkerAsync();

            // set the cbx index to the correct source
            cbx_Source.SelectedValue = _selectedSource;

            // text in the bottom right
            LblCopyright.Content =
                "Songify v" + Version.Substring(0, 5) + " Copyright © Jan \"Inzaniity\" Blömacher";

            // automatically start fetching songs
            SetFetchTimer();

            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                if (string.IsNullOrEmpty(Settings.AccessToken) && string.IsNullOrEmpty(Settings.RefreshToken))
                {
                    TxtblockLiveoutput.Text = "Please link your Spotify account\nSettings -> Integration";
                }
                else
                {
                    APIHandler.DoAuthAsync();
                }

                img_cover.Visibility = Visibility.Visible;
            }
            else
            {
                img_cover.Visibility = Visibility.Hidden;
            }

            if (Settings.TwAutoConnect)
            {
                TwitchHandler.BotConnect();
            }

        }

        private void AddSourcesToSourceBox()
        {
            string[] sourceBoxItems = new string[] { PlayerType.SpotifyWeb, PlayerType.SpotifyLegacy,
                PlayerType.Deezer, PlayerType.FooBar2000, PlayerType.Nightbot, PlayerType.VLC, PlayerType.Youtube };
            cbx_Source.ItemsSource = sourceBoxItems;
        }

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            // if the window state changes to minimize check run MinimizeToSysTray()
            if (WindowState != WindowState.Minimized) return;
            MinimizeToSysTray();
        }

        private void MinimizeToSysTray()
        {
            // if the setting is set, hide window
            if (Settings.Systray)
            {
                Hide();
            }
        }

        private void SendTelemetry(bool active)
        {
            // send telemetry data once
            AppActive = active;
            if (!WorkerTelemetry.IsBusy)
                WorkerTelemetry.RunWorkerAsync();
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
                MinHeight = 247.881;
                Height = 247.881;
            }
            else
            {
                // if accepted save to settings, restore window size
                Settings.Telemetry = false;
                Width = 588;
                MinHeight = 247.881;
                Height = 247.881;
            }
        }

        private void TelemetryTimer()
        {
            // call SendTelemetry every 5 minutes
            _timer = new System.Threading.Timer((e) =>
            {
                if (Settings.Telemetry)
                {
                    SendTelemetry(true);
                }
                else
                {
                    _timer.Dispose();
                }
            }, null, _startTimeSpan, _periodTimeSpan);
        }

        private void WriteSong(string artist, string title, string extra, string cover = null, bool forceUpdate = false, string trackID = null)
        {
            if (artist.Contains("Various Artists, "))
            {
                artist = artist.Replace("Various Artists, ", "");
                artist.Trim();
            }

            // get the output string
            CurrSong = Settings.OutputString;
            if (!String.IsNullOrEmpty(title))
            {
                // this only is used for spotify because here the artist and title are split
                // replace parameters with actual info
                CurrSong = CurrSong.Replace("{artist}", artist);
                CurrSong = CurrSong.Replace("{title}", title);
                CurrSong = CurrSong.Replace("{extra}", extra);
                CurrSong = CurrSong.Replace("{uri}", trackID);
            }
            else
            {
                // used for Youtube and Nightbot
                // replace parameters with actual info

                // get the first occurance of "}" to get the seperator from the custom output ({artist} - {title})
                // and replace it
                int pFrom = CurrSong.IndexOf("}", StringComparison.Ordinal);
                string result = CurrSong.Substring(pFrom + 2, 1);
                CurrSong = CurrSong.Replace(result, "");

                // artist is set to be artist and title in this case, {title} and {extra} are empty strings
                CurrSong = CurrSong.Replace("{artist}", artist);
                CurrSong = CurrSong.Replace("{title}", title);
                CurrSong = CurrSong.Replace("{extra}", extra);
                CurrSong = CurrSong.Replace("{uri}", trackID);

            }

            // get the songPath which is default the directory where the exe is, else get the user set directory
            if (string.IsNullOrEmpty(Settings.Directory))
            {
                root = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                songPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/Songify.txt";
                coverPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/cover.jpg";
            }
            else
            {
                root = Settings.Directory;
                songPath = Settings.Directory + "/Songify.txt";
                coverPath = Settings.Directory + "/cover.jpg";
            }

            // read the text file
            if (!File.Exists(songPath))
            {
                File.Create(songPath).Close();
                File.WriteAllText(songPath, CurrSong);
            }

            if (new FileInfo(songPath).Length == 0)
            {
                File.WriteAllText(songPath, CurrSong);
            }

            string[] temp = File.ReadAllLines(songPath);
            // if the text file is different to _currSong (fetched song) or update is forced
            if (temp[0].Trim() != CurrSong.Trim() || forceUpdate)
            {
                // write song to the text file
                File.WriteAllText(songPath, CurrSong);

                try
                {
                    ReqList.Remove(ReqList.Find(x => x.TrackID == trackID));
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (Window window in System.Windows.Application.Current.Windows)
                        {
                            if (window.GetType() != typeof(Window_Queue))
                                continue;
                            //(qw as Window_Queue).dgv_Queue.ItemsSource.
                            (window as Window_Queue).dgv_Queue.Items.Refresh();
                        }
                    }));
                }
                catch (Exception)
                {

                }

                if (Settings.SplitOutput)
                {
                    WriteSplitOutput(artist, title, extra);
                }

                // if upload is enabled
                if (Settings.Upload)
                {
                    UploadSong(CurrSong.Trim());
                }

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

                    int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    //save the history file
                    string historyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + "history.shr";
                    XDocument doc;
                    if (!File.Exists(historyPath))
                    {
                        doc = new XDocument(new XElement("History", new XElement("d_" + DateTime.Now.ToString("dd/MM/yyyy"))));
                        doc.Save(historyPath);
                    }
                    doc = XDocument.Load(historyPath);
                    if (!doc.Descendants("d_" + DateTime.Now.ToShortDateString()).Any())
                    {
                        doc.Descendants("History").FirstOrDefault()?.Add(new XElement("d_" + DateTime.Now.ToShortDateString()));
                    }
                    XElement elem = new XElement("Song", CurrSong.Trim());
                    elem.Add(new XAttribute("Time", unixTimestamp));
                    XElement x = doc.Descendants("d_" + DateTime.Now.ToShortDateString()).FirstOrDefault();
                    x?.Add(elem);
                    doc.Save(historyPath);
                }

                //Upload History
                if (Settings.UploadHistory && !string.IsNullOrEmpty(CurrSong.Trim()) &&
                CurrSong.Trim() != Settings.CustomPauseText)
                {
                    _prevSong = CurrSong.Trim();

                    int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    // Upload Song
                    try
                    {
                        string extras = Settings.Uuid + "&tst=" + unixTimestamp + "&song=" +
                                     HttpUtility.UrlEncode(CurrSong.Trim(), Encoding.UTF8);
                        string url = "http://songify.bloemacher.com/song_history.php/?id=" + extras;
                        Console.WriteLine(url);
                        // Create a new 'HttpWebRequest' object to the mentioned URL.
                        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                        myHttpWebRequest.UserAgent = Settings.Webua;

                        // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                        Logger.LogStr("Upload History:" + myHttpWebResponse.StatusDescription);
                        Logger.LogStr("Upload History:" + myHttpWebResponse.StatusCode.ToString());
                        myHttpWebResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                        // Writing to the statusstrip label
                        LblStatus.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
                    }

                }

                // Update Song Queue, Track has been player. All parameters are optional except track id, playerd and o. o has to be the value "u"
                if (trackID != null)
                {
                    WebHelper.UpdateWebQueue(trackID, "", "", "", "", "1", "u");
                }

                //Save Album Cover
                if (Settings.DownloadCover && !String.IsNullOrEmpty(cover))
                {
                    DownloadCover(cover);
                }
            }

            // write song to the output label 
            TxtblockLiveoutput.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() => { TxtblockLiveoutput.Text = CurrSong.Trim(); }));

            if (File.Exists(coverPath) && new FileInfo(coverPath).Length > 0)
            {
                img_cover.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                       new Action(() =>
                                       {
                                           BitmapImage image = new BitmapImage();
                                           image.BeginInit();
                                           image.CacheOption = BitmapCacheOption.OnLoad;
                                           image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                           image.UriSource = new Uri(coverPath);
                                           image.EndInit();
                                           img_cover.Source = image;
                                       }));
            }
        }

        private void DownloadCover(string cover)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(cover, Settings.Directory + "cover.jpg");
            webClient.Dispose();
            img_cover.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = new Uri(coverPath);
                    image.EndInit();
                    img_cover.Source = image;
                }));
        }

        private void WriteSplitOutput(string artist, string title, string extra)
        {
            if (!File.Exists(root + "/Artist.txt"))
            {
                File.Create(root + "/Artist.txt").Close();
                File.WriteAllText(root + "/Artist.txt", artist);
            }
            else
            {
                File.WriteAllText(root + "/Artist.txt", artist);
            }

            if (!File.Exists(root + "/Title.txt"))
            {
                File.Create(root + "/Title.txt").Close();
                File.WriteAllText(root + "/Title.txt", title + extra);
            }
            else
            {
                File.WriteAllText(root + "/Title.txt", title + extra);
            }
        }

        public void UploadSong(string currSong)
        {
            try
            {
                // extras are UUID and Songinfo
                string extras = Settings.Uuid + "&song=" + HttpUtility.UrlEncode(currSong.Trim().Replace("\"", ""), Encoding.UTF8);
                string url = "http://songify.bloemacher.com/song.php/?id=" + extras;
                Console.WriteLine(url);
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Logger.LogStr("Upload Song:" + myHttpWebResponse.StatusDescription);
                Logger.LogStr("Upload Song:" + myHttpWebResponse.StatusCode.ToString());
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                // if error occurs write text to the status asynchronous
                Console.WriteLine(ex.Message);
                LblStatus.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
            }
        }

        private void BtnTwitch_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Header.ToString().Equals("Connect"))
            {
                TwitchHandler.BotConnect();
            }
            else if (item.Header.ToString().Equals("Disconnect"))
            {
                TwitchHandler._client.Disconnect();
            }
        }

        private void mi_Queue_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            if (item.Header.ToString().Contains("Window"))
            {
                if (!IsWindowOpen<Window_Queue>())
                {
                    Window_Queue wQ = new Window_Queue { Top = this.Top, Left = this.Left };
                    wQ.Show();
                }

            }
            else if (item.Header.ToString().Contains("Browser"))
            {
                Process.Start("https://songify.bloemacher.com/queue.php?id=" + Settings.Uuid);
            }
        }

        private void mi_Blacklist_Click(object sender, RoutedEventArgs e)
        {
            if (!IsWindowOpen<Window_Blacklist>())
            {
                Window_Blacklist wB = new Window_Blacklist { Top = this.Top, Left = this.Left };
                wB.Show();
            }
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            // Opens the 'Settings'-Window
            HistoryWindow hW = new HistoryWindow { Top = this.Top, Left = this.Left };
            hW.ShowDialog();
        }

        private void mi_QueueClear_Click(object sender, RoutedEventArgs e)
        {
            ReqList.Clear();
            WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
        }

        private void BtnPaypal_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/Songify");
        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? System.Windows.Application.Current.Windows.OfType<T>().Any()
               : System.Windows.Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }
    }
}