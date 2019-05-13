using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;

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
        private string _currSong;
        private TimeSpan periodTimeSpan = TimeSpan.FromMinutes(5);
        private int selectedSource = Settings.GetSource();
        private TimeSpan startTimeSpan = TimeSpan.Zero;
        private string temp = "";
        private System.Threading.Timer timer;
        private System.Timers.Timer timerFetcher = new System.Timers.Timer();
        private AutomationElement parent = null;
        private bool forceClose = false;
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

            Settings.SetAutostart(isChecked);
        }

        public void Worker_Telemetry_DoWork(object sender, DoWorkEventArgs e)
        {
            // Backgroundworker is asynchronous
            // sending a webrequest that parses parameters to php code
            // it sends the UUID (randomly generated on first launch), unix timestamp, version number and if the app is active
            try
            {
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                var extras = Settings.GetUUID() + "&tst=" + unixTimestamp + "&v=" + Version + "&a=" + appActive;
                var url = "http://songify.bloemacher.com/songifydata.php/?id=" + extras;
                // Create a new 'HttpWebRequest' object to the mentioned URL.
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.UserAgent = Settings.getWebua();

                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                WriteLog(ex);
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
                WriteLog(ex);
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

            Settings.SetSource(selectedSource);

            // Dpending on which source is chosen, it starts the timer that fetches the song info
            switch (selectedSource)
            {
                case 0:
                    // Spotify
                    FetchTimer(1000);
                    break;

                case 1:
                    // Youtube
                    FetchTimer(Settings.GetChromeFetchRate() * 1000);
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
                WriteLog(ex);
            }
            timerFetcher = new System.Timers.Timer();
            timerFetcher.Elapsed += this.OnTimedEvent;
            timerFetcher.Interval = ms;
            timerFetcher.Enabled = true;
        }

        private static void WriteLog(Exception exception)
        {
            // Writes a log file with exceptions in it
            var logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".log";
            if (!File.Exists(logPath)) File.Create(logPath).Close();
            File.AppendAllText(logPath, @"----------------- " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + @" -----------------" + Environment.NewLine);
            File.AppendAllText(logPath, exception.Message + Environment.NewLine);
            File.AppendAllText(logPath, exception.StackTrace + Environment.NewLine);
            File.AppendAllText(logPath, exception.Source + Environment.NewLine);
            File.AppendAllText(logPath, @"----------------------------------------------------" + Environment.NewLine);

        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // when the timer 'ticks' this code gets executed
            this.GetCurrentSong();
            if (selectedSource == 1)
            {
                this.LblStatus.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => { LblStatus.Content = "Fetched Youtube: " + DateTime.Now.ToLocalTime(); }));
            }
        }

        private void GetCurrentSong()
        {
            switch (selectedSource)
            {
                case 0:

                    #region Spotify
                    // Get all processes that are called "Spotify"
                    var processes = Process.GetProcessesByName("Spotify");
                    foreach (var process in processes)
                    {
                        if (process.ProcessName == "Spotify" && !string.IsNullOrEmpty(process.MainWindowTitle))
                        {
                            // If the process name is "Spotify" and the window title is not empty
                            string wintitle = process.MainWindowTitle;
                            string artist = "", title = "", extra = "";
                            // Checks if the title is Spotify Premium or Spotify Free in which case we don't want to fetch anything
                            if (wintitle != "Spotify" && wintitle != "Spotify Premium" && wintitle != "Spotify Free")
                            {
                                // Splitting the wintitle which is always Artist - Title
                                string[] songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                                try
                                {
                                    artist = songinfo[0].Trim();
                                    title = songinfo[1].Trim();
                                    // Extra content like "- Offical Anthem" or "- XYZ Remix" and so on
                                    if (songinfo.Length > 2)
                                        extra = "(" + String.Join("", songinfo, 2, songinfo.Length - 2).Trim() + ")";
                                }
                                catch (Exception ex)
                                {
                                    WriteLog(ex);
                                }
                                WriteSong(artist, title, extra);
                            }
                            // the wintitle gets changed as soon as spotify is paused, therefore I'm checking 
                            //if custom pause text is enabled and if so spit out custom text
                            else
                            {
                                if (Settings.GetCustomPauseTextEnabled())
                                {
                                    WriteSong(Settings.GetCustomPauseText(), "", "");
                                }
                            }
                        }
                    }
                    break;

                #endregion Spotify

                case 1:

                    #region Chrome
                    Process[] procsChrome = Process.GetProcessesByName("chrome");
                    foreach (Process chrome in procsChrome)
                    {
                        // the chrome process must have a window
                        if (chrome.MainWindowHandle == IntPtr.Zero)
                        {
                            continue;
                        }

                        var elm = parent == null ? AutomationElement.FromHandle(chrome.MainWindowHandle) : parent;

                        // find the automation element
                        try
                        {
                            AutomationElementCollection elementCollection = elm.FindAll(TreeScope.Descendants,
                                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
                            foreach (AutomationElement elem in elementCollection)
                            {
                                // if the Tabitem Name contains Youtube
                                if (elem.Current.Name.Contains("YouTube"))
                                {
                                    parent = TreeWalker.RawViewWalker.GetParent(elem);
                                    Console.WriteLine(elem.Current.Name);
                                    // Regex pattern to replace the notification in front of the tab (1) - (99+) 
                                    temp = Regex.Replace(elem.Current.Name, @"^\([\d]*(\d+)[\d]*\+*\)", "");
                                    int index = temp.LastIndexOf("- YouTube", StringComparison.Ordinal);
                                    // Remove everything after the last "-" int the string 
                                    // which is "- Youtube" and info that music is playing on this tab
                                    if (index > 0)
                                        temp = temp.Substring(0, index);
                                    temp = temp.Trim();
                                    Console.WriteLine(temp);
                                    // Method that writes the song to the text file and uploads it
                                    WriteSong(temp, "", "");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex);
                            // Chrome has probably changed something, and above walking needs to be modified. :(
                            // put an assertion here or something to make sure you don't miss it
                        }
                    }
                    break;

                #endregion

                case 2:

                    #region Nightbot
                    // Checking if the user has set the setting for Nightbot
                    if (!String.IsNullOrEmpty(Settings.GetNBUserID()))
                    {
                        // Getting JSON from the nightbot API
                        string jsn = "";
                        using (WebClient wc = new WebClient()
                        {
                            Encoding = Encoding.UTF8
                        })
                        {
                            jsn = wc.DownloadString("https://api.nightbot.tv/1/song_requests/queue/?channel=" + Settings.GetNBUserID());
                        }

                        // Deserialize JSON and get the current song 
                        var serializer = new JsonSerializer();
                        NBObj json = JsonConvert.DeserializeObject<NBObj>(jsn);
                        if (json._currentsong == null)
                            return;
                        temp = json._currentsong.track.title;
                        WriteSong(temp, "", "");
                    }
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
            if (!Settings.GetSystray())
            {
                e.Cancel = false;
            }
            else
            {
                if (!forceClose)
                {
                    ConfigHandler.WriteXML(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
                    SendTelemetry(false);
                }
                e.Cancel = !forceClose;
                this.MinimizeToSysTray();
            }
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            // remove systray icon
            if (!Worker_Telemetry.IsBusy)
                Worker_Telemetry.RunWorkerAsync();
            this.NotifyIcon.Visible = false;
            this.NotifyIcon.Dispose();
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
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
            if (Settings.GetUUID() == "")
            {
                this.Width = 588 + 200;
                this.Height = 247.881 + 200;
                Settings.SetUUID(System.Guid.NewGuid().ToString());

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
            this.LblCopyright.Content = "Songify v" + Version.Substring(0, 5) + " Copyright © Jan \"Inzaniity\" Blömacher";

            // automatically start fetching songs
            switch (selectedSource)
            {
                case 0:
                    FetchTimer(1000);
                    break;

                case 1:
                    FetchTimer(Settings.GetChromeFetchRate() * 1000);
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
            if (Settings.GetSystray())
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
                , MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Accept", NegativeButtonText = "Decline", DefaultButtonFocus = MessageDialogResult.Affirmative });
            if (result == MessageDialogResult.Affirmative)
            {
                // if accepted save to settings, restore window size
                Settings.SetTelemetry(true);
                this.Width = 588;
                this.MinHeight = 247.881;
                this.Height = 247.881;
            }
            else
            {
                // if accepted save to settings, restore window size
                Settings.SetTelemetry(false);
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
                if (Settings.GetTelemetry())
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
            _currSong = Settings.GetOutputString();
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
            if (string.IsNullOrEmpty(Settings.GetDirectory()))
            {
                songPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt";
            }
            else
            {
                songPath = Settings.GetDirectory() + "/Songify.txt";
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
                if (Settings.GetUpload())
                {
                    try
                    {
                        // extras are UUID and Songinfo
                        // TODO: Fix encoding errors
                        var extras = Settings.GetUUID() + "&song=" + HttpUtility.UrlEncode(_currSong.Trim(), Encoding.UTF8);
                        var url = "http://songify.bloemacher.com/song.php/?id=" + extras;
                        Console.WriteLine(url);
                        // Create a new 'HttpWebRequest' object to the mentioned URL.
                        var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                        myHttpWebRequest.UserAgent = Settings.getWebua();

                        // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                        var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex);
                        // if error occurs write text to the status asynchronous
                        Console.WriteLine(ex.Message);
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

        // Nightbot JSON Object
        public class NBObj
        {
            public dynamic _currentsong { get; set; }
        }
    }
}