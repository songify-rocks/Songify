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
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;
using Songify_Slim.UserControls;
using Unosquare.Swan;
using Unosquare.Swan.Formatters;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Forms.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;
using Microsoft.Toolkit.Uwp.Notifications;
using TwitchLib.Api.Helix;
using Button = System.Windows.Controls.Button;
using ImageConverter = Songify_Slim.Util.General.ImageConverter;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Net.NetworkInformation;
using MahApps.Metro.Controls;
using Songify_Slim.Models.YTMD;
using Songify_Slim.Util.Songify.YTMDesktop;
using Songify_Slim.Properties;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace Songify_Slim.Views
{
    public partial class MainWindow
    {
        #region Variables

        public SocketIoClient IoClient;
        private bool _forceClose;
        private CancellationTokenSource _sCts;
        private DispatcherTimer _disclaimerTimer = new();
        private readonly DispatcherTimer _motdTimer = new();
        private int _secondsRemaining = 4;
        private readonly ContextMenu _contextMenu = new();
        private static readonly Timer Timer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);
        private string _selectedSource;
        private Timer _timerFetcher = new();
        private WindowConsole _consoleWindow;
        public NotifyIcon NotifyIcon = new();
        public SongFetcher Sf = new();
        public string SongArtist, SongTitle;
        public List<PSA> PSAs;

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
                PlayerType.Youtube,
                PlayerType.YTMDesktop
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
                if (IsWindowOpen<HistoryWindow>()) return;
                // Opens the 'History'-Window
                HistoryWindow hW = new() { Top = Top, Left = Left };
                hW.ShowDialog();
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
            // If a window of type Window_Settings is already open, focus that instead of opening a new one
            if (IsWindowOpen<Window_Settings>())
            {
                Window_Settings wS = Application.Current.Windows.OfType<Window_Settings>().First();
                wS.Focus();
                wS.Activate();
            }
            else
            {
                // Opens the 'Settings'-Window
                Window_Settings sW = new() { Top = Top, Left = Left };
                sW.Show();
            }
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
            {
            }
            else
            {
                // After user confirmation sends a command to the webserver which clears the queue
                MessageDialogResult msgResult = await this.ShowMessageAsync("",
                    Properties.Resources.mw_menu_Widget_disclaimer, MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult != MessageDialogResult.Affirmative) return;
                Settings.Upload = true;
            }

            Process.Start("https://widget.songify.rocks/" + Settings.Uuid);
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
                    if (_selectedSource is PlayerType.SpotifyWeb or PlayerType.YTMDesktop && Settings.DownloadCover)
                    {
                        img_cover.Visibility = Visibility.Visible;
                        GrdCover.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        img_cover.Visibility = Visibility.Collapsed;
                        GrdCover.Visibility = Visibility.Collapsed;
                    }
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

                case PlayerType.YTMDesktop:
                    //await Sf.FetchYTM();
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

        private async void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            InitialSetup();
            SetupUiAndThemes();
            CheckAndNotifyConfigurationIssues();
            SetupDisclaimer();
            SetupMotdTimer();

            if (!Settings.UseOwnApp)
            {
                GrdDisclaimer.Visibility = Visibility.Collapsed;

                MessageDialogResult result = await this.ShowMessageAsync(
                    "Warning",
                    "Songify now needs your own Spotify credentials (Client ID and Secret). Please follow the linked guide to set them up. This will help you avoid Spotify rate limits and ensure faster updates.",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "Open Guide",
                        NegativeButtonText = Properties.Resources.s_OK,
                    }
                );
                if (result == MessageDialogResult.Affirmative)
                    Process.Start(
                    "https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup");
                Settings.UseOwnApp = true;
            }

            bool internetAvailable = await WaitForInternetConnectionAsync();

            while (!internetAvailable)
            {
                // Show a dialog to the user that the app can't run without internet connection and wait for the user to click close or retry
                MessageDialogResult msgResult = await this.ShowMessageAsync("No Internet Connection",
                    "It seems that no internet connection could be established.\n\nDo you want to retry or close Songify?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings { AffirmativeButtonText = "Retry", NegativeButtonText = "Close" });
                switch (msgResult)
                {
                    case MessageDialogResult.Canceled:
                        this.Close();
                        break;

                    case MessageDialogResult.Negative:
                        this.Close();
                        break;

                    case MessageDialogResult.Affirmative:
                        internetAvailable = await WaitForInternetConnectionAsync();
                        break;

                    case MessageDialogResult.FirstAuxiliary:
                        break;

                    case MessageDialogResult.SecondAuxiliary:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            await HandleSpotifyInitializationAsync();
            await HandleTwitchInitializationAsync();
            await FinalSetupAndUpdatesAsync();
            await StartYtmdSocketIoClient();
        }

        public async Task StartYtmdSocketIoClient()
        {
            // Replace with your server URL and token
            const string serverUrl = "http://127.0.0.1:9863/api/v1/realtime";
            string token = Settings.YTMDToken;

            // Initialize the Socket.IO client
            IoClient = new SocketIoClient(serverUrl, token);

            // Run connection in a separate task
            try
            {
                await IoClient.ConnectAsync();
                GlobalObjects.IoClientConnected = true;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private static async Task<bool> WaitForInternetConnectionAsync()
        {
            int tries = 0;
            int maxRetries = 12;
            using HttpClient httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // Set a timeout for the request
            };

            // List of reliable URLs to check
            string[] urlsToCheck =
            [
                "https://www.google.com",
                "https://www.cloudflare.com",
                "https://www.amazon.com",
                "https://songify.rocks"
            ];

            while (true)
            {
                if (tries >= maxRetries)
                    return false;
                try
                {
                    // Create tasks for all URLs
                    List<Task<HttpResponseMessage>> tasks = urlsToCheck.Select(url => httpClient.GetAsync(url)).ToList();

                    // Wait for any task to complete successfully
                    Task<HttpResponseMessage> completedTask = await Task.WhenAny(tasks);

                    // Check if the response from the completed task was successful
                    if (completedTask is not null && (await completedTask).IsSuccessStatusCode)
                    {
                        Logger.LogStr("CORE: Internet Connection Established");
                        return true;
                    }
                }
                catch
                {
                    // Ignore exceptions and continue
                }

                Logger.LogStr("CORE: No Internet Connection");
                tries++;
                // Wait for a short period before retrying
                await Task.Delay(5000);
            }
        }

        private void SetupMotdTimer()
        {
            _motdTimer.Interval = TimeSpan.FromMinutes(5);
            _motdTimer.Tick += (o, args) =>
            {
                SetPSAs();
            };
            _motdTimer.Start();
            SetPSAs();
        }

        private async void SetPSAs()
        {
            PSAs = await WebHelper.GetPsa();
            if (PSAs == null || PSAs.Count == 0)
            {
                PnlMotds.Children.Clear();
                Badge.Badge = null!;
                badgeIcon.Kind = PackIconBootstrapIconsKind.Bell;
                return;
            }

            SetUnreadBadge();

            badgeIcon.Kind = PackIconBootstrapIconsKind.BellFill;

            if (PSAs.Any(motd => motd.Severity == "High"))
            {
                Badge.BadgeBackground = new SolidColorBrush(Colors.IndianRed);
                PSA highSeverityPsa = PSAs.First(motd => motd.Severity == "High");
                string msg = highSeverityPsa.MessageText;
                if (msg.Length > 190)
                    msg = msg.Substring(0, 190) + "...";
                if (highSeverityPsa != null)
                {
                    if (Settings.LastShownMotdId != highSeverityPsa.Id)
                    {
                        new ToastContentBuilder()
                            .AddArgument("msgId", highSeverityPsa.Id)
                            .AddText($"{highSeverityPsa.Author} from Songify")
                            .AddText(msg)
                            .AddAttributionText(highSeverityPsa.CreatedAtDateTime.ToString())
                            .Show(); // Not seeing the Show() method? Make sure you have versionversion 7.0, and if you're using .NET 6 (or later), then your TFM must be net6.0-windows10.0.17763.0 or greater
                                     // store the already shown psa id and don't show it again
                        Settings.LastShownMotdId = highSeverityPsa.Id;
                    }
                }
            }
            else if (PSAs.Any(motd => motd.Severity == "Medium"))
            {
                Badge.BadgeBackground = new SolidColorBrush(Colors.Orange);
            }
            else
                Badge.BadgeBackground = new SolidColorBrush(Colors.DarkGray);

            PnlMotds.Children.Clear();
            for (int i = 0; i < PSAs.Count; i++)
            {
                // Add the PsaControl
                PnlMotds.Children.Add(new PsaControl(PSAs[i]));

                // Add a spacer if it's not the last item
                if (i < PSAs.Count - 1)
                {
                    PnlMotds.Children.Add(new Rectangle
                    {
                        Height = 2,
                        Fill = Brushes.White,
                        Margin = new Thickness(0, 5, 0, 5) // Optional: adjust spacing around the line
                    });
                }
            }
        }

        public void SetUnreadBadge()
        {
            try
            {
                // compare motds ids with Settings.ReadNotificationIds and if there are new motds, show the badge
                if (Settings.ReadNotificationIds != null)
                {
                    List<PSA> unreadMotds = PSAs.Where(m => !Settings.ReadNotificationIds.Contains(m.Id)).ToList();
                    if (unreadMotds.Count > 0)
                    {
                        Badge.Badge = unreadMotds.Count;
                    }
                    else
                    {
                        Badge.Badge = null!;
                    }
                }
                else if (Badge.Badge.ToString() != PSAs.Count.ToString())
                {
                    Badge.Badge = PSAs.Count;
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        private void InitialSetup()
        {
            if (Settings.Systray)
                MinimizeToSysTray();
            // Initialize toast notification system (if needed)
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
            GrdDisclaimer.Visibility = Settings.DonationReminder ? Visibility.Collapsed : Visibility.Visible;

            if (!Directory.Exists(Settings.Directory) && MessageBox.Show($"The directory \"{Settings.Directory}\" doesn't exist.\nThe output directory has been set to \"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\".", "Directory doesn't exist", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Settings.Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            }

            Settings.MsgLoggingEnabled = false;
            AddSourcesToSourceBox();
            CreateSystrayIcon();
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Handle the toast notification activation (e.g., open a specific window or show a message)
            // Get the arguments string
            string arguments = e.Argument;

            // Convert the arguments to a dictionary
            Dictionary<string, string> argsDictionary = ParseArguments(arguments);

            if (!argsDictionary.TryGetValue("msgId", out string value1)) return;
            if (!int.TryParse(value1, out int intValue)) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Ensure PSAs is not null or empty
                    if (PSAs == null || !PSAs.Any())
                    {
                        throw new InvalidOperationException("PSAs collection is null or empty.");
                    }

                    // Attempt to find the PSA
                    PSA psa = PSAs.FirstOrDefault(o => o.Id == intValue);

                    // Check if psa is found
                    if (psa == null)
                    {
                        throw new InvalidOperationException($"No PSA found with Id {intValue}.");
                    }

                    // Create and show the dialog
                    WindowUniversalDialog wUd = new WindowUniversalDialog(psa, "Notification");
                    wUd.Show();
                }
                catch (Exception ex)
                {
                    // Handle or log the exception
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
        }

        // Method to parse arguments string to dictionary
        private Dictionary<string, string> ParseArguments(string arguments)
        {
            var argsDictionary = new Dictionary<string, string>();

            // Check if arguments are not null or empty
            if (!string.IsNullOrEmpty(arguments))
            {
                // Split the arguments string by '&' to separate key-value pairs
                string[] pairs = arguments.Split('&');

                foreach (string pair in pairs)
                {
                    // Split each pair by '=' to get the key and value
                    string[] keyValue = pair.Split('=');

                    if (keyValue.Length == 2)
                    {
                        // Add the key-value pair to the dictionary
                        argsDictionary[keyValue[0]] = keyValue[1];
                    }
                }
            }

            return argsDictionary;
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
            //LblCopyright.Content = App.IsBeta ? $"Songify v{GlobalObjects.AppVersion} BETA Copyright ©" : $"Songify v{GlobalObjects.AppVersion} Copyright ©";
            LblCopyright.Content = App.IsBeta ? $"Songify v1.6.8 BETA_1 Copyright ©" : $"Songify v{GlobalObjects.AppVersion} Copyright ©";
            BetaPanel.Visibility = App.IsBeta ? Visibility.Visible : Visibility.Collapsed;

            tbFontSize.Text = Settings.Fontsize.ToString();
            TxtblockLiveoutput.FontSize = Settings.Fontsize;
        }

        private async Task HandleSpotifyInitializationAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.SpotifyAccessToken) && string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                    TxtblockLiveoutput.Text = Properties.Resources.mw_LiveOutputLinkSpotify;
                else
                    await SpotifyApiHandler.DoAuthAsync();

                img_cover.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }

            img_cover.Visibility = _selectedSource is PlayerType.SpotifyWeb or PlayerType.YTMDesktop ? Visibility.Visible : Visibility.Collapsed;
        }

        public void PlayVideoFromUrl(string url)
        {
            img_cover.Visibility = Visibility.Collapsed;
            CoverCanvas.Visibility = Visibility.Visible;
            string newUri = url.Replace("\"", "");
            Uri uri = new Uri(newUri);
            CoverCanvas.Stop();
            CoverCanvas.Source = null;
            CoverCanvas.Source = uri;
            CoverCanvas.Play();
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
                await TwitchHandler.InitializeApi(Enums.TwitchAccount.Main);
            if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
                await TwitchHandler.InitializeApi(Enums.TwitchAccount.Bot);
        }

        private static void CheckAndNotifyConfigurationIssues()
        {
            Logger.LogStr($"LOCATION: {Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}");

            string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (assemblyLocation != null && assemblyLocation.Contains(".zip"))
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
                switch (Settings.IsLive)
                {
                    case true:
                        Logger.LogStr("Stream is LIVE");
                        break;

                    case false:
                        Logger.LogStr("Stream is NOT live");
                        break;
                }
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

        private void CheckForUpdates()
        {
            AutoUpdater.Mandatory = false;
            AutoUpdater.UpdateMode = Mode.Normal;
            AutoUpdater.AppTitle = "Songify";
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            Logger.LogStr("Checking for update...");
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

            BitmapImage img = App.IsBeta
                ? new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"))
                : new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"));
            Icon icon = ImageConverter.ConvertBitmapImageToIcon(img);

            NotifyIcon.Icon = icon;
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
            MessageDialogResult msgResult = await this.ShowMessageAsync(Properties.Resources.s_Warning,
                Properties.Resources.mw_clearQueueDisclaimer, MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = Properties.Resources.msgbx_Yes, NegativeButtonText = Properties.Resources.msgbx_No });
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
                Visibility vis = _selectedSource is PlayerType.SpotifyWeb or PlayerType.YTMDesktop && Settings.DownloadCover
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                double maxWidth = _selectedSource is PlayerType.SpotifyWeb or PlayerType.YTMDesktop && Settings.DownloadCover
                    ? 500
                    : (int)Width - 6;

                if (img_cover.Visibility != vis)
                    img_cover.Visibility = vis;
                if (GrdCover.Visibility != vis)
                    GrdCover.Visibility = vis;
                if (Math.Abs((int)TxtblockLiveoutput.MaxWidth - maxWidth) > 0)
                    TxtblockLiveoutput.MaxWidth = maxWidth;
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
                case PlayerType.YTMDesktop:
                    // stop the timer if the player is YTMDesktop
                    _timerFetcher.Enabled = false;
                    break;

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

                case PlayerType.SpotifyWeb:
                    // Prevent Rate Limiting
                    FetchTimer(Settings.UseOwnApp ? 1000 : 20000);
                    break;
            }
        }

        private void Mi_TwitchAPI_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(Enums.TwitchAccount.Main);
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            if (_consoleWindow == null) return;
            if (GlobalObjects.DetachConsole) return;
            _consoleWindow.Left = Left + Width;
            _consoleWindow.Top = Top;
        }

        public void SetCanvas(string canvasUrl)
        {
            const int numberOfRetries = 5;
            const int delayOnRetry = 1000;

            for (int i = 1; i < numberOfRetries; i++)
            {
                try
                {
                    PlayVideoFromUrl(canvasUrl);
                    // if Settings.Player (int) != playerType.SpotifyWeb, hide the cover image
                    if (Settings.Player != 0 && Settings.DownloadCover)
                    {
                        GrdCover.Visibility = Visibility.Collapsed;
                        CoverCanvas.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        GrdCover.Visibility = Visibility.Visible;
                        CoverCanvas.Visibility = Visibility.Visible;
                    }
                    img_cover.Visibility = Visibility.Collapsed;

                    Logger.LogStr("COVER: Set succesfully");
                    break;
                }
                catch (Exception) when (i <= numberOfRetries)
                {
                    Thread.Sleep(delayOnRetry);
                }
            }
        }

        public async void SetCoverImage(string coverPath)
        {
            const int numberOfRetries = 5;
            const int delayOnRetry = 1000;

            for (int i = 1; i < numberOfRetries; i++)
            {
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

                    // if Settings.Player (int) != playerType.SpotifyWeb, hide the cover image
                    if ((Settings.Player == 0 || Settings.Player == 6) && Settings.DownloadCover)
                    {
                        img_cover.Visibility = Visibility.Visible;
                        GrdCover.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        img_cover.Visibility = Visibility.Collapsed;
                        GrdCover.Visibility = Visibility.Collapsed;
                    }
                    CoverCanvas.Visibility = Visibility.Collapsed;

                    Logger.LogStr("COVER: Set succesfully");
                    break;
                }
                catch (Exception) when (i <= numberOfRetries)
                {
                    Thread.Sleep(delayOnRetry);
                }
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

        private void btnFontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = MathUtils.Clamp(Settings.Fontsize + 2, 2, 72);
            Settings.Fontsize = fontSize;
            TxtblockLiveoutput.FontSize = fontSize;
            tbFontSize.Text = fontSize.ToString();
        }

        private void btnFontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = MathUtils.Clamp(Settings.Fontsize - 2, 2, 72);
            Settings.Fontsize = fontSize;
            TxtblockLiveoutput.FontSize = fontSize;
            tbFontSize.Text = fontSize.ToString();
        }

        public void SetTextPreview(string replace)
        {
            TxtblockLiveoutput.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TxtblockLiveoutput.Text = replace;
            }));
        }

        private void Mi_Update_OnClick(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private void BtnMotd_Click(object sender, RoutedEventArgs e)
        {
            SetPSAs();
            // If any child of pnlMotds as MotdcControl is unread, show the all read button
            List<int> readIds = Settings.ReadNotificationIds ?? [];
            Button btnFlyOutAllread = GlobalObjects.FindChild<Button>(FlyMotd, "BtnFlyOutAllread");
            if (btnFlyOutAllread != null)
            {
                btnFlyOutAllread.Visibility = Visibility.Hidden; // Example usage
                foreach (UIElement pnlMotdsChild in PnlMotds.Children)
                {
                    if (pnlMotdsChild is not PsaControl motdControl) continue;
                    if (readIds.Contains(motdControl.Psa.Id)) continue;
                    if (btnFlyOutAllread != null)
                    {
                        // Now you can interact with the button
                    }
                    break;
                }
            }

            FlyMotd.IsOpen = !FlyMotd.IsOpen;
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FlyMotd.Width = ActualWidth;
        }

        private void BtnFlyOutClose_OnClick(object sender, RoutedEventArgs e)
        {
            FlyMotd.IsOpen = false;
        }

        private void BtnFlyOutAllread_OnClick(object sender, RoutedEventArgs e)
        {
            List<int> readIds = Settings.ReadNotificationIds ?? [];
            foreach (PSA motd in PSAs.Where(motd => !readIds.Contains(motd.Id)))
            {
                readIds.Add(motd.Id);
            }
            Settings.ReadNotificationIds = readIds;

            foreach (UIElement pnlMotdsChild in PnlMotds.Children)
            {
                ((PsaControl)pnlMotdsChild).btnRead.Content = new PackIconMaterial()
                {
                    Kind = PackIconMaterialKind.Check,
                    Width = 12,
                    Height = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            Badge.Badge = null!;
        }

        private void CoverCanvas_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            CoverCanvas.Position = TimeSpan.Zero; // Restart the video from the beginning
            CoverCanvas.Play(); // Play again
        }

        public void StopCanvas()
        {
            if (CoverCanvas != null)
            {
                CoverCanvas.Stop();      // Stop the playback
                CoverCanvas.Close();     // Close the MediaElement to release resources (optional, see note)
                CoverCanvas.Source = null; // Set Source to null to release the file lock
            }
        }

        private void BtnMenuViewUserList_Click(object sender, RoutedEventArgs e)
        {
            //Check if a window of type Window_Userlist is open. Focus it if it is, if not open a new one
            if (IsWindowOpen<Window_Userlist>()) return;
            Window_Userlist wU = new() { Top = Top, Left = Left };
            wU.Show();
        }

        private void BtnAppFolderClick(object sender, RoutedEventArgs e)
        {
            string direcotry = Directory.GetCurrentDirectory();
            Process.Start(direcotry);
        }
    }
}