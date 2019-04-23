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
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        private Window mW;

        private readonly string[] _colors = new string[]
                                       {
                                                   "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
                                                   "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
                                                   "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve",
                                                   "Taupe", "Sienna"
                                       };

        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
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

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void BtnUpdatesClick(object sender, RoutedEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() != typeof(MainWindow)) continue;
                if (!((MainWindow)window).Worker_Update.IsBusy)
                {
                    ((MainWindow)window).Worker_Update.RunWorkerAsync();
                }
            }
        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SetCustomPauseText(TxtbxCustompausetext.Text);
        }

        private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        {
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

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            //Settings.SetTheme(ThemeToggleSwitch.IsChecked == true ? "BaseDark" : "BaseLight");
            if ((bool)ThemeToggleSwitch.IsChecked)
            {
                Settings.SetTheme("BaseDark");
                //(mW as MainWindow).cbx_Source.Foreground = Brushes.White;
            }
            else
            {
                Settings.SetTheme("BaseLight");
                //(mW as MainWindow).cbx_Source.Foreground = Brushes.Black;

            }

            ThemeHandler.ApplyTheme();
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            var isChecked = this.ChbxMinimizeSystray.IsChecked;
            Settings.SetSystray(isChecked != null && (bool)isChecked);
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            this._fbd.Description = @"Path where the text file will be located.";
            this._fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (this._fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            this.TxtbxOutputdirectory.Text = this._fbd.SelectedPath;
            Settings.SetDirectory(this._fbd.SelectedPath);
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            var chbxAutostartIsChecked = this.ChbxAutostart.IsChecked;
            MainWindow.RegisterInStartup(chbxAutostartIsChecked != null && (bool)chbxAutostartIsChecked);
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    mW = window;
                }
            }

            foreach (var s in _colors)
            {
                ComboBoxColor.Items.Add(s);
            }

            foreach (string s in ComboBoxColor.Items)
            {
                if (s != Settings.GetColor()) continue;
                ComboBoxColor.SelectedItem = s;
                Settings.SetColor(s);
            }

            SetControls();
        }

        public void SetControls()
        {
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

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SetOutputString(TxtbxOutputformat.Text);
        }

        private void MenuBtnArtist_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{artist}");
        }

        private void MenuBtnTitle_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{title}");

        }

        private void MenuBtnExtra_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{extra}");
        }

        private void AppendText(System.Windows.Controls.TextBox tb, string text)
        {
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }

        private void ChbxTelemetry_IsCheckedChanged(object sender, EventArgs e)
        {
            Settings.SetTelemetry((bool)ChbxTelemetry.IsChecked);
        }

        private void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigHandler.SaveConfig();
        }

        private void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigHandler.LoadConfig();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://api.nightbot.tv/oauth2/authorize?response_type=code&client_id=f212248f12f5ca01838dcdc009578906&redirect_uri=https://bloemacher.com&scope=song_requests_queue");
        }

        private void txtbx_nbuser_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SetNBUser(txtbx_nbuser.Text);
        }

        private void btn_nblink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string js = "";
                using (WebClient wc = new WebClient())
                {
                    js = wc.DownloadString("https://api.nightbot.tv/1/channels/t/" + Settings.GetNBUser());
                }
                var serializer = new JsonSerializer();
                NBObj json = JsonConvert.DeserializeObject<NBObj>(js);
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

        public class NBObj
        {
            public dynamic channel { get; set; }
            public string status { get; set; }
        }

        private void BtnCopyURL_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject("http://songify.bloemacher.com/getsong.php?id="+Settings.GetUUID());
            Notification.ShowNotification("Link copied to clipboard", "s");
        }

        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            Settings.SetUpload((bool)ChbxUpload.IsChecked);
        }
    }
}