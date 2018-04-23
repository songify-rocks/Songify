using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using SpotifyAPI.Local; //Base Namespace
using SpotifyAPI.Local.Models; //Models for the JSON-responses
using System;
using MahApps.Metro;
using System.IO;
using System.Threading.Tasks;

namespace Songify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private SpotifyLocalAPI _spotify;
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
        private string artist, title, album;

        public MainWindow()
        {
            InitializeComponent();
            _spotify = new SpotifyLocalAPI();
            _spotify.ListenForEvents = true;
            _spotify.OnPlayStateChange += OnPlayStateChange;
            _spotify.OnTrackChange += OnTrackChange;
        }

        private void txtbx_customoutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = txtbx_customoutput.Text;
            txt = txt.Replace("{artist}", artist);
            txt = txt.Replace("{title}", title);
            txt = txt.Replace("{album}", album);

            lbl_livepreview.Content = txt;
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            flyout_About.IsOpen = false;
            if (flyout_Settings.IsOpen)
                flyout_Settings.IsOpen = false;
            else
                flyout_Settings.IsOpen = true;
        }

        private void Sognify_MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            #region Theme

            foreach (string s in colors)
            {
                ComboBox_Color.Items.Add(s);
            }

            foreach (string s in ComboBox_Color.Items)
            {
                if (s == Settings.GetColor())
                {
                    ComboBox_Color.SelectedItem = s;
                }
            }
            if (Settings.GetTheme() == "BaseDark") { themeToggleSwitch.IsChecked = true; } else { themeToggleSwitch.IsChecked = false; }
            ApplyTheme();

            #endregion Theme

            #region Settings

            if (Settings.GetDirectory() == "")
                Settings.SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            txtbx_customoutput.Text = Settings.GetCustomOutput();
            Txtbx_outputdirectory.Text = Settings.GetDirectory();

            #endregion Settings

            startSpotify();
        }

        private async void startSpotify()
        {
            try
            {
                await Task.Run(() => getSpotifyInfo());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ApplyTheme()
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Settings.GetColor()), ThemeManager.GetAppTheme(Settings.GetTheme()));
        }

        public async void getSpotifyInfo()
        {
            StatusResponse status = null;

            if (!SpotifyLocalAPI.IsSpotifyRunning())
                return; //Make sure the spotify client is running
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                return; //Make sure the WebHelper is running
            try
            {
                if (!_spotify.Connect())
                    return; //We need to call Connect before fetching infos, this will handle Auth stuff
            }
            catch (Exception ex)
            {
                // Could not Connect to the webservice
                Console.WriteLine(ex.Message);
            }
            status = _spotify.GetStatus(); //status contains infos

            if (status != null)
            {
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    artist = status.Track.ArtistResource.Name;
                    title = status.Track.TrackResource.Name;
                    album = status.Track.AlbumResource.Name;
                    txtbx_customoutput.AppendText("%TEMP%");
                    txtbx_customoutput.Text = txtbx_customoutput.Text.Replace("%TEMP%", "");
                    Lbl_Artist.Content = artist;
                    Lbl_Song.Content = title;
                    Lbl_Album.Content = album;
                    File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomOutput().Replace("{artist}", artist).Replace("{title}", title).Replace("{album}", album));
                }
                ));
            }
        }

        public void OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            startSpotify();
        }

        private void OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            startSpotify();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(ComboBox_Color.SelectedValue.ToString());
            ApplyTheme();
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetCustomOutputy(txtbx_customoutput.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            startSpotify();
        }

        private void btn_About_Click(object sender, RoutedEventArgs e)
        {
            flyout_Settings.IsOpen = false;

            if (flyout_About.IsOpen)
                flyout_About.IsOpen = false;
            else
                flyout_About.IsOpen = true;
        }

        private void btn_Donate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/inzaniity");
        }

        private void btn_Discord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/hB5hK");
        }

        private void MenuItem_InsertArtist_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{artist}");
        }

        private void MenuItem_InsertTitle_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{title}");
        }

        private void MenuItem_InsertAlbum_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{album}");
        }

        private void AppendText(TextBox tb, string text)
        {
            tb.AppendText(text);
            tb.Select(txtbx_customoutput.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }

        private void themeToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if (themeToggleSwitch.IsChecked == true)
            {
                Settings.SetTheme("BaseDark");
            }
            else
            {
                Settings.SetTheme("BaseLight");
            }
            ApplyTheme();
        }

        private void Btn_Outputdirectory_Click(object sender, RoutedEventArgs e)
        {
            fbd.Description = "Path where the text file should be";
            fbd.SelectedPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            Txtbx_outputdirectory.Text = fbd.SelectedPath;
            Settings.SetDirectory(fbd.SelectedPath);
        }
    }
}