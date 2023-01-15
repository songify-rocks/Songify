using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Songify_Slim
{
    /// <summary>
    ///     This window dispalys and manages the blacklist
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public partial class Window_Blacklist
    {
        public static string[] Blacklist;
        public static string[] UserBlacklist;

        public string Splitter = "|||";

        public Window_Blacklist()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBlacklists();
        }

        private void LoadBlacklists()
        {
            LoadAritstBlacklist();
            LoadUserBlacklist();
        }

        private void LoadUserBlacklist()
        {
            ListView_UserBlacklist.Items.Clear();

            if (Settings.UserBlacklist == null || Settings.UserBlacklist.Count == 0)
                return;

            foreach (string s in Settings.UserBlacklist.Where(s => !string.IsNullOrEmpty(s)))
                ListView_UserBlacklist.Items.Add(s);
        }

        private void LoadAritstBlacklist()
        {
            ListView_Blacklist.Items.Clear();

            if (Settings.ArtistBlacklist == null || Settings.ArtistBlacklist.Count == 0)
                return;

            foreach (string s in Settings.ArtistBlacklist.Where(s => !string.IsNullOrEmpty(s)))
                ListView_Blacklist.Items.Add(s);
        }

        private void btn_Add_Click(object sender, RoutedEventArgs e)
        {
            //This adds to the blacklist. 
            AddToBlacklist(tb_Blacklist.Text);
            tb_Blacklist.Text = "";
        }

        private async void AddToBlacklist(string search)
        {
            //Check if the string is empty
            if (string.IsNullOrEmpty(search))
                return;

            switch (cbx_Type.SelectedIndex)
            {
                case 0:
                    // Spotify Artist Blacklist
                    // If the API is not connected just don't do anything?
                    if (ApiHandler.Spotify == null)
                    {
                        await this.ShowMessageAsync("Notification",
                            "Spotify is not connected. You need to connect to Spotify in order to fill the blacklist.");
                        return;
                    }

                    // Perform a search via the spotify API
                    SearchItem searchItem = ApiHandler.GetArtist(search);
                    if (searchItem.Artists.Items.Count <= 0)
                        return;
                    if (searchItem.Artists.Items.Count > 1)
                    {
                        dgv_Artists.Items.Clear();
                        int count = 1;
                        foreach (FullArtist artist in searchItem.Artists.Items)
                        {
                            dgv_Artists.Items.Add(new BlockListArtists { Num = count, Artist = artist.Name, IsSelected = false });
                            count++;
                        }
                        cc_Content.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FullArtist fullartist = searchItem.Artists.Items[0];

                        if (ListView_Blacklist.Items.Cast<object>().Any(item => item.ToString() == fullartist.Name))
                        {
                            return;
                        }
                        ListView_Blacklist.Items.Add(fullartist.Name);
                        SaveBlacklist();
                    }
                    break;
                case 1:
                    ListView_UserBlacklist.Items.Add(search);
                    SaveBlacklist();
                    break;
            }
        }

        private void SaveBlacklist()
        {
            //Artist Blacklist
            string s = "";
            List<string> tempList = new List<string>();
            if (ListView_Blacklist.Items.Count > 0)
            {
                tempList.AddRange(from object item in ListView_Blacklist.Items where (string)item != "" select (string)item);
            }
            Settings.ArtistBlacklist = tempList;

            //User Blacklist
            tempList = new List<string>();
            if (ListView_UserBlacklist.Items.Count > 0)
            {
                tempList.AddRange(from object item in ListView_UserBlacklist.Items where (string)item != "" select (string)item);

            }
            Settings.UserBlacklist = tempList;
            ConfigHandler.WriteAllConfig(Settings.Export());
            Settings.Export();
            LoadBlacklists();
        }

        private async void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            // after user confirmation clear the list
            switch (cbx_Type.SelectedIndex)
            {
                case 0:
                    MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                        "Do you really want to clear the Artist blacklist?", MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        Settings.ArtistBlacklist.Clear();
                        ListView_Blacklist.Items.Clear();
                    }

                    break;
                case 1:
                    msgResult = await this.ShowMessageAsync("Notification",
                        "Do you really want to clear the User blacklist?", MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        Settings.UserBlacklist.Clear();
                        ListView_UserBlacklist.Items.Clear();
                    }

                    break;
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mnu)) return;

            ListBox listView = ((ContextMenu)mnu.Parent).PlacementTarget as ListBox;

            // right-click context menu to delete single blacklist entries
            if (listView != null && listView.SelectedItem == null)
                return;

            if (listView == null) return;
            MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                "Delete " + listView.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            listView.Items.Remove(listView.SelectedItem);
            SaveBlacklist();
        }

        private void tb_Blacklist_KeyDown(object sender, KeyEventArgs e)
        {
            // on enter key save to the blacklist
            if (e.Key == Key.Enter)
            {
                AddToBlacklist(tb_Blacklist.Text);
                tb_Blacklist.Text = "";
            }
        }

        private async void ListView_Blacklist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                    "Delete " + ListView_Blacklist.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    ListView_Blacklist.Items.Remove(ListView_Blacklist.SelectedItem);
                    SaveBlacklist();
                }
            }
        }

        private async void ListView_UserBlacklist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                    "Delete " + ListView_UserBlacklist.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    ListView_UserBlacklist.Items.Remove(ListView_UserBlacklist.SelectedItem);
                    SaveBlacklist();
                }
            }
        }

        private void btn_AddArtists_Click(object sender, RoutedEventArgs e)
        {
            foreach (BlockListArtists row in dgv_Artists.Items)
            {
                bool alreadyIn = false;
                if (!row.IsSelected)
                    continue;

                foreach (object item in ListView_Blacklist.Items)
                    if (item.ToString() == row.Artist)
                    {
                        alreadyIn = true;
                        break;

                    }
                if (!alreadyIn)
                    ListView_Blacklist.Items.Add(row.Artist);
            }
            dgv_Artists.Items.Clear();
            cc_Content.Visibility = Visibility.Hidden;
            SaveBlacklist();
        }

        private void btn_CancelArtists_Click(object sender, RoutedEventArgs e)
        {
            dgv_Artists.Items.Clear();
            cc_Content.Visibility = Visibility.Hidden;
        }

        private void cbx_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tb_Blacklist.SetValue(TextBoxHelper.WatermarkProperty,
                ((ComboBox)sender).SelectedIndex == 0 ? Properties.Resources.bw_cbArtist : Properties.Resources.bw_cbUser);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveBlacklist();
        }
    }

    internal class BlockListArtists
    {
        public int Num { get; set; }
        public string Artist { get; set; }
        public bool IsSelected { get; set; }
    }
}