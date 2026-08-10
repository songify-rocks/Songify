using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace Songify_Slim.Views.WPFUI;

public partial class ShellWindow : IAppShell, INotifyPropertyChanged
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContext = this;
        // Dark theme with Mica (app-wide WPF-UI is already Dark from App.xaml; ensure Mica for this window)
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
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

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AppShellBridge.Register(this);
        Title = "Songify";
        if (App.IsBeta)
            Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"));
        else
            Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"));

        // Restore position
        if (Settings.PosX != 0 || Settings.PosY != 0)
        {
            Left = Settings.PosX;
            Top = Settings.PosY;
        }

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

    private void ShellWindow_Closing(object sender, CancelEventArgs e)
    {
        AppShellBridge.Unregister(this);
        Settings.PosX = Left;
        Settings.PosY = Top;
        Util.Songify.AppFetchService.Stop();
    }

    #region IAppShell (no-op or fallback when Shell is main window)

    public Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
    {
        MessageBoxButton buttons = style == MessageDialogStyle.AffirmativeAndNegative || style == MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary
            ? MessageBoxButton.YesNo
            : MessageBoxButton.OK;
        MessageBoxResult result = MessageBox.Show(message, title ?? "Songify", buttons, MessageBoxImage.None);
        return Task.FromResult(result == MessageBoxResult.Yes || result == MessageBoxResult.OK ? MessageDialogResult.Affirmative : MessageDialogResult.Negative);
    }

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
        var settings = new Window_Settings { Owner = this };
        settings.ShowDialog();
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

    private WindowConsole _consoleWindow;

    public void OpenConsole()
    {
        _consoleWindow ??= new WindowConsole
        {
            Owner = this,
            Left = Left + Width,
            Top = Top
        };
        if (_consoleWindow.IsVisible)
            _consoleWindow.Hide();
        else
            _consoleWindow.Show();
    }

    private void TitleBar_OnCloseClicked(TitleBar sender, RoutedEventArgs args)
    {
        Settings.PosX = Left;
        Settings.PosY = Top;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}