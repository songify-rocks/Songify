using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Views;
using Songify_Slim.Views.WPFUI.ViewModels;
using SpotifyAPI.Web;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class BlacklistPage : Page
{
    public BlacklistPage()
    {
        InitializeComponent();
        if (DataContext is BlocklistViewModel vm)
            BlocklistUi.Register(vm);
        IsVisibleChanged += BlacklistPage_OnIsVisibleChanged;
        Unloaded += (_, _) =>
        {
            if (DataContext is BlocklistViewModel registered)
                BlocklistUi.Unregister(registered);
        };
    }

    private void CategoryTitle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock title || title.DataContext is not BlocklistCategoryItem item)
            return;
        if (!string.IsNullOrWhiteSpace(item.TitleResourceKey))
            title.SetResourceReference(TextBlock.TextProperty, item.TitleResourceKey);
    }

    private async void BlacklistPage_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm) return;

        // NavigationView often keeps pages alive; unload the ~6k artist snapshot when the page is hidden.
        if (IsVisible)
            await vm.LoadAsync();
        else
            vm.Unload();
    }

    private void ArtistInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.ClearArtistSuggestions();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && vm.IsArtistSuggestionsOpen && vm.ArtistSuggestions.Count > 0)
        {
            vm.SelectedArtistSuggestion ??= vm.ArtistSuggestions[0];
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        if (vm.IsArtistSuggestionsOpen && vm.SelectedArtistSuggestion != null)
        {
            vm.SelectArtistSuggestion(vm.SelectedArtistSuggestion);
            return;
        }

        if (vm.AddArtistCommand.CanExecute(null))
            vm.AddArtistCommand.Execute(null);
    }

    private void ArtistSuggestion_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm)
            return;
        if (vm.SelectedArtistSuggestion != null)
            vm.SelectArtistSuggestion(vm.SelectedArtistSuggestion);
    }

    private void SongInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.ClearSongSuggestions();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && vm.IsSongSuggestionsOpen && vm.SongSuggestions.Count > 0)
        {
            vm.SelectedSongSuggestion ??= vm.SongSuggestions[0];
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        if (vm.IsSongSuggestionsOpen && vm.SelectedSongSuggestion != null)
        {
            vm.SelectSongSuggestion(vm.SelectedSongSuggestion);
            return;
        }

        if (vm.AddSongCommand.CanExecute(null))
            vm.AddSongCommand.Execute(null);
    }

    private void SongSuggestion_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm)
            return;
        if (vm.SelectedSongSuggestion != null)
            vm.SelectSongSuggestion(vm.SelectedSongSuggestion);
    }

    private void SongPickerGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm || sender is not DataGrid grid)
            return;

        vm.SyncSongPickerSelection(grid.SelectedItems.OfType<ViewModels.SongPickerRow>());
    }

    private void SongPickerOverlay_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (DataContext is BlocklistViewModel vm)
            vm.CancelSongPickCommand.Execute(null);
        e.Handled = true;
    }

    private void SongPickerGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm) return;
        if (vm.ConfirmSongPickCommand.CanExecute(null))
            vm.ConfirmSongPickCommand.Execute(null);
    }

    private void ArtistPickerGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm || sender is not DataGrid grid)
            return;

        vm.SyncArtistPickerSelection(grid.SelectedItems.OfType<ViewModels.ArtistPickerRow>());
    }

    private void ArtistPickerOverlay_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (DataContext is BlocklistViewModel vm)
            vm.CancelArtistPickCommand.Execute(null);
        e.Handled = true;
    }

    private void ArtistPickerGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm) return;
        if (vm.ConfirmArtistPickCommand.CanExecute(null))
            vm.ConfirmArtistPickCommand.Execute(null);
    }

    private void BtnImportArtists_OnClick(object sender, RoutedEventArgs e)
    {
        Window_ArtistImport existing = Application.Current.Windows.OfType<Window_ArtistImport>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        Window_ArtistImport importWindow = new()
        {
            Owner = Window.GetWindow(this)
        };
        importWindow.ImportCompleted += async (_, _) =>
        {
            if (DataContext is BlocklistViewModel vm)
                await vm.ReloadAsync();
        };
        importWindow.Show();
    }

    private async void BtnRefreshArtistIds_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BlocklistViewModel vm)
            return;

        if (SpotifyApiHandler.Client == null)
        {
            await AppDialog.ShowAsync(
                TryFindResource("common_notification") as string ?? "Notification",
                TryFindResource("window_blocklist_spotify_refresh_hint") as string
                ?? "Spotify is not connected. Connect Spotify to refresh artist IDs.");
            return;
        }

        try
        {
            int fixedCount = await vm.RefreshMissingArtistIdsAsync(async query =>
            {
                System.Collections.Generic.List<FullArtist> matches = await SpotifyApiHandler.GetArtist(query);
                return matches?.FirstOrDefault();
            });

            string body = fixedCount == 0
                ? (TryFindResource("window_blocklist_refresh_none") as string
                   ?? "No legacy artist entries needed resolving.")
                : string.Format(
                    TryFindResource("window_blocklist_refresh_resolved") as string
                    ?? "Resolved {0} artist ID(s) (first Spotify match).",
                    fixedCount);

            await AppDialog.ShowAsync(
                TryFindResource("window_blocklist_refresh_complete") as string ?? "Refresh complete",
                body);
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            await AppDialog.ShowAsync(
                TryFindResource("common_error") as string ?? "Error",
                TryFindResource("window_blocklist_refresh_failed") as string
                ?? "Failed to refresh artist IDs. Check the logs for details.");
        }
    }
}
