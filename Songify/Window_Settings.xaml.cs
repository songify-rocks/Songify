using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Songify
{
    /// <summary>
    /// Interaction logic for Window_Settings.xaml
    /// </summary>
    public partial class Window_Settings : MetroWindow
    {
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

        public Window_Settings()
        {
            InitializeComponent();
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

        private void Btn_CopyToClip_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(Settings.GetDirectory() + "\\Songify.txt");

            Notification.showNotification("Copied path to clipboard.", "s");
        }

        private void PausetextToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)pausetextToggleSwitch.IsChecked)
                Settings.SetCustomPauseEnabled(true);
            else
                Settings.SetCustomPauseEnabled(false);
        }

        private void DownloadAlbumArtToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)downloadAlbumArtToggleSwitch.IsChecked)
                Settings.SetDownloadAlbumArt(true);
            else
                Settings.SetDownloadAlbumArt(false);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(ComboBox_Color.SelectedValue.ToString());
            ThemeHandler.ApplyTheme();
        }

        private void ThemeToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if (themeToggleSwitch.IsChecked == true)
            {
                Settings.SetTheme("BaseDark");
            }
            else
            {
                Settings.SetTheme("BaseLight");
            }
            ThemeHandler.ApplyTheme();
        }

        private void AlbumCoverToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            Settings.SetShowAlbumArt((bool)albumCoverToggleSwitch.IsChecked);
            if (Settings.GetShowAlbumArt())
                warningLabel.Visibility = Visibility.Visible;
            else
            {
                warningLabel.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetCustomOutputy(txtbx_customoutput.Text);
            Settings.SetCustomPauseText(Txtbx_Pausetext.Text);
            Settings.SetProxyHost(Txtbx_ProxyHost.Text);
            Settings.SetProxyPort(Txtbx_ProxyPort.Text);
            Settings.SetProxyUser(Txtbx_ProxyUsername.Text);
            Settings.SetProxyPass(Txtbx_ProxyPassword.Password);
            Notification.showNotification("Saved!", "s");
        }

        private void Txtbx_customoutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = txtbx_customoutput.Text;
            txt = txt.Replace("{artist}", MainWindow.artist);
            txt = txt.Replace("{title}", MainWindow.title);
            txt = txt.Replace("{album}", MainWindow.album);
            lbl_livepreview.Content = txt;
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

        private void Lbl_OAuth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitchapps.com/tmi/");
        }

        private void AppendText(TextBox tb, string text)
        {
            tb.AppendText(text);
            tb.Select(txtbx_customoutput.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }

        private void Window_Settings1_Loaded(object sender, RoutedEventArgs e)
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
            ThemeHandler.ApplyTheme();

            #endregion Theme

            #region Settings

            if (Settings.GetDirectory() == "")
                Settings.SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            txtbx_customoutput.Text = Settings.GetCustomOutput();
            Txtbx_outputdirectory.Text = Settings.GetDirectory();
            albumCoverToggleSwitch.IsChecked = Settings.GetShowAlbumArt();
            if (Settings.GetShowAlbumArt())
                warningLabel.Visibility = Visibility.Visible;
            else
                warningLabel.Visibility = Visibility.Hidden;
            pausetextToggleSwitch.IsChecked = Settings.GetCustomPauseEnabled();
            downloadAlbumArtToggleSwitch.IsChecked = Settings.GetDownloadAlbumArt();
            deleteAlbumArtOnpauseToggleSwitch.IsChecked = Settings.GetDeleteAlbumArtOnpause();
            Txtbx_Pausetext.Text = Settings.GetCustomPauseText();
            Txtbx_ProxyHost.Text = Settings.GetProxyHost();
            Txtbx_ProxyPort.Text = Settings.GetProxyPort();
            Txtbx_ProxyUsername.Text = Settings.GetProxyUser();
            Txtbx_ProxyPassword.Password = Settings.GetProxyPass();


            #endregion Settings
        }

        private void deleteAlbumArtOnpauseToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)deleteAlbumArtOnpauseToggleSwitch.IsChecked)
                Settings.SetDeleteAlbumArtOnpause(true);
            else
                Settings.SetDeleteAlbumArtOnpause(false);
        }
    }
}