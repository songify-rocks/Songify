using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Songify_Slim.Models;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class OverviewPage : Page
{
    private DispatcherTimer _updateTimer;
    private string _lastCoverUrl;
    private bool _playerDropdownInitialized;
    private string _lastUpNextFingerprint;

    // Progress interpolation between fetch polls
    private string _progressSongId;

    private int _anchorProgressMs;
    private int _anchorDurationMs;
    private bool _anchorPlaying;
    private DateTime _anchorUtc = DateTime.MinValue;
    private int _lastPolledProgressMs = int.MinValue;
    private bool _lastPolledPlaying;

    private sealed class UpNextItem
    {
        public string Position { get; init; }
        public string Title { get; init; }
        public string Subtitle { get; init; }
        public string CoverUrl { get; init; }
    }

    public OverviewPage()
    {
        InitializeComponent();
        Loaded += OverviewPage_Loaded;
        Unloaded += OverviewPage_Unloaded;
    }

    private void OverviewPage_Loaded(object sender, RoutedEventArgs e)
    {
        EnsurePlayerDropdown();

        if (BtnSupport != null)
            BtnSupport.Content = Properties.Resources.cta_support;
        UpdateNowPlaying();
        _updateTimer = new DispatcherTimer
        {
            // Smooth progress between Spotify/player fetch polls
            Interval = TimeSpan.FromMilliseconds(250)
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

    private async void CbxPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_playerDropdownInitialized || !IsLoaded)
            return;

        if (CbxPlayer?.SelectedValue is not PlayerType selected)
            return;

        if (Settings.Player == selected)
            return;

        PlayerType previous = Settings.Player;
        Settings.Player = selected;

        // Apply new fetch interval/source immediately (also enables Pear WebSocket auto-connect).
        await Util.Songify.AppFetchService.ApplyPlayerSourceAsync(previous, selected);

        // Force refresh visuals (cover might change source semantics).
        _lastCoverUrl = null;
        _lastUpNextFingerprint = null;
        ResetProgressAnchor();
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
                        ImgCover.Source = new BitmapImage(new Uri(coverUrl));
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

            UpdateProgressUi(current);
        }
        else
        {
            TxtNowPlaying.Text = "Nothing playing";
            TxtArtist.Text = "";
            if (ImgCover != null) ImgCover.Visibility = Visibility.Collapsed;
            if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Visible;
            ClearProgressUi();
            _lastCoverUrl = null;
        }

        UpdateUpNext();
    }

    private void UpdateUpNext()
    {
        if (UpNextList == null || TxtUpNextEmpty == null)
            return;

        string currentId = GlobalObjects.CurrentSong?.SongId;
        List<RequestObject> queue = GlobalObjects.QueueTracks?
            .Where(t => t != null
                        && t.Played != -1
                        && (string.IsNullOrEmpty(currentId) || !string.Equals(t.Trackid, currentId, StringComparison.Ordinal)))
            .Take(3)
            .ToList()
            ?? [];

        string fingerprint = string.Join("|", queue.Select(t => $"{t.Trackid}:{t.Title}:{t.Requester}:{t.Albumcover}"));
        if (fingerprint == _lastUpNextFingerprint)
            return;

        _lastUpNextFingerprint = fingerprint;

        if (queue.Count == 0)
        {
            UpNextList.ItemsSource = null;
            UpNextList.Visibility = Visibility.Collapsed;
            TxtUpNextEmpty.Visibility = Visibility.Visible;
            return;
        }

        List<UpNextItem> items = [];
        for (int i = 0; i < queue.Count; i++)
        {
            RequestObject t = queue[i];
            string requester = t.Requester;
            bool showRequester = !string.IsNullOrWhiteSpace(requester)
                                 && !string.Equals(requester, "Spotify", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(requester, "YouTube", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(requester, "Skipping...", StringComparison.OrdinalIgnoreCase);

            string subtitle = t.Artist ?? "";
            if (showRequester)
                subtitle = string.IsNullOrEmpty(subtitle) ? requester : $"{subtitle} · {requester}";

            items.Add(new UpNextItem
            {
                Position = $"{i + 1}",
                Title = string.IsNullOrWhiteSpace(t.Title) ? "—" : t.Title,
                Subtitle = subtitle,
                CoverUrl = t.Albumcover
            });
        }

        UpNextList.ItemsSource = items;
        UpNextList.Visibility = Visibility.Visible;
        TxtUpNextEmpty.Visibility = Visibility.Collapsed;
    }

    private void UpdateProgressUi(TrackInfo current)
    {
        int durationMs = current.DurationTotal > 0 ? current.DurationTotal : current.DurationMs;
        int polledProgress = Math.Max(0, current.Progress);
        if (durationMs > 0 && polledProgress > durationMs)
            polledProgress = durationMs;

        string songId = current.SongId ?? current.Title ?? "";
        bool songChanged = !string.Equals(songId, _progressSongId, StringComparison.Ordinal);
        bool stateChanged = songChanged
                            || polledProgress != _lastPolledProgressMs
                            || current.IsPlaying != _lastPolledPlaying
                            || durationMs != _anchorDurationMs;

        // Always re-anchor to the latest fetched playback state.
        if (stateChanged)
        {
            _progressSongId = songId;
            _anchorProgressMs = polledProgress;
            _anchorDurationMs = durationMs;
            _anchorPlaying = current.IsPlaying;
            _anchorUtc = DateTime.UtcNow;
            _lastPolledProgressMs = polledProgress;
            _lastPolledPlaying = current.IsPlaying;
        }

        if (_anchorDurationMs <= 0)
        {
            ClearProgressUi();
            return;
        }

        int displayMs = _anchorProgressMs;
        if (_anchorPlaying)
        {
            displayMs = _anchorProgressMs + (int)(DateTime.UtcNow - _anchorUtc).TotalMilliseconds;
            displayMs = Math.Clamp(displayMs, 0, _anchorDurationMs);
        }

        double pct = 100.0 * displayMs / _anchorDurationMs;
        if (TrackProgressBar != null)
        {
            TrackProgressBar.Visibility = Visibility.Visible;
            TrackProgressBar.Value = Math.Clamp(pct, 0, 100);
        }

        if (TxtElapsed != null) TxtElapsed.Text = FormatMs(displayMs);
        if (TxtDuration != null) TxtDuration.Text = FormatMs(_anchorDurationMs);
    }

    private void ClearProgressUi()
    {
        ResetProgressAnchor();
        if (TrackProgressBar != null)
        {
            TrackProgressBar.Value = 0;
            TrackProgressBar.Visibility = Visibility.Collapsed;
        }

        if (TxtElapsed != null) TxtElapsed.Text = "";
        if (TxtDuration != null) TxtDuration.Text = "";
    }

    private void ResetProgressAnchor()
    {
        _progressSongId = null;
        _anchorProgressMs = 0;
        _anchorDurationMs = 0;
        _anchorPlaying = false;
        _anchorUtc = DateTime.MinValue;
        _lastPolledProgressMs = int.MinValue;
        _lastPolledPlaying = false;
    }

    private static string FormatMs(int ms)
    {
        if (ms < 0) ms = 0;
        int totalSec = ms / 1000;
        int min = totalSec / 60;
        int sec = totalSec % 60;
        return $"{min}:{sec:D2}";
    }

    private void UpNextCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        BtnQueue_Click(sender, e);
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
        Process.Start(new ProcessStartInfo("https://ko-fi.com/overcodetv") { UseShellExecute = true });
    }
}