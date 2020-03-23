
using System;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für Window_Blacklist.xaml
    /// </summary>
    public partial class Window_Blacklist
    {

        public static string[] Blacklist;
        public string splitter = "|||";

        public Window_Blacklist()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadBlacklist();
        }

        private void LoadBlacklist()
        {
            //Loads the Blacklist from the user settings

            ListView_Blacklist.Items.Clear();

            Blacklist = Settings.ArtistBlacklist.Split(new[] { splitter }, StringSplitOptions.None);

            foreach (string s in Blacklist)
            {
                ListView_Blacklist.Items.Add(s);
            }
        }

        private void btn_Add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //This adds to the blacklist. 
            addToBlacklist(tb_Blacklist.Text);
            tb_Blacklist.Text = "";
        }

        private void addToBlacklist(string search)
        {
            //Check if the string is empty
            if (string.IsNullOrEmpty(search))
                return;

            // Perform a search via the spotify API
            SpotifyAPI.Web.Models.SearchItem searchItem = APIHandler.GetArtist(search);
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

            SaveBlacklist();
        }

        private void SaveBlacklist()
        {
            string s = "";
            if (ListView_Blacklist.Items.Count > 0)
            {
                foreach (object item in ListView_Blacklist.Items)
                {
                    if ((string)item != "")
                    {
                        s += item + splitter;
                    }
                }
                s = s.Remove(s.Length - 3);
            }

            Settings.ArtistBlacklist = s;
            LoadBlacklist();
        }

        private void btn_Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Settings.ArtistBlacklist = "";
            ListView_Blacklist.Items.Clear();
        }

        private void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ListView_Blacklist.Items.Remove(ListView_Blacklist.SelectedItem);
        }

        private void tb_Blacklist_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                addToBlacklist(tb_Blacklist.Text);
                tb_Blacklist.Text = "";
            }
        }
    }
}
