using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Songify_Slim
{
    using System.Windows.Media;

    using Clipboard = System.Windows.Clipboard;

    public partial class MainWindow
    {


        public NotifyIcon NotifyIcon = new NotifyIcon();

        private readonly System.Windows.Forms.ContextMenu _contextMenu = new System.Windows.Forms.ContextMenu();

        private readonly System.Windows.Forms.MenuItem _menuItem1 = new System.Windows.Forms.MenuItem();

        private readonly System.Windows.Forms.MenuItem _menuItem2 = new System.Windows.Forms.MenuItem();

        private string _currentsong;

        public static string Version;

        public MainWindow()
        {
            this.InitializeComponent();
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

            CheckForUpdates();
            this.LblCopyright.Content = "Songify v" + Version.Substring(0, 5) + " Copyright © Jan Blömacher";

            this.StartTimer(1000);
        }

        public static void CheckForUpdates()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;
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

        private void StartTimer(int ms)
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
                        string[] songinfo = wintitle.Split('-');
                        try
                        {
                            artist = songinfo[0].Trim();
                            title = songinfo[1].Trim();
                            extra = songinfo[2].Trim();
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
                                this._currentsong + @"               ");
                        }
                        else
                        {
                            File.WriteAllText(Settings.GetDirectory() + "/Songify.txt",
                                this._currentsong + @"               ");
                        }

                        this.TxtblockLiveoutput.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(() => { this.TxtblockLiveoutput.Text = this._currentsong; }));
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
            this.NotifyIcon.Visible = false;
            this.NotifyIcon.Dispose();
        }


        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            this.FlyoutAbout.IsOpen = (!this.FlyoutAbout.IsOpen);
        }


        private void BtnDonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/inzaniity");
        }

        private void BtnDiscordClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Inzaniity/Songify");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow sW = new SettingsWindow();
            sW.ShowDialog();
        }
    }
}