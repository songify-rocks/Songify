using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Songify_Slim
{
    public partial class MainWindow
    {
        #region Variables

        public static string Version;
        public bool appActive = false;
        public NotifyIcon NotifyIcon = new NotifyIcon();
        public bool updateError = false;
        public BackgroundWorker Worker_Telemetry = new BackgroundWorker();
        public BackgroundWorker Worker_Update = new BackgroundWorker();
        private static readonly HttpClient client = new HttpClient();
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private readonly MenuItem _menuItem1 = new MenuItem();
        private readonly MenuItem _menuItem2 = new MenuItem();
        public string _currSong;
        private TimeSpan periodTimeSpan = TimeSpan.FromMinutes(5);
        private int selectedSource = Settings.Source;
        private TimeSpan startTimeSpan = TimeSpan.Zero;
        private string temp = "";
        private System.Threading.Timer timer;
        private System.Timers.Timer timerFetcher = new System.Timers.Timer();
        private bool forceClose = false;
        bool firstRun = true;
        string prevSong;

        #endregion

        public MainWindow()
        {
            this.InitializeComponent();
            // Backgroundworker for telemetry, and methods
            Worker_Telemetry.DoWork += Worker_Telemetry_DoWork;

            // Backgroundworker for updates, and methods
            Worker_Update.DoWork += Worker_Update_DoWork;
            Worker_Update.RunWorkerCompleted += Worker_Update_RunWorkerCompleted;
        }

        public static void RegisterInStartup(bool isChecked)
        {
            // Adding the RegKey for Songify in startup (autostart with windows)
            var registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                true);
            if (isChecked)
            {
                registryKey?.SetValue("Songify", Assembly.GetEntryAssembly().Location);
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
                var extras = Settings.UUID + "&tst=" + unixTimestamp + "&v=" + Version + "&a=" + appActive;
                var url = "http://songify.bloemacher.com/songifydata.php/?id=" + extras;
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                // Writing to the statusstrip label
                this.LblStatus.Dispatcher.Invoke(
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
                updateError = false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                updateError = true;
            }
        }

        public void Worker_Update_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Writing to the statusstrip label when the updater encountered an error
            if (updateError)
                LblStatus.Content = "Unable to check for new version.";
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            // Opens the 'About'-Window
            AboutWindow aW = new AboutWindow();
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
            SettingsWindow sW = new SettingsWindow();
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

            selectedSource = cbx_Source.SelectedIndex;

            Settings.Source = selectedSource;

            // Dpending on which source is chosen, it starts the timer that fetches the song info
            switch (selectedSource)
            {
                case 0:
                    // Spotify
                    FetchTimer(1000);
                    break;

                case 1:
                    // Youtube User-Set Poll Rate (seconds) * 1000 for milliseconds
                    FetchTimer(Settings.ChromeFetchRate * 1000);
                    break;

                case 2:
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
                timerFetcher.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            timerFetcher = new System.Timers.Timer();
            timerFetcher.Elapsed += this.OnTimedEvent;
            timerFetcher.Interval = ms;
            timerFetcher.Enabled = true;
        }

        //private static void WriteLog(Exception exception)
        //{
        //    // Writes a log file with exceptions in it
        //    var logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".log";
        //    if (!File.Exists(logPath)) File.Create(logPath).Close();
        //    File.AppendAllText(logPath, @"----------------- " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + @" -----------------" + Environment.NewLine);
        //    File.AppendAllText(logPath, exception.Message + Environment.NewLine);
        //    File.AppendAllText(logPath, exception.StackTrace + Environment.NewLine);
        //    File.AppendAllText(logPath, exception.Source + Environment.NewLine);
        //    File.AppendAllText(logPath, @"----------------------------------------------------" + Environment.NewLine);

        //}

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // when the timer 'ticks' this code gets executed
            this.GetCurrentSong();
        }

        private void GetCurrentSong()
        {
            SongFetcher sf = new SongFetcher();
            switch (selectedSource)
            {
                case 0:

                    #region Spotify
                    // Fetching the song thats currently playing on spotify
                    // and updating the output on success
                    string[] currentlyPlaying = sf.FetchSpotify();
                    if (currentlyPlaying != null)
                    {
                        WriteSong(currentlyPlaying[0], currentlyPlaying[1], currentlyPlaying[2]);
                    }
                    break;

                #endregion Spotify

                case 1:

                    #region Chrome
                    // Fetching the song thats currently playing on youtube
                    // and updating the output on success
                    temp = sf.FetchYoutube("chrome");
                    if (String.IsNullOrWhiteSpace(temp))
                    {
                        if (!String.IsNullOrWhiteSpace(prevSong))
                        {
                            WriteSong(prevSong, "", "");
                        }
                        break;
                    }
                    WriteSong(temp, "", "");

                    break;

                #endregion

                case 2:

                    #region Nightbot
                    // Fetching the currently playing song on NB Song Request
                    // and updating the output on success
                    temp = sf.FetchNightBot();
                    if (String.IsNullOrWhiteSpace(temp))
                    {
                        if (!String.IsNullOrWhiteSpace(prevSong))
                        {
                            WriteSong(prevSong, "", "");
                        }
                        break;
                    }
                    WriteSong(temp, "", "");

                    break;

                    #endregion Nightbot
            }
        }

        private void MenuItem1Click(object sender, EventArgs e)
        {
            // Click on "Exit" in the Systray
            forceClose = true;
            this.Close();
        }

        private void MenuItem2Click(object sender, EventArgs e)
        {
            // Click on "Show" in the Systray
            this.Show();
            this.WindowState = WindowState.Normal;
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
                e.Cancel = !forceClose;
                this.MinimizeToSysTray();
            }
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            // write config file on closing
            ConfigHandler.WriteXML(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            // send inactive
            SendTelemetry(false);
            // remove systray icon
            this.NotifyIcon.Visible = false;
            this.NotifyIcon.Dispose();
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Load Config file if one exists
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml"))
            {
                ConfigHandler.LoadConfig(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            }

            // Create systray menu and icon and show it
            this._menuItem1.Text = @"Exit";
            this._menuItem1.Click += this.MenuItem1Click;
            this._menuItem2.Text = @"Show";
            this._menuItem2.Click += this.MenuItem2Click;

            this._contextMenu.MenuItems.AddRange(new[] { this._menuItem2, this._menuItem1 });

            this.NotifyIcon.Icon = Properties.Resources.songify;
            this.NotifyIcon.ContextMenu = this._contextMenu;
            this.NotifyIcon.Visible = true;
            this.NotifyIcon.DoubleClick += this.MenuItem2Click;
            this.NotifyIcon.Text = @"Songify";

            // set the current theme
            ThemeHandler.ApplyTheme();

            // start minimized in systray (hide)
            if (this.WindowState == WindowState.Minimized) this.MinimizeToSysTray();

            // get the software version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;

            // generate UUID if not exists, expand the window and show the telemetrydisclaimer
            if (Settings.UUID == "")
            {
                this.Width = 588 + 200;
                this.Height = 247.881 + 200;
                Settings.UUID = Guid.NewGuid().ToString();

                TelemetryDisclaimer();
            }
            else
            {
                // start the timer that sends telemetry every 5 Minutes
                TelemetryTimer();
            }

            // check for update 
            Worker_Update.RunWorkerAsync();

            // set the cbx index to the correct source
            cbx_Source.SelectedIndex = selectedSource;

            // text in the bottom right
            this.LblCopyright.Content =
                "Songify v" + Version.Substring(0, 5) + " Copyright © Jan \"Inzaniity\" Blömacher";

            // automatically start fetching songs
            switch (selectedSource)
            {
                case 0:
                    FetchTimer(1000);
                    break;

                case 1:
                    FetchTimer(Settings.ChromeFetchRate * 1000);
                    break;

                case 2:
                    FetchTimer(3000);
                    break;
            }
        }

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            // if the window state changes to minimize check run MinimizeToSysTray()
            if (this.WindowState != WindowState.Minimized) return;
            this.MinimizeToSysTray();
        }

        private void MinimizeToSysTray()
        {
            // if the setting is set, hide window
            if (Settings.Systray)
            {
                this.Hide();
            }
        }

        private void SendTelemetry(bool active)
        {
            // send telemetry data once
            appActive = active;
            if (!Worker_Telemetry.IsBusy)
                Worker_Telemetry.RunWorkerAsync();
        }

        private async void TelemetryDisclaimer()
        {
            SendTelemetry(true);
            // show messagebox with the Telemetry disclaimer
            var result = await this.ShowMessageAsync("Anonymous Data",
                this.FindResource("data_colletion") as string
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
                this.Width = 588;
                this.MinHeight = 247.881;
                this.Height = 247.881;
            }
            else
            {
                // if accepted save to settings, restore window size
                Settings.Telemetry = false;
                this.Width = 588;
                this.MinHeight = 247.881;
                this.Height = 247.881;
            }
        }

        private void TelemetryTimer()
        {
            // call SendTelemetry every 5 minutes
            timer = new System.Threading.Timer((e) =>
            {
                if (Settings.Telemetry)
                {
                    SendTelemetry(true);
                }
                else
                {
                    timer.Dispose();
                }
            }, null, startTimeSpan, periodTimeSpan);
        }

        private void WriteSong(string artist, string title, string extra)
        {
            // get the output string
            _currSong = Settings.OutputString;
            if (!String.IsNullOrEmpty(title))
            {
                // this only is used for spotify because here the artist and title are split
                // replace parameters with actual info
                _currSong = _currSong.Replace("{artist}", artist);
                _currSong = _currSong.Replace("{title}", title);
                _currSong = _currSong.Replace("{extra}", extra);
            }
            else
            {
                // used for Youtube and Nightbot
                // replace parameters with actual info

                // get the first occurance of "}" to get the seperator from the custom output ({artist} - {title})
                // and replace it
                int pFrom = _currSong.IndexOf("}");
                String result = _currSong.Substring(pFrom + 2, 1);
                _currSong = _currSong.Replace(result, "");

                // artist is set to be artist and title in this case, {title} and {extra} are empty strings
                _currSong = _currSong.Replace("{artist}", artist);
                _currSong = _currSong.Replace("{title}", title);
                _currSong = _currSong.Replace("{extra}", extra);
            }


            string songPath;

            // get the songPath which is default the directory where the exe is, else get the user set directory
            if (string.IsNullOrEmpty(Settings.Directory))
            {
                songPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt";
            }
            else
            {
                songPath = Settings.Directory + "/Songify.txt";
            }

            // read the text file
            if (!File.Exists(songPath))
            {
                File.Create(songPath).Close();
                File.WriteAllText(songPath, _currSong);
            }

            if (new FileInfo(songPath).Length == 0)
            {
                File.WriteAllText(songPath, _currSong);
            }

            var temp = File.ReadAllLines(songPath);
            // if the text file is different to _currSong (fetched song) 
            if (temp[0].Trim() != _currSong.Trim())
            {
                // write song to the text file
                File.WriteAllText(songPath, _currSong);

                // if upload is enabled
                if (Settings.Upload)
                {
                    UploadSong(_currSong.Trim());
                }

                if (firstRun)
                {
                    prevSong = _currSong.Trim();
                    firstRun = false;
                }
                else
                {
                    if (prevSong == _currSong.Trim())
                        return;
                }

                //Write History
                if (Settings.SaveHistory && !string.IsNullOrEmpty(_currSong.Trim()) &&
                    _currSong.Trim() != Settings.CustomPauseText)
                {
                    prevSong = _currSong.Trim();

                    int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    //save the history file
                    var historyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + "history.shr";
                    XDocument doc;
                    if (!File.Exists(historyPath))
                    {
                        doc = new XDocument(new XElement("History", new XElement("d_" + DateTime.Now.ToString("dd/MM/yyyy"))));
                        doc.Save(historyPath);
                    }
                    doc = XDocument.Load(historyPath);
                    if (!doc.Descendants("d_" + DateTime.Now.ToShortDateString()).Any())
                    {
                        doc.Descendants("History").FirstOrDefault().Add(new XElement("d_" + DateTime.Now.ToShortDateString()));
                    }
                    XElement elem = new XElement("Song", _currSong.Trim());
                    elem.Add(new XAttribute("Time", unixTimestamp));
                    var x = doc.Descendants("d_" + DateTime.Now.ToShortDateString()).FirstOrDefault();
                    x.Add(elem);
                    doc.Save(historyPath);
                }

                //Upload History
                if (Settings.History && !string.IsNullOrEmpty(_currSong.Trim()) &&
                _currSong.Trim() != Settings.CustomPauseText)
                {
                    prevSong = _currSong.Trim();

                    int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    // Upload Song
                    try
                    {
                        var extras = Settings.UUID + "&tst=" + unixTimestamp + "&song=" +
                                     HttpUtility.UrlEncode(_currSong.Trim(), Encoding.UTF8);
                        var url = "http://songify.bloemacher.com/song_history.php/?id=" + extras;
                        Console.WriteLine(url);
                        // Create a new 'HttpWebRequest' object to the mentioned URL.
                        var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                        myHttpWebRequest.UserAgent = Settings.Webua;

                        // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                        var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                        myHttpWebResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        // Writing to the statusstrip label
                        this.LblStatus.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
                    }

                }
            }

            // write song to the output label 
            this.TxtblockLiveoutput.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() => { TxtblockLiveoutput.Text = _currSong.Trim(); }));
        }

        public void UploadSong(string currSong)
        {
            try
            {
                // extras are UUID and Songinfo
                // TODO: Fix encoding errors
                var extras = Settings.UUID + "&song=" + HttpUtility.UrlEncode(currSong.Trim(), Encoding.UTF8);
                var url = "http://songify.bloemacher.com/song.php/?id=" + extras;
                Console.WriteLine(url);
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.Webua;
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                // if error occurs write text to the status asynchronous
                Console.WriteLine(ex.Message);
                this.LblStatus.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Error uploading Songinformation"; }));
            }
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            // Opens the 'Settings'-Window
            HistoryWindow hW = new HistoryWindow();
            hW.ShowDialog();
        }
    }
}