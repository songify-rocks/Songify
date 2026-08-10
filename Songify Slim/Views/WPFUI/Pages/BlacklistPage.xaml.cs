using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        IsVisibleChanged += BlacklistPage_OnIsVisibleChanged;
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
            await AppDialog.ShowAsync("Notification",
                "Spotify is not connected. Connect Spotify to refresh artist IDs.");
            return;
        }

        try
        {
            int fixedCount = await vm.RefreshMissingArtistIdsAsync(async query =>
            {
                System.Collections.Generic.List<FullArtist> matches = await SpotifyApiHandler.GetArtist(query);
                return matches?.FirstOrDefault();
            });

            await AppDialog.ShowAsync("Refresh complete",
                fixedCount == 0
                    ? "No legacy artist entries needed resolving."
                    : $"Resolved {fixedCount} artist ID(s) (first Spotify match).");
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            await AppDialog.ShowAsync("Error", "Failed to refresh artist IDs. Check the logs for details.");
        }
    }
}
