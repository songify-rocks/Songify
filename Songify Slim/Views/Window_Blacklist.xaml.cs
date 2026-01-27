using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.UserControls;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Logger = Songify_Slim.Util.General.Logger;
using Task = System.Threading.Tasks.Task;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     This window dispalys and manages the blacklist
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public partial class Window_Blacklist
    {
        private readonly ObservableCollection<ArtistPickerRow> _artistPickerItems = [];
        public ObservableCollection<ArtistPickerRow> ArtistPickerItems => _artistPickerItems;

        public ArtistPickerRow SelectedArtistPickerItem { get; set; }

        private List<FullArtist> _artistSearchResults;
        private CustomDialog _artistDialog;

        private readonly ObservableCollection<BlockedArtist> _uiArtists = [];
        private readonly ObservableCollection<BlockedSong> _uiSongs = [];
        private readonly ObservableCollection<BlockedUser> _uiUsers = [];
        private readonly ObservableCollection<ArtistResolveCandidate> _resolveCandidates = [];
        public ObservableCollection<ArtistResolveCandidate> ResolveCandidates => _resolveCandidates;

        public ArtistResolveCandidate SelectedResolveCandidate { get; set; }

        public string ResolveDialogHint { get; set; } = "";

        public Window_Blacklist()
        {
            InitializeComponent();

            ArtistList.ItemsSource = _uiArtists;
            SongList.ItemsSource = _uiSongs;
            UserList.ItemsSource = _uiUsers;

            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshArtists();
            RefreshSongs();
            RefreshUsers();
        }

        private void RefreshArtists()
        {
            _uiArtists.Clear();
            foreach (BlockedArtist a in Settings.ArtistBlacklist)
                _uiArtists.Add(a);
        }

        private void RefreshSongs()
        {
            _uiSongs.Clear();
            foreach (BlockedSong s in Settings.SongBlacklist)
                _uiSongs.Add(s);
        }

        private void RefreshUsers()
        {
            _uiUsers.Clear();
            foreach (BlockedUser u in Settings.UserBlacklist)
                _uiUsers.Add(u);
        }

        private void ArtistEntry_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is UC_BlacklistEntry ctrl)
            {
                ctrl.DeleteRequested -= Artist_DeleteRequested;
                ctrl.DeleteRequested += Artist_DeleteRequested;
            }
        }

        private void Artist_DeleteRequested(object sender, IBlacklistItem item)
        {
            if (item is not BlockedArtist artist)
                return;

            RemoveFromBlacklist(
                Settings.ArtistBlacklist,
                artist,
                () => Settings.ArtistBlacklist = Settings.ArtistBlacklist,
                RefreshArtists);
        }

        private void SongEntry_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is UC_BlacklistEntry ctrl)
            {
                ctrl.DeleteRequested -= Song_DeleteRequested;
                ctrl.DeleteRequested += Song_DeleteRequested;
            }
        }

        private void Song_DeleteRequested(object sender, IBlacklistItem item)
        {
            if (item is not BlockedSong song)
                return;

            RemoveFromBlacklist(
                Settings.SongBlacklist,
                song,
                () => Settings.SongBlacklist = Settings.SongBlacklist,
                RefreshSongs);
        }

        private void User_DeleteRequested(object sender, IBlacklistItem item)
        {
            if (item is not BlockedUser user)
                return;

            RemoveFromBlacklist(
                Settings.UserBlacklist,
                user,
                () => Settings.UserBlacklist = Settings.UserBlacklist,
                RefreshUsers);
        }

        private void UserEntry_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is UC_BlacklistEntry ctrl)
            {
                ctrl.DeleteRequested -= User_DeleteRequested;
                ctrl.DeleteRequested += User_DeleteRequested;
            }
        }

        private static bool AddToBlacklist<T>(
            IList<T> list,
            T item,
            Action persist,
            Action onChanged = null)
            where T : IBlacklistItem
        {
            if (list.Any(x => x.Key == item.Key))
                return false;

            list.Add(item);
            persist();
            onChanged?.Invoke();
            return true;
        }

        private static void RemoveFromBlacklist<T>(
            IList<T> list,
            T item,
            Action persist,
            Action onChanged = null)
        {
            if (!list.Remove(item))
                return;

            persist();
            onChanged?.Invoke();
        }

        private static void ClearBlacklist<T>(
            IList<T> list,
            Action persist,
            Action onChanged = null)
        {
            if (list.Count == 0)
                return;

            list.Clear();
            persist();
            onChanged?.Invoke();
        }

        #region Songs

        private void AddSong_Click(object sender, RoutedEventArgs e)
        {
            AddSongToBlacklist();
        }

        private void SongInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            AddSongToBlacklist();
            SongInput.Clear();
            SongInput.Focus();
            e.Handled = true;
        }

        private void ClearSongs_Click(object sender, RoutedEventArgs e)
        {
            ClearBlacklist(Settings.SongBlacklist, () => Settings.SongBlacklist = Settings.SongBlacklist, RefreshSongs);
        }

        private async void AddSongToBlacklist()
        {
            try
            {
                string input = SongInput.Text.Trim();
                FullTrack x = await SpotifyApiHandler.FindTrack(input, 1);
                if (x != null)
                {
                    BlockedSong song = new BlockedSong
                    {
                        Id = x.Id,
                        Artist = string.Join(", ", x.Artists.Select(a => a.Name)),
                        Title = x.Name
                    };
                    AddToBlacklist(Settings.SongBlacklist, song, () => Settings.SongBlacklist = Settings.SongBlacklist, RefreshSongs);
                }
                else
                {
                    await this.ShowMessageAsync("Song Not Found", "Could not find the specified song on Spotify.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to add song to blocklist", ex);
            }
        }

        #endregion Songs

        #region Users

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            AddUserToBlacklist();
        }

        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            AddUserToBlacklist();
            UserInput.Clear();
            UserInput.Focus();
            e.Handled = true;
        }

        private void ClearUsers_Click(object sender, RoutedEventArgs e)
        {
            ClearBlacklist(Settings.UserBlacklist, () => Settings.UserBlacklist = Settings.UserBlacklist, RefreshUsers);
        }

        private async void AddUserToBlacklist()
        {
            try
            {
                List<string> users =
                [
                    UserInput.Text.Trim()
                ];
                User[] twitchUsers = await TwitchApiHelper.GetTwitchUsersAsync(users);
                if (twitchUsers.Length == 0)
                {
                    await this.ShowMessageAsync("User Not Found", "Could not find the specified user on Twitch.");
                    return;
                }
                foreach (User twitchUser in twitchUsers)
                {
                    BlockedUser user = new()
                    {
                        Id = twitchUser.Id,
                        Username = twitchUser.DisplayName,
                    };
                    AddToBlacklist(Settings.UserBlacklist, user, () => Settings.UserBlacklist = Settings.UserBlacklist, RefreshUsers);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error adding User to blocklist", ex);
            }
        }

        #endregion Users

        #region Artists

        private void AddArtist_Click(object sender, RoutedEventArgs e)
        {
            AddArtistToBlacklist();
        }

        private void ArtistInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            AddArtistToBlacklist();
            ArtistInput.Clear();
            ArtistInput.Focus();
            e.Handled = true;
        }

        private void ClearArtists_Click(object sender, RoutedEventArgs e)
        {
            ClearBlacklist(Settings.ArtistBlacklist, () => Settings.ArtistBlacklist = Settings.ArtistBlacklist, RefreshArtists);
        }

        private async void AddArtistToBlacklist()
        {
            try
            {
                // Spotify Artist Blacklist
                // If the API is not connected just don't do anything?
                if (SpotifyApiHandler.Client == null)
                {
                    await this.ShowMessageAsync("Notification",
                        "Spotify is not connected. You need to connect to Spotify in order to fill the blocklist.");
                    return;
                }

                // Perform a search via the spotify API
                List<FullArtist> searchItem = await SpotifyApiHandler.GetArtist(ArtistInput.Text.Trim());
                switch (searchItem.Count)
                {
                    case <= 0:
                        return;

                    case > 1:
                        {
                            _artistPickerItems.Clear();
                            int count = 1;

                            foreach (FullArtist a in searchItem)
                            {
                                _artistPickerItems.Add(new ArtistPickerRow
                                {
                                    Num = count,
                                    Artist = a.Name,
                                    ArtistId = a.Id   // recommended, see below
                                });
                                count++;
                            }

                            // keep the FullArtist list around so we can map selection -> ID
                            _artistSearchResults = searchItem;

                            _artistDialog ??= (CustomDialog)Resources["ArtistPickerDialog"];
                            _artistDialog.DataContext = this;   // <-- important
                            await this.ShowMetroDialogAsync(_artistDialog);

                            break;
                        }

                    default:
                        {
                            FullArtist fullartist = searchItem[0];
                            BlockedArtist artist = new()
                            {
                                Id = fullartist.Id,
                                Name = fullartist.Name
                            };
                            AddToBlacklist(Settings.ArtistBlacklist, artist, () => Settings.ArtistBlacklist = Settings.ArtistBlacklist, RefreshArtists);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error adding Artist to blocklist", ex);
            }
        }

        private async void ArtistDialog_Select_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtistPickerItem == null)
                return;

            BlockedArtist artist = new BlockedArtist
            {
                Id = SelectedArtistPickerItem.ArtistId,
                Name = SelectedArtistPickerItem.Artist
            };

            AddToBlacklist(Settings.ArtistBlacklist, artist, () => Settings.ArtistBlacklist = Settings.ArtistBlacklist, RefreshArtists);

            if (_artistDialog != null)
                await this.HideMetroDialogAsync(_artistDialog);
        }

        #endregion Artists

        private async void ArtistDialog_Close_Click(object sender, RoutedEventArgs e)
        {
            if (_artistDialog != null)
                await this.HideMetroDialogAsync(_artistDialog);
        }

        private void ArtistsRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // sender is the DataGridRow
            if (sender is DataGridRow row)
            {
                // ensure selection matches the row you double-clicked
                dgv_SelectedWorkaroundSelectRow(row); // optional, see below
            }

            // reuse the same logic as the "Block selected" button
            ArtistDialog_Select_Click(sender, new RoutedEventArgs());
        }

        // Optional: force selection to the double-clicked row (helps if selection lags)
        private void dgv_SelectedWorkaroundSelectRow(DataGridRow row)
        {
            if (row.Item is ArtistPickerRow item)
                SelectedArtistPickerItem = item;
        }

        private async void ArtistPickerDialog_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _artistDialog != null)
            {
                await this.HideMetroDialogAsync(_artistDialog);
                e.Handled = true;
            }
        }

        private async void RefreshArtists_Click(object sender, RoutedEventArgs e)
        {
            try { await RefreshArtistsInteractiveAsync(); }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to refresh artist IDs (interactive)", ex);
            }
        }

        private CustomDialog _resolveDialog;
        private TaskCompletionSource<ArtistResolveCandidate> _resolveTcs;

        private async Task RefreshArtistsInteractiveAsync()
        {
            if (SpotifyApiHandler.Client == null)
            {
                await this.ShowMessageAsync("Notification",
                    "Spotify is not connected. Connect Spotify to refresh artist IDs.");
                return;
            }

            List<BlockedArtist> missing = Settings.ArtistBlacklist
                .Where(a => string.IsNullOrWhiteSpace(a.Id) && !string.IsNullOrWhiteSpace(a.Name))
                .ToList();

            if (missing.Count == 0)
            {
                await this.ShowMessageAsync("Refresh IDs", "No legacy artist entries found.");
                return;
            }

            _resolveDialog ??= (CustomDialog)Resources["ArtistResolveDialog"];
            _resolveDialog.DataContext = this;

            int fixedCount = 0;

            for (int i = 0; i < missing.Count; i++)
            {
                BlockedArtist entry = missing[i];
                string query = entry.Name.Trim();

                // Get candidates from Spotify
                List<FullArtist> matches = await SpotifyApiHandler.GetArtist(query);
                List<FullArtist> top = (matches ?? []).Take(5).ToList();

                if (top.Count == 0)
                    continue; // nothing to choose from, leave it unresolved

                // Populate dialog candidates
                _resolveCandidates.Clear();
                foreach (FullArtist a in top)
                {
                    _resolveCandidates.Add(new ArtistResolveCandidate
                    {
                        Name = a.Name ?? "",
                        Id = a.Id ?? ""
                    });
                }

                SelectedResolveCandidate = _resolveCandidates.FirstOrDefault();
                ResolveDialogHint = $"Resolve: '{query}' ({i + 1} of {missing.Count}) - pick the correct artist.";

                // Show and wait for user choice
                _resolveTcs = new TaskCompletionSource<ArtistResolveCandidate>();
                await this.ShowMetroDialogAsync(_resolveDialog);

                ArtistResolveCandidate chosen = await _resolveTcs.Task;

                await this.HideMetroDialogAsync(_resolveDialog);

                if (chosen == null)
                    continue; // user skipped

                // Apply choice
                entry.Id = chosen.Id;
                entry.Name = chosen.Name; // optional: normalize to Spotify name

                fixedCount++;

                // Persist & refresh UI after each change (safer if app closes mid-process)
                Settings.ArtistBlacklist = Settings.ArtistBlacklist;
                RefreshArtists();
            }

            await this.ShowMessageAsync("Refresh complete",
                $"Resolved {fixedCount} of {missing.Count} legacy artist entries.");
        }

        private void Resolve_UseSelected_Click(object sender, RoutedEventArgs e)
        {
            _resolveTcs?.TrySetResult(SelectedResolveCandidate);
        }

        private void Resolve_Skip_Click(object sender, RoutedEventArgs e)
        {
            _resolveTcs?.TrySetResult(null);
        }

        private void ResolveRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (sender is DataGridRow row && row.Item is ArtistResolveCandidate c)
                SelectedResolveCandidate = c;

            _resolveTcs?.TrySetResult(SelectedResolveCandidate);
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUserBlacklistIdsAsync();
        }

        private async Task RefreshUserBlacklistIdsAsync()
        {
            try
            {
                // Get all legacy entries (missing Id)
                List<BlockedUser> missing = Settings.UserBlacklist
                    .Where(u => string.IsNullOrWhiteSpace(u.Id) && !string.IsNullOrWhiteSpace(u.Username))
                    .ToList();

                if (missing.Count == 0)
                {
                    await this.ShowMessageAsync("Refresh IDs", "No legacy user entries found.");
                    return;
                }

                // Normalize usernames (lowercase, trim, remove leading @)
                static string Norm(string s)
                {
                    s = (s ?? "").Trim();
                    if (s.StartsWith("@")) s = s.Substring(1);
                    return s.ToLowerInvariant();
                }

                List<string> usernames = missing
                    .Select(u => Norm(u.Username))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                // Batch query Twitch
                User[] twitchUsers = await TwitchApiHelper.GetTwitchUsersAsync(usernames);

                if (twitchUsers == null || twitchUsers.Length == 0)
                {
                    await this.ShowMessageAsync("Refresh IDs", "No users were found on Twitch for the legacy entries.");
                    return;
                }

                // Build lookup: prefer Login for matching, fallback to DisplayName
                // (adjust property names if your User type differs)
                Dictionary<string, User> byName = new(StringComparer.OrdinalIgnoreCase);

                foreach (User tu in twitchUsers)
                {
                    // Twitch usually returns both Login and DisplayName; Login is best for stable matching
                    string login = Norm(tu.Login);                 // if your model doesn't have Login, remove this
                    string display = Norm(tu.DisplayName);

                    if (!string.IsNullOrWhiteSpace(login) && !byName.ContainsKey(login))
                        byName[login] = tu;

                    if (!string.IsNullOrWhiteSpace(display) && !byName.ContainsKey(display))
                        byName[display] = tu;
                }

                int fixedCount = 0;

                foreach (BlockedUser entry in missing)
                {
                    string key = Norm(entry.Username);

                    if (byName.TryGetValue(key, out User tu))
                    {
                        entry.Id = tu.Id;
                        // Optional: normalize username to Twitch DisplayName
                        entry.Username = tu.DisplayName;
                        fixedCount++;
                    }
                }

                // Persist + refresh UI
                Settings.UserBlacklist = Settings.UserBlacklist;
                RefreshUsers();

                await this.ShowMessageAsync(
                    "Refresh complete",
                    $"Updated {fixedCount} of {missing.Count} user entries.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to refresh user blacklist IDs", ex);
                await this.ShowMessageAsync("Error", "Failed to refresh user IDs. Check the logs for details.");
            }
        }

    }

    public class ArtistResolveCandidate
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
    }


    public class ArtistPickerRow
    {
        public int Num { get; set; }
        public string Artist { get; set; } = "";
        public string ArtistId { get; set; } = "";
    }
}