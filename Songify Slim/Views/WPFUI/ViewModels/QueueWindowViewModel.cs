using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Songify_Slim.Models;
using Songify_Slim.Util;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Pear;
using Songify_Slim.Util.Spotify;
using Songify_Slim.ViewModels;
using Swan.Formatters;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Views.WPFUI.ViewModels;

/// <summary>
/// Simple display model for the "now playing" row. Used when binding from either queue (Played == -1) or GlobalObjects.CurrentSong.
/// </summary>
public sealed class NowPlayingDisplay
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Albumcover { get; set; }
    public ImageSource AlbumcoverImageSource { get; set; }
}

/// <summary>
/// ViewModel for the WPF-UI Queue window. Binds to GlobalObjects.QueueTracks and exposes commands.
/// </summary>
public sealed class QueueWindowViewModel : INotifyPropertyChanged
{
    private int _fontSize = 12;
    private bool _playerControlsVisible = true;
    private bool _isPlaying = true;
    private RequestObject _selectedQueueItem;
    private RequestObject _selectedReqListItem;
    private RequestObject _currentQueueItem;
    private NowPlayingDisplay _nowPlayingDisplay;

    public QueueWindowViewModel()
    {
        QueueTracks = GlobalObjects.QueueTracks;
        ReqList = GlobalObjects.ReqList;
        QueueTracks.CollectionChanged += (s, e) => UpdateCurrentQueueItem();
        UpdateCurrentQueueItem();

        ClearQueueCommand = new RelayCommand(async () => await ClearQueueAsync(), () => QueueTracks?.Count > 0);
        SkipCommand = new RelayCommand(async (p) => await SkipAsync(p as RequestObject), p => p is RequestObject req && CanSkip(req));
        AddToFavoritesCommand = new RelayCommand(async (p) => await AddToFavoritesAsync(p as RequestObject), p => p is RequestObject req && CanAddToFavorites(req));
        RemoveFromReqListCommand = new RelayCommand(RemoveFromReqList, p => p is RequestObject || SelectedReqListItem != null);
        FontSizeDownCommand = new RelayCommand(FontSizeDown);
        FontSizeUpCommand = new RelayCommand(FontSizeUp);
        TogglePlayerControlsCommand = new RelayCommand(TogglePlayerControls);
        PlayPauseCommand = new RelayCommand(async () => await PlayPauseAsync());
        BackCommand = new RelayCommand(async () => await BackAsync());
        NextCommand = new RelayCommand(async () => await NextAsync());

        _fontSize = MathUtils.Clamp(Settings.FontsizeQueue, 12, 72);
        _playerControlsVisible = Settings.SpotifyControlVisible;
    }

    public ObservableCollection<RequestObject> QueueTracks { get; }
    public ObservableCollection<RequestObject> ReqList { get; }

    public RequestObject SelectedQueueItem
    {
        get => _selectedQueueItem;
        set { _selectedQueueItem = value; OnPropertyChanged(); }
    }

    public RequestObject SelectedReqListItem
    {
        get => _selectedReqListItem;
        set { _selectedReqListItem = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The queue item currently playing (Played == -1). Used for the fixed "now playing" row at the top of the queue.
    /// </summary>
    public RequestObject CurrentQueueItem
    {
        get => _currentQueueItem;
        private set
        {
            if (_currentQueueItem == value) return;
            _currentQueueItem = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Display model for the "now playing" row. Falls back to GlobalObjects.CurrentSong when no queue item has Played == -1.
    /// </summary>
    public NowPlayingDisplay NowPlayingDisplay
    {
        get => _nowPlayingDisplay;
        private set
        {
            if (_nowPlayingDisplay == value) return;
            _nowPlayingDisplay = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNowPlayingDisplay));
        }
    }

    /// <summary>
    /// True when NowPlayingDisplay is not null. Used for visibility binding without a null-to-visibility converter.
    /// </summary>
    public bool HasNowPlayingDisplay => _nowPlayingDisplay != null;

    public int FontSize
    {
        get => _fontSize;
        set
        {
            int clamped = MathUtils.Clamp(value, 12, 72);
            if (_fontSize == clamped) return;
            _fontSize = clamped;
            Settings.FontsizeQueue = clamped;
            OnPropertyChanged();
        }
    }

    public bool PlayerControlsVisible
    {
        get => _playerControlsVisible;
        set { _playerControlsVisible = value; Settings.SpotifyControlVisible = value; OnPropertyChanged(); }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(); }
    }

    // Column visibility (bound to CheckBoxes; we keep same indices 0..5)
    public bool ColQueueIdVisible { get; set; } = true;

    public bool ColArtistVisible { get; set; } = true;
    public bool ColTitleVisible { get; set; } = true;
    public bool ColLengthVisible { get; set; } = true;
    public bool ColRequesterVisible { get; set; } = true;
    public bool ColActionsVisible { get; set; } = true;

    public ICommand ClearQueueCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand AddToFavoritesCommand { get; }
    public ICommand RemoveFromReqListCommand { get; }
    public ICommand FontSizeDownCommand { get; }
    public ICommand FontSizeUpCommand { get; }
    public ICommand TogglePlayerControlsCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand NextCommand { get; }

    public void LoadColumnVisibility()
    {
        var cols = Settings.QueueWindowColumns ?? new List<int> { 0, 1, 2, 3, 4 };
        ColQueueIdVisible = cols.Contains(0);
        ColArtistVisible = cols.Contains(1);
        ColTitleVisible = cols.Contains(2);
        ColLengthVisible = cols.Contains(3);
        ColRequesterVisible = cols.Contains(4);
        ColActionsVisible = cols.Contains(5);
        OnPropertyChanged(nameof(ColQueueIdVisible));
        OnPropertyChanged(nameof(ColArtistVisible));
        OnPropertyChanged(nameof(ColTitleVisible));
        OnPropertyChanged(nameof(ColLengthVisible));
        OnPropertyChanged(nameof(ColRequesterVisible));
        OnPropertyChanged(nameof(ColActionsVisible));
    }

    public void SaveColumnVisibility()
    {
        var list = new List<int>();
        if (ColQueueIdVisible) list.Add(0);
        if (ColArtistVisible) list.Add(1);
        if (ColTitleVisible) list.Add(2);
        if (ColLengthVisible) list.Add(3);
        if (ColRequesterVisible) list.Add(4);
        if (ColActionsVisible) list.Add(5);
        Settings.QueueWindowColumns = list;
    }

    private static bool CanSkip(RequestObject req)
    {
        if (req == null) return false;
        return string.Equals(req.PlayerType, "Spotify", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(req.Requester, "Skipping...", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanAddToFavorites(RequestObject req)
    {
        if (req == null) return false;
        return string.Equals(req.PlayerType, "Spotify", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(req.Requester, "Skipping...", StringComparison.OrdinalIgnoreCase);
    }

    private void FontSizeDown()
    {
        FontSize = _fontSize - 2;
    }

    private void FontSizeUp()
    {
        FontSize = _fontSize + 2;
    }

    private void TogglePlayerControls()
    {
        PlayerControlsVisible = !PlayerControlsVisible;
    }

    private async Task ClearQueueAsync()
    {
        var result = MessageBox.Show(
            Application.Current.MainWindow,
            Songify_Slim.Properties.Resources.mw_clearQueueDisclaimer,
            Songify_Slim.Properties.Resources.s_Warning,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        GlobalObjects.ReqList.Clear();
        var payload = new { uuid = Settings.Uuid, key = Settings.AccessKey };
        await SongifyApi.ClearQueueAsync(Json.Serialize(payload));
        await GlobalObjects.QueueUpdateQueueWindow();
    }

    private async Task SkipAsync(RequestObject req)
    {
        if (req == null) return;
        if (GlobalObjects.CurrentSong != null && req.Trackid == GlobalObjects.CurrentSong.SongId)
        {
            await SpotifyApiHandler.SkipSong();
            return;
        }
        var payload = new { uuid = Settings.Uuid, key = Settings.AccessKey, queueid = req.Queueid };
        await SongifyApi.PatchQueueAsync(Json.Serialize(payload));
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            GlobalObjects.ReqList.Remove(req);
            GlobalObjects.SkipList.Add(req);
        });
        await GlobalObjects.QueueUpdateQueueWindow();
    }

    private async Task AddToFavoritesAsync(RequestObject req)
    {
        if (req == null) return;
        try
        {
            await SpotifyApiHandler.AddToPlaylist(req.Trackid);
            await GlobalObjects.QueueUpdateQueueWindow();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void RemoveFromReqList(object parameter)
    {
        var item = parameter as RequestObject ?? SelectedReqListItem;
        if (item == null) return;
        GlobalObjects.ReqList.Remove(item);
    }

    private async Task PlayPauseAsync()
    {
        switch (Settings.Player)
        {
            case PlayerType.Spotify:
                bool isPlaying = await SpotifyApiHandler.PlayPause();
                IsPlaying = !isPlaying;
                break;

            case PlayerType.Pear:
                var nowPlaying = await PearApi.GetNowPlayingAsync();
                bool np = !nowPlaying.IsPaused;
                if (np) await PearApi.Pause();
                else await PearApi.Play();
                IsPlaying = np;
                break;

            default:
                break;
        }
    }

    private async Task BackAsync()
    {
        // Same logic as original: double-click within 3s = previous track, else restart current
        // Simplified: just restart/previous based on player
        switch (Settings.Player)
        {
            case PlayerType.Spotify:
                await SpotifyApiHandler.PlayFromStart();
                break;

            case PlayerType.Pear:
                await PearApi.SeekTo(0);
                break;

            default:
                break;
        }
    }

    private async Task NextAsync()
    {
        switch (Settings.Player)
        {
            case PlayerType.Spotify:
                await SpotifyApiHandler.SkipSong();
                break;

            case PlayerType.Pear:
                await PearApi.Next();
                break;

            default:
                break;
        }
    }

    public void RefreshPlayPauseState()
    {
        if (GlobalObjects.CurrentSong != null)
            IsPlaying = GlobalObjects.CurrentSong.IsPlaying;
        UpdateCurrentQueueItem();
    }

    private void UpdateCurrentQueueItem()
    {
        CurrentQueueItem = QueueTracks?.FirstOrDefault(x => x.Played == -1);
        UpdateNowPlayingDisplay();
    }

    private void UpdateNowPlayingDisplay()
    {
        var fromQueue = CurrentQueueItem;
        if (fromQueue != null)
        {
            NowPlayingDisplay = new NowPlayingDisplay
            {
                Title = fromQueue.Title ?? "",
                Artist = fromQueue.Artist ?? "",
                Albumcover = fromQueue.Albumcover,
                AlbumcoverImageSource = fromQueue.AlbumcoverImageSource ?? UrlToImageSourceConverter.FromUrl(fromQueue.Albumcover)
            };
            return;
        }
        var current = GlobalObjects.CurrentSong;
        if (current != null && (!string.IsNullOrWhiteSpace(current.Title) || !string.IsNullOrWhiteSpace(current.Artists)))
        {
            string coverUrl = null;
            if (current.Albums != null && current.Albums.Count > 0 && current.Albums[0]?.Url != null)
                coverUrl = current.Albums[0].Url;
            NowPlayingDisplay = new NowPlayingDisplay
            {
                Title = current.Title ?? "",
                Artist = current.Artists ?? "",
                Albumcover = coverUrl,
                AlbumcoverImageSource = UrlToImageSourceConverter.FromUrl(coverUrl)
            };
            return;
        }
        NowPlayingDisplay = null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}