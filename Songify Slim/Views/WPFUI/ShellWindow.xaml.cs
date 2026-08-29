using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Songify_Slim.Models;
using Songify_Slim.Models.Responses;
using Songify_Slim.Models.Spotify;
using Songify_Slim.UserControls;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Youtube.Pear;
using Songify_Slim.Util.Youtube.YTMYHCH.YtmDesktopApi;
using Songify_Slim.Views;
using TwitchLib.Api.Helix.Models.EventSub;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;
using static Songify_Slim.Util.General.Enums;
using Button = System.Windows.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;

namespace Songify_Slim.Views.WPFUI;

public partial class ShellWindow : IAppShell, INotifyPropertyChanged
{
    private bool _forceClose;
    private DispatcherTimer _spotifyIssueEtaTimer;
    private DispatcherTimer _nowPlayingTimer;

    public ShellWindow()
    {
        InitializeComponent();
        DataContext = this;
        ThemeHandler.ApplyTheme();
        UiScaleHandler.ApplyToWindow(this, Settings.UiScale);
        ApplyMinSizeOverride();
        // Don't navigate here: NavigationView's frame may not be ready until Loaded
    }

    /// <summary>Designed minimum size when "overrule min size" is off.</summary>
    public const double DefaultMinWidth = 900;
    public const double DefaultMinHeight = 500;

    /// <summary>Applies or clears the shell min-width/height constraints from settings.</summary>
    public void ApplyMinSizeOverride()
    {
        bool overrule = Settings.OverruleShellMinWidth;
        UiScaleHandler.SetUnscaledMinSize(
            this,
            overrule ? 0 : DefaultMinWidth,
            overrule ? 0 : DefaultMinHeight);
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
    private Brush _pearBrush = Brushes.Gray;
    private string _pearStatusText = "";
    private int _tourStep;
    private DispatcherTimer _premiumReminderTimer;
    private int _premiumReminderSecondsLeft;

    private string Loc(string key, string fallback)
        => TryFindResource(key) as string ?? fallback;

    private string LocFormat(string key, string fallback, params object[] args)
    {
        try { return string.Format(Loc(key, fallback), args); }
        catch (FormatException) { return fallback; }
    }

    public Brush TwitchApiBrush => _twitchApiState == ConnectionIndicatorState.Connected ? Brushes.GreenYellow : Brushes.IndianRed;
    public Brush TwitchBotBrush => _twitchBotState == ConnectionIndicatorState.Connected ? Brushes.GreenYellow : Brushes.IndianRed;
    public Brush WebServerBrush => _webServerRunning ? Brushes.GreenYellow : Brushes.DarkGray;

    /// <summary>Spotify stays visible even when Pear is selected (same as main); gray = not the active player.</summary>
    public Brush SpotifyBrush
    {
        get
        {
            if (Settings.Player != PlayerType.Spotify)
                return Brushes.Gray;

            return _spotifyState == SpotifyIndicatorState.Premium ? Brushes.GreenYellow
                : _spotifyState == SpotifyIndicatorState.Free ? Brushes.DarkOrange
                : Brushes.IndianRed;
        }
    }

    public Brush PearBrush => _pearBrush;
    public string PearStatusText => _pearStatusText;

    /// <summary>Allow Exit / AppActions to bypass minimize-to-tray.</summary>
    public void RequestForceClose() => _forceClose = true;

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyMinSizeOverride();
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
        StartTitleBarNowPlayingTimer();
        AppFetchService.IdleBackoffChanged -= OnSpotifyIdleBackoffChanged;
        AppFetchService.IdleBackoffChanged += OnSpotifyIdleBackoffChanged;
        AppFetchService.PlayerSourceChanged -= OnPlayerSourceChanged;
        AppFetchService.PlayerSourceChanged += OnPlayerSourceChanged;
        PearWebSocketClient.ConnectionStateChanged -= OnPearConnectionStateChanged;
        PearWebSocketClient.ConnectionStateChanged += OnPearConnectionStateChanged;
        SongifyPremiumService.StatusChanged -= OnSongifyPremiumStatusChanged;
        SongifyPremiumService.StatusChanged += OnSongifyPremiumStatusChanged;
        ApplySongifyPremiumStatus();
        UpdateSpotifyIdleBackoffIndicator();
        UpdatePearStatusIndicator();
        OnPropertyChanged(nameof(SpotifyBrush));

        PsaManager.Changed -= OnPsaChanged;
        PsaManager.Changed += OnPsaChanged;
        PsaManager.ListUpdated -= OnPsaListUpdated;
        PsaManager.ListUpdated += OnPsaListUpdated;
        PsaManager.Start();
        RebuildPsaPanel();
        UpdatePsaBadge();
        UpdatePsaMarkAllReadVisibility();
#if DEBUG
        SetupDebugPsaMenu();
#endif
        ApplyOpaqueFlyPsaBackground();

        // Navigate to Overview once the window and NavigationView are fully loaded
        if (RootNavigationView != null)
            RootNavigationView.Navigate(typeof(Pages.OverviewPage));

        // Migrate legacy history.shr → history.yaml before the fetcher starts writing.
        try
        {
            await HistoryStore.MigrateLegacyIfNeededAsync(this);
        }
        catch (Exception ex)
        {
            Util.General.Logger.LogExc(ex);
        }

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

        HidePremiumReminder();
        _nowPlayingTimer?.Stop();
        TeardownSpotifyPersistentIssueBanner();
        AppFetchService.IdleBackoffChanged -= OnSpotifyIdleBackoffChanged;
        AppFetchService.PlayerSourceChanged -= OnPlayerSourceChanged;
        PearWebSocketClient.ConnectionStateChanged -= OnPearConnectionStateChanged;
        SongifyPremiumService.StatusChanged -= OnSongifyPremiumStatusChanged;
        PsaManager.Changed -= OnPsaChanged;
        PsaManager.ListUpdated -= OnPsaListUpdated;
        PsaManager.Stop();
        SongifyPremiumService.Stop();
        AppShellBridge.Unregister(this);
        Settings.PosX = Left;
        Settings.PosY = Top;
        QueueWindow.CloseIfOpen();
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

    private void OnPlayerSourceChanged()
    {
        void Apply()
        {
            UpdatePearStatusIndicator();
            OnPropertyChanged(nameof(SpotifyBrush));
            UpdateSpotifyIdleBackoffIndicator();
        }

        if (!Dispatcher.CheckAccess())
            _ = Dispatcher.BeginInvoke(Apply);
        else
            Apply();
    }

    private void OnPearConnectionStateChanged()
    {
        if (!Dispatcher.CheckAccess())
            _ = Dispatcher.BeginInvoke(UpdatePearStatusIndicator);
        else
            UpdatePearStatusIndicator();
    }

    private void UpdatePearStatusIndicator()
    {
        ServiceIndicatorState state = new(
            isSelected: Settings.Player == PlayerType.Pear,
            isConnecting: PearWebSocketClient.IsConnecting,
            isConnected: PearWebSocketClient.IsConnected,
            connectedStatusText: Loc("common_connected", "Connected"),
            disconnectedStatusText: Loc("common_disconnected", "Disconnected"),
            showInactiveStatusWhenUnselected: false,
            inactiveStatusText: Loc("common_inactive", "Inactive"));

        _pearBrush = state.Foreground;
        _pearStatusText = Settings.Player == PlayerType.Pear
            ? state.StatusText
            : Loc("window_main_status_pear_inactive_unselected", "Inactive (not selected)");
        OnPropertyChanged(nameof(PearBrush));
        OnPropertyChanged(nameof(PearStatusText));
    }

    private void UpdateSpotifyIdleBackoffIndicator()
    {
        void Apply()
        {
            if (BtnSpotifyIdleBackoff == null || TbSpotifyIdleBackoff == null)
                return;

            bool show = Settings.Player == PlayerType.Spotify && AppFetchService.IsSpotifyIdleBackoffActive;
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

        SpotifyPersistentIssue issue = list.FirstOrDefault();

        if (issue == null)
        {
            if (!refreshOnly)
            {
                BrdSpotifyPersistentIssue.Visibility = Visibility.Collapsed;
                TbSpotifyPersistentIssueTitle.Text = "";
                TbSpotifyPersistentIssueBody.Text = "";
                TbSpotifyPersistentIssueEta.Text = "";
                ExpSpotifyPersistentIssues.Header = Loc("menu_more", "More…");
                ExpSpotifyPersistentIssues.IsExpanded = false;
                PnlSpotifyPersistentIssues.Children.Clear();
            }
            return;
        }

        BrdSpotifyPersistentIssue.Visibility = Visibility.Visible;

        if (!refreshOnly)
        {
            TbSpotifyPersistentIssueTitle.Text = issue.Title
                ?? Loc("window_main_spotify_issue_fallback", "Spotify issue");
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
                    ? LocFormat("common_duration_hours", "{0} hour(s)", (int)Math.Ceiling(remaining.TotalHours))
                    : remaining.TotalMinutes >= 1
                        ? LocFormat("common_duration_minutes", "{0} minute(s)", (int)Math.Ceiling(remaining.TotalMinutes))
                        : LocFormat("common_duration_seconds", "{0} second(s)", Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds)));

                TbSpotifyPersistentIssueEta.Text = LocFormat(
                    "window_main_spotify_issue_eta",
                    "Estimated: works again in about {0} (around {1}).",
                    human, localTime);
            }
            else
            {
                TbSpotifyPersistentIssueEta.Text = LocFormat(
                    "window_main_spotify_issue_eta_ready",
                    "Estimated cooldown ended (around {0}). If it still fails, it may take a bit longer.",
                    localTime);
            }
        }
        else
        {
            string whenLocal = DateTime.SpecifyKind(issue.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime().ToString("g");
            TbSpotifyPersistentIssueEta.Text = LocFormat("window_main_spotify_issue_seen_at", "Seen at {0}.", whenLocal);
        }

        if (!refreshOnly)
        {
            int moreCount = Math.Max(0, list.Count - 1);
            ExpSpotifyPersistentIssues.Header = moreCount > 0
                ? LocFormat("window_main_more_count", "More… ({0})", moreCount)
                : Loc("menu_more", "More…");
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
                    ToolTip = Loc("common_dismiss", "Dismiss"),
                    Tag = it.Id
                };
                dismiss.Click += BtnSpotifyPersistentIssueItemDismiss_Click;
                DockPanel.SetDock(dismiss, Dock.Right);
                row.Children.Add(dismiss);

                string whenLocal = DateTime.SpecifyKind(it.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime().ToString("g");
                string text = $"{it.Title ?? Loc("window_main_spotify_issue_fallback", "Spotify issue")} — {whenLocal}";
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
            DateTime nowUtc = DateTime.UtcNow;
            SpotifyPersistentIssue current = Settings.SpotifyPersistentIssues?
                .FirstOrDefault(x => x != null && !x.IsStale(nowUtc));
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

    private void MenuQueueWindow_OnClick(object sender, RoutedEventArgs e) => QueueWindow.ShowOrActivate();

    private void MenuConsoleWindow_OnClick(object sender, RoutedEventArgs e) => ConsoleWindow.ShowOrActivate();

    private void MenuWebServerUrl_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenWebServerUrl();

    private void MenuQueueBrowser_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenQueueInBrowser();

    private void MenuTwitchLogin_OnClick(object sender, RoutedEventArgs e) => AppActions.TwitchLoginMain();

    private async void MenuTwitchConnect_OnClick(object sender, RoutedEventArgs e) =>
        await AppActions.TwitchConnectAsync();

    private async void MenuTwitchOnline_OnClick(object sender, RoutedEventArgs e)
    {
        bool isLive = await AppActions.CheckTwitchOnlineStatusAsync();
        string header =
            $"{Properties.Resources.menu_twitch_check_online_status} ({(isLive ? "Live" : "Offline")})";
        if (MiTwitchCheckOnlineStatus != null)
            MiTwitchCheckOnlineStatus.Header = header;
        SetTwitchLiveMenuHeader(BtnStatusTwitchApi?.ContextMenu, header);
        SetTwitchLiveMenuHeader(BtnStatusTwitchBot?.ContextMenu, header);
    }

    private static void SetTwitchLiveMenuHeader(System.Windows.Controls.ContextMenu menu, string header)
    {
        if (menu == null)
            return;

        foreach (object item in menu.Items)
        {
            if (item is System.Windows.Controls.MenuItem { Tag: "TwitchLiveCheck" } mi)
                mi.Header = header;
        }
    }

    private void OpenTwitchStatusMenu(FrameworkElement host)
    {
        if (host?.ContextMenu is not { } menu)
            return;

        menu.PlacementTarget = host;
        menu.Placement = PlacementMode.Top;
        // Open after the click finishes so the mouse-up does not immediately close the menu.
        Dispatcher.BeginInvoke(() => menu.IsOpen = true, DispatcherPriority.Input);
    }

    private void MenuPatchNotes_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenPatchNotes();

    private async void MenuSetupWizard_OnClick(object sender, RoutedEventArgs e)
    {
        bool startTour = await GuidedSetup.ShowWizardAsync(this);
        Pages.OverviewPage.RefreshChecklist();
        if (startTour)
            await StartSetupTourAsync();
    }

    private void MenuFaq_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenFaq();

    private void MenuGitHub_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenGitHubIssues();

    private void MenuDiscord_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenDiscord();

    private void MenuLogFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenLogFolder();

    private void MenuAppFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenAppFolder();

    private void MenuCheckUpdates_OnClick(object sender, RoutedEventArgs e) => AppActions.CheckForUpdates();

    private void TitleBarNowPlaying_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        => NavigateToQueue();

    private void StartTitleBarNowPlayingTimer()
    {
        _nowPlayingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _nowPlayingTimer.Tick += (_, _) => UpdateTitleBarNowPlaying();
        _nowPlayingTimer.Start();
        UpdateTitleBarNowPlaying();
    }

    private void UpdateTitleBarNowPlaying()
    {
        if (TxtTitleBarNowPlaying == null)
            return;

        TrackInfo current = GlobalObjects.CurrentSong;
        bool hasSong = current != null &&
                       (!string.IsNullOrWhiteSpace(current.Title) || !string.IsNullOrWhiteSpace(current.Artists));

        if (!hasSong)
        {
            TxtTitleBarNowPlaying.Text = "";
            if (TxtTitleBarUpNext != null)
                TxtTitleBarUpNext.Text = "";
            return;
        }

        string artist = current.Artists ?? "";
        string title = current.Title ?? "";
        TxtTitleBarNowPlaying.Text = string.IsNullOrWhiteSpace(artist)
            ? title
            : $"{artist} — {title}";

        if (TxtTitleBarUpNext == null)
            return;

        string currentId = current.SongId;
        RequestObject next = GlobalObjects.QueueTracks?
            .FirstOrDefault(t => t != null
                                 && t.Played != -1
                                 && (string.IsNullOrEmpty(currentId) ||
                                     !string.Equals(t.Trackid, currentId, StringComparison.Ordinal)));

        if (next == null)
        {
            TxtTitleBarUpNext.Text = "";
            return;
        }

        string nextLine = string.IsNullOrWhiteSpace(next.Artist)
            ? next.Title
            : $"{next.Artist} — {next.Title}";
        string fmt = TryFindResource("window_titlebar_up_next") as string ?? "Up next: {0}";
        TxtTitleBarUpNext.Text = string.Format(fmt, nextLine);
    }

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

    private void OnSongifyPremiumStatusChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(ApplySongifyPremiumStatus);
            return;
        }

        ApplySongifyPremiumStatus();
    }

    private void ApplySongifyPremiumStatus()
    {
        UpdateWindowTitle();
        if (SongifyPremiumService.IsActive)
            HidePremiumReminder();
    }

    private void UpdateWindowTitle()
    {
        string title = SongifyPremiumService.IsActive
            ? Loc("window_main_title_premium", "Songify Premium")
            : "Songify";
        Title = title;
        if (TxtTitleBarAppName != null)
            TxtTitleBarAppName.Text = title;
    }

    private async void ServiceToolTipOpening(object sender, ToolTipEventArgs e)
    {
        try
        {
            await ShowServiceToolTip(sender);
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private async Task ShowServiceToolTip(object sender)
    {
        if (sender is not FrameworkElement { Tag: string tag } host)
            return;

        Style style = TryFindResource("StatusToolTip") as Style;
        SymbolIcon icon = new() { Width = 14, Height = 14 };
        string header;
        List<(string Label, string Value)> rows;

        switch (tag)
        {
            case "TwitchBot":
            {
                header = "Twitch Chat Bot";
                icon.Symbol = SymbolRegular.Live24;
                List<EventSubSubscription> subs = await GetEventSubsSafeAsync();
                bool connected = _twitchBotState == ConnectionIndicatorState.Connected ||
                                 subs.Any(sub => sub.Type == "channel.chat.message" && sub.Status == "enabled");
                rows =
                [
                    ("Status", connected ? "Connected" : "Disconnected"),
                    ("Channel", Settings.TwitchUser?.DisplayName ?? "—"),
                    ("Action", Loc("window_main_status_twitch_click_hint", "Click for Login, Connect, and live status")),
                ];
                break;
            }

            case "TwitchAPI":
            {
                header = "Twitch API";
                icon.Symbol = SymbolRegular.Live24;
                List<EventSubSubscription> subs = await GetEventSubsSafeAsync();
                string eventSubs = string.Join("\n",
                    subs.Where(s => s.Status == "enabled").Select(s => s.Type));
                rows =
                [
                    ("Status", _twitchApiState == ConnectionIndicatorState.Connected ? "Connected" : "Disconnected"),
                    ("Channel", Settings.TwitchUser?.DisplayName ?? "—"),
                    ("EventSubs", string.IsNullOrWhiteSpace(eventSubs) ? "—" : eventSubs),
                    ("Action", Loc("window_main_status_twitch_click_hint", "Click for Login, Connect, and live status")),
                ];
                break;
            }

            case "Spotify":
                header = "Spotify";
                icon.Symbol = SymbolRegular.MusicNote224;
                rows = await BuildSpotifyStatusRowsAsync();
                break;

            case "PearDesktop":
                header = "Pear Desktop";
                icon.Symbol = SymbolRegular.PlayCircle24;
                rows = BuildPearStatusRows();
                break;

            default:
                header = "WebServer";
                icon.Symbol = SymbolRegular.Server24;
                rows =
                [
                    ("Status", _webServerRunning ? "Running" : "Not running"),
                    ("Port", Settings.WebServerPort.ToString()),
                    ("Action", "Click to start/stop")
                ];
                break;
        }

        host.ToolTip = ServiceToolTip.Build(header, rows, style, icon);
    }

    private static async Task<List<EventSubSubscription>> GetEventSubsSafeAsync()
    {
        try
        {
            if (TwitchHandler.TwitchApi == null || string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
                return [];

            return await TwitchApiHelper.GetEventSubscriptions() ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            return [];
        }
    }

    private ServiceIndicatorState GetSpotifyIndicatorState()
    {
        string product = Settings.SpotifyProfile?.Product ?? GlobalObjects.SpotifyProfile?.Product;
        bool isPremium = string.Equals(product, "premium", StringComparison.OrdinalIgnoreCase);
        return new ServiceIndicatorState(
            isSelected: Settings.Player == PlayerType.Spotify,
            isConnecting: false,
            isConnected: SpotifyApiHandler.Client != null,
            connectedStatusText: isPremium ? "Connected (Premium)" : "Connected (Free)");
    }

    private async Task<List<(string Label, string Value)>> BuildSpotifyStatusRowsAsync()
    {
        ServiceIndicatorState indicatorState = GetSpotifyIndicatorState();
        bool hasTokens = !string.IsNullOrWhiteSpace(Settings.SpotifyAccessToken) ||
                         !string.IsNullOrWhiteSpace(Settings.SpotifyRefreshToken);

        string action = !indicatorState.IsSelected
            ? "Click indicator to switch to Spotify and connect"
            : !indicatorState.IsConnected
                ? "Click indicator to connect"
                : "Click indicator to refresh Spotify status";

        string deviceName;
        try
        {
            deviceName = await SpotifyApiHandler.GetDeviceNameForId(Settings.SpotifyDeviceId);
        }
        catch
        {
            deviceName = "Unknown";
        }

        return indicatorState.BuildRows(
            ("Linked", hasTokens ? "Yes" : "No"),
            ("User", Settings.SpotifyProfile?.DisplayName ?? "Unknown"),
            ("Device", string.IsNullOrWhiteSpace(deviceName) ? "Unknown" : deviceName),
            ("Fetch rate", AppFetchService.IsSpotifyIdleBackoffActive
                ? $"{AppFetchService.GetEffectiveSpotifyFetchIntervalSeconds()}s (idle — click status chip to restore)"
                : $"{MathUtils.Clamp(Settings.SpotifyFetchRate, 1, 30)}s"),
            ("Action", action));
    }

    private List<(string Label, string Value)> BuildPearStatusRows()
    {
        ServiceIndicatorState indicatorState = new(
            isSelected: Settings.Player == PlayerType.Pear,
            isConnecting: PearWebSocketClient.IsConnecting,
            isConnected: PearWebSocketClient.IsConnected,
            connectedStatusText: Loc("common_connected", "Connected"),
            disconnectedStatusText: Loc("common_disconnected", "Disconnected"),
            showInactiveStatusWhenUnselected: false,
            inactiveStatusText: Loc("common_inactive", "Inactive"));

        TimeSpan? backoffRemaining = AppFetchService.GetPearConnectBackoffRemaining();

        string action = indicatorState.IsConnecting
            ? "Connecting"
            : indicatorState.IsConnected
                ? "Click indicator to disconnect"
                : Settings.Player == PlayerType.Pear && backoffRemaining is { } remaining
                    ? $"Auto-retry in {Math.Ceiling(remaining.TotalSeconds)}s (backoff active). Click to force check"
                    : Settings.Player == PlayerType.Pear
                        ? "Click indicator to reconnect"
                        : "Click indicator to switch to Pear and connect";

        return indicatorState.BuildRows(
            ("WebSocket", PearWebSocketClient.Endpoint),
            ("HTTP API", YtmDesktopApi.Endpoint),
            ("Action", action));
    }

    private async void ServiceIcon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;

        try
        {
            switch (tag)
            {
                case "TwitchBot":
                case "TwitchAPI":
                    OpenTwitchStatusMenu(sender as FrameworkElement);
                    break;

                case "Spotify":
                    if (Settings.Player != PlayerType.Spotify)
                    {
                        PlayerType previous = Settings.Player;
                        Settings.Player = PlayerType.Spotify;
                        await AppFetchService.ApplyPlayerSourceAsync(previous, PlayerType.Spotify);
                        OnPropertyChanged(nameof(SpotifyBrush));
                        UpdatePearStatusIndicator();
                        break;
                    }

                    if (SpotifyApiHandler.Client == null)
                        await SpotifyApiHandler.Auth();
                    else
                        await AppFetchService.ForceFetchSpotifyAsync();

                    SpotifyApiHandler.RefreshShellSpotifyIndicator();
                    OnPropertyChanged(nameof(SpotifyBrush));
                    break;

                case "PearDesktop":
                    if (Settings.Player != PlayerType.Pear)
                    {
                        PlayerType previous = Settings.Player;
                        Settings.Player = PlayerType.Pear;
                        await AppFetchService.ApplyPlayerSourceAsync(previous, PlayerType.Pear);
                        OnPropertyChanged(nameof(SpotifyBrush));
                        UpdatePearStatusIndicator();
                        break;
                    }

                    if (PearWebSocketClient.IsConnecting || PearWebSocketClient.IsConnected)
                        await AppFetchService.NotifyPearPlayerInactiveAsync();
                    else
                        await AppFetchService.ForceFetchPearAsync();

                    UpdatePearStatusIndicator();
                    break;

                case "WebServer":
                    if (GlobalObjects.WebServer.Run)
                        GlobalObjects.WebServer.StopWebServer();
                    else
                        GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
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
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetCanvas(path));
            return;
        }

        Pages.OverviewPage.NotifyCanvas(path);
    }

    public void StopCanvas()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(StopCanvas);
            return;
        }

        Pages.OverviewPage.NotifyCanvasStopped();
    }

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

    public async Task OpenSettingsTabAsync(string tabTag, string elementName = null)
    {
        RootNavigationView.Navigate(typeof(Pages.SettingsPage));
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Task.Delay(80);
        Pages.SettingsPage page = Pages.SettingsPage.Instance;
        if (page == null)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            page = Pages.SettingsPage.Instance;
        }

        page?.SelectTab(tabTag, elementName);
    }

    /// <summary>
    /// Shows the startup Premium reminder unless the user hid it or Premium is already active.
    /// </summary>
    public Task TryShowPremiumReminderAsync()
    {
        if (!Dispatcher.CheckAccess())
            return Dispatcher.InvokeAsync(TryShowPremiumReminderCore).Task;

        TryShowPremiumReminderCore();
        return Task.CompletedTask;
    }

    private void TryShowPremiumReminderCore()
    {
        if (!IsLoaded || Settings.DonationReminder || SongifyPremiumService.IsActive)
            return;

        if (TourOverlay is { Visibility: Visibility.Visible })
            return;

        ShowPremiumReminder();
    }

    private void ShowPremiumReminder()
    {
        if (GrdPremiumReminder == null || SongifyPremiumService.IsActive)
            return;

        _premiumReminderSecondsLeft = 5;
        UpdatePremiumReminderCountdown();
        GrdPremiumReminder.Visibility = Visibility.Visible;
        BtnPremiumReminderClose.Visibility = Visibility.Visible;

        _premiumReminderTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _premiumReminderTimer.Tick -= PremiumReminderTimerOnTick;
        _premiumReminderTimer.Tick += PremiumReminderTimerOnTick;
        _premiumReminderTimer.Start();
    }

    private void PremiumReminderTimerOnTick(object sender, EventArgs e)
    {
        _premiumReminderSecondsLeft--;
        UpdatePremiumReminderCountdown();
        if (_premiumReminderSecondsLeft > 0)
            return;

        HidePremiumReminder();
    }

    private void UpdatePremiumReminderCountdown()
    {
        if (TbPremiumReminderDismiss == null)
            return;

        if (_premiumReminderSecondsLeft <= 0)
        {
            TbPremiumReminderDismiss.Text = Loc("window_main_premium_reminder_dismiss_now",
                "This message will disappear now :)");
            return;
        }

        TbPremiumReminderDismiss.Text = _premiumReminderSecondsLeft == 1
            ? LocFormat("window_main_premium_reminder_dismiss_one",
                "This message will disappear in {0} second", _premiumReminderSecondsLeft)
            : LocFormat("window_main_premium_reminder_dismiss",
                "This message will disappear in {0} seconds", _premiumReminderSecondsLeft);
    }

    private void HidePremiumReminder()
    {
        if (_premiumReminderTimer != null)
        {
            _premiumReminderTimer.Stop();
            _premiumReminderTimer.Tick -= PremiumReminderTimerOnTick;
        }

        if (TbPremiumReminderDismiss != null)
            TbPremiumReminderDismiss.Text = "";

        if (GrdPremiumReminder != null)
            GrdPremiumReminder.Visibility = Visibility.Collapsed;
    }

    private void BtnPremiumReminderClose_OnClick(object sender, RoutedEventArgs e) => HidePremiumReminder();

    private void BtnPremiumReminderCta_OnClick(object sender, RoutedEventArgs e)
    {
        HidePremiumReminder();
        AccountLinking.OpenPremium();
    }

    public async Task StartSetupTourAsync()
    {
        _tourStep = 0;
        if (TourOverlay != null)
            TourOverlay.Visibility = Visibility.Visible;
        await ShowTourStepAsync();
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
        if (ConsoleWindow.IsOpen)
        {
            ConsoleWindow.ShowOrActivate();
            return;
        }

        RootNavigationView.Navigate(typeof(Pages.ConsolePage));
    }

    private void TitleBar_OnCloseClicked(TitleBar sender, RoutedEventArgs args)
    {
        Settings.PosX = Left;
        Settings.PosY = Top;
        // Closing event handles Systray cancel / force close
    }

    #region PSAs / notifications

    private async void BtnPsa_OnClick(object sender, RoutedEventArgs e)
    {
#if DEBUG
        // Title bar steals right-click for the system menu; Ctrl+Click simulates PSAs instead.
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            PsaManager.SimulateDebugNotifications();
            FlyPsa.Visibility = Visibility.Visible;
            ApplyOpaqueFlyPsaBackground();
            return;
        }
#endif
        // Closing must not refresh — a refresh replaces local/debug items with the API result.
        if (FlyPsa.Visibility == Visibility.Visible)
        {
            FlyPsa.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            await PsaManager.RefreshAsync();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }

        FlyPsa.Visibility = Visibility.Visible;
        ApplyOpaqueFlyPsaBackground();
    }

    private void BtnPsaClose_OnClick(object sender, RoutedEventArgs e)
    {
        FlyPsa.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Mica / layer brushes are translucent; force a fully opaque panel fill from theme colors.
    /// </summary>
    private void ApplyOpaqueFlyPsaBackground()
    {
        if (FlyPsa == null)
            return;

        FlyPsa.Background = ThemeBrushes.CreateOpaqueSurfaceBrush();
    }

    private void BtnPsaMarkAllRead_OnClick(object sender, RoutedEventArgs e)
    {
        PsaManager.MarkAllAsRead();
    }

#if DEBUG
    private void SetupDebugPsaMenu()
    {
        if (BtnPsa != null)
            BtnPsa.ToolTip = "Notifications (Debug: Ctrl+Click to simulate PSAs)";

        if (HelpContextMenu == null)
            return;

        HelpContextMenu.Items.Add(new Separator());

        System.Windows.Controls.MenuItem debugRoot = new() { Header = "Debug" };
        System.Windows.Controls.MenuItem simulate = new() { Header = "Simulate PSAs (High / Medium / Low)" };
        simulate.Click += (_, _) =>
        {
            PsaManager.SimulateDebugNotifications();
            FlyPsa.Visibility = Visibility.Visible;
            ApplyOpaqueFlyPsaBackground();
        };
        System.Windows.Controls.MenuItem clear = new() { Header = "Clear simulated PSAs" };
        clear.Click += (_, _) => PsaManager.ClearDebugNotifications();
        debugRoot.Items.Add(simulate);
        debugRoot.Items.Add(clear);
        HelpContextMenu.Items.Add(debugRoot);
    }
#endif

    private void OnPsaChanged()
    {
        UpdatePsaBadge();
        UpdatePsaMarkAllReadVisibility();
        foreach (UIElement child in PnlPsas.Children)
        {
            if (child is PsaControl control)
                control.ApplyReadState();
        }
    }

    private void OnPsaListUpdated()
    {
        RebuildPsaPanel();
        UpdatePsaBadge();
        UpdatePsaMarkAllReadVisibility();
    }

    private void RebuildPsaPanel()
    {
        if (PnlPsas == null)
            return;

        PnlPsas.Children.Clear();
        foreach (Psa psa in PsaManager.Current)
            PnlPsas.Children.Add(new PsaControl(psa));
    }

    private void UpdatePsaBadge()
    {
        if (PsaBadgeIcon == null || PsaBadgePill == null || PsaBadgeText == null)
            return;

        bool hasAny = PsaManager.HasAny();
        PsaBadgeIcon.Filled = hasAny;

        int unread = PsaManager.GetUnreadCount();
        if (unread > 0)
        {
            PsaBadgeText.Text = unread.ToString();
            PsaBadgePill.Background = PsaManager.GetSeverityBadgeBrush();
            PsaBadgePill.Visibility = Visibility.Visible;
        }
        else
        {
            PsaBadgeText.Text = string.Empty;
            PsaBadgePill.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdatePsaMarkAllReadVisibility()
    {
        if (BtnPsaMarkAllRead == null)
            return;

        BtnPsaMarkAllRead.Visibility = PsaManager.HasUnread()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    #endregion

    #region Setup tour

    private async void BtnTourBack_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tourStep <= 0)
            return;
        _tourStep--;
        await ShowTourStepAsync();
    }

    private async void BtnTourNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tourStep >= 3)
        {
            EndSetupTour();
            return;
        }

        _tourStep++;
        await ShowTourStepAsync();
    }

    private void TourOverlay_OnSkip(object sender, RoutedEventArgs e) => EndSetupTour();

    private void EndSetupTour()
    {
        if (TourOverlay != null)
            TourOverlay.Visibility = Visibility.Collapsed;
        if (TourHighlight != null)
            TourHighlight.Visibility = Visibility.Collapsed;
        RootNavigationView?.Navigate(typeof(Pages.OverviewPage));
        Pages.OverviewPage.RefreshChecklist();
    }

    private async Task ShowTourStepAsync()
    {
        if (BtnTourBack != null)
            BtnTourBack.Visibility = _tourStep > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (BtnTourNext != null)
            BtnTourNext.Content = _tourStep >= 3
                ? Loc("setup_finish", "Finish")
                : Loc("setup_next", "Next");

        FrameworkElement highlight = null;
        switch (_tourStep)
        {
            case 0:
                RootNavigationView.Navigate(typeof(Pages.OverviewPage));
                TxtTourTitle.Text = Loc("setup_tour_home_title", "Home");
                TxtTourBody.Text = Loc("setup_tour_home_body",
                    "This is the overview. Use the player dropdown to choose Spotify or another source. Now playing and the next requested songs show up here.");
                await WaitForLayoutAsync();
                highlight = Pages.OverviewPage.Instance?.PlayerCombo;
                break;
            case 1:
                await OpenSettingsTabAsync("Spotify");
                TxtTourTitle.Text = Loc("setup_tour_spotify_title", "Spotify settings");
                TxtTourBody.Text = Loc("setup_tour_spotify_body",
                    "Settings → Music → Spotify is where you enter your Client ID and link your account. You can change this any time.");
                break;
            case 2:
                await OpenSettingsTabAsync("Twitch");
                TxtTourTitle.Text = Loc("setup_tour_twitch_title", "Twitch accounts");
                TxtTourBody.Text = Loc("setup_tour_twitch_body",
                    "Link your broadcaster account here for song requests, rewards, and chat commands. A bot account is optional.");
                break;
            default:
                await OpenSettingsTabAsync("Output");
                TxtTourTitle.Text = Loc("setup_tour_output_title", "Output and widget");
                TxtTourBody.Text = Loc("setup_tour_output_body",
                    "Point OBS at the Songify.txt file in Output settings. Tools → Widget opens the browser overlay. That's the tour — you're set.");
                break;
        }

        PlaceTourHighlight(highlight);
    }

    private async Task WaitForLayoutAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Task.Delay(80);
    }

    private void PlaceTourHighlight(FrameworkElement target)
    {
        if (TourHighlight == null)
            return;

        if (target == null || !target.IsVisible || target.ActualWidth < 1)
        {
            TourHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            Point topLeft = target.TransformToVisual(TourOverlay).Transform(new Point(0, 0));
            TourHighlight.Width = target.ActualWidth + 8;
            TourHighlight.Height = target.ActualHeight + 8;
            TourHighlight.Margin = new Thickness(topLeft.X - 4, topLeft.Y - 4, 0, 0);
            TourHighlight.HorizontalAlignment = HorizontalAlignment.Left;
            TourHighlight.VerticalAlignment = VerticalAlignment.Top;
            TourHighlight.Visibility = Visibility.Visible;
        }
        catch
        {
            TourHighlight.Visibility = Visibility.Collapsed;
        }
    }

    #endregion

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}