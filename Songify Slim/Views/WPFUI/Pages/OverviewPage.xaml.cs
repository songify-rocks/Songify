using System.Diagnostics;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class OverviewPage : Page
{
    private System.Windows.Threading.DispatcherTimer _updateTimer;
    private string _lastCoverUrl;
    private bool _playerDropdownInitialized;

    public OverviewPage()
    {
        InitializeComponent();
        Loaded += OverviewPage_Loaded;
        Unloaded += OverviewPage_Unloaded;
    }

    private void OverviewPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Bind to ShellWindow so Overview can show connection indicators.
        if (Application.Current.MainWindow is ShellWindow shell)
            DataContext = shell;

        EnsurePlayerDropdown();

        BtnSettings.Content = Properties.Resources.menu_file_settings;
        BtnTwitchConnect.Content = Properties.Resources.menu_twitch_connect;
        BtnSupport.Content = Properties.Resources.cta_support;
        UpdateNowPlaying();
        _updateTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += (_, __) => UpdateNowPlaying();
        _updateTimer.Start();
    }

    private void OverviewPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
    }

    private void EnsurePlayerDropdown()
    {
        if (_playerDropdownInitialized || CbxPlayer == null)
            return;

        var items = Enum.GetValues(typeof(PlayerType))
            .Cast<PlayerType>()
            .Select(p => new
            {
                Value = p,
                Name = EnumHelper.GetDescription(p)
            })
            .ToList();

        CbxPlayer.ItemsSource = items;
        CbxPlayer.DisplayMemberPath = "Name";
        CbxPlayer.SelectedValuePath = "Value";
        CbxPlayer.SelectedValue = (PlayerType)Settings.Player;
        _playerDropdownInitialized = true;
    }

    private void CbxPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_playerDropdownInitialized || !IsLoaded)
            return;

        if (CbxPlayer?.SelectedValue is not PlayerType selected)
            return;

        if (Settings.Player == selected)
            return;

        Settings.Player = selected;

        // Apply new fetch interval/source immediately.
        Util.Songify.AppFetchService.Stop();
        Util.Songify.AppFetchService.Start();

        // Force refresh visuals (cover might change source semantics).
        _lastCoverUrl = null;
        UpdateNowPlaying();
    }

    private void UpdateNowPlaying()
    {
        if (TxtNowPlaying == null) return;

        TrackInfo current = GlobalObjects.CurrentSong;
        if (current != null)
        {
            TxtNowPlaying.Text = string.IsNullOrEmpty(current.Title) ? "—" : current.Title;
            TxtArtist.Text = current.Artists ?? "";

            // Album cover: null-safe (Albums can be null or empty; Image has Url)
            string coverUrl = null;
            if (current.Albums != null && current.Albums.Count > 0)
            {
                var img = current.Albums.FirstOrDefault();
                if (img != null && !string.IsNullOrEmpty(img.Url))
                    coverUrl = img.Url;
            }
            if (ImgCover != null)
            {
                if (!string.IsNullOrEmpty(coverUrl) && coverUrl != _lastCoverUrl)
                {
                    try
                    {
                        ImgCover.Source = new BitmapImage(new System.Uri(coverUrl));
                        _lastCoverUrl = coverUrl;
                        ImgCover.Visibility = Visibility.Visible;
                        if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        ImgCover.Visibility = Visibility.Collapsed;
                        if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Visible;
                    }
                }
                else if (string.IsNullOrEmpty(coverUrl))
                {
                    ImgCover.Visibility = Visibility.Collapsed;
                    if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Visible;
                }
            }

            // Playing state
            if (PlayingBadge != null && TxtPlayState != null)
            {
                PlayingBadge.Visibility = Visibility.Visible;
                TxtPlayState.Text = current.IsPlaying ? "Playing" : "Paused";
            }
            if (TxtDuration != null && current.DurationMs > 0)
            {
                int sec = current.DurationMs / 1000;
                int min = sec / 60;
                sec %= 60;
                TxtDuration.Text = $"{min}:{sec:D2}";
            }
            else if (TxtDuration != null)
                TxtDuration.Text = "";
        }
        else
        {
            TxtNowPlaying.Text = "Nothing playing";
            TxtArtist.Text = "";
            if (ImgCover != null) ImgCover.Visibility = Visibility.Collapsed;
            if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Visible;
            if (PlayingBadge != null) PlayingBadge.Visibility = Visibility.Collapsed;
            if (TxtDuration != null) TxtDuration.Text = "";
            _lastCoverUrl = null;
        }
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is ShellWindow shell)
            shell.OpenSettings();
        else
            new Window_Settings { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    private void BtnQueue_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is ShellWindow shell)
            shell.NavigateToQueue();
    }

    private void BtnTwitchConnect_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is ShellWindow shell)
            shell.ConnectTwitch();
    }

    private void BtnSupport_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://ko-fi.com/songify") { UseShellExecute = true });
    }
}