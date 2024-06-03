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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
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

        private bool _forceClose;
        private CancellationTokenSource _sCts;
        private DispatcherTimer _disclaimerTimer = new();
        private int _secondsRemaining = 4;
        private readonly ContextMenu _contextMenu = new();
        private static readonly Timer Timer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);
        private string _selectedSource;
        private Timer _timerFetcher = new();
        private WindowConsole _consoleWindow;
        public NotifyIcon NotifyIcon = new();
        public SongFetcher Sf = new();
        public string SongArtist, SongTitle;

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
        {
            InitializeComponent();
            Timer.Elapsed += TelemetryTask;
            Timer.Start();
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
            [
                PlayerType.SpotifyWeb,
                PlayerType.SpotifyLegacy,
                PlayerType.Deezer,
                PlayerType.FooBar2000,
                PlayerType.Vlc,
                PlayerType.Youtube
            ];
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
            switch (_selectedSource)
            {
                case PlayerType.SpotifyLegacy:
                    await Sf.FetchDesktopPlayer("Spotify");
                    break;

                case PlayerType.Youtube:
                    await Sf.FetchBrowser("YouTube");
                    break;

                case PlayerType.Vlc:
                    await Sf.FetchDesktopPlayer("vlc");
                    break;

                case PlayerType.FooBar2000:
                    await Sf.FetchDesktopPlayer("foobar2000");
                    break;

                case PlayerType.Deezer:
                    await Sf.FetchBrowser("Deezer");
                    break;

                case PlayerType.SpotifyWeb:
                    await Sf.FetchSpotifyWeb();
                    break;
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

        //private async void MetroWindowLoaded(object sender, RoutedEventArgs e)
        //{
        //    GrdDisclaimer.Visibility = Settings.DonationReminder ? Visibility.Collapsed : Visibility.Visible;
        //    if (!Directory.Exists(Settings.Directory) && MessageBox.Show($"The directory \"{Settings.Directory}\" doesn't exist.\nThe output directory has been set to \"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\".", "Directory doesn't exist", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
        //    {
        //        Settings.Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        //    }

        //    SetIconColors();

        //    Settings.MsgLoggingEnabled = false;

        //    // Add sources to combobox
        //    AddSourcesToSourceBox();

        //    // Create systray menu and icon and show it
        //    CreateSystrayIcon();

        //    // set the current theme
        //    ThemeHandler.ApplyTheme();

        //    // get the software version from assembly
        //    Assembly assembly = Assembly.GetExecutingAssembly();
        //    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        //    GlobalObjects.AppVersion = fvi.FileVersion;

        //    // check for update
        //    CheckForUpdates();

        //    // set the cbx index to the correct source
        //    cbx_Source.SelectedIndex = Settings.Player;
        //    _selectedSource = cbx_Source.SelectedValue.ToString();
        //    cbx_Source.SelectionChanged += Cbx_Source_SelectionChanged;

        //    // text in the bottom right
        //    LblCopyright.Content = GlobalObjects.IsBeta ? $"Songify v Copyright ©" : $"Songify v{GlobalObjects.AppVersion} Copyright ©";

        //    if (_selectedSource == PlayerType.SpotifyWeb)
        //    {
        //        if (string.IsNullOrEmpty(Settings.SpotifyAccessToken) && string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
        //            TxtblockLiveoutput.Text = Properties.Resources.mw_LiveOutputLinkSpotify;
        //        else
        //            await SpotifyApiHandler.DoAuthAsync();

        //        img_cover.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        img_cover.Visibility = Visibility.Hidden;
        //    }
        //    if (Settings.AutoStartWebServer) GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);
        //    if (Settings.OpenQueueOnStartup) OpenQueue();
        //    if (Settings.TwAutoConnect)
        //    {
        //        TwitchHandler.MainConnect();
        //        TwitchHandler.BotConnect();
        //    }
        //    if (Settings.AutoClearQueue)
        //    {
        //        GlobalObjects.ReqList.Clear();
        //        dynamic payload = new
        //        {
        //            uuid = Settings.Uuid,
        //            key = Settings.AccessKey
        //        };
        //        await WebHelper.QueueRequest(WebHelper.RequestMethod.Clear, Json.Serialize(payload));
        //        //WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
        //    }

        //    if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
        //        await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Main);
        //    if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
        //        await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Bot);
        //    await SendTelemetry();
        //    Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
        //    // automatically start fetching songs
        //    SetFetchTimer();

        //    Logger.LogStr($"LOCATION: {Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}");
        //    // if location is in AppData\Local\Temp or location contains Songify.zip or is in a System folder, Notify the user

        //    if (Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)!.Contains("Songify.zip"))
        //    {
        //        MessageBox.Show(
        //            "Please extract Songify to a directory. The app can't save the config when run directly from the zip file.\nWe suggest a folder on the Desktop or in Documents.",
        //            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        Application.Current.Shutdown();
        //    }
        //    if (Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)!.Contains(@"C:\Program Files") ||
        //        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)!.Contains(@"C:\Program Files (x86)") ||
        //        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)!.Contains(@"C:\ProgramData"))
        //    {
        //        //Try to save a file at the current location, if we have permission to write / read files here don't do aynthing, else show the messagebox
        //        try
        //        {
        //            File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/test.txt", @"test");
        //            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/test.txt");
        //        }
        //        catch (Exception)
        //        {
        //            MessageBox.Show(
        //                "Please move Songify to a different directory. The app can't save the config when run from this directory.\nWe suggest a folder on the Desktop or in Documents.",
        //                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            Application.Current.Shutdown();
        //        }
        //    }

        //    if (!Settings.DonationReminder)
        //    {
        //        GrdDisclaimer.Visibility = Visibility.Visible;
        //        _disclaimerTimer = new DispatcherTimer
        //        {
        //            Interval = TimeSpan.FromSeconds(1)
        //        };
        //        _disclaimerTimer.Tick += DisclaimerTimerOnTick;
        //        _disclaimerTimer.Start();
        //        BtnDisclaimerClose.Visibility = Visibility.Visible;
        //        TbDisclaimerDismiss.Text = "This message will disappear in 5 seconds";
        //    }

        //    if (!Settings.UpdateRequired) return;
        //    List<int> userLevels = new();
        //    for (int i = 0; i <= Settings.TwSrUserLevel; i++)
        //    {
        //        userLevels.Add(i);
        //    }
        //    if (Settings.UserLevelsCommand.Count == 0) Settings.UserLevelsCommand = userLevels;
        //    if (Settings.UserLevelsReward.Count == 0) Settings.UserLevelsReward = userLevels;
        //    OpenPatchNotes();
        //    Settings.UpdateRequired = false;
        //}

        private async void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            InitialSetup();

            SetupUiAndThemes();

            await HandleSpotifyInitializationAsync();
            await HandleTwitchInitializationAsync();

            CheckAndNotifyConfigurationIssues();

            SetupDisclaimer();

            await FinalSetupAndUpdatesAsync();
        }

        private void InitialSetup()
        {
            if (Settings.Systray)
                MinimizeToSysTray();

            GrdDisclaimer.Visibility = Settings.DonationReminder ? Visibility.Collapsed : Visibility.Visible;

            if (!Directory.Exists(Settings.Directory) && MessageBox.Show($"The directory \"{Settings.Directory}\" doesn't exist.\nThe output directory has been set to \"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\".", "Directory doesn't exist", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Settings.Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            }

            Settings.MsgLoggingEnabled = false;
            AddSourcesToSourceBox();
            CreateSystrayIcon();
        }

        private void SetupUiAndThemes()
        {
            SetIconColors();
            ThemeHandler.ApplyTheme();

            // get the software version from assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            GlobalObjects.AppVersion = fvi.FileVersion;

            // set the cbx index to the correct source
            cbx_Source.SelectedIndex = Settings.Player;
            _selectedSource = cbx_Source.SelectedValue.ToString();
            cbx_Source.SelectionChanged += Cbx_Source_SelectionChanged;

            // text in the bottom right
            LblCopyright.Content = GlobalObjects.IsBeta ? $"Songify v Copyright ©" : $"Songify v{GlobalObjects.AppVersion} Copyright ©";
        }

        private async Task HandleSpotifyInitializationAsync()
        {
            if (_selectedSource == PlayerType.SpotifyWeb)
            {
                if (string.IsNullOrEmpty(Settings.SpotifyAccessToken) && string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                    TxtblockLiveoutput.Text = Properties.Resources.mw_LiveOutputLinkSpotify;
                else
                    await SpotifyApiHandler.DoAuthAsync();

                img_cover.Visibility = Visibility.Visible;
            }
            else
            {
                img_cover.Visibility = Visibility.Hidden;
            }
        }

        private static async Task HandleTwitchInitializationAsync()
        {
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
            }

            if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
                await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Main);
            if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
                await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Bot);
        }

        private static void CheckAndNotifyConfigurationIssues()
        {
            Logger.LogStr($"LOCATION: {Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}");

            string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (assemblyLocation != null && assemblyLocation.Contains("Songify.zip"))
            {
                MessageBox.Show("Please extract Songify to a directory. The app can't save the config when run directly from the zip file.\nWe suggest a folder on the Desktop or in Documents.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }

            if (assemblyLocation == null || (!assemblyLocation.Contains(@"C:\Program Files") &&
                                             !assemblyLocation.Contains(@"C:\Program Files (x86)") &&
                                             !assemblyLocation.Contains(@"C:\ProgramData"))) return;
            try
            {
                File.WriteAllText(Path.Combine(assemblyLocation, "test.txt"), @"test");
                File.Delete(Path.Combine(assemblyLocation, "test.txt"));
            }
            catch (Exception)
            {
                MessageBox.Show("Please move Songify to a different directory. The app can't save the config when run from this directory.\nWe suggest a folder on the Desktop or in Documents.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
        }

        private void SetupDisclaimer()
        {
            if (Settings.DonationReminder) return;
            GrdDisclaimer.Visibility = Visibility.Visible;
            _disclaimerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _disclaimerTimer.Tick += DisclaimerTimerOnTick;
            _disclaimerTimer.Start();
            BtnDisclaimerClose.Visibility = Visibility.Visible;
            TbDisclaimerDismiss.Text = "This message will disappear in 5 seconds";
        }

        private async Task FinalSetupAndUpdatesAsync()
        {
            try
            {
                Logger.LogStr("Starting final setup and updates");
                Logger.LogStr("Sending Telemetry");
                await SendTelemetry();
                Logger.LogStr("Telemetry sent");
                Logger.LogStr("Check Stream up");
                Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
                Logger.LogStr("Check Stream up done");
                Logger.LogStr("SetFetchTimer");
                SetFetchTimer();
                Logger.LogStr("SetFetchTimer done");

                if (Settings.UpdateRequired)
                {
                    OpenPatchNotes();
                    Settings.UpdateRequired = false;
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }

            CheckForUpdates();
        }



        private void DisclaimerTimerOnTick(object sender, EventArgs e)
        {
            if (_secondsRemaining >= 0)
            {
                if (_secondsRemaining == 0)
                    TbDisclaimerDismiss.Text = "This message will disappear now :)";
                else
                    TbDisclaimerDismiss.Text = _secondsRemaining == 1
                        ? $"This message will disappear in {_secondsRemaining} second"
                        : $"This message will disappear in {_secondsRemaining} seconds";
                _secondsRemaining--;
            }
            else
            {
                _disclaimerTimer.Stop();
                TbDisclaimerDismiss.Text = ""; // Clears the message after countdown
                GrdDisclaimer.Visibility = Visibility.Collapsed;
            }
        }

        private static void CheckForUpdates()
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
            _contextMenu.MenuItems.AddRange([
                new System.Windows.Forms.MenuItem("Twitch", [
                    new System.Windows.Forms.MenuItem("Connect", (_, _) =>
                    {
                        TwitchHandler.BotConnect();
                        TwitchHandler.MainConnect();
                    }),
                    new System.Windows.Forms.MenuItem("Disconnect", (_, _) => { TwitchHandler.Client.Disconnect(); })
                ]),
                new System.Windows.Forms.MenuItem("Show", (_, _) =>
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Show();
                        WindowState = WindowState.Normal;
                    }));
                }),
                new System.Windows.Forms.MenuItem("Exit", (_, _) =>
                {
                    _forceClose = true;
                    Close();
                })
            ]);
            NotifyIcon.Icon = Properties.Resources.songify;
            NotifyIcon.ContextMenu = _contextMenu;
            NotifyIcon.Visible = true;
            NotifyIcon.DoubleClick += (_, _) =>
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

        private static void AutoUpdater_ApplicationExitEvent()
        {
            //Create folder for the current version and export the config to the folder
            if (!Directory.Exists(GlobalObjects.RootDirectory + $"/Backup/{GlobalObjects.AppVersion.Replace(".", "_")}"))
                Directory.CreateDirectory(GlobalObjects.RootDirectory + $"/Backup/{GlobalObjects.AppVersion.Replace(".", "_")}");

            ConfigHandler.WriteAllConfig(Settings.Export(), GlobalObjects.RootDirectory + $"/Backup/{GlobalObjects.AppVersion.Replace(".", "_")}", true);
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

        private void Mi_Exit_Click(object sender, RoutedEventArgs e)
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

        private void Mi_TW_BotResponses_Click(object sender, RoutedEventArgs e)
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
                await GetCurrentSongAsync();
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

        private static void OpenQueue()
        {
            if (IsWindowOpen<WindowQueue>()) return;
            WindowQueue wQ = new();
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
                    FetchTimer(Settings.UseOwnApp ? 2000 : 20000);
                    break;
            }
        }

        private void Mi_TwitchAPI_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(TwitchHandler.TwitchAccount.Main);
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            if (_consoleWindow == null) return;
            if (GlobalObjects.DetachConsole) return;
            _consoleWindow.Left = Left + Width;
            _consoleWindow.Top = Top;
        }

        public async void SetCoverImage(string coverPath)
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
                        image.UriSource = new Uri(coverPath);
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

        private void BtnDisclaimerClose_Click(object sender, RoutedEventArgs e)
        {
            _disclaimerTimer.Stop();
            GrdDisclaimer.Visibility = Visibility.Collapsed;
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

        public void SetTextPreview(string replace)
        {
            TxtblockLiveoutput.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TxtblockLiveoutput.Text = replace;
            }));
        }
    }
}