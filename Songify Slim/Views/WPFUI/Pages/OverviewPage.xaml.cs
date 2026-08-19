using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;
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
using Songify_Slim.Views.WPFUI;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class OverviewPage : Page
{
    internal ComboBox PlayerCombo => CbxPlayer;

    internal static OverviewPage Instance { get; private set; }

    private static string _pendingCanvasPath;

    private DispatcherTimer _updateTimer;
    private string _lastCoverUrl;
    private bool _playerDropdownInitialized;
    private string _lastUpNextFingerprint;
    private bool _canvasPlaying;
    private bool _stoppingCanvas;

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

        public string Requester { get; init; }
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
        Instance = this;
        EnsurePlayerDropdown();
        SettingsUi.Refreshed += OnSettingsRefreshed;
        IsVisibleChanged += OverviewPage_IsVisibleChanged;

        if (BtnSupport != null)
            BtnSupport.Content = Properties.Resources.cta_support;
        UpdateNowPlaying();
        ApplyPendingCanvas();
        UpdateChecklist();
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
        SettingsUi.Refreshed -= OnSettingsRefreshed;
        IsVisibleChanged -= OverviewPage_IsVisibleChanged;
        _updateTimer?.Stop();
        _canvasPlaying = false;
        StopCanvasPlayback();
        if (Instance == this)
            Instance = null;
    }

    public static void RefreshChecklist()
    {
        if (Instance == null)
            return;
        if (!Instance.Dispatcher.CheckAccess())
        {
            Instance.Dispatcher.Invoke(Instance.UpdateChecklist);
            return;
        }

        Instance.UpdateChecklist();
    }

    private void OnSettingsRefreshed() => RefreshChecklist();

    private void OverviewPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            UpdateChecklist();
    }

    public static void NotifyCanvas(string path)
    {
        bool available = !string.IsNullOrWhiteSpace(path) && File.Exists(path) && Settings.DownloadCanvas;
        _pendingCanvasPath = available ? path : null;
        Instance?.ApplyPendingCanvas();
    }

    public static void NotifyCanvasStopped()
    {
        _pendingCanvasPath = null;
        Instance?.StopCanvasVisual();
    }

    private void ApplyPendingCanvas()
    {
        if (!Settings.DownloadCanvas)
        {
            StopCanvasVisual();
            return;
        }

        string path = _pendingCanvasPath;
        if (string.IsNullOrEmpty(path) && GlobalObjects.Canvas is { Item1: true })
            path = Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4");

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            StopCanvasVisual();
            return;
        }

        _pendingCanvasPath = path;
        PlayCanvas(path);
    }

    private void PlayCanvas(string path)
    {
        if (CanvasPlayer == null || CanvasHost == null)
            return;

        try
        {
            CanvasPlayer.Stop();
            CanvasPlayer.Source = null;
            CanvasPlayer.Volume = 0;
            CanvasPlayer.IsMuted = true;
            CanvasPlayer.Source = new Uri(path, UriKind.Absolute);
            CanvasPlayer.Position = TimeSpan.Zero;
            CanvasPlayer.Play();
            _canvasPlaying = true;
            ShowCanvasOverlay();
        }
        catch
        {
            StopCanvasVisual();
        }
    }

    private void StopCanvasVisual()
    {
        if (_stoppingCanvas)
            return;

        _stoppingCanvas = true;
        try
        {
            _canvasPlaying = false;
            StopCanvasPlayback();
            if (CanvasHost != null)
                CanvasHost.Visibility = Visibility.Collapsed;
            _lastCoverUrl = null;
            UpdateNowPlaying();
        }
        finally
        {
            _stoppingCanvas = false;
        }
    }

    private void StopCanvasPlayback()
    {
        if (CanvasPlayer == null)
            return;

        try
        {
            CanvasPlayer.Stop();
            CanvasPlayer.Source = null;
        }
        catch
        {
            // Ignore media shutdown errors (file already gone / not opened).
        }
    }

    private void ShowCanvasOverlay()
    {
        if (CanvasHost != null)
            CanvasHost.Visibility = Visibility.Visible;
        if (ImgCover != null)
            ImgCover.Visibility = Visibility.Collapsed;
        if (CoverPlaceholder != null)
            CoverPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void CanvasPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (!_canvasPlaying || CanvasPlayer == null)
            return;

        CanvasPlayer.Position = TimeSpan.Zero;
        CanvasPlayer.Play();
    }

    private void CanvasPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (!_canvasPlaying)
            return;

        StopCanvasVisual();
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
        UpdateChecklist();
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
            if (_canvasPlaying)
            {
                ShowCanvasOverlay();
            }
            else if (ImgCover != null)
            {
                if (CanvasHost != null)
                    CanvasHost.Visibility = Visibility.Collapsed;
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
            TxtNowPlaying.Text = TryFindResource("window_overview_nothing_playing") as string
                                 ?? "Nothing playing";
            TxtArtist.Text = "";
            if (_canvasPlaying)
            {
                ShowCanvasOverlay();
            }
            else
            {
                if (CanvasHost != null)
                    CanvasHost.Visibility = Visibility.Collapsed;
                if (ImgCover != null) ImgCover.Visibility = Visibility.Collapsed;
                if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Visible;
            }
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

            items.Add(new UpNextItem
            {
                Position = $"{i + 1}",
                Title = string.IsNullOrWhiteSpace(t.Title) ? "—" : t.Title,
                Subtitle = subtitle,
                CoverUrl = t.Albumcover,
                Requester = showRequester ? requester : ""
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

    private void BtnDismissChecklist_Click(object sender, RoutedEventArgs e)
    {
        Settings.SetupChecklistDismissed = true;
        UpdateChecklist();
    }

    private void UpdateChecklist()
    {
        if (CardGettingStarted == null || PnlChecklistItems == null)
            return;

        if (!GuidedSetup.ShouldShowChecklist())
        {
            CardGettingStarted.Visibility = Visibility.Collapsed;
            return;
        }

        CardGettingStarted.Visibility = Visibility.Visible;
        PnlChecklistItems.Children.Clear();

        string goLabel = TryFindResource("setup_checklist_go") as string ?? "Go";
        foreach (SetupChecklistItem item in GuidedSetup.GetChecklistItems())
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8), LastChildFill = true };
            var go = new System.Windows.Controls.Button
            {
                Content = goLabel,
                MinWidth = 64,
                Padding = new Thickness(10, 2, 10, 2),
                Tag = item.SettingsTab,
                VerticalAlignment = VerticalAlignment.Center
            };
            go.Click += ChecklistGo_Click;
            DockPanel.SetDock(go, Dock.Right);
            row.Children.Add(go);
            row.Children.Add(new TextBlock
            {
                Text = (item.IsDone ? "✓  " : "○  ") + item.Title,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Opacity = item.IsDone ? 0.7 : 1
            });
            PnlChecklistItems.Children.Add(row);
        }
    }

    private async void ChecklistGo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tab } ||
            Application.Current.MainWindow is not ShellWindow shell)
            return;
        await shell.OpenSettingsTabAsync(tab);
    }
}