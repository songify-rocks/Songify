using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.UserControls;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.TwitchOAuth;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using NumericUpDown = MahApps.Metro.Controls.NumericUpDown;
using TextBox = System.Windows.Controls.TextBox;

namespace Songify_Slim.Views
{
    // ReSharper disable once InconsistentNaming
    public partial class Window_Settings
    {
        private readonly bool _appIdInitialValue = Settings.UseOwnApp;
        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();
        private Window _mW;
        private readonly List<int> _refundConditons = new List<int>();

        public Window_Settings()
        {
            InitializeComponent();
            Title = Properties.Resources.mw_menu_Settings;
            if (Settings.Language == "en") return;
            Width = MinWidth = 830;
        }

        public async void SetControls()
        {
            // Add TwitchHandler.TwitchUserLevels values to the combobox CbxUserLevels
            CbxUserLevelsMaxReq.SelectionChanged -= CbxUserLevelsMaxReq_SelectionChanged;
            CbxUserLevels.Items.Clear();
            CbxUserLevelsMaxReq.Items.Clear();
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
            NudMaxReq.Value = Settings.TwSrMaxReqEveryone;
            CbxUserLevelsMaxReq.SelectionChanged += CbxUserLevelsMaxReq_SelectionChanged;
            if (CbxUserLevelsMaxReq.Items.Count > 0)
                CbxUserLevelsMaxReq.SelectedIndex = 0;
            // Sets all the controls from settings
            ThemeToggleSwitch.IsOn = Settings.Theme == "BaseDark" || Settings.Theme == "Dark";
            //TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException();
            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;
            ChbxAutoClear.IsOn = Settings.AutoClearQueue;
            ChbxMessageLogging.IsChecked = Settings.MsgLoggingEnabled;
            ChbxTwAutoconnect.IsOn = Settings.TwAutoConnect;
            ChbxTwReward.IsOn = Settings.TwSrReward;
            ChbxAutostart.IsOn = Settings.Autostart;
            ChbxCover.IsOn = Settings.DownloadCover;
            ChbxCustomPause.IsOn = Settings.CustomPauseTextEnabled;
            ChbxMinimizeSystray.IsOn = Settings.Systray;
            ChbxOpenQueueOnStartup.IsOn = Settings.OpenQueueOnStartup;
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            ChbxSplit.IsOn = Settings.SplitOutput;
            ChbxUpload.IsOn = Settings.Upload;
            NudSpaces.Value = Settings.SpaceCount;
            NudChrome.Value = Settings.ChromeFetchRate;
            NudCooldown.Value = Settings.TwSrCooldown;
            NudMaxlength.Value = Settings.MaxSongLength;
            TbClientId.Text = Settings.ClientId;
            TbClientSecret.Password = Settings.ClientSecret;
            TglAnnounceInChat.IsOn = Settings.AnnounceInChat;
            TglswSpotify.IsOn = Settings.UseOwnApp;
            TxtbxRewardId.Text = Settings.TwRewardId;
            TxtbxTwChannel.Text = Settings.TwChannel;
            TxtbxTwOAuth.Password = Settings.TwOAuth;
            TxtbxTwUser.Text = Settings.TwAcc;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            TxtbxOutputformat2.Text = Settings.OutputString2;
            CbxUserLevels.SelectedIndex = Settings.TwSrUserLevel == -1 ? 0 : Settings.TwSrUserLevel;
            NudServerPort.Value = Settings.WebServerPort;
            TglAutoStartWebserver.IsOn = Settings.AutoStartWebServer;
            TglBetaUpdates.IsOn = Settings.BetaUpdates;
            TglOnlyWorkWhenLive.IsOn = Settings.BotOnlyWorkWhenLive;
            TglInformChat.IsEnabled = Settings.BotOnlyWorkWhenLive;
            BtnWebserverStart.Content = GlobalObjects.WebServer.Run ? Properties.Resources.sw_WebServer_StopWebServer : Properties.Resources.sw_WebServer_StartWebServer;
            ToggleSwitchUnlimitedSr.IsOn = Settings.TwSrUnlimitedSr;
            TglInformChat.IsOn = Settings.ChatLiveStatus;
            TglAddToPlaylist.IsOn = Settings.AddSrToPlaylist;
            ComboboxRedirectPort.SelectionChanged -= ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged -= ComboboxfetchPort_SelectionChanged;
            ComboboxRedirectPort.Items.Clear();
            ComboboxfetchPort.Items.Clear();
            ApplicationDetails.RedirectPorts.ForEach(i => ComboboxRedirectPort.Items.Add(i));
            ApplicationDetails.FetchPorts.ForEach(i => ComboboxfetchPort.Items.Add(i));
            ComboboxRedirectPort.SelectionChanged += ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged += ComboboxfetchPort_SelectionChanged;
            ComboboxRedirectPort.SelectedItem = Settings.TwitchRedirectPort;
            ComboboxfetchPort.SelectedItem = Settings.TwitchFetchPort;
            ToggleRewardGoalEnabled.IsOn = Settings.RewardGoalEnabled;
            TextBoxRewardGoalSong.Text = Settings.RewardGoalSong;
            NumUpDpwnRewardGoalAmount.ValueChanged -= NumUpDpwnRewardGoalAmount_ValueChanged;
            NumUpDpwnRewardGoalAmount.Value = Settings.RewardGoalAmount;
            NumUpDpwnRewardGoalAmount.ValueChanged += NumUpDpwnRewardGoalAmount_ValueChanged;
            Cctrl.Content = new UcBotResponses();
            TglBotcmdPos.IsOn = Settings.BotCmdPos;
            TglBotcmdSong.IsOn = Settings.BotCmdSong;
            TglBotcmdNext.IsOn = Settings.BotCmdNext;
            TglBotcmdSkip.IsOn = Settings.BotCmdSkip;
            TglBotcmdSkipvote.IsOn = Settings.BotCmdSkipVote;
            TglBotcmdSsr.IsOn = Settings.TwSrCommand;
            TglBotcmdRemove.IsOn = Settings.BotCmdRemove;
            TglBotcmdSonglike.IsOn = Settings.BotCmdSonglike;
            NudSkipVoteCount.Value = Settings.BotCmdSkipVoteCount;
            TextBoxTriggerSong.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSongTrigger) ? "song" : Settings.BotCmdSongTrigger;
            TextBoxTriggerPos.Text = string.IsNullOrWhiteSpace(Settings.BotCmdPosTrigger) ? "pos" : Settings.BotCmdPosTrigger;
            TextBoxTriggerNext.Text = string.IsNullOrWhiteSpace(Settings.BotCmdNextTrigger) ? "next" : Settings.BotCmdNextTrigger;
            TextBoxTriggerSkip.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSkipTrigger) ? "skip" : Settings.BotCmdSkipTrigger;
            TextBoxTriggerVoteskip.Text = string.IsNullOrWhiteSpace(Settings.BotCmdVoteskipTrigger) ? "voteskip" : Settings.BotCmdVoteskipTrigger;
            TextBoxTriggerSsr.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSsrTrigger) ? "ssr" : Settings.BotCmdSsrTrigger;
            TextBoxTriggerRemove.Text = string.IsNullOrWhiteSpace(Settings.BotCmdRemoveTrigger) ? "remove" : Settings.BotCmdRemoveTrigger;
            TextBoxTriggerSonglike.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSonglikeTrigger) ? "songlike" : Settings.BotCmdSonglikeTrigger;

            if (Settings.UserLevelsCommand == null)
            {
                Settings.UserLevelsCommand = new List<int>();
            }

            if (Settings.UserLevelsReward == null)
            {
                Settings.UserLevelsReward = new List<int>();
            }

            ChckULCommandViewer.IsChecked = Settings.UserLevelsCommand.Contains(0);
            ChckULCommandVip.IsChecked = Settings.UserLevelsCommand.Contains(1);
            ChckULCommandSub.IsChecked = Settings.UserLevelsCommand.Contains(2);
            ChckULCommandMod.IsChecked = Settings.UserLevelsCommand.Contains(3);

            ChckULRewardViewer.IsChecked = Settings.UserLevelsReward.Contains(0);
            ChckULRewardVip.IsChecked = Settings.UserLevelsReward.Contains(1);
            ChckULRewardSub.IsChecked = Settings.UserLevelsReward.Contains(2);
            ChckULRewardMod.IsChecked = Settings.UserLevelsReward.Contains(3);

            if (ApiHandler.Spotify != null)
            {
                try
                {
                    PrivateProfile profile = await ApiHandler.Spotify.GetPrivateProfileAsync();
                    LblSpotifyAcc.Content = $"{Properties.Resources.sw_Integration_SpotifyLinked} {profile.DisplayName}";
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    if (profile.Images.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(profile.Images[0].Url))
                            bitmap.UriSource = new Uri(profile.Images[0].Url, UriKind.Absolute);
                    }

                    bitmap.EndInit();
                    ImgSpotifyProfile.ImageSource = bitmap;
                    await LoadSpotifyPlaylists();
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }
            }

            ThemeHandler.ApplyTheme();
            CbxLanguage.SelectionChanged -= ComboBox_SelectionChanged;
            switch (Settings.Language)
            {
                case "en":
                    CbxLanguage.SelectedIndex = 0;
                    break;
                case "de-DE":
                    CbxLanguage.SelectedIndex = 1;
                    break;
                case "ru-RU":
                    CbxLanguage.SelectedIndex = 2;
                    break;
                case "es":
                    CbxLanguage.SelectedIndex = 3;
                    break;
                case "fr":
                    CbxLanguage.SelectedIndex = 4;
                    break;
            }
            CbxLanguage.SelectionChanged += ComboBox_SelectionChanged;
            CbAccountSelection.SelectionChanged -= CbAccountSelection_SelectionChanged;
            CbAccountSelection.Items.Clear();
            if (Settings.TwitchUser != null)
            {
                UpdateTwitchUserUi(Settings.TwitchUser, ImgTwitchProfile, LblTwitchName, BtnLogInTwitch, 0);
                TxtbxTwChannel.Text = Settings.TwitchUser.Login;
                CbAccountSelection.Items.Add(new ComboBoxItem
                {
                    Content = new UC_AccountItem(Settings.TwitchUser.Login, Settings.TwitchAccessToken)
                });
            }

            if (Settings.TwitchBotUser != null)
            {
                UpdateTwitchUserUi(Settings.TwitchBotUser, ImgTwitchBotProfile, LblTwitchBotName, BtnLogInTwitchBot, 1);
                CbAccountSelection.Items.Add(new ComboBoxItem
                {
                    Content = new UC_AccountItem(Settings.TwitchBotUser.Login, Settings.TwitchBotToken)
                });
            }
            CbAccountSelection.SelectedItem = CbAccountSelection.Items.Cast<ComboBoxItem>().FirstOrDefault(item => ((UC_AccountItem)item.Content).Username != null && ((UC_AccountItem)item.Content).Username == Settings.TwAcc);
            CbAccountSelection.SelectionChanged += CbAccountSelection_SelectionChanged;
            await LoadRewards();

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

        private static void UpdateTwitchUserUi(User user, ImageBrush img, ContentControl lbl, UIElement btn, int account)
        {
            if (user == null)
            {
                btn.Visibility = Visibility.Visible;
                return;
            }

            lbl.Content = lbl.Tag.ToString() == "main" ? "Main Account:\n" : "Bot Account:\n";

            switch (account)
            {
                case 0 when GlobalObjects.TwitchUserTokenExpired:
                case 1 when GlobalObjects.TwitchBotTokenExpired:
                    btn.Visibility = Visibility.Visible;
                    lbl.Content += $"{user.DisplayName} (Token Expired)";

                    break;
                default:
                    btn.Visibility = Visibility.Collapsed;
                    lbl.Content += $"{user.DisplayName}";

                    break;
            }

            if (user.ProfileImageUrl != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(user.ProfileImageUrl, UriKind.Absolute);
                bitmap.EndInit();
                switch (account)
                {
                    case 0 when GlobalObjects.TwitchUserTokenExpired:
                    case 1 when GlobalObjects.TwitchBotTokenExpired:
                        img.ImageSource = new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, BitmapPalettes.Gray256, 0);
                        break;
                    default:
                        img.ImageSource = bitmap;
                        break;
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
            tb?.AppendText(text);
            tb?.Select(TxtbxOutputformat.Text.Length, 0);
            if (tb?.ContextMenu != null) tb.ContextMenu.IsOpen = false;
        }

        private void btn_Botresponse_Click(object sender, RoutedEventArgs e)
        {
            WindowBotresponse wBr = new WindowBotresponse();
            wBr.Show();
        }

        private void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler

            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                Site = null,
                Tag = null,
                ShowNewFolderButton = false,
                SelectedPath = null,
                RootFolder = Environment.SpecialFolder.Desktop,
                Description = null
            };
            fbd.Description = @"Select a folder to save the config file";
            fbd.ShowNewFolderButton = true;
            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ConfigHandler.WriteAllConfig(Settings.Export(), fbd.SelectedPath);
            this.ShowMessageAsync("Success", "Config file saved successfully");
        }

        private void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler
            ConfigHandler.LoadConfig();
        }

        private void btn_OwnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start($"{GlobalObjects.BaseUrl}/faq.html#appid");
        }

        private async void Btn_ResetConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult msgResult = await this.ShowMessageAsync("Warning",
                "Are you sure you want to reset all settings?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/AppConfig.yaml");
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/BotConfig.yaml");
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/TwitchCredentials.yaml");
            File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/SpotifyCredentials.yaml");
            Settings.ResetConfig();
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
            Clipboard.SetDataObject($"{GlobalObjects.ApiUrl}/getsong.php?id=" + Settings.Uuid);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Opens twitchapps to generate a TMI oAuth Token
            Process.Start("https://twitchapps.com/tmi/");
        }

        private void Chbx_AutoClear_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to clear the queue on startup or not
            Settings.AutoClearQueue = ChbxAutoClear.IsOn;
        }

        private void Chbx_MessageLogging_Checked(object sender, RoutedEventArgs e)
        {
            // Sets message loggint enabled or not
            if (ChbxMessageLogging.IsChecked != null)
                Settings.MsgLoggingEnabled = (bool)ChbxMessageLogging.IsChecked;
        }

        private void Chbx_TwAutoconnect_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to autoconnect or not
            Settings.TwAutoConnect = ChbxTwAutoconnect.IsOn;
        }

        private void Chbx_TwReward_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            Settings.TwSrReward = ChbxTwReward.IsOn;
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
            switch (CbxLanguage.SelectedIndex)
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

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            //ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            Settings.BotCmdSongTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSong.Text) ? "song" : TextBoxTriggerSong.Text;
            Settings.BotCmdPosTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerPos.Text) ? "pos" : TextBoxTriggerPos.Text;
            Settings.BotCmdNextTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerNext.Text) ? "next" : TextBoxTriggerNext.Text;
            Settings.BotCmdSkipTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSkip.Text) ? "skip" : TextBoxTriggerSkip.Text;
            Settings.BotCmdVoteskipTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerVoteskip.Text) ? "voteskip" : TextBoxTriggerVoteskip.Text;
            Settings.BotCmdSsrTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSsr.Text) ? "ssr" : TextBoxTriggerSsr.Text;

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
            if (NudSpaces.Value != null) Settings.SpaceCount = (int)NudSpaces.Value;
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
            Settings.ClientId = TbClientId.Text;
        }

        private void tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientSecret = TbClientSecret.Password;
        }

        private void tgl_AnnounceInChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AnnounceInChat = TglAnnounceInChat.IsOn;
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, EventArgs e)
        {
            Settings.UseOwnApp = TglswSpotify.IsOn;
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
            Settings.TwRewardId = TxtbxRewardId.Text;
        }

        private void txtbx_twChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch channel
            Settings.TwChannel = TxtbxTwChannel.Text.Trim();
        }

        private void txtbx_twOAuth_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Sets the twitch oauth token
            Settings.TwOAuth = TxtbxTwOAuth.Password;
        }

        private void txtbx_twUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sets the twitch acc
            Settings.TwAcc = TxtbxTwUser.Text.Trim();
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
            if (!IsLoaded)
                return;
            UcRewardItem item = ((((ComboBox)sender).SelectedItem as ComboBoxItem)?.Content as UcRewardItem);
            if (item == null)
                return;
            string rewardId = item.Reward == null ? "" : item.Reward.Id;

            switch (((ComboBox)sender)?.Tag.ToString())
            {
                case "sr":
                    {
                        if (rewardId == null) break;
                        Settings.TwRewardId = rewardId;
                        SetCheckBoxEnabledState(TwitchHandler.PubSubEnabled && item.IsManagable);
                        break;
                    }
                case "skip":
                    {
                        if (rewardId != null)
                            Settings.TwRewardSkipId = rewardId;
                        break;
                    }
                case "reward":
                    {
                        if (rewardId != null)
                            Settings.TwRewardGoalRewardId = rewardId;
                        break;
                    }
            }
        }

        private void SetCheckBoxEnabledState(bool itemIsManagable)
        {
            if (!TwitchHandler.PubSubEnabled)
            {
                SMILEY.Visibility = Visibility.Visible;
                TextRefundDisclaimer.Text =
                    "Refunds are not possible because PubSub has been temporarily disabled until TwitchLib, a third party library I use for Twitch API integration, fixes the disconnect issues which crash the application.";
            }
            else
                TextRefundDisclaimer.Text = Properties.Resources.sw_Integration_RefundDisclaimer;

            GridNonManageable.Visibility = itemIsManagable ? Visibility.Collapsed : Visibility.Visible;
            foreach (CheckBox cb in GlobalObjects.FindVisualChildren<CheckBox>(GrdTwitchReward))
            {
                cb.IsEnabled = itemIsManagable;
            }
        }

        private void BtnFocusRewards_Click(object sender, RoutedEventArgs e)
        {
            TabItemTwitch.Focus();
            TabItemTwitch.IsSelected = true;
            TabItemTwitchReward.Focus();
            TabItemTwitchReward.IsSelected = true;
        }

        private async void BtnUpdateRewards_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await LoadRewards();
            ((Button)sender).IsEnabled = true;
        }

        public async Task LoadRewards()
        {
            if (TwitchHandler.TokenCheck == null)
                return;
            CbxRewards.IsEnabled = false;
            CbxRewardsSkip.IsEnabled = false;
            CbxRewards.SelectionChanged -= CbxRewards_OnSelectionChanged;
            CbxRewardsSkip.SelectionChanged -= CbxRewards_OnSelectionChanged;
            ComboboxRewardGoalReward.SelectionChanged -= CbxRewards_OnSelectionChanged;
            CbxRewards.Items.Clear();
            CbxRewardsSkip.Items.Clear();
            ComboboxRewardGoalReward.Items.Clear();
            if (Settings.TwitchUser.BroadcasterType != "")
                try
                {
                    List<CustomReward> managableRewards = await TwitchHandler.GetChannelRewards(true);
                    List<CustomReward> rewards = await TwitchHandler.GetChannelRewards(false);
                    if (rewards.Count > 0)
                    {
                        CbxRewards.Items.Add(new ComboBoxItem
                        {
                            Content = new UcRewardItem(null, false)
                        });

                        CbxRewardsSkip.Items.Add(new ComboBoxItem
                        {
                            Content = new UcRewardItem(null, false)
                        });

                        ComboboxRewardGoalReward.Items.Add(new ComboBoxItem
                        {
                            Content = new UcRewardItem(null, false)
                        });

                        foreach (CustomReward reward in await TwitchHandler.GetChannelRewards(false))
                        {
                            bool managable = managableRewards.Find(r => r.Id == reward.Id) != null;

                            CbxRewards.Items.Add(new ComboBoxItem
                            {
                                Content = new UcRewardItem(reward, managable)
                            });

                            CbxRewardsSkip.Items.Add(new ComboBoxItem
                            {
                                Content = new UcRewardItem(reward, managable)
                            });
                            ComboboxRewardGoalReward.Items.Add(new ComboBoxItem
                            {
                                Content = new UcRewardItem(reward, managable)
                            });
                        }

                        CbxRewards.SelectedItem = GetItemFromList(CbxRewards, Settings.TwRewardId);
                        SetCheckBoxEnabledState(TwitchHandler.PubSubEnabled && CbxRewards.SelectedItem != null && ((UcRewardItem)((ComboBoxItem)CbxRewards.SelectedItem).Content).IsManagable);
                        CbxRewardsSkip.SelectedItem = GetItemFromList(CbxRewardsSkip, Settings.TwRewardSkipId);
                        ComboboxRewardGoalReward.SelectedItem = GetItemFromList(ComboboxRewardGoalReward, Settings.TwRewardGoalRewardId);
                    }
                    CbxRewards.IsEnabled = true;
                    CbxRewardsSkip.IsEnabled = TwitchHandler.PubSubEnabled;
                    BtnCreateNewReward.IsEnabled = true;
                }
                catch (Exception e)
                {
                    Logger.LogExc(e);
                }

            CbxRewards.SelectionChanged += CbxRewards_OnSelectionChanged;
            CbxRewardsSkip.SelectionChanged += CbxRewards_OnSelectionChanged;
            ComboboxRewardGoalReward.SelectionChanged += CbxRewards_OnSelectionChanged;
        }

        public object GetItemFromList(ItemsControl comboBox, string s)
        {
            return comboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => ((UcRewardItem)item.Content).Reward != null && ((UcRewardItem)item.Content).Reward.Id == s);
        }

        private void CheckRefundChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            _refundConditons.Add(int.Parse(((CheckBox)sender).Tag.ToString()));
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
            Settings.RefundConditons = _refundConditons.ToArray();

        }

        private void CheckRefundUnchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            _refundConditons.Remove(int.Parse(((CheckBox)sender).Tag.ToString()));
            if (int.Parse(((CheckBox)sender).Tag.ToString()) == -1)
                foreach (UIElement child in GrdTwitchReward.Children)
                {
                    if (child is CheckBox box && box.Name.StartsWith("ChkRefund"))
                    {
                        box.IsChecked = false;
                    }
                }
            //Debug.WriteLine(string.Join(", ", refundConditons));
            Settings.RefundConditons = _refundConditons.ToArray();
        }

        private void BtnLogInTwitch_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(TwitchHandler.TwitchAccount.Main);
        }

        private void ToggleSwitchPrivacy_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
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
            if (!GlobalObjects.WebServer.Run)
                GlobalObjects.WebServer.StartWebServer((int)NudServerPort.Value);
            else
                GlobalObjects.WebServer.StopWebServer();

            BtnWebserverStart.Content = GlobalObjects.WebServer.Run ? Properties.Resources.sw_WebServer_StopWebServer : Properties.Resources.sw_WebServer_StartWebServer;
        }

        private void NudServerPort_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded) return;
            int? value = (int?)((NumericUpDown)sender).Value;
            if (value == null) return;
            NudServerPort.ValueChanged -= NudServerPort_ValueChanged;
            if (value < 1025)
            {
                NudServerPort.Value = 1025;
                value = 1025;
            }

            if (value > 66535)
            {
                NudServerPort.Value = 66535;
                value = 66535;
            }

            Settings.WebServerPort = (int)value;
            NudServerPort.ValueChanged += NudServerPort_ValueChanged;
        }

        private void TglAutoStartWebserver_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AutoStartWebServer = true;
        }

        private void BtnCreateNewReward_Click(object sender, RoutedEventArgs e)
        {
            WindowCreateCustomReward createCustomReward = new WindowCreateCustomReward
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
            if (GlobalObjects.WebServer.Run)
                Process.Start($"http://localhost:{Settings.WebServerPort}");
        }

        private void Tgl_OnlyWorkWhenLive_OnToggled(object sender, RoutedEventArgs e)
        {
            Settings.BotOnlyWorkWhenLive = TglOnlyWorkWhenLive.IsOn;
            TglInformChat.IsEnabled = TglOnlyWorkWhenLive.IsOn;
            if (TglOnlyWorkWhenLive.IsOn) return;
            TglInformChat.IsOn = false;
        }

        private void ToggleSwitchUnlimitedSR_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.TwSrUnlimitedSr = ToggleSwitchUnlimitedSr.IsOn;
        }

        private void BtnTwitchLogout_OnClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Tag.ToString().ToLower())
            {
                case "main":
                    Settings.TwitchAccessToken = "";
                    Settings.TwitchUser = null;
                    break;
                case "bot":
                    Settings.TwitchBotToken = "";
                    Settings.TwitchBotUser = null;
                    break;
            }
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void NudServerPort_MinimumReached(object sender, RoutedEventArgs e)
        {
            NudServerPort.Value = 1025;
        }

        private void NudServerPort_MaximumReached(object sender, RoutedEventArgs e)
        {
            NudServerPort.Value = 66535;
        }

        private void tgl_InformChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.ChatLiveStatus = TglInformChat.IsOn;
        }

        private void BtnLogInTwitchBot_OnClick(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(TwitchHandler.TwitchAccount.Bot);

        }

        private void ComboboxRedirectPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.TwitchRedirectPort = (int)ComboboxRedirectPort.SelectedItem;
        }

        private void ComboboxfetchPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.TwitchFetchPort = (int)ComboboxfetchPort.SelectedItem;
        }

        private void ToggleRewardGoalEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.RewardGoalEnabled = ToggleRewardGoalEnabled.IsOn;
        }

        private void TextBoxRewardGoalSong_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.RewardGoalSong = TextBoxRewardGoalSong.Text;
        }

        private void NumUpDpwnRewardGoalAmount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (NumUpDpwnRewardGoalAmount.Value != null)
                Settings.RewardGoalAmount = (int)NumUpDpwnRewardGoalAmount.Value;
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
        private void tgl_botcmd_skip_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSkip = ((ToggleSwitch)sender).IsOn;
        }

        private void tgl_botcmd_skipvote_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSkipVote = ((ToggleSwitch)sender).IsOn;
        }

        private void NudSkipVoteCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            double? value = ((NumericUpDown)sender).Value;
            if (value != null)
                Settings.BotCmdSkipVoteCount = (int)value;
        }

        private void TextBoxTrigger_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            switch ((sender as TextBox)?.Tag.ToString())
            {
                case "song":
                    Settings.BotCmdSongTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "song"
                        : ((TextBox)sender).Text;
                    break;
                case "pos":
                    Settings.BotCmdPosTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "pos"
                        : ((TextBox)sender).Text;
                    break;
                case "next":
                    Settings.BotCmdNextTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "next"
                        : ((TextBox)sender).Text;
                    break;
                case "skip":
                    Settings.BotCmdSkipTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "skip"
                        : ((TextBox)sender).Text;
                    break;
                case "voteskip":
                    Settings.BotCmdVoteskipTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "voteskip"
                        : ((TextBox)sender).Text;
                    break;
                case "ssr":
                    Settings.BotCmdSsrTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "ssr"
                        : ((TextBox)sender).Text;
                    break;
                case "remove":
                    Settings.BotCmdRemoveTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "remove"
                        : ((TextBox)sender).Text;
                    break;
                case "like":
                    Settings.BotCmdSonglikeTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "like"
                        : ((TextBox)sender).Text;
                    break;
            }
        }

        private void TextBoxTrigger_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }

        private void Tgl_botcmd_ssr_OnToggled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.TwSrCommand = ((ToggleSwitch)sender).IsOn;
        }

        private void cb_SpotifyPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            UcPlaylistItem item = ((((ComboBox)sender).SelectedItem as ComboBoxItem)?.Content as UcPlaylistItem);
            if (item == null)
                return;
            Settings.SpotifyPlaylistId = item.Playlist.Id;
        }

        private void Tgl_botcmd_remove_OnToggled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdRemove = ((ToggleSwitch)sender).IsOn;
        }

        private void Tgl_botcmd_songlike_OnToggled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSonglike = ((ToggleSwitch)sender).IsOn;
        }

        private void CbAccountSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            Settings.TwAcc = ((UC_AccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).Username;
            Settings.TwOAuth = ((UC_AccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).OAuth;
            TwitchHandler.Client.Disconnect();
            TwitchHandler.Client = null;
            TwitchHandler.BotConnect();
            TwitchHandler.MainConnect();
            SetControls();
        }

        private void Tgl_botcmd_PlayPause_OnToggled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdPlayPause = ((ToggleSwitch)sender).IsOn;
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            ((ComboBox)sender).SelectedIndex = 0;
        }

        private void CbxUserLevelsRewardChecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsReward.Contains(value)) return;
            List<int> list = new List<int>(Settings.UserLevelsReward) { value };
            Settings.UserLevelsReward = list;
        }

        private void CbxUserLevelsRewardUnchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsReward.Contains(value)) return;
            List<int> list = new List<int>(Settings.UserLevelsReward);
            list.Remove(value);
            Settings.UserLevelsReward = list;
        }
        private void CbxUserLevelsCommandChecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = new List<int>(Settings.UserLevelsCommand) { value };
            Settings.UserLevelsCommand = list;
        }

        private void CbxUserLevelsCommandUnchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = new List<int>(Settings.UserLevelsCommand);
            list.Remove(value);
            Settings.UserLevelsCommand = list;
        }

        private void TglAddToPlaylist_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AddSrToPlaylist = ((ToggleSwitch)sender).IsOn;
        }

        private async void BtnReloadPlaylists_Click(object sender, RoutedEventArgs e)
        {
            await LoadSpotifyPlaylists();
        }

        private async Task LoadSpotifyPlaylists()
        {
            if (ApiHandler.Spotify == null) return;
            PrivateProfile profile = await ApiHandler.Spotify.GetPrivateProfileAsync();
            CbSpotifyPlaylist.Items.Clear();
            Paging<SimplePlaylist> playlists = await ApiHandler.Spotify.GetUserPlaylistsAsync(profile.Id, 50);
            do
            {
                foreach (SimplePlaylist playlist in playlists.Items.Where(playlist => playlist.Owner.Id == profile.Id))
                {
                    CbSpotifyPlaylist.Items.Add(new ComboBoxItem { Content = new UcPlaylistItem(playlist) });
                }
                playlists = await ApiHandler.Spotify.GetUserPlaylistsAsync(profile.Id, 50, playlists.Offset + playlists.Limit);
            } while (playlists.HasNextPage());

            CbSpotifyPlaylist.SelectedItem = CbSpotifyPlaylist.Items.Cast<ComboBoxItem>().FirstOrDefault(item => ((UcPlaylistItem)item.Content).Playlist != null && ((UcPlaylistItem)item.Content).Playlist.Id == Settings.SpotifyPlaylistId);
        }
    }
}