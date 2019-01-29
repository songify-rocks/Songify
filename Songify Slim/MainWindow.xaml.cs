using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Forms;

namespace Songify_Slim
{
    public partial class MainWindow
    {
        #region Variables
        private static readonly HttpClient client = new HttpClient();
        public NotifyIcon NotifyIcon = new NotifyIcon();
        public BackgroundWorker Worker_Telemetry = new BackgroundWorker();
        public BackgroundWorker Worker_Update = new BackgroundWorker();
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private readonly MenuItem _menuItem1 = new MenuItem();
        private readonly MenuItem _menuItem2 = new MenuItem();
        private string _currentsong;
        public static string Version;
        public bool appActive = false;
        public bool updateError = false;
        TimeSpan startTimeSpan = TimeSpan.Zero;
        TimeSpan periodTimeSpan = TimeSpan.FromMinutes(5);
        System.Threading.Timer timer;
        #endregion
        public MainWindow()
        {
            this.InitializeComponent();
            // Backgroundworker for telemetry
            Worker_Telemetry.DoWork += Worker_DoWork;
            Worker_Telemetry.RunWorkerCompleted += Worker_RunWorkerCompleted;

            // Backgroundworker for updates
            Worker_Update.DoWork += Worker_update_DoWork;
            Worker_Update.RunWorkerCompleted += Worker_update_RunWorkerCompleted;
        }

        public void Worker_update_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (updateError)
                LblStatus.Content = "Unable to check for new version.";
        }

        public void Worker_update_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Updater.CheckForUpdates(new Version(Version));
                updateError = false;
            }
            catch
            {
                updateError = true;
            }
        }

        public void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        public void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
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
                Console.WriteLine("-----------------Telemetry-Error-----------------");
                Console.WriteLine(ex);
                Console.WriteLine("-------------------------------------------------");
            }
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
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

            ThemeHandler.ApplyTheme();
            if (this.WindowState == WindowState.Minimized) this.MinimizeToSysTray();

            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;

            if (Settings.GetUUID() == "")
            {
                this.Width = 588 + 200;
                this.Height = 247.881 + 200;
                Settings.SetUUID(System.Guid.NewGuid().ToString());

                TelemetryDisclaimer();
            }
            else
            {
                TelemetryTimer();
            }

            Worker_Update.RunWorkerAsync();
            
            this.LblCopyright.Content = "Songify v" + Version.Substring(0, 5) + " Copyright © Jan \"Inzaniity\" Blömacher";

            this.FetchTimer(1000);
        }

        private void TelemetryTimer()
        {
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

        private async void TelemetryDisclaimer()
        {
            SendTelemetry(true);
            var result = await this.ShowMessageAsync("Anonymous Data",
                this.FindResource("data_colletion") as string
                , MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Accept", NegativeButtonText = "Decline", DefaultButtonFocus = MessageDialogResult.Affirmative });
            if (result == MessageDialogResult.Affirmative)
            {
                Settings.SetTelemetry(true);
                this.Width = 588;
                this.MinHeight = 247.881;
                this.Height = 247.881;
            }
            else
            {
                Settings.SetTelemetry(false);
                this.Width = 588;
                this.MinHeight = 247.881;
                this.Height = 247.881;
            }
        }

        public static void CheckForUpdates()
        {
            try
            {
                Updater.CheckForUpdates(new Version(Version));
            }
            catch
            {
                foreach (Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        (window as MainWindow).LblStatus.Content = "Unable to check for new version.";
                    }
                }
            }
        }

        private void FetchTimer(int ms)
        {
            var timer = new System.Timers.Timer();
            timer.Elapsed += this.OnTimedEvent;
            timer.Interval = ms;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.GetCurrentSong();
        }

        private void GetCurrentSong()
        {
            var processes = Process.GetProcessesByName("Spotify");

            foreach (var process in processes)
            {
                if (process.ProcessName == "Spotify" && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    string wintitle = process.MainWindowTitle;
                    string artist = "", title = "", extra = "";
                    if (wintitle != "Spotify")
                    {
                        string[] songinfo = wintitle.Split(new[] { " - " }, StringSplitOptions.None);
                        try
                        {
                            artist = songinfo[0].Trim();
                            title = songinfo[1].Trim();
                            if (songinfo.Length > 2)
                                extra = "(" + String.Join("", songinfo, 2, songinfo.Length - 2).Trim() + ")";
                        }
                        catch
                        {
                        }
                        _currentsong = Settings.GetOutputString().Replace("{artist}", artist);
                        _currentsong = _currentsong.Replace("{title}", title);
                        _currentsong = _currentsong.Replace("{extra}", extra);

                        if (string.IsNullOrEmpty(Settings.GetDirectory()))
                        {
                            File.WriteAllText(
                                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt",
                                _currentsong);
                        }
                        else
                        {
                            File.WriteAllText(Settings.GetDirectory() + "/Songify.txt",
                                _currentsong);
                        }

                        this.TxtblockLiveoutput.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(() => { TxtblockLiveoutput.Text = _currentsong.Trim(); }));
                    }
                    else
                    {
                        if (Settings.GetCustomPauseTextEnabled())
                        {
                            if (string.IsNullOrEmpty(Settings.GetDirectory()))
                            {
                                File.WriteAllText(
                                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt",
                                    Settings.GetCustomPauseText());
                            }
                            else
                            {
                                File.WriteAllText(Settings.GetDirectory() + "/Songify.txt",
                                    Settings.GetCustomPauseText());
                            }
                        }
                    }
                }
            }
        }

        public static void RegisterInStartup(bool isChecked)
        {
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

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized) return;
            this.MinimizeToSysTray();
        }

        private void MinimizeToSysTray()
        {
            if (Settings.GetSystray())
            {
                this.Hide();
            }
        }

        private void MenuItem2Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void MenuItem1Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            if (!Worker_Telemetry.IsBusy)
                Worker_Telemetry.RunWorkerAsync();
            this.NotifyIcon.Visible = false;
            this.NotifyIcon.Dispose();
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            AboutWindow aW = new AboutWindow();
            aW.ShowDialog();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow sW = new SettingsWindow();
            sW.ShowDialog();
        }

        private void SendTelemetry(bool active)
        {
            appActive = active;
            if(!Worker_Telemetry.IsBusy)
                Worker_Telemetry.RunWorkerAsync();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SendTelemetry(false);
        }
    }
}