using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Views;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;
using Button = System.Windows.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace Songify_Slim.Views.WPFUI;

public partial class ShellWindow : IAppShell, INotifyPropertyChanged
{
    private bool _forceClose;
    private DispatcherTimer _spotifyIssueEtaTimer;

    public ShellWindow()
    {
        InitializeComponent();
        DataContext = this;
        ThemeHandler.ApplyTheme();
        // Don't navigate here: NavigationView's frame may not be ready until Loaded
    }

    /// <summary>For status bar binding.</summary>
    public ApiMetricsVm ApiMetrics => GlobalObjects.ApiMetrics;

    /// <summary>Version text for status bar.</summary>
    public string StatusBarVersion =>
        App.IsBeta ? $"Songify v{GlobalObjects.AppVersion} BETA © Songify.Rocks" : $"Songify v{Util.General.GlobalObjects.AppVersion} © Songify.Rocks";

    private ConnectionIndicatorState _twitchApiState = ConnectionIndicatorState.Unknown;
    private ConnectionIndicatorState _twitchBotState = ConnectionIndicatorState.Unknown;
    private bool _webServerRunning;
    private SpotifyIndicatorState _spotifyState = SpotifyIndicatorState.Disconnected;

    public Brush TwitchApiBrush => _twitchApiState == ConnectionIndicatorState.Connected ? Brushes.GreenYellow : Brushes.IndianRed;
    public Brush TwitchBotBrush => _twitchBotState == ConnectionIndicatorState.Connected ? Brushes.GreenYellow : Brushes.IndianRed;
    public Brush WebServerBrush => _webServerRunning ? Brushes.GreenYellow : Brushes.DarkGray;

    public Brush SpotifyBrush => _spotifyState == SpotifyIndicatorState.Premium ? Brushes.GreenYellow
        : _spotifyState == SpotifyIndicatorState.Free ? Brushes.DarkOrange
        : Brushes.Gray;

    /// <summary>Allow Exit / AppActions to bypass minimize-to-tray.</summary>
    public void RequestForceClose() => _forceClose = true;

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AppShellBridge.Register(this);
        Title = "Songify";

        string iconPack = App.IsBeta
            ? "pack://application:,,,/Resources/songifyBeta.ico"
            : "pack://application:,,,/Resources/songify.ico";
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(iconPack);
        bitmap.DecodePixelWidth = 32;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        Icon = bitmap;
        if (TitleBarLogo != null)
            TitleBarLogo.Source = bitmap;
        if (TrayIcon != null)
            TrayIcon.Icon = bitmap;

        // Restore position
        if (Settings.PosX != 0 || Settings.PosY != 0)
        {
            Left = Settings.PosX;
            Top = Settings.PosY;
        }

        StateChanged += ShellWindow_OnStateChanged;
        SetupSpotifyPersistentIssueBanner();
        AppFetchService.IdleBackoffChanged -= OnSpotifyIdleBackoffChanged;
        AppFetchService.IdleBackoffChanged += OnSpotifyIdleBackoffChanged;
        UpdateSpotifyIdleBackoffIndicator();

        // Navigate to Overview once the window and NavigationView are fully loaded
        if (RootNavigationView != null)
            RootNavigationView.Navigate(typeof(Pages.OverviewPage));

        // Run app startup logic (config checks, Spotify/Twitch init, song fetcher timer)
        try
        {
            await Util.General.AppStartup.RunAsync(this, useShellWindow: true);
        }
        catch (Exception ex)
        {
            Util.General.Logger.LogExc(ex);
        }
    }

    private void ShellWindow_OnStateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && Settings.Systray)
            MinimizeToTray();
    }

    private void ShellWindow_Closing(object sender, CancelEventArgs e)
    {
        if (!_forceClose && Settings.Systray)
        {
            e.Cancel = true;
            MinimizeToTray();
            return;
        }

        TeardownSpotifyPersistentIssueBanner();
        AppFetchService.IdleBackoffChanged -= OnSpotifyIdleBackoffChanged;
        AppShellBridge.Unregister(this);
        Settings.PosX = Left;
        Settings.PosY = Top;
        Util.Songify.AppFetchService.Stop();
        try
        {
            TrayIcon?.Unregister();
            TrayIcon?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void MinimizeToTray()
    {
        Hide();
        WindowState = WindowState.Normal;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    #region Spotify idle fetch backoff

    private void OnSpotifyIdleBackoffChanged()
    {
        UpdateSpotifyIdleBackoffIndicator();
    }

    private void UpdateSpotifyIdleBackoffIndicator()
    {
        void Apply()
        {
            if (BtnSpotifyIdleBackoff == null || TbSpotifyIdleBackoff == null)
                return;

            bool show = Settings.Player == Enums.PlayerType.Spotify && AppFetchService.IsSpotifyIdleBackoffActive;
            BtnSpotifyIdleBackoff.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!show)
                return;

            int seconds = AppFetchService.GetEffectiveSpotifyFetchIntervalSeconds();
            TbSpotifyIdleBackoff.Text = seconds >= 60
                ? (TryFindResource("window_main_spotify_idle_polling_minute") as string
                   ?? Properties.Resources.window_main_spotify_idle_polling_minute)
                : string.Format(
                    TryFindResource("window_main_spotify_idle_polling") as string
                    ?? Properties.Resources.window_main_spotify_idle_polling,
                    seconds);

            BtnSpotifyIdleBackoff.ToolTip =
                TryFindResource("window_main_spotify_idle_polling_tooltip") as string
                ?? Properties.Resources.window_main_spotify_idle_polling_tooltip;
        }

        if (!Dispatcher.CheckAccess())
            _ = Dispatcher.BeginInvoke(Apply);
        else
            Apply();
    }

    private void BtnSpotifyIdleBackoff_Click(object sender, RoutedEventArgs e)
    {
        AppFetchService.NotifySpotifyRelatedActivity("user restored from status bar");
    }

    #endregion Spotify idle fetch backoff

    #region Spotify persistent issue banner

    private void SetupSpotifyPersistentIssueBanner()
    {
        try
        {
            SpotifyUserNotifier.PersistentIssuesChanged -= OnSpotifyPersistentIssuesChanged;
            SpotifyUserNotifier.PersistentIssuesChanged += OnSpotifyPersistentIssuesChanged;

            UpdateSpotifyPersistentIssuesUi(Settings.SpotifyPersistentIssues);

            _spotifyIssueEtaTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _spotifyIssueEtaTimer.Tick -= SpotifyIssueEtaTimerOnTick;
            _spotifyIssueEtaTimer.Tick += SpotifyIssueEtaTimerOnTick;
            _spotifyIssueEtaTimer.Start();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, LogSource.Spotify, "SetupSpotifyPersistentIssueBanner failed: " + ex.Message);
        }
    }

    private void TeardownSpotifyPersistentIssueBanner()
    {
        try
        {
            SpotifyUserNotifier.PersistentIssuesChanged -= OnSpotifyPersistentIssuesChanged;
            if (_spotifyIssueEtaTimer != null)
            {
                _spotifyIssueEtaTimer.Stop();
                _spotifyIssueEtaTimer.Tick -= SpotifyIssueEtaTimerOnTick;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void SpotifyIssueEtaTimerOnTick(object sender, EventArgs e)
    {
        try
        {
            if (BrdSpotifyPersistentIssue.Visibility != Visibility.Visible)
                return;

            UpdateSpotifyPersistentIssuesUi(Settings.SpotifyPersistentIssues, refreshOnly: true);
        }
        catch
        {
            // ignored
        }
    }

    private void OnSpotifyPersistentIssuesChanged(IReadOnlyList<SpotifyPersistentIssue> issues)
    {
        try
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() => UpdateSpotifyPersistentIssuesUi(issues)));
                return;
            }

            UpdateSpotifyPersistentIssuesUi(issues);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, LogSource.Spotify, "OnSpotifyPersistentIssuesChanged failed: " + ex.Message);
        }
    }

    private void UpdateSpotifyPersistentIssuesUi(IReadOnlyList<SpotifyPersistentIssue> issues, bool refreshOnly = false)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateSpotifyPersistentIssuesUi(issues, refreshOnly));
            return;
        }

        DateTime nowUtc = DateTime.UtcNow;

        List<SpotifyPersistentIssue> list = (issues ?? Settings.SpotifyPersistentIssues ?? new List<SpotifyPersistentIssue>())
            .Where(x => x != null && !x.IsStale(nowUtc))
            .ToList();

        if (list.Count != (Settings.SpotifyPersistentIssues?.Count ?? 0))
            Settings.SpotifyPersistentIssues = list;

        SpotifyPersistentIssue issue = list.FirstOrDefault();

        if (issue == null)
        {
            if (!refreshOnly)
            {
                BrdSpotifyPersistentIssue.Visibility = Visibility.Collapsed;
                TbSpotifyPersistentIssueTitle.Text = "";
                TbSpotifyPersistentIssueBody.Text = "";
                TbSpotifyPersistentIssueEta.Text = "";
                ExpSpotifyPersistentIssues.Header = "More…";
                ExpSpotifyPersistentIssues.IsExpanded = false;
                PnlSpotifyPersistentIssues.Children.Clear();
            }
            return;
        }

        BrdSpotifyPersistentIssue.Visibility = Visibility.Visible;

        if (!refreshOnly)
        {
            TbSpotifyPersistentIssueTitle.Text = issue.Title ?? "Spotify issue";
            TbSpotifyPersistentIssueBody.Text = issue.Body ?? "";
        }

        if (issue.RetryUntilUtc is { } retryUtc)
        {
            TimeSpan remaining = retryUtc - nowUtc;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            string localTime = DateTime.SpecifyKind(retryUtc, DateTimeKind.Utc).ToLocalTime().ToString("g");
            if (remaining > TimeSpan.Zero)
            {
                string human = remaining.TotalHours >= 1
                    ? $"{(int)Math.Ceiling(remaining.TotalHours)} hour(s)"
                    : remaining.TotalMinutes >= 1
                        ? $"{(int)Math.Ceiling(remaining.TotalMinutes)} minute(s)"
                        : $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds))} second(s)";

                TbSpotifyPersistentIssueEta.Text = $"Estimated: works again in about {human} (around {localTime}).";
            }
            else
            {
                TbSpotifyPersistentIssueEta.Text = $"Estimated cooldown ended (around {localTime}). If it still fails, it may take a bit longer.";
            }
        }
        else
        {
            string whenLocal = DateTime.SpecifyKind(issue.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime().ToString("g");
            TbSpotifyPersistentIssueEta.Text = $"Seen at {whenLocal}.";
        }

        if (!refreshOnly)
        {
            int moreCount = Math.Max(0, list.Count - 1);
            ExpSpotifyPersistentIssues.Header = moreCount > 0 ? $"More… ({moreCount})" : "More…";
            PnlSpotifyPersistentIssues.Children.Clear();

            foreach (SpotifyPersistentIssue it in list.Skip(1))
            {
                var row = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 4, 0, 0) };

                Brush secondary = TryFindResource("TextFillColorSecondaryBrush") as Brush ?? Brushes.Gray;

                var dismiss = new Button
                {
                    Content = "×",
                    Width = 22,
                    Height = 22,
                    Padding = new Thickness(0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderBrush = null,
                    Foreground = secondary,
                    ToolTip = "Dismiss",
                    Tag = it.Id
                };
                dismiss.Click += BtnSpotifyPersistentIssueItemDismiss_Click;
                DockPanel.SetDock(dismiss, Dock.Right);
                row.Children.Add(dismiss);

                string whenLocal = DateTime.SpecifyKind(it.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime().ToString("g");
                string text = $"{it.Title ?? "Spotify issue"} — {whenLocal}";
                var tb = new TextBlock
                {
                    Text = text,
                    Foreground = secondary,
                    TextWrapping = TextWrapping.Wrap
                };
                row.Children.Add(tb);

                PnlSpotifyPersistentIssues.Children.Add(row);
            }

            ExpSpotifyPersistentIssues.Visibility = list.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void BtnSpotifyPersistentIssueDismiss_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SpotifyPersistentIssue current = Settings.SpotifyPersistentIssues?.FirstOrDefault();
            if (current != null)
                SpotifyUserNotifier.DismissPersistentIssue(current.Id);

            BrdSpotifyPersistentIssue.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, LogSource.Spotify, "Dismiss Spotify issue failed: " + ex.Message);
        }
    }

    private void BtnSpotifyPersistentIssueItemDismiss_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button b)
                return;

            string id = b.Tag as string;
            if (string.IsNullOrWhiteSpace(id))
                return;

            SpotifyUserNotifier.DismissPersistentIssue(id);
            UpdateSpotifyPersistentIssuesUi(Settings.SpotifyPersistentIssues);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, LogSource.Spotify, "Dismiss Spotify issue item failed: " + ex.Message);
        }
    }

    #endregion Spotify persistent issue banner

    #region TitleBar menu

    private void MenuWidget_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenWidget();

    private void MenuWebServerUrl_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenWebServerUrl();

    private void MenuQueueBrowser_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenQueueInBrowser();

    private void MenuHistoryBrowser_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenHistoryInBrowser();

    private void MenuTwitchLogin_OnClick(object sender, RoutedEventArgs e) => AppActions.TwitchLoginMain();

    private async void MenuTwitchConnect_OnClick(object sender, RoutedEventArgs e) =>
        await AppActions.TwitchConnectAsync();

    private async void MenuTwitchOnline_OnClick(object sender, RoutedEventArgs e)
    {
        bool isLive = await AppActions.CheckTwitchOnlineStatusAsync();
        if (MiTwitchCheckOnlineStatus != null)
            MiTwitchCheckOnlineStatus.Header =
                $"{Properties.Resources.menu_twitch_check_online_status} ({(isLive ? "Live" : "Offline")})";
    }

    private void MenuPatchNotes_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenPatchNotes();

    private void MenuFaq_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenFaq();

    private void MenuGitHub_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenGitHubIssues();

    private void MenuDiscord_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenDiscord();

    private void MenuLogFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenLogFolder();

    private void MenuAppFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenAppFolder();

    private void MenuCheckUpdates_OnClick(object sender, RoutedEventArgs e) => AppActions.CheckForUpdates();

    #endregion TitleBar menu

    #region Tray

    private void TrayIcon_OnLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e) => RestoreFromTray();

    private void TrayShow_OnClick(object sender, RoutedEventArgs e) => RestoreFromTray();

    private void TrayConnect_OnClick(object sender, RoutedEventArgs e) => _ = AppActions.TwitchConnectAsync();

    private async void TrayDisconnect_OnClick(object sender, RoutedEventArgs e) => await AppActions.TwitchDisconnectAsync();

    private void TrayExit_OnClick(object sender, RoutedEventArgs e) => AppActions.ExitApplication();

    #endregion Tray

    #region IAppShell (no-op or fallback when Shell is main window)

    public Task<AppDialogResult> ShowMessageAsync(
        string title,
        string message,
        AppDialogStyle style = AppDialogStyle.Primary,
        AppDialogSettings settings = null)
        => AppDialog.ShowMessageBoxAsync(title, message, style, settings);

    public void SetStatusText(string text)
    {
        // Shell has no free-text status field yet.
    }

    public void SetTwitchApiState(ConnectionIndicatorState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetTwitchApiState(state));
            return;
        }

        if (_twitchApiState == state)
            return;

        _twitchApiState = state;
        OnPropertyChanged(nameof(TwitchApiBrush));
    }

    public void SetTwitchBotState(ConnectionIndicatorState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetTwitchBotState(state));
            return;
        }

        if (_twitchBotState == state)
            return;

        _twitchBotState = state;
        OnPropertyChanged(nameof(TwitchBotBrush));
    }

    public void SetWebServerRunning(bool running)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetWebServerRunning(running));
            return;
        }

        if (_webServerRunning == running)
            return;

        _webServerRunning = running;
        OnPropertyChanged(nameof(WebServerBrush));
    }

    public void SetSpotifyState(SpotifyIndicatorState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetSpotifyState(state));
            return;
        }

        if (_spotifyState == state)
            return;

        _spotifyState = state;
        OnPropertyChanged(nameof(SpotifyBrush));
    }

    public void SetCoverImage(string coverPath)
    {
        // OverviewPage reads from GlobalObjects.CurrentSong
    }

    public void SetTextPreview(string text)
    {
        // OverviewPage shows current song; live output not shown in shell
    }

    public void SetCanvas(string path)
    { }

    public void StopCanvas()
    { }

    public string GetCurrentSongDisplayString()
    {
        var s = Util.General.GlobalObjects.CurrentSong;
        return s != null ? $"{s.Artists} - {s.Title}" : "";
    }

    #endregion IAppShell (no-op or fallback when Shell is main window)

    public void NavigateToQueue()
    {
        RootNavigationView.Navigate(typeof(Pages.QueuePage));
    }

    public void OpenSettings()
    {
        RootNavigationView.Navigate(typeof(Pages.SettingsPage));
    }

    public async void ConnectTwitch()
    {
        try
        {
            await Util.Songify.Twitch.TwitchHandler.StartOrRestartAsync();
        }
        catch (Exception ex)
        {
            Util.General.Logger.LogExc(ex);
        }
    }

    public void OpenConsole()
    {
        RootNavigationView.Navigate(typeof(Pages.ConsolePage));
    }

    private void TitleBar_OnCloseClicked(TitleBar sender, RoutedEventArgs args)
    {
        Settings.PosX = Left;
        Settings.PosY = Top;
        // Closing event handles Systray cancel / force close
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}