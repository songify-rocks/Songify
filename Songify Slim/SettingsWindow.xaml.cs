using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace Songify_Slim
{
    public partial class SettingsWindow : MetroWindow
    {
        private readonly string[] _colors = new string[]
                                       {
                                                   "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
                                                   "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
                                                   "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve",
                                                   "Taupe", "Sienna"
                                       };

        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();
        private Window mW;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        public void SetControls()
        {
            // Sets all the controls from settings
            ThemeToggleSwitch.IsChecked = Settings.GetTheme() == "BaseDark";
            TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly().Location;
            if (!string.IsNullOrEmpty(Settings.GetDirectory()))
                TxtbxOutputdirectory.Text = Settings.GetDirectory();
            ChbxAutostart.IsChecked = Settings.GetAutostart();
            ChbxMinimizeSystray.IsChecked = Settings.GetSystray();
            ChbxCustomPause.IsChecked = Settings.GetCustomPauseTextEnabled();
            ChbxTelemetry.IsChecked = Settings.GetTelemetry();
            TxtbxCustompausetext.Text = Settings.GetCustomPauseText();
            TxtbxOutputformat.Text = Settings.GetOutputString();
            txtbx_nbuser.Text = Settings.GetNBUser();
            ChbxUpload.IsChecked = Settings.GetUpload();
            if (Settings.GetNBUserID() != null)
            {
                lbl_nightbot.Content = "Nightbot (ID: " + Settings.GetNBUserID() + ")";
            }
            ThemeHandler.ApplyTheme();
        }

        private void AppendText(System.Windows.Controls.TextBox tb, string text)
        {
            // Appends Rightclick-Text from the output text box (parameters)
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }

        private void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler
            ConfigHandler.SaveConfig();
        }

        private void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler
            ConfigHandler.LoadConfig();
        }

        private void Btn_nblink_Click(object sender, RoutedEventArgs e)
        {
            // Links the nightbot account using username
            try
            {
                // accessing nightbot API with username to get user id
                string jsn = "";
                using (WebClient wc = new WebClient())
                {
                    jsn = wc.DownloadString("https://api.nightbot.tv/1/channels/t/" + Settings.GetNBUser());
                }
                var serializer = new JsonSerializer();
                NBObj json = JsonConvert.DeserializeObject<NBObj>(jsn);
                string temp = json.channel._id;
                temp = temp.Replace("{", "").Replace("}", "");
                Settings.SetNBUserID(temp);
                Notification.ShowNotification("Nightbot account linked", "s");

            }
            catch
            {
                Notification.ShowNotification("Unable to link account", "e");
            }

            SetControls();
        }

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            // Copies the txt path to the clipboard and shows a notification
            if (string.IsNullOrEmpty(Settings.GetDirectory()))
            {
                System.Windows.Clipboard.SetDataObject(Assembly.GetEntryAssembly().Location.Replace("Songify Slim.exe", "Songify.txt"));
            }
            else
            {
                System.Windows.Clipboard.SetDataObject(Settings.GetDirectory() + "\\Songify.txt");
            }
            (mW as MainWindow).LblStatus.Content = @"Path copied to clipboard.";
            Notification.ShowNotification("Path saved to clipboard.", "s");
        }

        private void BtnCopyURL_Click(object sender, RoutedEventArgs e)
        {
            // Copies the song info URL to the clipboard and shows notification
            System.Windows.Clipboard.SetDataObject("http://songify.bloemacher.com/getsong.php?id=" + Settings.GetUUID());
            Notification.ShowNotification("Link copied to clipboard", "s");
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            // Where the user wants the text file to be saved in
            this._fbd.Description = @"Path where the text file will be located.";
            this._fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (this._fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            this.TxtbxOutputdirectory.Text = this._fbd.SelectedPath;
            Settings.SetDirectory(this._fbd.SelectedPath);
        }

        private void BtnUpdatesClick(object sender, RoutedEventArgs e)
        {
            // checks for updates
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() != typeof(MainWindow)) continue;
                if (!((MainWindow)window).Worker_Update.IsBusy)
                {
                    ((MainWindow)window).Worker_Update.RunWorkerAsync();
                }
            }
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            // checkbox for autostart
            var chbxAutostartIsChecked = this.ChbxAutostart.IsChecked;
            MainWindow.RegisterInStartup(chbxAutostartIsChecked != null && (bool)chbxAutostartIsChecked);
        }

        private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        {
            // enables / disables custom pause
            Settings.SetCustomPauseTextEnabled((bool)ChbxCustomPause.IsChecked);
            if (!(bool)ChbxCustomPause.IsChecked)
            {
                TxtbxCustompausetext.IsEnabled = false;
            }
            else
            {
                TxtbxCustompausetext.IsEnabled = true;
            }
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            // enables / disbales minimize to systray
            var isChecked = this.ChbxMinimizeSystray.IsChecked;
            Settings.SetSystray(isChecked != null && (bool)isChecked);
        }

        private void ChbxTelemetry_IsCheckedChanged(object sender, EventArgs e)
        {
            // enables / disables telemetry
            Settings.SetTelemetry((bool)ChbxTelemetry.IsChecked);
        }

        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables upload
            Settings.SetUpload((bool)ChbxUpload.IsChecked);
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // sets the color, when selecting yellow it changes foreground color because else its hard to read
            Settings.SetColor(this.ComboBoxColor.SelectedValue.ToString());
            ThemeHandler.ApplyTheme();
            if (Settings.GetColor() != "Yellow")
            {
                (mW as MainWindow).LblStatus.Foreground = Brushes.White;
                (mW as MainWindow).LblCopyright.Foreground = Brushes.White;
            }
            else
            {
                (mW as MainWindow).LblStatus.Foreground = Brushes.Black;
                (mW as MainWindow).LblCopyright.Foreground = Brushes.Black;
            }
        }

        private void MenuBtnArtist_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{artist}");
        }

        private void MenuBtnExtra_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{extra}");
        }

        private void MenuBtnTitle_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{title}");
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // assing mw to mainwindow for calling methods and setting texts etc
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    mW = window;
                }
            }

            // add colors to the combobox
            foreach (var s in _colors)
            {
                ComboBoxColor.Items.Add(s);
            }

            // select the current color
            foreach (string s in ComboBoxColor.Items)
            {
                if (s != Settings.GetColor()) continue;
                ComboBoxColor.SelectedItem = s;
                Settings.SetColor(s);
            }

            SetControls();
        }

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            // set the theme (BaseLight / BaseDark)
            if ((bool)ThemeToggleSwitch.IsChecked)
            {
                Settings.SetTheme("BaseDark");
            }
            else
            {
                Settings.SetTheme("BaseLight");

            }

            ThemeHandler.ApplyTheme();
        }

        private void Txtbx_nbuser_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write Nightbot username to settings
            Settings.SetNBUser(txtbx_nbuser.Text);
        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write CustomPausetext to settings
            Settings.SetCustomPauseText(TxtbxCustompausetext.Text);
        }

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write custom output format to settings
            Settings.SetOutputString(TxtbxOutputformat.Text);
        }

        // nightbot JSON object
        public class NBObj
        {
            public dynamic channel { get; set; }
            public string status { get; set; }
        }
    }
}