using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.ViewModels;

namespace Songify_Slim.Views.WPFUI.ViewModels;

public sealed class BlocklistViewModel : INotifyPropertyChanged
{
    public ObservableCollection<BlockedArtist> Artists { get; } = new();
    public ObservableCollection<BlockedUser> Users { get; } = new();
    public ObservableCollection<BlockedSong> Songs { get; } = new();

    private string _newArtistName;
    public string NewArtistName
    {
        get => _newArtistName;
        set { _newArtistName = value; OnPropertyChanged(); RelayCommand.InvalidateRequerySuggested(); }
    }

    private string _newUsername;
    public string NewUsername
    {
        get => _newUsername;
        set { _newUsername = value; OnPropertyChanged(); RelayCommand.InvalidateRequerySuggested(); }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddArtistCommand { get; }
    public RelayCommand AddUserCommand { get; }
    public RelayCommand RemoveArtistCommand { get; }
    public RelayCommand RemoveUserCommand { get; }
    public RelayCommand RemoveSongCommand { get; }
    public RelayCommand ClearArtistsCommand { get; }
    public RelayCommand ClearUsersCommand { get; }
    public RelayCommand ClearSongsCommand { get; }

    public BlocklistViewModel()
    {
        RefreshCommand = new RelayCommand(Refresh);

        AddArtistCommand = new RelayCommand(AddArtist, () => !string.IsNullOrWhiteSpace(NewArtistName));
        AddUserCommand = new RelayCommand(AddUser, () => !string.IsNullOrWhiteSpace(NewUsername));

        RemoveArtistCommand = new RelayCommand(p => RemoveArtist(p as BlockedArtist), p => p is BlockedArtist);
        RemoveUserCommand = new RelayCommand(p => RemoveUser(p as BlockedUser), p => p is BlockedUser);
        RemoveSongCommand = new RelayCommand(p => RemoveSong(p as BlockedSong), p => p is BlockedSong);

        ClearArtistsCommand = new RelayCommand(ClearArtists, () => Artists.Count > 0);
        ClearUsersCommand = new RelayCommand(ClearUsers, () => Users.Count > 0);
        ClearSongsCommand = new RelayCommand(ClearSongs, () => Songs.Count > 0);

        // Designer often instantiates this before config is loaded; don't crash the designer.
        TryRefresh();
    }

    public void Refresh()
    {
        Artists.Clear();
        foreach (var a in Settings.ArtistBlacklist ?? new List<BlockedArtist>())
            Artists.Add(a);

        Users.Clear();
        foreach (var u in Settings.UserBlacklist ?? new List<BlockedUser>())
            Users.Add(u);

        Songs.Clear();
        foreach (var s in Settings.SongBlacklist ?? new List<BlockedSong>())
            Songs.Add(s);

        RelayCommand.InvalidateRequerySuggested();
    }

    private void TryRefresh()
    {
        try
        {
            Refresh();
        }
        catch
        {
            // Likely Settings.CurrentConfig/AppConfig not initialized in designer.
            Artists.Clear();
            Users.Clear();
            Songs.Clear();
        }
    }

    private void AddArtist()
    {
        string name = (NewArtistName ?? "").Trim();
        if (name.Length == 0) return;

        // If settings aren't ready (designer), no-op.
        try { _ = Settings.ArtistBlacklist; } catch { return; }

        string key = name.ToLowerInvariant();
        var existing = (Settings.ArtistBlacklist ?? new List<BlockedArtist>())
            .Any(a => (a?.Key ?? "").Equals(key, StringComparison.OrdinalIgnoreCase));
        if (existing) { NewArtistName = ""; return; }

        var list = (Settings.ArtistBlacklist ?? new List<BlockedArtist>()).ToList();
        list.Add(new BlockedArtist { Id = null, Name = name });
        Settings.ArtistBlacklist = list;

        NewArtistName = "";
        TryRefresh();
    }

    private void AddUser()
    {
        string username = (NewUsername ?? "").Trim();
        if (username.Length == 0) return;

        try { _ = Settings.UserBlacklist; } catch { return; }

        string key = username.ToLowerInvariant();
        var existing = (Settings.UserBlacklist ?? new List<BlockedUser>())
            .Any(u => (u?.Key ?? "").Equals(key, StringComparison.OrdinalIgnoreCase));
        if (existing) { NewUsername = ""; return; }

        var list = (Settings.UserBlacklist ?? new List<BlockedUser>()).ToList();
        list.Add(new BlockedUser { Id = null, Username = username });
        Settings.UserBlacklist = list;

        NewUsername = "";
        TryRefresh();
    }

    private void RemoveArtist(BlockedArtist artist)
    {
        if (artist == null) return;
        try
        {
            var list = (Settings.ArtistBlacklist ?? new List<BlockedArtist>()).ToList();
            list.RemoveAll(a => string.Equals(a?.Key, artist.Key, StringComparison.OrdinalIgnoreCase));
            Settings.ArtistBlacklist = list;
            TryRefresh();
        }
        catch { }
    }

    private void RemoveUser(BlockedUser user)
    {
        if (user == null) return;
        try
        {
            var list = (Settings.UserBlacklist ?? new List<BlockedUser>()).ToList();
            list.RemoveAll(u => string.Equals(u?.Key, user.Key, StringComparison.OrdinalIgnoreCase));
            Settings.UserBlacklist = list;
            TryRefresh();
        }
        catch { }
    }

    private void RemoveSong(BlockedSong song)
    {
        if (song == null) return;
        try
        {
            var list = (Settings.SongBlacklist ?? new List<BlockedSong>()).ToList();
            list.RemoveAll(s => string.Equals(s?.Key, song.Key, StringComparison.OrdinalIgnoreCase));
            Settings.SongBlacklist = list;
            TryRefresh();
        }
        catch { }
    }

    private void ClearArtists()
    {
        try
        {
            Settings.ArtistBlacklist = new List<BlockedArtist>();
            TryRefresh();
        }
        catch { }
    }

    private void ClearUsers()
    {
        try
        {
            Settings.UserBlacklist = new List<BlockedUser>();
            TryRefresh();
        }
        catch { }
    }

    private void ClearSongs()
    {
        try
        {
            Settings.SongBlacklist = new List<BlockedSong>();
            TryRefresh();
        }
        catch { }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

