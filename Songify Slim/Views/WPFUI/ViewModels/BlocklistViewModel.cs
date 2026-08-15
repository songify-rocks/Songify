using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify;
using Songify_Slim.ViewModels;
using SpotifyAPI.Web;
using Wpf.Ui.Controls;
using Logger = Songify_Slim.Util.General.Logger;

namespace Songify_Slim.Views.WPFUI.ViewModels;

public enum BlocklistCategory
{
    Artists,
    Users,
    Songs
}

public sealed class BlocklistCategoryItem : INotifyPropertyChanged
{
    private int _count;

    public BlocklistCategory Category { get; init; }
    public string Title { get; init; }
    public SymbolRegular Symbol { get; init; }

    public int Count
    {
        get => _count;
        set
        {
            if (_count == value) return;
            _count = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CountLabel));
        }
    }

    public string CountLabel => _count == 1 ? "1 item" : $"{_count} items";

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ArtistPickerRow
{
    public int Num { get; set; }
    public string Artist { get; set; } = "";
    public string ArtistId { get; set; } = "";
}

/// <summary>
/// Blocklist UI never binds the full 6k+ artist list. We keep an in-memory snapshot and only
/// expose a capped, filtered window so WPF cannot materialize thousands of containers.
/// </summary>
public sealed class BlocklistViewModel : INotifyPropertyChanged
{
    private const int MaxVisible = 250;

    private List<BlockedArtist> _allArtists = [];
    private List<BlockedUser> _allUsers = [];
    private List<BlockedSong> _allSongs = [];

    private readonly ObservableCollection<BlockedArtist> _visibleArtists = [];
    private readonly ObservableCollection<BlockedUser> _visibleUsers = [];
    private readonly ObservableCollection<BlockedSong> _visibleSongs = [];

    private int _loadVersion;
    private bool _isLoaded;
    private bool _isLoading;
    private CancellationTokenSource _filterCts;
    private BlocklistCategoryItem _selectedCategoryItem;
    private readonly ObservableCollection<ArtistPickerRow> _artistPickerItems = [];
    private readonly ObservableCollection<ArtistPickerRow> _selectedArtistPickerItems = [];
    private bool _isArtistPickerOpen;
    private bool _isSearchingArtist;

    public ObservableCollection<BlockedArtist> VisibleArtists => _visibleArtists;
    public ObservableCollection<BlockedUser> VisibleUsers => _visibleUsers;
    public ObservableCollection<BlockedSong> VisibleSongs => _visibleSongs;

    public ObservableCollection<BlocklistCategoryItem> Categories { get; }
    public ObservableCollection<ArtistPickerRow> ArtistPickerItems => _artistPickerItems;
    public ObservableCollection<ArtistPickerRow> SelectedArtistPickerItems => _selectedArtistPickerItems;

    public bool IsArtistPickerOpen
    {
        get => _isArtistPickerOpen;
        private set { if (_isArtistPickerOpen == value) return; _isArtistPickerOpen = value; OnPropertyChanged(); }
    }

    public bool IsSearchingArtist
    {
        get => _isSearchingArtist;
        private set
        {
            if (_isSearchingArtist == value) return;
            _isSearchingArtist = value;
            OnPropertyChanged();
            RelayCommand.InvalidateRequerySuggested();
        }
    }

    public BlocklistCategoryItem SelectedCategoryItem
    {
        get => _selectedCategoryItem;
        set
        {
            if (ReferenceEquals(_selectedCategoryItem, value)) return;
            _selectedCategoryItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedCategoryTitle));
            OnPropertyChanged(nameof(IsArtistsSelected));
            OnPropertyChanged(nameof(IsUsersSelected));
            OnPropertyChanged(nameof(IsSongsSelected));
        }
    }

    public BlocklistCategory SelectedCategory =>
        _selectedCategoryItem?.Category ?? BlocklistCategory.Artists;

    public string SelectedCategoryTitle => _selectedCategoryItem?.Title ?? "Artists";

    public bool IsArtistsSelected => SelectedCategory == BlocklistCategory.Artists;
    public bool IsUsersSelected => SelectedCategory == BlocklistCategory.Users;
    public bool IsSongsSelected => SelectedCategory == BlocklistCategory.Songs;

    public int ArtistCount
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArtistStatusText));
            UpdateCategoryCount(BlocklistCategory.Artists, value);
        }
    }

    public int UserCount
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserStatusText));
            UpdateCategoryCount(BlocklistCategory.Users, value);
        }
    }

    public int SongCount
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SongStatusText));
            UpdateCategoryCount(BlocklistCategory.Songs, value);
        }
    }

    public string ArtistStatusText => BuildStatus(ArtistCount, _visibleArtists.Count, _artistFilter);
    public string UserStatusText => BuildStatus(UserCount, _visibleUsers.Count, _userFilter);
    public string SongStatusText => BuildStatus(SongCount, _visibleSongs.Count, _songFilter);

    public bool IsLoading
    {
        get => _isLoading;
        private set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(); }
    }

    private string _artistFilter = "";
    public string ArtistFilter
    {
        get => _artistFilter;
        set
        {
            if (_artistFilter == value) return;
            _artistFilter = value ?? "";
            OnPropertyChanged();
            ScheduleRebuildVisible(RebuildArtistsVisible);
        }
    }

    private string _userFilter = "";
    public string UserFilter
    {
        get => _userFilter;
        set
        {
            if (_userFilter == value) return;
            _userFilter = value ?? "";
            OnPropertyChanged();
            ScheduleRebuildVisible(RebuildUsersVisible);
        }
    }

    private string _songFilter = "";
    public string SongFilter
    {
        get => _songFilter;
        set
        {
            if (_songFilter == value) return;
            _songFilter = value ?? "";
            OnPropertyChanged();
            ScheduleRebuildVisible(RebuildSongsVisible);
        }
    }

    private string _newArtistName;
    public string NewArtistName
    {
        get => _newArtistName;
        set { _newArtistName = value; OnPropertyChanged(); }
    }

    private string _newUsername;
    public string NewUsername
    {
        get => _newUsername;
        set { _newUsername = value; OnPropertyChanged(); }
    }

    private BlockedArtist _selectedArtist;
    public BlockedArtist SelectedArtist
    {
        get => _selectedArtist;
        set { _selectedArtist = value; OnPropertyChanged(); }
    }

    private BlockedUser _selectedUser;
    public BlockedUser SelectedUser
    {
        get => _selectedUser;
        set { _selectedUser = value; OnPropertyChanged(); }
    }

    private BlockedSong _selectedSong;
    public BlockedSong SelectedSong
    {
        get => _selectedSong;
        set { _selectedSong = value; OnPropertyChanged(); }
    }

    public RelayCommand AddArtistCommand { get; }
    public RelayCommand ConfirmArtistPickCommand { get; }
    public RelayCommand CancelArtistPickCommand { get; }
    public RelayCommand AddUserCommand { get; }
    public RelayCommand RemoveSelectedArtistCommand { get; }
    public RelayCommand RemoveSelectedUserCommand { get; }
    public RelayCommand RemoveSelectedSongCommand { get; }
    public RelayCommand ClearArtistsCommand { get; }
    public RelayCommand ClearUsersCommand { get; }
    public RelayCommand ClearSongsCommand { get; }

    public BlocklistViewModel()
    {
        Categories =
        [
            new BlocklistCategoryItem
            {
                Category = BlocklistCategory.Artists,
                Title = "Artists",
                Symbol = SymbolRegular.MusicNote224
            },
            new BlocklistCategoryItem
            {
                Category = BlocklistCategory.Users,
                Title = "Users",
                Symbol = SymbolRegular.Person24
            },
            new BlocklistCategoryItem
            {
                Category = BlocklistCategory.Songs,
                Title = "Songs",
                Symbol = SymbolRegular.MusicNote124
            }
        ];
        _selectedCategoryItem = Categories[0];

        AddArtistCommand = new RelayCommand(
            () => _ = AddArtistAsync(),
            () => !string.IsNullOrWhiteSpace(NewArtistName) && !IsSearchingArtist);
        ConfirmArtistPickCommand = new RelayCommand(ConfirmArtistPick, () => SelectedArtistPickerItems.Count > 0);
        CancelArtistPickCommand = new RelayCommand(CancelArtistPick);
        AddUserCommand = new RelayCommand(AddUser, () => !string.IsNullOrWhiteSpace(NewUsername));

        RemoveSelectedArtistCommand = new RelayCommand(_ => RemoveArtist(SelectedArtist), _ => SelectedArtist != null);
        RemoveSelectedUserCommand = new RelayCommand(_ => RemoveUser(SelectedUser), _ => SelectedUser != null);
        RemoveSelectedSongCommand = new RelayCommand(_ => RemoveSong(SelectedSong), _ => SelectedSong != null);

        ClearArtistsCommand = new RelayCommand(ClearArtists, () => ArtistCount > 0);
        ClearUsersCommand = new RelayCommand(ClearUsers, () => UserCount > 0);
        ClearSongsCommand = new RelayCommand(ClearSongs, () => SongCount > 0);
    }

    private void UpdateCategoryCount(BlocklistCategory category, int count)
    {
        BlocklistCategoryItem item = Categories.FirstOrDefault(c => c.Category == category);
        if (item != null)
            item.Count = count;
    }

    /// <summary>Drop UI-held artist objects when leaving the page.</summary>
    public void Unload()
    {
        _filterCts?.Cancel();
        _allArtists = [];
        _visibleArtists.Clear();
        ArtistCount = 0;
        SelectedArtist = null;
        _isLoaded = false;
        Settings.UnloadArtistBlacklist();
    }

    public async Task LoadAsync()
    {
        if (_isLoaded || IsLoading) return;
        await LoadCoreAsync();
    }

    /// <summary>Force a fresh snapshot from disk (e.g. after CSV import).</summary>
    public async Task ReloadAsync()
    {
        _isLoaded = false;
        await LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        if (IsLoading) return;

        int version = Interlocked.Increment(ref _loadVersion);
        IsLoading = true;

        try
        {
            // Snapshot off the UI thread — copying 6k items is cheap, but keep layout free.
            var snapshot = await Task.Run(() =>
            {
                List<BlockedArtist> artists;
                List<BlockedUser> users;
                List<BlockedSong> songs;
                try
                {
                    // Copy from disk; do not leave the full list resident on CurrentConfig.
                    artists = ArtistBlocklistStore.LoadCopy();
                    Settings.UnloadArtistBlacklist();
                    users = (Settings.UserBlacklist ?? []).ToList();
                    songs = (Settings.SongBlacklist ?? []).ToList();
                }
                catch
                {
                    artists = [];
                    users = [];
                    songs = [];
                }

                return (artists, users, songs);
            }).ConfigureAwait(true);

            if (version != _loadVersion) return;

            _allArtists = snapshot.artists;
            _allUsers = snapshot.users;
            _allSongs = snapshot.songs;

            ArtistCount = _allArtists.Count;
            UserCount = _allUsers.Count;
            SongCount = _allSongs.Count;

            RebuildArtistsVisible();
            RebuildUsersVisible();
            RebuildSongsVisible();

            _isLoaded = true;
            RelayCommand.InvalidateRequerySuggested();
        }
        finally
        {
            if (version == _loadVersion)
                IsLoading = false;
        }
    }

    /// <summary>
    /// Resolves Spotify IDs for artists that only have a name, using the provided lookup (first match).
    /// </summary>
    public async Task<int> RefreshMissingArtistIdsAsync(Func<string, Task<FullArtist>> resolveArtist)
    {
        if (!_isLoaded)
            await LoadAsync();

        List<BlockedArtist> missing = _allArtists
            .Where(a => string.IsNullOrWhiteSpace(a.Id) && !string.IsNullOrWhiteSpace(a.Name))
            .ToList();

        if (missing.Count == 0)
            return 0;

        int fixedCount = 0;
        foreach (BlockedArtist entry in missing)
        {
            FullArtist match = await resolveArtist(entry.Name.Trim()).ConfigureAwait(true);
            if (match == null || string.IsNullOrWhiteSpace(match.Id))
                continue;

            entry.Id = match.Id;
            if (!string.IsNullOrWhiteSpace(match.Name))
                entry.Name = match.Name;
            fixedCount++;
        }

        if (fixedCount > 0)
            PersistArtists();

        return fixedCount;
    }

    private static string BuildStatus(int total, int visible, string filter)
    {
        if (total == 0) return "0 total";
        if (string.IsNullOrWhiteSpace(filter))
            return visible < total
                ? $"Showing {visible} of {total} — type to filter"
                : $"{total} total";
        return visible < total && visible >= MaxVisible
            ? $"Showing first {visible} matches of {total}"
            : $"{visible} match(es) of {total}";
    }

    private void ScheduleRebuildVisible(Action rebuild)
    {
        _filterCts?.Cancel();
        _filterCts = new CancellationTokenSource();
        CancellationToken token = _filterCts.Token;

        // Debounce filter so typing over 6k names doesn't rebuild every keystroke.
        _ = Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(120, token);
                if (!token.IsCancellationRequested)
                    rebuild();
            }
            catch (TaskCanceledException)
            {
                // expected
            }
        }, DispatcherPriority.Background);
    }

    private void RebuildArtistsVisible()
    {
        ReplaceVisible(_visibleArtists, Query(_allArtists, _artistFilter, a => a.Display));
        OnPropertyChanged(nameof(ArtistStatusText));
    }

    private void RebuildUsersVisible()
    {
        ReplaceVisible(_visibleUsers, Query(_allUsers, _userFilter, u => u.Display));
        OnPropertyChanged(nameof(UserStatusText));
    }

    private void RebuildSongsVisible()
    {
        ReplaceVisible(_visibleSongs, Query(_allSongs, _songFilter, s => s.Display));
        OnPropertyChanged(nameof(SongStatusText));
    }

    private static List<T> Query<T>(List<T> source, string filter, Func<T, string> display)
    {
        IEnumerable<T> q = source;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = source.Where(x => (display(x) ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return q.Take(MaxVisible).ToList();
    }

    private static void ReplaceVisible<T>(ObservableCollection<T> target, List<T> next)
    {
        // Avoid Clear()+N Adds which raise N change events; replace when possible by clearing once then bulk-add.
        // For capped lists (<=250) this is fine on UI thread.
        target.Clear();
        foreach (T item in next)
            target.Add(item);
    }

    private void PersistArtists()
    {
        Settings.ArtistBlacklist = _allArtists.ToList();
        ArtistCount = _allArtists.Count;
        RebuildArtistsVisible();
        RelayCommand.InvalidateRequerySuggested();
    }

    private void PersistUsers()
    {
        Settings.UserBlacklist = _allUsers.ToList();
        UserCount = _allUsers.Count;
        RebuildUsersVisible();
        RelayCommand.InvalidateRequerySuggested();
    }

    private void PersistSongs()
    {
        Settings.SongBlacklist = _allSongs.ToList();
        SongCount = _allSongs.Count;
        RebuildSongsVisible();
        RelayCommand.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Searches Spotify for the typed name. One match is added immediately; multiple opens the picker.
    /// </summary>
    public async Task AddArtistAsync()
    {
        string query = (NewArtistName ?? "").Trim();
        if (query.Length == 0 || IsSearchingArtist)
            return;

        if (SpotifyApiHandler.Client == null)
        {
            await AppDialog.ShowAsync("Notification",
                "Spotify is not connected. You need to connect to Spotify in order to fill the blocklist.");
            return;
        }

        IsSearchingArtist = true;
        try
        {
            List<FullArtist> searchItem = await SpotifyApiHandler.GetArtist(query);
            if (searchItem == null || searchItem.Count == 0)
            {
                await AppDialog.ShowAsync("Artist not found",
                    "Could not find an artist matching that name on Spotify.");
                return;
            }

            if (searchItem.Count == 1)
            {
                AddArtistFromSpotify(searchItem[0].Id, searchItem[0].Name);
                NewArtistName = "";
                return;
            }

            _artistPickerItems.Clear();
            _selectedArtistPickerItems.Clear();
            int count = 1;
            foreach (FullArtist a in searchItem)
            {
                _artistPickerItems.Add(new ArtistPickerRow
                {
                    Num = count++,
                    Artist = a.Name,
                    ArtistId = a.Id
                });
            }

            IsArtistPickerOpen = true;
            RelayCommand.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            await AppDialog.ShowAsync("Error", "Failed to search Spotify for that artist.");
        }
        finally
        {
            IsSearchingArtist = false;
        }
    }

    /// <summary>
    /// DataGrid.SelectedItems is not bindable — page syncs selection here on SelectionChanged.
    /// </summary>
    public void SyncArtistPickerSelection(IEnumerable<ArtistPickerRow> selected)
    {
        _selectedArtistPickerItems.Clear();
        if (selected != null)
        {
            foreach (ArtistPickerRow row in selected)
                _selectedArtistPickerItems.Add(row);
        }

        RelayCommand.InvalidateRequerySuggested();
    }

    private void ConfirmArtistPick()
    {
        if (_selectedArtistPickerItems.Count == 0)
            return;

        bool added = false;
        foreach (ArtistPickerRow row in _selectedArtistPickerItems.ToList())
        {
            if (TryAddArtistFromSpotify(row.ArtistId, row.Artist))
                added = true;
        }

        if (added)
            PersistArtists();

        NewArtistName = "";
        CancelArtistPick();
    }

    private void CancelArtistPick()
    {
        IsArtistPickerOpen = false;
        _artistPickerItems.Clear();
        _selectedArtistPickerItems.Clear();
        RelayCommand.InvalidateRequerySuggested();
    }

    private bool TryAddArtistFromSpotify(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        BlockedArtist artist = new()
        {
            Id = id,
            Name = name
        };

        if (_allArtists.Any(a => string.Equals(a?.Key, artist.Key, StringComparison.OrdinalIgnoreCase)))
            return false;

        _allArtists.Add(artist);
        return true;
    }

    private void AddArtistFromSpotify(string id, string name)
    {
        if (TryAddArtistFromSpotify(id, name))
            PersistArtists();
    }

    private void AddUser()
    {
        string username = (NewUsername ?? "").Trim();
        if (username.Length == 0) return;

        string key = username.ToLowerInvariant();
        if (_allUsers.Any(u => (u?.Key ?? "").Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            NewUsername = "";
            return;
        }

        _allUsers.Add(new BlockedUser { Id = null, Username = username });
        NewUsername = "";
        PersistUsers();
    }

    private void RemoveArtist(BlockedArtist artist)
    {
        if (artist == null) return;
        _allArtists.RemoveAll(a => string.Equals(a?.Key, artist.Key, StringComparison.OrdinalIgnoreCase));
        SelectedArtist = null;
        PersistArtists();
    }

    private void RemoveUser(BlockedUser user)
    {
        if (user == null) return;
        _allUsers.RemoveAll(u => string.Equals(u?.Key, user.Key, StringComparison.OrdinalIgnoreCase));
        SelectedUser = null;
        PersistUsers();
    }

    private void RemoveSong(BlockedSong song)
    {
        if (song == null) return;
        _allSongs.RemoveAll(s => string.Equals(s?.Key, song.Key, StringComparison.OrdinalIgnoreCase));
        SelectedSong = null;
        PersistSongs();
    }

    private void ClearArtists()
    {
        _allArtists = [];
        PersistArtists();
    }

    private void ClearUsers()
    {
        _allUsers = [];
        PersistUsers();
    }

    private void ClearSongs()
    {
        _allSongs = [];
        PersistSongs();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
