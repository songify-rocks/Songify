
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;

namespace Songify_Slim
{
    /// <summary>
    /// This window dispalys and manages the blacklist
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

        private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
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

            if (string.IsNullOrEmpty(Settings.UserBlacklist))
                return;

            UserBlacklist = Settings.UserBlacklist.Split(new[] { Splitter }, StringSplitOptions.None);

            foreach (string s in UserBlacklist)
            {
                if (!string.IsNullOrEmpty(s))
                    ListView_UserBlacklist.Items.Add(s);
            }
        }

        private void LoadAritstBlacklist()
        {
            ListView_Blacklist.Items.Clear();

            if (string.IsNullOrEmpty(Settings.ArtistBlacklist))
                return;

            Blacklist = Settings.ArtistBlacklist.Split(new[] { Splitter }, StringSplitOptions.None);

            foreach (string s in Blacklist)
            {
                if (!string.IsNullOrEmpty(s))
                    ListView_Blacklist.Items.Add(s);
            }
        }

        private void btn_Add_Click(object sender, System.Windows.RoutedEventArgs e)
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
                        await this.ShowMessageAsync("Notification", "Spotify is not connected. You need to connect to Spotify in order to fill the blacklist.");
                        return;
                    }


                    // Perform a search via the spotify API
                    SpotifyAPI.Web.Models.SearchItem searchItem = ApiHandler.GetArtist(search);
                    if (searchItem.Artists.Items.Count <= 0)
                        return;

                    SpotifyAPI.Web.Models.FullArtist fullartist = searchItem.Artists.Items[0];

                    foreach (object item in ListView_Blacklist.Items)
                    {
                        if (item.ToString() == fullartist.Name)
                        {
                            return;
                        }
                    }
                    ListView_Blacklist.Items.Add(fullartist.Name);
                    break;
                case 1:
                    ListView_UserBlacklist.Items.Add(search);
                    break;
            }

            SaveBlacklist();
        }

        private void SaveBlacklist()
        {
            //Artist Blacklist
            string s = "";
            if (ListView_Blacklist.Items.Count > 0)
            {
                foreach (object item in ListView_Blacklist.Items)
                {
                    if ((string)item != "")
                    {
                        s += item + Splitter;
                    }
                }
                s = s.Remove(s.Length - Splitter.Length);
            }
            Settings.ArtistBlacklist = s;

            //User Blacklist
            s = "";
            if (ListView_UserBlacklist.Items.Count > 0)
            {
                foreach (object item in ListView_UserBlacklist.Items)
                {
                    if ((string)item != "")
                    {
                        s += item + Splitter;
                    }
                }
                s = s.Remove(s.Length - Splitter.Length);
            }
            Settings.UserBlacklist = s;

            LoadBlacklists();
        }

        private async void btn_Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // after user confirmation clear the list
            switch (cbx_Type.SelectedIndex)
            {
                case 0:
                    MessageDialogResult msgResult = await this.ShowMessageAsync("Notification", "Do you really want to clear the Artist blacklist?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        Settings.ArtistBlacklist = "";
                        ListView_Blacklist.Items.Clear();
                    }
                    break;
                case 1:
                    msgResult = await this.ShowMessageAsync("Notification", "Do you really want to clear the User blacklist?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        Settings.UserBlacklist = "";
                        ListView_UserBlacklist.Items.Clear();
                    }
                    break;
            }

        }

        private async void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem mnu = sender as MenuItem;
            ListBox listView;

            if (mnu == null)
            {
                return;
            }

            listView = ((ContextMenu)mnu.Parent).PlacementTarget as ListBox;

            // right-click context menu to delete single blacklist entries
            if (listView != null && listView.SelectedItem == null)
                return;

            if (listView != null)
            {
                MessageDialogResult msgResult = await this.ShowMessageAsync("Notification", "Delete " + listView.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    listView.Items.Remove(listView.SelectedItem);
                    SaveBlacklist();
                }
            }
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
                MessageDialogResult msgResult = await this.ShowMessageAsync("Notification", "Delete " + ListView_Blacklist.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
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
                MessageDialogResult msgResult = await this.ShowMessageAsync("Notification", "Delete " + ListView_UserBlacklist.SelectedItem + "?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    ListView_UserBlacklist.Items.Remove(ListView_UserBlacklist.SelectedItem);
                    SaveBlacklist();
                }
            }
        }
    }
}
