using AutoUpdaterDotNET;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Songify_Slim.UserControls;
using Songify_Slim.Util.General;
using SpotifyAPI.Web.Models;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.PubSub.Models.Responses.Messages.AutomodCaughtMessage;
using Application = System.Windows.Application;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using MenuItem = System.Windows.Controls.MenuItem;
using NumericUpDown = MahApps.Metro.Controls.NumericUpDown;
using TextBox = System.Windows.Controls.TextBox;

namespace Songify_Slim
{
    // ReSharper disable once InconsistentNaming
    public partial class Window_Settings
    {
        private readonly bool _appIdInitialValue = Settings.UseOwnApp;
        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();
        private Window _mW;
        private List<CustomReward> CustomRewardsManagable = new List<CustomReward>();
        private List<CustomReward> CustomRewards = new List<CustomReward>();
        private List<int> refundConditons = new List<int>();

        private enum RewardActions
        {
            SongRequest,
            Skip,
            PlayThisSongNow
        }

        public Window_Settings()
        {
            InitializeComponent();
            Title = Properties.Resources.mw_menu_Settings;
        }

        public async void SetControls()
        {
            // Add TwitchHandler.TwitchUserLevels values to the combobox CbxUserLevels
            CbxUserLevels.Items.Clear();
            Array values = Enum.GetValues(typeof(TwitchHandler.TwitchUserLevels));
            foreach (var value in values)
            {
                switch (value.ToString())
                {
                    case "Broadcaster":
                        continue;
                    case "Everyone":
                        CbxUserLevels.Items.Add(value);
                        CbxUserLevelsMaxReq.Items.Add("Viewer (non vip/sub)");
                        continue;
                    default:
                        CbxUserLevels.Items.Add(value);
                        CbxUserLevelsMaxReq.Items.Add(value);
                        break;
                }
            }

            // Sets all the controls from settings
            ThemeToggleSwitch.IsOn = Settings.Theme == "BaseDark" || Settings.Theme == "Dark";
            TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException();
            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;
            Chbx_AutoClear.IsOn = Settings.AutoClearQueue;
            Chbx_MessageLogging.IsChecked = Settings.MsgLoggingEnabled;
            Chbx_TwAutoconnect.IsOn = Settings.TwAutoConnect;
            Chbx_TwCommand.IsOn = Settings.TwSrCommand;
            Chbx_TwReward.IsOn = Settings.TwSrReward;
            ChbxAutostart.IsOn = Settings.Autostart;
            ChbxCover.IsOn = Settings.DownloadCover;
            ChbxCustomPause.IsOn = Settings.CustomPauseTextEnabled;
            ChbxMinimizeSystray.IsOn = Settings.Systray;
            ChbxOpenQueueOnStartup.IsOn = Settings.OpenQueueOnStartup;
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            ChbxSplit.IsOn = Settings.SplitOutput;
            ChbxUpload.IsOn = Settings.Upload;
            nud_Spaces.Value = Settings.SpaceCount;
            NudChrome.Value = Settings.ChromeFetchRate;
            NudCooldown.Value = Settings.TwSrCooldown;
            NudMaxlength.Value = Settings.MaxSongLength;
            tb_ClientID.Text = Settings.ClientId;
            tb_ClientSecret.Password = Settings.ClientSecret;
            tgl_AnnounceInChat.IsOn = Settings.AnnounceInChat;
            Tglsw_Spotify.IsOn = Settings.UseOwnApp;
            txtbx_RewardID.Text = Settings.TwRewardId;
            txtbx_twChannel.Text = Settings.TwChannel;
            txtbx_twOAuth.Password = Settings.TwOAuth;
            txtbx_twUser.Text = Settings.TwAcc;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            TxtbxOutputformat2.Text = Settings.OutputString2;
            CbxUserLevels.SelectedIndex = Settings.TwSrUserLevel == -1 ? 0 : Settings.TwSrUserLevel;
            NudServerPort.Value = Settings.WebServerPort;
            TglAutoStartWebserver.IsOn = Settings.AutoStartWebServer;
            TglBetaUpdates.IsOn = Settings.BetaUpdates;
            tgl_OnlyWorkWhenLive.IsOn = Settings.BotOnlyWorkWhenLive;

            BtnWebserverStart.Content = GlobalObjects.WebServer.run ? "Stop WebServer" : "Start WebServer";


            if (ApiHandler.Spotify != null)
            {
                try
                {
                    PrivateProfile profile = await ApiHandler.Spotify.GetPrivateProfileAsync();
                    lbl_SpotifyAcc.Content = $"{Properties.Resources.sw_Integration_SpotifyLinked} {profile.DisplayName}";
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    if (profile.Images[0].Url != null) bitmap.UriSource = new Uri(profile.Images[0].Url, UriKind.Absolute);
                    bitmap.EndInit();
                    ImgSpotifyProfile.ImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }


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

            if (TwitchHandler.TokenCheck != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                if (Settings.TwitchUser.ProfileImageUrl != null) bitmap.UriSource = new Uri(Settings.TwitchUser.ProfileImageUrl, UriKind.Absolute);
                bitmap.EndInit();
                ImgTwitchProfile.ImageSource = bitmap;
                lblTwitchName.Text = Settings.TwitchUser.DisplayName;
                BtnLogInTwitch.Visibility = Visibility.Collapsed;
                PnlTwich.Visibility = Visibility.Visible;

                CbxRewards.Items.Clear();
                CbxRewards.SelectionChanged -= CbxRewards_OnSelectionChanged;
                foreach (CustomReward reward in await TwitchHandler.GetChannelRewards(false))
                {
                    ComboBoxItem item = new ComboBoxItem()
                    {
                        Content = new UC_RewardItem(reward)
                    };
                    CbxRewards.Items.Add(item);
                    if (txtbx_RewardID.Text == reward.Id)
                        CbxRewards.SelectedItem = item;
                }
                CbxRewards.SelectionChanged += CbxRewards_OnSelectionChanged;
            }
            else
            {
                PnlTwich.Visibility = Visibility.Collapsed;
                BtnLogInTwitch.Visibility = Visibility.Visible;
            }

            if (Settings.RefundConditons == null) return;
            foreach (int conditon in Settings.RefundConditons)
            {
                foreach (UIElement child in GrdTwitchReward.Children)
                {
                    if (child is CheckBox box && box.Name.StartsWith("ChkRefund") && box.Tag.ToString() == conditon.ToString())
                    {
                        box.IsChecked = true;
                    }
                }
            }
        }

        private void AppendText(string s, string text)
        {
            TextBox tb = null;
            switch (s)
            {
                case "1":
                    tb = TxtbxOutputformat;
                    break;
                case "2":
                    tb = TxtbxOutputformat2;
                    break;
            }

            // Appends Rightclick-Text from the output text box (parameters)
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            if (tb.ContextMenu != null) tb.ContextMenu.IsOpen = false;
        }

        private void btn_Botresponse_Click(object sender, RoutedEventArgs e)
        {
            Window_Botresponse wBr = new Window_Botresponse();
            wBr.Show();
        }

        private void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler
            ConfigHandler.WriteAllConfig(Settings.Export());
        }

        private void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler
            ConfigHandler.LoadConfig();
        }

        private void btn_OwnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://songify.rocks/faq.html#appid");
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

        private void btn_spotifyLink_Click(object sender, RoutedEventArgs e)
        {
            // Links Spotify
            Settings.SpotifyRefreshToken = "";
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

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            // Copies the txt path to the clipboard and shows a notification
            if (string.IsNullOrEmpty(Settings.Directory))
                Clipboard.SetDataObject(
                    Assembly.GetEntryAssembly()?.Location.Replace("Songify Slim.exe", "Songify.txt") ??
                    throw new InvalidOperationException());
            else
                Clipboard.SetDataObject(Settings.Directory + "\\Songify.txt");
        }

        private void BtnCopyURL_Click(object sender, RoutedEventArgs e)
        {
            // Copies the song info URL to the clipboard and shows notification
            Clipboard.SetDataObject("https://songify.rocks/getsong.php?id=" + Settings.Uuid);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Opens twitchapps to generate a TMI oAuth Token
            Process.Start("https://twitchapps.com/tmi/");
        }

        private void Chbx_AutoClear_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to clear the queue on startup or not
            Settings.AutoClearQueue = Chbx_AutoClear.IsOn;
        }

        private void Chbx_MessageLogging_Checked(object sender, RoutedEventArgs e)
        {
            // Sets message loggint enabled or not
            if (Chbx_MessageLogging.IsChecked != null)
                Settings.MsgLoggingEnabled = (bool)Chbx_MessageLogging.IsChecked;
        }

        private void Chbx_TwAutoconnect_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to autoconnect or not
            Settings.TwAutoConnect = Chbx_TwAutoconnect.IsOn;
        }

        private void Chbx_TwCommand_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.TwSrCommand = Chbx_TwCommand.IsOn;
        }

        private void Chbx_TwReward_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.TwSrReward = Chbx_TwReward.IsOn;
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            // checkbox for autostart
            bool? chbxAutostartIsChecked = ChbxAutostart.IsOn;
            MainWindow.RegisterInStartup((bool)chbxAutostartIsChecked);
        }

        private void ChbxCover_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.DownloadCover = ChbxCover.IsOn;
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

        private void ChbxOpenQueueOnStartup_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.OpenQueueOnStartup = ((ToggleSwitch)sender).IsOn;
        }

        private void ChbxSpaces_Checked(object sender, RoutedEventArgs e)
        {
            if (ChbxSpaces.IsChecked != null) Settings.AppendSpaces = (bool)ChbxSpaces.IsChecked;
        }

        private void ChbxSplit_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.SplitOutput = ChbxSplit.IsOn;
        }

        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables upload
            Settings.Upload = ChbxUpload.IsOn;
            ((MainWindow)_mW).UploadSong(((MainWindow)_mW).CurrSong);
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
            AppendText((sender as MenuItem)?.Tag.ToString(), "{artist}");
            // appends text
        }

        private void MenuBtnExtra_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText((sender as MenuItem)?.Tag.ToString(), "{extra}");
        }

        private void MenuBtnReq_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText((sender as MenuItem)?.Tag.ToString(), "{{requested by {req}}}");
        }

        private void MenuBtnTitle_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText((sender as MenuItem)?.Tag.ToString(), "{title}");
        }

        private void MenuBtnUrl_Click(object sender, RoutedEventArgs e)
        {
            AppendText((sender as MenuItem)?.Tag.ToString(), "{url}");
        }

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            ConfigHandler.WriteAllConfig(Settings.Export());
            if (_appIdInitialValue == Settings.UseOwnApp) return;
            e.Cancel = true;
            Settings.SpotifyAccessToken = "";
            Settings.SpotifyRefreshToken = "";
            string temp = _appIdInitialValue == false ? "You switched from Songify's internal app-ID to your own. This is great because you won't get throttled by rate limits! \n\nIn order to use it though, Songify needs to be restarted and you have to relink with your Spotify account!" : "You switched from your own app-ID to Songify's internal one. This is bad and you will likely encounter problems. The API only allows a certain amount of requests done through an app. We have been exceeding this amount by a lot. Please use your own app-ID instead!\n\nSongify needs a restart and you have to link your Spotify account again.";

            MessageDialogResult msgResult = await this.ShowMessageAsync("Warning", temp, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AffirmativeButtonText = "Restart" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void nud_Spaces_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nud_Spaces.Value != null) Settings.SpaceCount = (int)nud_Spaces.Value;
        }

        private void NudChrome_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets the source (Spotify, Youtube, Nightbot)
            if (!IsLoaded)
                // This prevents that the selected is always 0 (initialize components)
                return;

            if (NudChrome.Value != null) Settings.ChromeFetchRate = (int)NudChrome.Value;
        }

        private void NudCooldown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets command cooldown
            if (NudCooldown.Value != null) Settings.TwSrCooldown = (int)NudCooldown.Value;
        }

        private void NudMaxlength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (NudMaxlength.Value != null) Settings.MaxSongLength = (int)NudMaxlength.Value;
        }

        private void NudMaxReq_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            //Sets max requests per user value
            switch ((TwitchHandler.TwitchUserLevels)CbxUserLevelsMaxReq.SelectedIndex)
            {
                case TwitchHandler.TwitchUserLevels.Everyone:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqEveryone = (int)NudMaxReq.Value;
                    break;
                case TwitchHandler.TwitchUserLevels.Vip:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqVip = (int)NudMaxReq.Value;
                    break;
                case TwitchHandler.TwitchUserLevels.Subscriber:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqSubscriber = (int)NudMaxReq.Value;
                    break;
                case TwitchHandler.TwitchUserLevels.Moderator:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqModerator = (int)NudMaxReq.Value;
                    break;
                case TwitchHandler.TwitchUserLevels.Broadcaster:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqBroadcaster = (int)NudMaxReq.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        private void tb_ClientID_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.ClientId = tb_ClientID.Text;
        }

        private void tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientSecret = tb_ClientSecret.Password;
        }

        private void tgl_AnnounceInChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AnnounceInChat = tgl_AnnounceInChat.IsOn;
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

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            // set the theme (BaseLight / BaseDark)
            Settings.Theme = ThemeToggleSwitch.IsOn ? "Dark" : "Light";

            ThemeHandler.ApplyTheme();

        }

        private void txtbx_RewardID_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Saves the RewardID
            Settings.TwRewardId = txtbx_RewardID.Text;
        }

        private void txtbx_twChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch channel
            Settings.TwChannel = txtbx_twChannel.Text.Trim();
        }

        private void txtbx_twOAuth_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Sets the twitch oauth token
            Settings.TwOAuth = txtbx_twOAuth.Password;
        }

        private void txtbx_twUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch acc
            Settings.TwAcc = txtbx_twUser.Text.Trim();
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

        private void TxtbxOutputformat2_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.OutputString2 = ((TextBox)sender).Text;
        }

        private void CbxUserLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.TwSrUserLevel = CbxUserLevels.SelectedIndex;
        }

        private void CbxUserLevelsMaxReq_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            NudMaxReq.ValueChanged -= NudMaxReq_ValueChanged;
            switch ((TwitchHandler.TwitchUserLevels)CbxUserLevelsMaxReq.SelectedIndex)
            {
                case TwitchHandler.TwitchUserLevels.Everyone:
                    NudMaxReq.Value = Settings.TwSrMaxReqEveryone;
                    break;
                case TwitchHandler.TwitchUserLevels.Vip:
                    NudMaxReq.Value = Settings.TwSrMaxReqVip;
                    break;
                case TwitchHandler.TwitchUserLevels.Subscriber:
                    NudMaxReq.Value = Settings.TwSrMaxReqSubscriber;
                    break;
                case TwitchHandler.TwitchUserLevels.Moderator:
                    NudMaxReq.Value = Settings.TwSrMaxReqModerator;
                    break;
                case TwitchHandler.TwitchUserLevels.Broadcaster:
                    NudMaxReq.Value = Settings.TwSrMaxReqBroadcaster;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            NudMaxReq.ValueChanged += NudMaxReq_ValueChanged;
        }

        private void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            if (NudMaxReq.Value == null) return;
            Settings.TwSrMaxReqEveryone = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqVip = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqSubscriber = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqModerator = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqBroadcaster = (int)NudMaxReq.Value;
        }

        private void CbxRewards_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string rewardId = ((CbxRewards.SelectedItem as ComboBoxItem)?.Content as UC_RewardItem)?.Reward.Id;
            if (rewardId != null)
                txtbx_RewardID.Text = rewardId;
        }

        private async void BtnCreateReward_Click(object sender, RoutedEventArgs e)
        {
            CreateCustomRewardsResponse response = await TwitchHandler._twitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(Settings.TwitchChannelId,
                new CreateCustomRewardsRequest
                {
                    Title = null,
                    Prompt = null,
                    Cost = 0,
                    IsEnabled = false,
                    BackgroundColor = null,
                    IsUserInputRequired = false,
                    IsMaxPerStreamEnabled = false,
                    MaxPerStream = null,
                    IsMaxPerUserPerStreamEnabled = false,
                    MaxPerUserPerStream = null,
                    IsGlobalCooldownEnabled = false,
                    GlobalCooldownSeconds = null,
                    ShouldRedemptionsSkipRequestQueue = false
                }, Settings.TwitchAccessToken);
            if (response != null)
                Debug.WriteLine(response);
        }

        private void BtnFocusRewards_Click(object sender, RoutedEventArgs e)
        {
            TabItemTwitch.Focus();
            TabItemTwitch.IsSelected = true;
            TabItemTwitchRewards.Focus();
            TabItemTwitchRewards.IsSelected = true;
        }

        private async void BtnUpdateRewards_Click(object sender, RoutedEventArgs e)
        {
            if (TwitchHandler.TokenCheck == null)
                return;
            CbxRewards.IsEnabled = false;
            CbxRewards.Items.Clear();
            CbxRewards.SelectionChanged -= CbxRewards_OnSelectionChanged;
            foreach (CustomReward reward in await TwitchHandler.GetChannelRewards(false))
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = new UC_RewardItem(reward)
                };
                CbxRewards.Items.Add(item);
                if (txtbx_RewardID.Text == reward.Id)
                    CbxRewards.SelectedItem = item;
            }
            CbxRewards.SelectionChanged += CbxRewards_OnSelectionChanged;
            CbxRewards.IsEnabled = true;
        }

        private void CheckRefundChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            refundConditons.Add(int.Parse(((CheckBox)sender).Tag.ToString()));
            if (int.Parse(((CheckBox)sender).Tag.ToString()) == -1)
            {
                foreach (UIElement child in GrdTwitchReward.Children)
                {
                    if (child is CheckBox box && box.Name.StartsWith("ChkRefund"))
                    {
                        box.IsChecked = true;
                    }
                }
            }
            //Debug.WriteLine(string.Join(", ", refundConditons));
            Settings.RefundConditons = refundConditons.ToArray();

        }

        private void CheckRefundUnchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            refundConditons.Remove(int.Parse(((CheckBox)sender).Tag.ToString()));
            if (int.Parse(((CheckBox)sender).Tag.ToString()) == -1)
                foreach (UIElement child in GrdTwitchReward.Children)
                {
                    if (child is CheckBox box && box.Name.StartsWith("ChkRefund"))
                    {
                        box.IsChecked = false;
                    }
                }
            Debug.WriteLine(string.Join(", ", refundConditons));
            Settings.RefundConditons = refundConditons.ToArray();
        }

        private void BtnLogInTwitch_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.APIConnect();
        }

        private void ToggleSwitchPrivacy_Toggled(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch).IsOn)
            {
                PnlTwich.Visibility = Visibility.Collapsed;
                PnlSpotify.Visibility = Visibility.Collapsed;
            }
            else
            {
                PnlTwich.Visibility = Visibility.Visible;
                PnlSpotify.Visibility = Visibility.Visible;
            }

        }

        private void BtnWebserverStart_Click(object sender, RoutedEventArgs e)
        {
            if (NudServerPort.Value == null) return;
            if (!GlobalObjects.WebServer.run)
                GlobalObjects.WebServer.StartWebServer((int)NudServerPort.Value);
            else
                GlobalObjects.WebServer.StopWebServer();

            BtnWebserverStart.Content = GlobalObjects.WebServer.run ? "Stop WebServer" : "Start WebServer";
        }

        private void NudServerPort_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded) return;
            double? value = ((NumericUpDown)sender).Value;
            if (value != null) Settings.WebServerPort = (int)value;
        }

        private void TglAutoStartWebserver_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AutoStartWebServer = true;
        }

        private void BtnCreateNewReward_Click(object sender, RoutedEventArgs e)
        {
            Window_CreateCustomReward createCustomReward = new Window_CreateCustomReward
            {
                Owner = this
            };
            createCustomReward.ShowDialog();
        }

        private void TglBetaUpdates_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BetaUpdates = true;
        }

        private void BtnWebserverOpenUrl_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"http://localhost:{Settings.WebServerPort}");
        }

        private void Tgl_OnlyWorkWhenLive_OnToggled(object sender, RoutedEventArgs e)
        {
            Settings.BotOnlyWorkWhenLive = (bool)tgl_OnlyWorkWhenLive.IsOn;
        }
    }
}