using AutoUpdaterDotNET;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using TextBox = System.Windows.Controls.TextBox;

namespace Songify_Slim
{
    // ReSharper disable once InconsistentNaming
    public partial class Window_Settings
    {
        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();
        private Window _mW;
        private readonly bool _appIdInitialValue = Settings.UseOwnApp;

        public Window_Settings()
        {
            InitializeComponent();
            Title = Properties.Resources.mw_menu_Settings;
        }

        public void SetControls()
        {
            // Sets all the controls from settings
            ThemeToggleSwitch.IsOn = Settings.Theme == "BaseDark" || Settings.Theme == "Dark";
            TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException();
            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;
            ChbxAutostart.IsOn = Settings.Autostart;
            ChbxMinimizeSystray.IsOn = Settings.Systray;
            ChbxCustomPause.IsOn = Settings.CustomPauseTextEnabled;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            ChbxUpload.IsOn = Settings.Upload;
            NudChrome.Value = Settings.ChromeFetchRate;
            ChbxCover.IsOn = Settings.DownloadCover;
            ChbxSplit.IsOn = Settings.SplitOutput;
            txtbx_twChannel.Text = Settings.TwChannel;
            txtbx_twOAuth.Password = Settings.TwOAuth;
            txtbx_twUser.Text = Settings.TwAcc;
            txtbx_RewardID.Text = Settings.TwRewardId;
            Chbx_TwReward.IsOn = Settings.TwSrReward;
            Chbx_TwCommand.IsOn = Settings.TwSrCommand;
            NudMaxReq.Value = Settings.TwSrMaxReq;
            NudCooldown.Value = Settings.TwSrCooldown;
            Chbx_MessageLogging.IsChecked = Settings.MsgLoggingEnabled;
            Chbx_TwAutoconnect.IsOn = Settings.TwAutoConnect;
            Chbx_AutoClear.IsOn = Settings.AutoClearQueue;
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            nud_Spaces.Value = Settings.SpaceCount;
            tb_ClientID.Text = Settings.ClientId;
            tb_ClientSecret.Password = Settings.ClientSecret;
            Tglsw_Spotify.IsOn = Settings.UseOwnApp;
            NudMaxlength.Value = Settings.MaxSongLength;
            tgl_AnnounceInChat.IsOn = Settings.AnnounceInChat;
            tgl_botcmd_next.IsOn = Settings.BotCmdNext;
            tgl_botcmd_pos.IsOn = Settings.BotCmdPos;
            tgl_botcmd_song.IsOn = Settings.BotCmdSong;
            
            if (ApiHandler.Spotify != null)
                lbl_SpotifyAcc.Content = Properties.Resources.sw_Integration_SpotifyLinked + " " +
                                         ApiHandler.Spotify.GetPrivateProfile().DisplayName;

            ThemeHandler.ApplyTheme();

            cbx_Language.SelectionChanged -= ComboBox_SelectionChanged;
            switch (Settings.Language)
            {
                case "en":
                    cbx_Language.SelectedIndex = 0;
                    break;
                case "de-DE":
                    cbx_Language.SelectedIndex = 1;
                    break;
                case "ru-RU":
                    cbx_Language.SelectedIndex = 2;
                    break;
                case "es":
                    cbx_Language.SelectedIndex = 3;
                    break;
                case "fr":
                    cbx_Language.SelectedIndex = 4;
                    break;
            }

            cbx_Language.SelectionChanged += ComboBox_SelectionChanged;
        }

        private void AppendText(TextBox tb, string text)
        {
            // Appends Rightclick-Text from the output text box (parameters)
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            if (tb.ContextMenu != null) tb.ContextMenu.IsOpen = false;
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

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            // Copies the txt path to the clipboard and shows a notification
            if (string.IsNullOrEmpty(Settings.Directory))
                Clipboard.SetDataObject(
                    Assembly.GetEntryAssembly()?.Location.Replace("Songify Slim.exe", "Songify.txt") ??
                    throw new InvalidOperationException());
            else
                Clipboard.SetDataObject(Settings.Directory + "\\Songify.txt");
            Lbl_Status.Content = @"Path copied to clipboard.";
        }

        private void BtnCopyURL_Click(object sender, RoutedEventArgs e)
        {
            // Copies the song info URL to the clipboard and shows notification
            Clipboard.SetDataObject("https://songify.rocks/getsong.php?id=" + Settings.Uuid);
            Lbl_Status.Content = @"URL copied to clipboard.";
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            // Where the user wants the text file to be saved in
            _fbd.Description = @"Path where the text file will be located.";
            _fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (_fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            TxtbxOutputdirectory.Text = _fbd.SelectedPath;
            Settings.Directory = _fbd.SelectedPath;
        }

        private void BtnUpdatesClick(object sender, RoutedEventArgs e)
        {
            // checks for updates
            AutoUpdater.Mandatory = true;
            AutoUpdater.UpdateMode = Mode.ForcedDownload;
            AutoUpdater.RunUpdateAsAdmin = false;

            AutoUpdater.Start("https://songify.rocks/update.xml");
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            // checkbox for autostart
            bool? chbxAutostartIsChecked = ChbxAutostart.IsOn;
            MainWindow.RegisterInStartup((bool)chbxAutostartIsChecked);
        }

        private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        {
            Settings.CustomPauseTextEnabled = ChbxCustomPause.IsOn;
            TxtbxCustompausetext.IsEnabled = ChbxCustomPause.IsOn;
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            // enables / disbales minimize to systray
            bool isChecked = ChbxMinimizeSystray.IsOn;
            Settings.Systray = isChecked;
        }

        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables upload
            Settings.Upload = ChbxUpload.IsOn;
            ((MainWindow)_mW).UploadSong(((MainWindow)_mW).CurrSong);
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // sets the color, when selecting yellow it changes foreground color because else its hard to read
            Settings.Color = (string)(ComboBoxColor.SelectedItem as ComboBoxItem)?.Content;
            ThemeHandler.ApplyTheme();
            if (Settings.Color != "Yellow")
            {
                ((MainWindow)_mW).LblStatus.Foreground = Brushes.White;
                ((MainWindow)_mW).LblCopyright.Foreground = Brushes.White;
            }
            else
            {
                ((MainWindow)_mW).LblStatus.Foreground = Brushes.Black;
                ((MainWindow)_mW).LblCopyright.Foreground = Brushes.Black;
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
            // assign mw to mainwindow for calling methods and setting texts etc
            foreach (Window window in Application.Current.Windows)
                if (window.GetType() == typeof(MainWindow))
                    _mW = window;



            // add colors to the combobox
            foreach (string s in ThemeManager.Current.ColorSchemes)
            {
                ComboBoxItem i = new ComboBoxItem
                {
                    Content = s,
                };

                var x = ThemeManager.Current.GetTheme(Settings.Theme + "." + s);
                if (x != null)
                {
                    SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(x.PrimaryAccentColor.R, x.PrimaryAccentColor.G, x.PrimaryAccentColor.B));
                    i.BorderThickness = new Thickness(0, 0, 0, 2);
                    i.BorderBrush = brush;
                }
                ComboBoxColor.Items.Add(i);
            }

            // select the current color
            foreach (ComboBoxItem s in ComboBoxColor.Items)
            {
                if ((string)s.Content == Settings.Color)
                {
                    ComboBoxColor.SelectedItem = s;
                    Settings.Color = (string)s.Content;
                }
            }
            SetControls();
        }

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            // set the theme (BaseLight / BaseDark)
            Settings.Theme = ThemeToggleSwitch.IsOn ? "Dark" : "Light";

            ThemeHandler.ApplyTheme();

        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write CustomPausetext to settings
            Settings.CustomPauseText = TxtbxCustompausetext.Text;
        }

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write custom output format to settings
            Settings.OutputString = TxtbxOutputformat.Text;
        }

        private void NudChrome_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets the source (Spotify, Youtube, Nightbot)
            if (!IsLoaded)
                // This prevents that the selected is always 0 (initialize components)
                return;

            if (NudChrome.Value != null) Settings.ChromeFetchRate = (int)NudChrome.Value;
        }

        private void ChbxCover_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.DownloadCover = ChbxCover.IsOn;
        }

        private void btn_spotifyLink_Click(object sender, RoutedEventArgs e)
        {
            // Links Spotify
            Settings.RefreshToken = "";
            try
            {
                ApiHandler.DoAuthAsync();
                SetControls();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void ChbxSplit_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.SplitOutput = ChbxSplit.IsOn;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Opens twitchapps to generate a TMI oAuth Token
            Process.Start("https://twitchapps.com/tmi/");
        }

        private void txtbx_RewardID_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Saves the RewardID
            Settings.TwRewardId = txtbx_RewardID.Text;
        }

        private void Chbx_TwReward_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.TwSrReward = Chbx_TwReward.IsOn;
        }

        private void Chbx_TwCommand_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.TwSrCommand = Chbx_TwCommand.IsOn;
        }

        private void NudMaxReq_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets max requests per user value
            if (NudMaxReq.Value != null) Settings.TwSrMaxReq = (int)NudMaxReq.Value;
        }

        private void NudCooldown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets command cooldown
            if (NudCooldown.Value != null) Settings.TwSrCooldown = (int)NudCooldown.Value;
        }

        private void Chbx_TwAutoconnect_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to autoconnect or not
            Settings.TwAutoConnect = Chbx_TwAutoconnect.IsOn;
        }

        private void Chbx_MessageLogging_Checked(object sender, RoutedEventArgs e)
        {
            // Sets message loggint enabled or not
            if (Chbx_MessageLogging.IsChecked != null)
                Settings.MsgLoggingEnabled = (bool)Chbx_MessageLogging.IsChecked;
        }

        private void txtbx_twUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch acc
            Settings.TwAcc = txtbx_twUser.Text.Trim();
        }

        private void txtbx_twOAuth_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Sets the twitch oauth token
            Settings.TwOAuth = txtbx_twOAuth.Password;
        }

        private void txtbx_twChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch channel
            Settings.TwChannel = txtbx_twChannel.Text.Trim();
        }

        private void Chbx_AutoClear_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to clear the queue on startup or not
            Settings.AutoClearQueue = Chbx_AutoClear.IsOn;
        }

        private void MenuBtnReq_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{{requested by {req}}}");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbx_Language.SelectedIndex)
            {
                case 0:
                    // English
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                    Settings.Language = "en";
                    break;
                case 1:
                    // German
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
                    Settings.Language = "de-DE";
                    break;
                case 2:
                    // Russian
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                    Settings.Language = "ru-RU";
                    break;
                case 3:
                    // Spansih
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");
                    Settings.Language = "es";
                    break;
                case 4:
                    // Spansih
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");
                    Settings.Language = "fr";
                    break;
            }

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void btn_Botresponse_Click(object sender, RoutedEventArgs e)
        {
            Window_Botresponse wBr = new Window_Botresponse();
            wBr.Show();
        }

        private void nud_Spaces_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nud_Spaces.Value != null) Settings.SpaceCount = (int)nud_Spaces.Value;
        }

        private void ChbxSpaces_Checked(object sender, RoutedEventArgs e)
        {
            if (ChbxSpaces.IsChecked != null) Settings.AppendSpaces = (bool)ChbxSpaces.IsChecked;
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, EventArgs e)
        {
            Settings.UseOwnApp = Tglsw_Spotify.IsOn;
            //if (_appIdInitialValue != Settings.UseOwnApp)
            //{
            //    btn_save.Visibility = Visibility.Visible;
            //    lbl_savingRestart.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    btn_save.Visibility = Visibility.Hidden;
            //    lbl_savingRestart.Visibility = Visibility.Hidden;
            //}
        }

        private void tb_ClientID_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.ClientId = tb_ClientID.Text;
        }

        private void tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientSecret = tb_ClientSecret.Password;
        }

        private async void btn_save_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult msgResult = await this.ShowMessageAsync("Information",
                "The restart is only necessary if you switched the API clients.\n\nYou DO NOT have to do this when linking your account!",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "restart", NegativeButtonText = "cancel" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            Settings.AccessToken = "";
            Settings.RefreshToken = "";
            ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void btn_OwnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://songify.rocks/faq.html#appid");
        }

        private void NudMaxlength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (NudMaxlength.Value != null) Settings.MaxSongLength = (int)NudMaxlength.Value;
        }

        private async void Btn_ResetConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult msgResult = await this.ShowMessageAsync("Warning",
                "Are you sure you want to reset all settings?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            Properties.Settings.Default.Reset();
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            if (_appIdInitialValue == Settings.UseOwnApp) return;
            e.Cancel = true;
            Settings.AccessToken = "";
            Settings.RefreshToken = "";
            string temp = _appIdInitialValue == false ? "You switched from Songify's internal app-ID to your own. This is great because you won't get throttled by rate limits! \n\nIn order to use it though, Songify needs to be restarted and you have to relink with your Spotify account!" : "You switched from your own app-ID to Songify's internal one. This is bad and you will likely encounter problems. The API only allows a certain amount of requests done through an app. We have been exceeding this amount by a lot. Please use your own app-ID instead!\n\nSongify needs a restart and you have to link your Spotify account again.";

            MessageDialogResult msgResult = await this.ShowMessageAsync("Warning", temp, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AffirmativeButtonText = "Restart" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void tgl_AnnounceInChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AnnounceInChat = tgl_AnnounceInChat.IsOn;
        }

        private void tgl_botcmd_pos_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdPos = ((ToggleSwitch)sender).IsOn;
        }

        private void tgl_botcmd_song_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSong = ((ToggleSwitch)sender).IsOn;
        }

        private void tgl_botcmd_next_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdNext = ((ToggleSwitch)sender).IsOn;
        }
    }
}