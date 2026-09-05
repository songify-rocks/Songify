using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class UserListPage : Page
{
    public UserListPage()
    {
        InitializeComponent();

        ICollectionView view = CollectionViewSource.GetDefaultView(GlobalObjects.TwitchUsers);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(
            nameof(TwitchUser.HighestUserLevel),
            ListSortDirection.Descending));
        DgvViewers.ItemsSource = view;
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        DgvViewers.IsEnabled = false;
        GrdLoading.Visibility = Visibility.Visible;
        try
        {
            await TwitchHandler.RunTwitchUserSync();
        }
        finally
        {
            DgvViewers.IsEnabled = true;
            GrdLoading.Visibility = Visibility.Collapsed;
        }
    }

    private void MenuItem_BlockSr_Click(object sender, RoutedEventArgs e)
    {
        if (DgvViewers.SelectedItem is not TwitchUser selectedItem) return;

        if (Settings.UserBlacklist.Any(u =>
                string.Equals(u.Username, selectedItem.DisplayName, StringComparison.CurrentCultureIgnoreCase)))
        {
            Settings.UserBlacklist.Remove(Settings.UserBlacklist.First(u =>
                string.Equals(u.Username, selectedItem.DisplayName, StringComparison.CurrentCultureIgnoreCase)));
            selectedItem.IsSrBlocked = false;
        }
        else
        {
            Settings.UserBlacklist.Add(new BlockedUser
            {
                Id = selectedItem.UserId,
                Username = selectedItem.DisplayName
            });
            selectedItem.IsSrBlocked = true;
        }

        Settings.UserBlacklist = Settings.UserBlacklist;
    }
}
