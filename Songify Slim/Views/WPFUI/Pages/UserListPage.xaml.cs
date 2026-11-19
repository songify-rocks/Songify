using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Collections.Generic;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class UserListPage : Page
{
    private readonly ICollectionView _view;

    // One shared tick for the cooldown column; runs only while the page is shown.
    private readonly DispatcherTimer _cooldownTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public UserListPage()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(GlobalObjects.TwitchUsers);
        _view.SortDescriptions.Clear();
        _view.SortDescriptions.Add(new SortDescription(
            nameof(TwitchUser.HighestUserLevel),
            ListSortDirection.Descending));
        // The default view is shared and outlives this page; drop any filter from a previous instance.
        _view.Filter = null;
        DgvViewers.ItemsSource = _view;

        _cooldownTimer.Tick += (_, _) => RefreshCooldowns();
        Loaded += (_, _) =>
        {
            RefreshCooldowns();
            _cooldownTimer.Start();
        };
        Unloaded += (_, _) => _cooldownTimer.Stop();
    }

    // Refreshes every row (not only active cooldowns) so exemption changes from settings show up;
    // RefreshCooldown only raises PropertyChanged when something actually changed.
    private static void RefreshCooldowns()
    {
        foreach (TwitchUser user in GlobalObjects.TwitchUsers)
            user.RefreshCooldown();
    }

    private void TbSearch_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = TbSearch.Text.Trim();
        _view.Filter = string.IsNullOrEmpty(query)
            ? null
            : o => o is TwitchUser u &&
                   ((u.DisplayName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (u.UserName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
    }

    private void MenuItem_ResetCooldown_Click(object sender, RoutedEventArgs e)
    {
        if (DgvViewers.SelectedItem is not TwitchUser selectedItem) return;
        selectedItem.UpdateCommandTime(true);
    }

    private void MenuItem_ToggleCooldownExempt_Click(object sender, RoutedEventArgs e)
    {
        if (DgvViewers.SelectedItem is not TwitchUser selectedItem) return;

        List<BlockedUser> list = [.. Settings.CooldownExemptUsers];
        int removed = list.RemoveAll(u =>
            (!string.IsNullOrEmpty(u.Id) && u.Id == selectedItem.UserId) ||
            (string.IsNullOrEmpty(u.Id) &&
             (string.Equals(u.Username, selectedItem.UserName, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(u.Username, selectedItem.DisplayName, StringComparison.OrdinalIgnoreCase))));
        if (removed == 0)
            list.Add(new BlockedUser { Id = selectedItem.UserId, Username = selectedItem.DisplayName });

        Settings.CooldownExemptUsers = list;
        selectedItem.RefreshCooldown();
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
