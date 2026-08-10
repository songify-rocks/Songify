using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;

namespace Songify_Slim.Views.WPFUI;

public partial class ShellWindow : IAppShell, INotifyPropertyChanged
{
    private bool _forceClose;

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

    #region TitleBar menu

    private void MenuWidget_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenWidget();
    private void MenuWebServerUrl_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenWebServerUrl();
    private void MenuQueueBrowser_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenQueueInBrowser();
    private void MenuHistoryBrowser_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenHistoryInBrowser();
    private void MenuTwitchLogin_OnClick(object sender, RoutedEventArgs e) => AppActions.TwitchLoginMain();
    private void MenuTwitchConnect_OnClick(object sender, RoutedEventArgs e) => AppActions.TwitchConnect();
    private async void MenuTwitchOnline_OnClick(object sender, RoutedEventArgs e) => await AppActions.CheckTwitchOnlineStatusAsync();
    private void MenuPatchNotes_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenPatchNotes();
    private void MenuFaq_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenFaq();
    private void MenuGitHub_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenGitHubIssues();
    private void MenuDiscord_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenDiscord();
    private void MenuLogFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenLogFolder();
    private void MenuAppFolder_OnClick(object sender, RoutedEventArgs e) => AppActions.OpenAppFolder();
    private void MenuCheckUpdates_OnClick(object sender, RoutedEventArgs e) => AppActions.CheckForUpdates();
    private void MenuExit_OnClick(object sender, RoutedEventArgs e) => AppActions.ExitApplication();

    #endregion TitleBar menu

    #region Tray

    private void TrayIcon_OnLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e) => RestoreFromTray();
    private void TrayShow_OnClick(object sender, RoutedEventArgs e) => RestoreFromTray();
    private void TrayConnect_OnClick(object sender, RoutedEventArgs e) => AppActions.TwitchConnect();
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
        // Shell has no status strip; could bind a property to status bar later
    }

    public void SetTwitchApiState(ConnectionIndicatorState state)
    {
        _twitchApiState = state;
        OnPropertyChanged(nameof(TwitchApiBrush));
    }

    public void SetTwitchBotState(ConnectionIndicatorState state)
    {
        _twitchBotState = state;
        OnPropertyChanged(nameof(TwitchBotBrush));
    }

    public void SetWebServerRunning(bool running)
    {
        _webServerRunning = running;
        OnPropertyChanged(nameof(WebServerBrush));
    }

    public void SetSpotifyState(SpotifyIndicatorState state)
    {
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
        catch (System.Exception ex)
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
