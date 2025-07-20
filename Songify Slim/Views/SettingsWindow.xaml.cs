using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Models;
using Songify_Slim.UserControls;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Songify.TwitchOAuth;
using Songify_Slim.Util.Songify.YTMDesktop;
using Songify_Slim.Util.Spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using YamlDotNet.Core.Tokens;
using static Songify_Slim.App;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using File = System.IO.File;
using Image = SpotifyAPI.Web.Image;
using MenuItem = System.Windows.Controls.MenuItem;
using NumericUpDown = MahApps.Metro.Controls.NumericUpDown;
using TextBox = System.Windows.Controls.TextBox;

namespace Songify_Slim.Views
{
    // ReSharper disable once InconsistentNaming
    public partial class Window_Settings
    {
        private readonly bool _appIdInitialValue = Settings.UseOwnApp;
        private readonly FolderBrowserDialog _fbd = new();
        private Window _mW;
        private Window_ResponseParams _wRp;
        private Dictionary<string, string> _supportedLanguages = new()
        {
            { "en", "English" },
            { "de-DE", "German" },
            { "ru-RU", "Russian" },
            { "es", "Spanish" },
            { "fr", "French" },
            { "pl-PL", "Polish" },
            { "pt-PT", "Portuguese" },
            { "it-IT", "Italian" },
            { "pt-BR", "Brazilian Portuguese" },
            { "be-BY", "Belarusian" }
        };

        public Window_Settings()
        {
            InitializeComponent();
            if (Settings.Language == "en") return;
            Width = MinWidth = 830;
        }

        public async Task SetControls()
        {
            GridLoading.Visibility = Visibility.Visible;
            TabCtrl.IsEnabled = false;

            // Add TwitchHandler.TwitchUserLevels values to the combobox CbxUserLevels
            CbxUserLevelsMaxReq.SelectionChanged -= CbxUserLevelsMaxReq_SelectionChanged;
            CbxUserLevels.Items.Clear();
            CbxUserLevelsMaxReq.Items.Clear();
            Array values = Enum.GetValues(typeof(Enums.TwitchUserLevels));
            foreach (object value in values)
            {
                switch (value.ToString())
                {
                    case "Broadcaster":
                        continue;
                    default:
                        CbxUserLevels.Items.Add(value);
                        CbxUserLevelsMaxReq.Items.Add(value);
                        break;
                }
            }

            await LoadCommands();

            NudMaxReq.Value = Settings.TwSrMaxReqEveryone;
            CbxUserLevelsMaxReq.SelectionChanged += CbxUserLevelsMaxReq_SelectionChanged;
            if (CbxUserLevelsMaxReq.Items.Count > 0)
                CbxUserLevelsMaxReq.SelectedIndex = 0;
            // Sets all the controls from settings
            ThemeToggleSwitch.IsOn = Settings.Theme == "BaseDark" || Settings.Theme == "Dark";
            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;
            ChbxAutoClear.IsOn = Settings.AutoClearQueue;
            ChbxTwAutoconnect.IsOn = Settings.TwAutoConnect;
            ChbxTwReward.IsOn = Settings.TwSrReward;
            ChbxAutostart.IsOn = Settings.Autostart;
            ChbxCover.IsOn = Settings.DownloadCover;
            TglCanvas.IsOn = Settings.DownloadCanvas;
            CbPauseOptions.SelectedIndex = (int)Settings.PauseOption;
            //ChbxCustomPause.IsOn = Settings.CustomPauseTextEnabled;
            ChbxMinimizeSystray.IsOn = Settings.Systray;
            ChbxOpenQueueOnStartup.IsOn = Settings.OpenQueueOnStartup;
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            ChbxSpacesSplitFiles.IsChecked = Settings.AppendSpacesSplitFiles;
            ChbxSplit.IsOn = Settings.SplitOutput;
            ChbxUpload.IsOn = Settings.Upload;
            NudSpaces.Value = Settings.SpaceCount;
            NudChrome.Value = Settings.ChromeFetchRate;
            NudCooldown.Value = Settings.TwSrCooldown;
            NudCooldownPerUser.Value = Settings.TwSrPerUserCooldown;
            NudMaxlength.Value = Settings.MaxSongLength;
            TbClientId.Text = Settings.ClientId;
            TbClientSecret.Password = Settings.ClientSecret;
            TglAnnounceInChat.IsOn = Settings.AnnounceInChat;
            TglswSpotify.IsOn = true;
            TglUseDefaultBrowser.IsOn = Settings.UseDefaultBrowser;
            Tglsw_OnlyAddToPlaylist.IsOn = Settings.AddSrtoPlaylistOnly;
            //TxtbxRewardId.Text = Settings.TwRewardId;
            NudBits.Value = Settings.MinimumBitsForSR;
            CbxSpotifyRedirectUri.SelectedIndex = Settings.SpotifyRedirectUri switch
            {
                "localhost" => 0,
                "127.0.0.1" => 1,
                _ => CbxSpotifyRedirectUri.SelectedIndex
            };
            TxtbxTwChannel.Text = Settings.TwChannel;
            TxtbxTwOAuth.Password = Settings.TwOAuth;
            TxtbxTwUser.Text = Settings.TwAcc;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            TxtbxOutputformat2.Text = Settings.OutputString2;
            TbYTMDesktopToken.Password = Settings.YtmdToken;
            CbxUserLevels.SelectedIndex = Settings.TwSrUserLevel == -1 ? 0 : Settings.TwSrUserLevel;
            NudServerPort.Value = Settings.WebServerPort;
            tgl_KeepCover.IsOn = Settings.KeepAlbumCover;
            TglAutoStartWebserver.IsOn = Settings.AutoStartWebServer;
            TglBetaUpdates.IsOn = Settings.BetaUpdates;
            TglOnlyWorkWhenLive.IsOn = Settings.BotOnlyWorkWhenLive;
            TglInformChat.IsEnabled = Settings.BotOnlyWorkWhenLive;
            BtnWebserverStart.Content = GlobalObjects.WebServer.Run
                ? Properties.Resources.sw_WebServer_StopWebServer
                : Properties.Resources.sw_WebServer_StartWebServer;
            ToggleSwitchUnlimitedSr.IsOn = Settings.TwSrUnlimitedSr;
            TglInformChat.IsOn = Settings.ChatLiveStatus;
            TglAddToPlaylist.IsOn = Settings.AddSrToPlaylist;
            Tglsw_BlockAllExplicitSongs.IsOn = Settings.BlockAllExplicitSongs;
            ComboboxRedirectPort.SelectionChanged -= ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged -= ComboboxfetchPort_SelectionChanged;
            ComboboxRedirectPort.Items.Clear();
            ComboboxfetchPort.Items.Clear();
            TbRequesterPrefix.Text = Settings.RequesterPrefix;
            ApplicationDetails.RedirectPorts.ForEach(i => ComboboxRedirectPort.Items.Add(i));
            ApplicationDetails.FetchPorts.ForEach(i => ComboboxfetchPort.Items.Add(i));
            ComboboxRedirectPort.SelectionChanged += ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged += ComboboxfetchPort_SelectionChanged;
            ComboboxRedirectPort.SelectedItem = Settings.TwitchRedirectPort;
            ComboboxfetchPort.SelectedItem = Settings.TwitchFetchPort;
            Cctrl.Content = new UcBotResponses();
            TglDonationReminder.IsOn = Settings.DonationReminder;
            TglsLongBadgeNames.IsOn = Settings.LongBadgeNames;
            Settings.UserLevelsCommand ??= [];
            Settings.UserLevelsReward ??= [];

            Settings.UnlimitedSrUserlevelsCommand ??= [];
            Settings.UnlimitedSrUserlevelsReward ??= [];


            ChckUlCommandViewer.IsChecked = Settings.UserLevelsCommand.Contains(0);
            ChckUlCommandFollower.IsChecked = Settings.UserLevelsCommand.Contains(1);
            ChckUlCommandSub.IsChecked = Settings.UserLevelsCommand.Contains(2);
            ChckUlCommandSubT2.IsChecked = Settings.UserLevelsCommand.Contains(3);
            ChckUlCommandSubT3.IsChecked = Settings.UserLevelsCommand.Contains(4);
            ChckUlCommandVip.IsChecked = Settings.UserLevelsCommand.Contains(5);
            ChckUlCommandMod.IsChecked = Settings.UserLevelsCommand.Contains(6);

            ChckUlRewardViewer.IsChecked = Settings.UserLevelsReward.Contains(0);
            ChckUlRewardFollower.IsChecked = Settings.UserLevelsReward.Contains(1);
            ChckUlRewardSub.IsChecked = Settings.UserLevelsReward.Contains(2);
            ChckUlRewardSubT2.IsChecked = Settings.UserLevelsReward.Contains(3);
            ChckUlRewardSubT3.IsChecked = Settings.UserLevelsReward.Contains(4);
            ChckUlRewardVip.IsChecked = Settings.UserLevelsReward.Contains(5);
            ChckUlRewardMod.IsChecked = Settings.UserLevelsReward.Contains(6);

            ChckUnlimitedCommandViewer.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(0);
            ChckUnlimitedCommandFollower.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(1);
            ChckUnlimitedCommandSub.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(2);
            ChckUnlimitedCommandSubT2.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(3);
            ChckUnlimitedCommandSubT3.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(4);
            ChckUnlimitedCommandVip.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(5);
            ChckUnlimitedCommandMod.IsChecked = Settings.UnlimitedSrUserlevelsCommand.Contains(6);

            ChckUnlimitedRewardViewer.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(0);
            ChckUnlimitedRewardFollower.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(1);
            ChckUnlimitedRewardSub.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(2);
            ChckUnlimitedRewardSubT2.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(3);
            ChckUnlimitedRewardSubT3.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(4);
            ChckUnlimitedRewardVip.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(5);
            ChckUnlimitedRewardMod.IsChecked = Settings.UnlimitedSrUserlevelsReward.Contains(6);

            TglLimitSrPlaylist.IsOn = Settings.LimitSrToPlaylist;
            CbSpotifySongLimitPlaylist.IsEnabled = Settings.LimitSrToPlaylist;

            if (SpotifyApiHandler.Client != null)
            {
                PrivateUser profile = await SpotifyApiHandler.GetUser();
                LblSpotifyAcc.Content = $"{Properties.Resources.sw_Integration_SpotifyLinked} {profile.DisplayName}";
                try
                {
                    if (profile.Images is { Count: > 0 } && !string.IsNullOrEmpty(profile.Images[0].Url))
                    {
                        BitmapImage bitmap = new();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(profile.Images[0].Url, UriKind.Absolute);
                        bitmap.EndInit();
                        ImgSpotifyProfile.ImageSource = bitmap;
                        ImgSpotifyProfilePlaceholder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ImgSpotifyProfile.ImageSource = null;
                        ImgSpotifyProfilePlaceholder.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogExc(ex);
                }

                await LoadSpotifyPlaylists();
            }

            ThemeHandler.ApplyTheme();
            CbxLanguage.SelectionChanged -= ComboBox_SelectionChanged;
            CbxLanguage.ItemsSource = _supportedLanguages;
            CbxLanguage.SelectedValue = Settings.Language;

            CbxLanguage.SelectionChanged += ComboBox_SelectionChanged;
            CbAccountSelection.SelectionChanged -= CbAccountSelection_SelectionChanged;
            CbAccountSelection.Items.Clear();
            if (Settings.TwitchUser != null)
            {
                BtnTwitchLogout.Visibility = Visibility.Visible;
                BtnTwitchRefreshMain.Visibility = Visibility.Collapsed;
                UpdateTwitchUserUi(Settings.TwitchUser, ImgTwitchProfile, LblTwitchName, BtnLogInTwitch, 0,
                    BtnLogInTwitchAlt);
                //TxtbxTwChannel.Text = Settings.TwitchUser.Login;
                CbAccountSelection.Items.Add(new ComboBoxItem
                {
                    Content = new UcAccountItem(Settings.TwitchUser.Login, Settings.TwitchAccessToken)
                });
            }
            else
            {
                BtnLogInTwitch.Visibility = Visibility.Visible;
                BtnLogInTwitchAlt.Visibility = Visibility.Visible;
                LblMainExpiry.Visibility = Visibility.Collapsed;
                Icon icon = Properties.Resources.songify; // Retrieve from Resources.resx
                Bitmap bitmap = icon.ToBitmap();
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                ImgTwitchProfile.ImageSource = bitmapImage;

                BtnTwitchLogout.Visibility = Visibility.Collapsed;
                BtnTwitchRefreshMain.Visibility = Visibility.Collapsed;

                LblTwitchName.Content = "Main Account:";
            }

            if (Settings.TwitchBotUser != null)
            {
                BtnTwitchBotLogout.Visibility = Visibility.Visible;
                BtnTwitchRefreshBot.Visibility = Visibility.Collapsed;
                UpdateTwitchUserUi(Settings.TwitchBotUser, ImgTwitchBotProfile, LblTwitchBotName, BtnLogInTwitchBot, 1,
                    BtnLogInTwitchAltBot);
                CbAccountSelection.Items.Add(new ComboBoxItem
                {
                    Content = new UcAccountItem(Settings.TwitchBotUser.Login, Settings.TwitchBotToken)
                });
            }
            else
            {
                BtnLogInTwitchBot.Visibility = Visibility.Visible;
                BtnLogInTwitchAltBot.Visibility = Visibility.Visible;
                LblBotExpiry.Visibility = Visibility.Collapsed;
                Icon icon = Properties.Resources.songify; // Retrieve from Resources.resx
                Bitmap bitmap = icon.ToBitmap();
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                ImgTwitchBotProfile.ImageSource = bitmapImage;

                BtnTwitchBotLogout.Visibility = Visibility.Collapsed;
                BtnTwitchRefreshBot.Visibility = Visibility.Collapsed;

                LblTwitchBotName.Content = "Bot Account:";
            }

            if (string.IsNullOrEmpty(Settings.TwAcc))
                CbAccountSelection.SelectedIndex = 0;
            else
                CbAccountSelection.SelectedItem = CbAccountSelection.Items.Cast<ComboBoxItem>().FirstOrDefault(item =>
                    ((UcAccountItem)item.Content).Username != null &&
                    ((UcAccountItem)item.Content).Username == Settings.TwAcc);
            CbAccountSelection.SelectionChanged += CbAccountSelection_SelectionChanged;
            if (TwitchHandler.TwitchApi != null)
                await LoadRewards();


            if (Settings.RefundConditons == null) return;
            foreach (int condition in Settings.RefundConditons)
            {
                foreach (UIElement child in GrdTwitchReward.Children)
                {
                    if (child is CheckBox box && box.Name.StartsWith("ChkRefund") &&
                        box.Tag.ToString() == condition.ToString())
                    {
                        box.IsChecked = true;
                    }
                }
            }

            GridLoading.Visibility = Visibility.Collapsed;
            TabCtrl.IsEnabled = true;

        }

        private async Task LoadCommands()
        {
            StackCommands.Children.Clear();
            foreach (TwitchCommand command in Settings.Commands.OrderBy(cmd => cmd.CommandType))
            {
                bool showBottomBorder = command != Settings.Commands.OrderBy(cmd => cmd.CommandType).Last();
                StackCommands.Children.Add(new UC_CommandItem(command) { ShowBottomBorder = showBottomBorder });
                await Task.Delay(10);
            }
        }

        private void UpdateTwitchUserUi(User user, ImageBrush img, ContentControl lbl, UIElement btn,
            int account, UIElement btnAlt)
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
                    btn.Visibility = Visibility.Visible;
                    btnAlt.Visibility = Visibility.Visible;
                    lbl.Content += $"{user.DisplayName} (Token Expired)";
                    break;
                case 1 when GlobalObjects.TwitchBotTokenExpired:
                    btn.Visibility = Visibility.Visible;
                    btnAlt.Visibility = Visibility.Visible;
                    lbl.Content += $"{user.DisplayName} (Token Expired)";
                    break;

                default:
                    btnAlt.Visibility = Visibility.Collapsed;
                    btn.Visibility = Visibility.Collapsed;
                    lbl.Content += $"{user.DisplayName}";
                    break;
            }

            switch (account)
            {
                case 0:
                    LblMainExpiry.Content = $"Expires on {Settings.TwitchAccessTokenExpiryDate}";
                    break;
                case 1:
                    LblBotExpiry.Content = $"Expires on {Settings.BotAccessTokenExpiryDate}";
                    break;
            }

            if (user.ProfileImageUrl == null) return;
            try
            {
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(user.ProfileImageUrl, UriKind.Absolute);
                bitmap.EndInit();
                switch (account)
                {
                    case 0 when GlobalObjects.TwitchUserTokenExpired:
                    case 1 when GlobalObjects.TwitchBotTokenExpired:
                        img.ImageSource =
                            new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, BitmapPalettes.Gray256, 0);
                        break;

                    default:
                        img.ImageSource = bitmap;
                        break;
                }
            }
            catch
            {
                Logger.LogStr($"Couldn't load profile picture for {(account == 0 ? "Main" : "Bot")}");
            }
        }

        private void AppendText(string s, string text)
        {
            TextBox tb = s switch
            {
                "1" => TxtbxOutputformat,
                "2" => TxtbxOutputformat2,
                _ => null
            };

            // Get the current caret position and the length of the selected text
            if (tb == null) return;
            int selectionStart = tb.SelectionStart;
            int selectionLength = tb.SelectionLength;

            // Remove any selected text (if any)
            if (selectionLength > 0)
            {
                tb.Text = tb.Text.Remove(selectionStart, selectionLength);
            }

            // Insert the new text at the caret position
            tb.Text = tb.Text.Insert(selectionStart, text);

            // Place the caret after the inserted text
            tb.SelectionStart = selectionStart + text.Length;


            tb.SelectionLength = 0;

            //// Appends Rightclick-Text from the output text box (parameters)
            //tb?.AppendText(text);
            //tb?.Select(TxtbxOutputformat.Text.Length, 0);
            if (tb.ContextMenu != null) tb.ContextMenu.IsOpen = false;
        }

        private void Btn_Botresponse_Click(object sender, RoutedEventArgs e)
        {
            WindowBotresponse wBr = new();
            wBr.Show();
        }

        private void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            // calls confighandler

            FolderBrowserDialog fbd = new()
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

        private async void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // Open a dialog to select a folder to import the config files
            using FolderBrowserDialog fbd = new();
            fbd.Description = @"Select the folder containing the config files";
            fbd.ShowNewFolderButton = false; // Optional, prevents creating new folders
            // set the apps directory as the default directory
            fbd.SelectedPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            // Get the selected folder path and call the config handler
            string selectedFolder = fbd.SelectedPath;
            ConfigHandler.ReadConfig(selectedFolder);
            await SetControls();
        }

        private void Btn_OwnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup");
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
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private async void Btn_spotifyLink_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.ClientId) || string.IsNullOrEmpty(Settings.ClientSecret))
            {
                // Shows a message box if the client id or secret is missing
                MessageDialogResult result = await this.ShowMessageAsync(
                    "Error",
                    Properties.Resources.s_FillClientIdAndSecret,
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = Properties.Resources.s_OK,
                        NegativeButtonText = "How to get Client ID and Secret"
                    });
                if (result == MessageDialogResult.Negative)
                    Process.Start("https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup");

                return;
            }

            // Shows a message box if the client id or secret is missing
            MessageDialogResult res = await this.ShowMessageAsync(
                "Important",
                $"Just to make sure: Even though you have \"{((ComboBoxItem)CbxSpotifyRedirectUri.SelectedItem).Content}\" selected.\nWhich redirect URI are you using on the Spotify Developer App?",
                MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings
                {
                    ColorScheme = MetroDialogColorScheme.Theme,
                    OwnerCanCloseWithDialog = true,
                    AffirmativeButtonText = "http://127.0.0.1:4002/auth",
                    NegativeButtonText = "http://localhost:4002/auth",
                    FirstAuxiliaryButtonText = "Cancel",
                });
            switch (res)
            {
                case MessageDialogResult.Negative:
                    Settings.SpotifyRedirectUri = "localhost";
                    break;
                case MessageDialogResult.Affirmative:
                    Settings.SpotifyRedirectUri = "127.0.0.1";
                    break;
                case MessageDialogResult.Canceled:
                case MessageDialogResult.FirstAuxiliary:
                case MessageDialogResult.SecondAuxiliary:
                default:
                    return;
            }

            // Links Spotify
            Settings.SpotifyRefreshToken = "";
            try
            {
                await SpotifyApiHandler.Auth();
                await SetControls();
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
            Clipboard.SetDataObject($"{GlobalObjects.ApiUrl}/getsong?uuid=" + Settings.Uuid);
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
            Process.Start("https://twitchtokengenerator.com/");
        }

        private void Chbx_AutoClear_Checked(object sender, RoutedEventArgs e)
        {
            // Sets wether to clear the queue on startup or not
            Settings.AutoClearQueue = ChbxAutoClear.IsOn;
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

        //private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        //{
        //    Settings.CustomPauseTextEnabled = ChbxCustomPause.IsOn;
        //    TxtbxCustompausetext.IsEnabled = ChbxCustomPause.IsOn;
        //}

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
            //((MainWindow)_mW).UploadSong(((MainWindow)_mW).CurrSong);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (CbxLanguage.SelectedValue is not string selectedLanguageCode) return;
            //// Update the current UI culture and settings
            //Settings.Language = selectedLanguageCode;
            //// Restart the application to apply the language change
            //Process.Start(Application.ResourceAssembly.Location);
            //Application.Current.Shutdown();


            if (CbxLanguage.SelectedValue is not string selectedLanguageCode)
                return;

            CultureInfo newCulture = new(selectedLanguageCode);
            Thread.CurrentThread.CurrentUICulture = newCulture;

            // Create a new ResourceDictionary from the RESX for the selected culture.
            ResourceDictionary newLocalizationDict = ResxToDictionaryHelper.CreateResourceDictionary(newCulture);

            // Find the existing localization dictionary by checking for a known key.
            Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary localizationDict = dictionaries.FirstOrDefault(dict => dict.Contains("sw_tcSystem_lblLanguage"));

            if (localizationDict != null)
            {
                int index = dictionaries.IndexOf(localizationDict);
                dictionaries.Remove(localizationDict);
                dictionaries.Insert(index, newLocalizationDict);
            }
            else
            {
                // If no localization dictionary was found, simply add the new one.
                dictionaries.Add(newLocalizationDict);
            }

            _supportedLanguages = new Dictionary<string, string>
            {
                { "en", Application.Current.TryFindResource("lang_en") as string ?? "English"},
                { "de-DE", Application.Current.TryFindResource("lang_deDE") as string ??"German" },
                { "ru-RU", Application.Current.TryFindResource("lang_ru") as string ??"Russian" },
                { "es", Application.Current.TryFindResource("lang_es") as string ??"Spanish" },
                { "fr", Application.Current.TryFindResource("lang_fr") as string ??"French" },
                { "pl-PL", Application.Current.TryFindResource("lang_pl") as string ??"Polish" },
                { "pt-PT", Application.Current.TryFindResource("lang_pt") as string ?? "Portuguese" },
                { "it-IT", Application.Current.TryFindResource("lang_it") as string ??"Italian" },
                { "pt-BR", Application.Current.TryFindResource("lang_br") as string ??"Brazilian Portuguese" },
                { "be-BY", Application.Current.TryFindResource("lang_beBY") as string ??"Belarusian" }
            };
            CbxLanguage.ItemsSource = _supportedLanguages;
            CbxLanguage.SelectedValue = selectedLanguageCode;

            // Optionally update your settings.
            Settings.Language = selectedLanguageCode;

            _wRp?.LoadItems();
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // sets the color, when selecting yellow it changes foreground color because else its hard to read
            Settings.Color = (string)(ComboBoxColor.SelectedItem as ComboBoxItem)?.Content;
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
            ThemeHandler.ApplyTheme();

            foreach (Window currentWindow in Application.Current.Windows)
            {
                if (currentWindow is not Window_ResponseParams @params) continue;
                @params.LoadItems();
            }
        }

        private void MenuBtnArtist_Click(object sender, RoutedEventArgs e)
        {
            AppendText((sender as MenuItem)?.Tag.ToString(), "{artist}");
            // appends text
        }

        private void MenuBtnReturn_Click(object sender, RoutedEventArgs e)
        {
            AppendText((sender as MenuItem)?.Tag.ToString(), @"\n");
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
            ConfigHandler.WriteAllConfig(Settings.Export());
            if (_appIdInitialValue == Settings.UseOwnApp) return;
            e.Cancel = true;
            Settings.SpotifyAccessToken = "";
            Settings.SpotifyRefreshToken = "";
            string temp = _appIdInitialValue == false
                ? "You switched from Songify's internal app-ID to your own. This is great because you won't get throttled by rate limits! \n\nIn order to use it though, Songify needs to be restarted and you have to relink with your Spotify account!"
                : "You switched from your own app-ID to Songify's internal one. This is bad and you will likely encounter problems. The API only allows a certain amount of requests done through an app. We have been exceeding this amount by a lot. Please use your own app-ID instead!\n\nSongify needs a restart and you have to link your Spotify account again.";

            MessageDialogResult msgResult = await this.ShowMessageAsync("Warning", temp, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AffirmativeButtonText = "Restart" });
            if (msgResult != MessageDialogResult.Affirmative) return;
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void Nud_Spaces_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (NudSpaces.Value != null) Settings.SpaceCount = (int)NudSpaces.Value;
        }

        private void NudChrome_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets the source (Spotify, BrowserCompanion, Nightbot)
            if (!IsLoaded)
                // This prevents that the selected is always 0 (initialize components)
                return;

            if (NudChrome.Value != null) Settings.ChromeFetchRate = (int)NudChrome.Value;
        }

        private void NudCooldown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            // Sets command cooldown
            if (NudCooldown.Value == null)
                return;
            if (!IsLoaded)
                return;
            Settings.TwSrCooldown = (int)NudCooldown.Value;
            if (!NudCooldown.Value.HasValue) return;
            int totalSeconds = (int)NudCooldown.Value.Value;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            GlobalCooldownDisplay.Text = $"({minutes:D2}:{seconds:D2})";
        }

        private void NudMaxlength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (NudMaxlength.Value != null) Settings.MaxSongLength = (int)NudMaxlength.Value;
        }

        private void NudMaxReq_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            //Sets max requests per user value
            switch ((Enums.TwitchUserLevels)CbxUserLevelsMaxReq.SelectedIndex)
            {
                case Enums.TwitchUserLevels.Viewer:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqEveryone = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.Follower:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqFollower = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.Vip:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqVip = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.Subscriber:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqSubscriber = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.SubscriberT2:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqSubscriberT2 = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.SubscriberT3:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqSubscriberT3 = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.Moderator:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqModerator = (int)NudMaxReq.Value;
                    break;

                case Enums.TwitchUserLevels.Broadcaster:
                    if (NudMaxReq.Value != null) Settings.TwSrMaxReqBroadcaster = (int)NudMaxReq.Value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // assign mw to mainwindow for calling methods and setting texts etc
            foreach (Window window in Application.Current.Windows)
                if (window.GetType() == typeof(MainWindow))
                    _mW = window;

            // add colors to the combobox
            foreach (string s in ThemeManager.Current.ColorSchemes)
            {
                ComboBoxItem i = new()
                {
                    Content = s,
                };

                Theme x = ThemeManager.Current.GetTheme(Settings.Theme + "." + s);
                if (x != null)
                {
                    SolidColorBrush brush = new(Color.FromRgb(x.PrimaryAccentColor.R, x.PrimaryAccentColor.G,
                        x.PrimaryAccentColor.B));
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

            await SetControls();
        }

        private void Tb_ClientID_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.ClientId = TbClientId.Text;
        }

        private void Tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientSecret = TbClientSecret.Password;
        }

        private void Tgl_AnnounceInChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AnnounceInChat = TglAnnounceInChat.IsOn;
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, EventArgs e)
        {
            //Settings.UseOwnApp = TglswSpotify.IsOn;
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

        private void Txtbx_twChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).Text = ((TextBox)sender).Text.ToLower().Trim();
            // Sets the twitch channel
            Settings.TwChannel = TxtbxTwChannel.Text.Trim();
        }

        private void Txtbx_twOAuth_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Sets the twitch oauth token
            Settings.TwOAuth = TxtbxTwOAuth.Password;
        }

        private void Txtbx_twUser_TextChanged(object sender, TextChangedEventArgs e)
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
            if (!IsLoaded)
                return;
            // write custom output format to settings
            if (TxtbxOutputformat.Text == Settings.OutputString)
                return;
            Settings.OutputString = TxtbxOutputformat.Text;
            GlobalObjects.ForceUpdate = true;
        }

        private void TxtbxOutputformat2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            if (TxtbxOutputformat2.Text == Settings.OutputString2)
                return;
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
            NudMaxReq.Value = (Enums.TwitchUserLevels)CbxUserLevelsMaxReq.SelectedIndex switch
            {
                Enums.TwitchUserLevels.Viewer => Settings.TwSrMaxReqEveryone,
                Enums.TwitchUserLevels.Follower => Settings.TwSrMaxReqFollower,
                Enums.TwitchUserLevels.Vip => Settings.TwSrMaxReqVip,
                Enums.TwitchUserLevels.Subscriber => Settings.TwSrMaxReqSubscriber,
                Enums.TwitchUserLevels.SubscriberT2 => Settings.TwSrMaxReqSubscriberT2,
                Enums.TwitchUserLevels.SubscriberT3 => Settings.TwSrMaxReqSubscriberT3,
                Enums.TwitchUserLevels.Moderator => Settings.TwSrMaxReqModerator,
                Enums.TwitchUserLevels.Broadcaster => Settings.TwSrMaxReqBroadcaster,
                _ => throw new ArgumentOutOfRangeException()
            };
            NudMaxReq.ValueChanged += NudMaxReq_ValueChanged;
        }

        private void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            if (NudMaxReq.Value == null) return;
            Settings.TwSrMaxReqEveryone = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqFollower = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqVip = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqSubscriber = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqSubscriberT2 = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqSubscriberT3 = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqModerator = (int)NudMaxReq.Value;
            Settings.TwSrMaxReqBroadcaster = (int)NudMaxReq.Value;
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
            if (TwitchHandler.TwitchApi == null)
                return;
            if (TwitchHandler.TokenCheck == null)
                return;

            if (Settings.TwitchUser.BroadcasterType != "")
                try
                {
                    List<CustomReward> managableRewards = await TwitchHandler.GetChannelRewards(true);
                    List<CustomReward> rewards = await TwitchHandler.GetChannelRewards(false);
                    //Comapre all reward.id with Settings.TwRewardId and remove from Settings where no ID was found
                    List<string> idsToRemove = Settings.TwRewardId.Where(s => rewards.All(o => o.Id != s)).ToList();
                    foreach (string s in idsToRemove)
                    {
                        Settings.TwRewardId.Remove(s);
                    }

                    ListboxRewards.Items.Clear();

                    if (rewards.Count > 0)
                    {
                        foreach (CustomReward reward in rewards.OrderBy(o => o.Cost))
                        {

                            bool manageable = managableRewards.Find(r => r.Id == reward.Id) != null;
                            ListboxRewards.Items.Add(new UcTwitchReward(reward, manageable));
                        }
                    }
                    BtnCreateNewReward.IsEnabled = true;
                }
                catch (Exception e)
                {
                    Logger.LogExc(e);
                }
        }

        private void BtnLogInTwitch_Click(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(Enums.TwitchAccount.Main);
        }

        private void ToggleSwitchPrivacy_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
            {
                PnlTwich.Visibility = Visibility.Collapsed;
                PnlTwichBot.Visibility = Visibility.Collapsed;
                PnlSpotify.Visibility = Visibility.Collapsed;
            }
            else
            {
                PnlTwich.Visibility = Visibility.Visible;
                PnlTwichBot.Visibility = Visibility.Visible;
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

            BtnWebserverStart.Content = GlobalObjects.WebServer.Run
                ? Properties.Resources.sw_WebServer_StopWebServer
                : Properties.Resources.sw_WebServer_StartWebServer;
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
            WindowCreateCustomReward createCustomReward = new()
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

        private async void BtnTwitchLogout_OnClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Tag.ToString().ToLower())
            {
                case "main":
                    Settings.TwitchAccessToken = "";
                    Settings.TwitchUser = null;
                    TwitchHandler.TwitchApi = null;
                    break;

                case "bot":
                    Settings.TwitchBotToken = "";
                    Settings.TwitchBotUser = null;
                    break;
            }

            await SetControls();
        }

        private void NudServerPort_MinimumReached(object sender, RoutedEventArgs e)
        {
            NudServerPort.Value = 1025;
        }

        private void NudServerPort_MaximumReached(object sender, RoutedEventArgs e)
        {
            NudServerPort.Value = 66535;
        }

        private void Tgl_InformChat_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.ChatLiveStatus = TglInformChat.IsOn;
        }

        private void BtnLogInTwitchBot_OnClick(object sender, RoutedEventArgs e)
        {
            TwitchHandler.ApiConnect(Enums.TwitchAccount.Bot);
        }

        private void ComboboxRedirectPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.TwitchRedirectPort = (int)ComboboxRedirectPort.SelectedItem;
        }

        private void ComboboxfetchPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.TwitchFetchPort = (int)ComboboxfetchPort.SelectedItem;
        }

        private void Cb_SpotifyPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            if ((((ComboBox)sender).SelectedItem as ComboBoxItem)?.Content is not UcPlaylistItem item)
                return;
            Settings.SpotifyPlaylistId = item.Playlist.Id;
        }

        private async void CbAccountSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded)
                    return;
                await ResetTwitchConnection();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public async Task ResetTwitchConnection()
        {
            Settings.TwAcc = ((UcAccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).Username;
            Settings.TwOAuth = ((UcAccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).OAuth;
            await TwitchHandler.Client?.DisconnectAsync()!;
            TwitchHandler.Client = null;
            await TwitchHandler.ConnectTwitchChatClient();
            _ = SetControls();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            ((ComboBox)sender).SelectedIndex = 0;
        }

        private void CbxUserLevelsRewardChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsReward, value];
            Settings.UserLevelsReward = list;
        }

        private void CbxUserLevelsRewardUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsReward];
            list.Remove(value);
            Settings.UserLevelsReward = list;
        }

        private void CbxUserLevelsCommandChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsCommand, value];
            Settings.UserLevelsCommand = list;
        }

        private void CbxUserLevelsCommandUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsCommand];
            list.Remove(value);
            Settings.UserLevelsCommand = list;
        }

        private void TglAddToPlaylist_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.AddSrToPlaylist = ((ToggleSwitch)sender).IsOn;
        }

        private async void BtnReloadPlaylists_Click(object sender, RoutedEventArgs e)
        {
            CbSpotifyPlaylist.IsEnabled = false;
            ((Button)sender).IsEnabled = false;
            await LoadSpotifyPlaylists(true);
            CbSpotifyPlaylist.IsEnabled = true;
            ((Button)sender).IsEnabled = true;
        }

        private async Task LoadSpotifyPlaylists(bool forceSync = false)
        {
            while (true)
            {
                if (forceSync)
                {
                    if (SpotifyApiHandler.Client == null) return;
                    try
                    {
                        GlobalObjects.SpotifyProfile ??= await SpotifyApiHandler.GetUser();
                        if (GlobalObjects.SpotifyProfile == null) return;

                        CbSpotifyPlaylist.Items.Clear();
                        CbSpotifySongLimitPlaylist.Items.Clear();

                        CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                        {
                            Content = new UcPlaylistItem(new FullPlaylist
                            {
                                Collaborative = false,
                                ExternalUrls = null,
                                Href = null,
                                Id = "-1",
                                Images =
                                [
                                    new Image()
                                    {
                                        Url = "https://misc.scdn.co/liked-songs/liked-songs-640.png", Width = 640,
                                        Height = 640
                                    }
                                ],
                                Name = "Liked Songs",
                                Owner = null,
                                Public = false,
                                SnapshotId = null,
                                Tracks = null,
                                Type = null,
                                Uri = null
                            })
                        });

                        Paging<FullPlaylist> playlists =
                            await SpotifyApiHandler.GetUserPlaylists();
                        if (playlists == null) return;
                        List<FullPlaylist> playlistCache = [];
                        CbSpotifyPlaylist.SelectionChanged -= Cb_SpotifyPlaylist_SelectionChanged;

                        foreach (FullPlaylist playlist in playlists.Items
                                     .Where(playlist =>
                                         playlist?.Owner?.Id != null &&
                                         playlist.Owner.Id == GlobalObjects.SpotifyProfile?.Id))
                        {
                            CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                            { Content = new UcPlaylistItem(playlist) });
                            CbSpotifySongLimitPlaylist.Items.Add(new ComboBoxItem
                            { Content = new UcPlaylistItem(playlist) });
                            playlistCache.Add(playlist);
                            Thread.Sleep(100);
                        }

                        CbSpotifyPlaylist.SelectionChanged += Cb_SpotifyPlaylist_SelectionChanged;
                        Settings.SpotifyPlaylistCache = playlistCache;

                        if (!string.IsNullOrEmpty(Settings.SpotifyPlaylistId))
                            CbSpotifyPlaylist.SelectedItem = CbSpotifyPlaylist.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id == Settings.SpotifyPlaylistId);
                        if (!string.IsNullOrEmpty(Settings.SpotifySongLimitPlaylist))
                            CbSpotifySongLimitPlaylist.SelectedItem = CbSpotifySongLimitPlaylist.Items
                                .Cast<ComboBoxItem>().FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id == Settings.SpotifySongLimitPlaylist);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExc(ex);
                    }
                }
                else
                {
                    if (Settings.SpotifyPlaylistCache.Count > 0)
                    {
                        CbSpotifySongLimitPlaylist.Items.Clear();
                        CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                        {
                            Content = new UcPlaylistItem(new FullPlaylist
                            {
                                Collaborative = false,
                                ExternalUrls = null,
                                Href = null,
                                Id = "-1",
                                Images =
                                [
                                    new Image
                                    {
                                        Url = "https://misc.scdn.co/liked-songs/liked-songs-640.png", Width = 640,
                                        Height = 640
                                    }
                                ],
                                Name = "Liked Songs",
                                Owner = null,
                                Public = false,
                                SnapshotId = null,
                                Tracks = null,
                                Type = null,
                                Uri = null
                            })
                        });
                        foreach (FullPlaylist playlist in Settings.SpotifyPlaylistCache)
                        {
                            CbSpotifyPlaylist.Items.Add(new ComboBoxItem { Content = new UcPlaylistItem(playlist) });
                            CbSpotifySongLimitPlaylist.Items.Add(new ComboBoxItem
                            { Content = new UcPlaylistItem(playlist) });
                        }

                        if (!string.IsNullOrEmpty(Settings.SpotifyPlaylistId))
                            CbSpotifyPlaylist.SelectedItem = CbSpotifyPlaylist.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id == Settings.SpotifyPlaylistId);
                        if (!string.IsNullOrEmpty(Settings.SpotifySongLimitPlaylist))
                            CbSpotifySongLimitPlaylist.SelectedItem = CbSpotifySongLimitPlaylist.Items
                                .Cast<ComboBoxItem>().FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id == Settings.SpotifySongLimitPlaylist);
                    }
                    else
                    {
                        forceSync = true;
                        continue;
                    }
                }

                break;
            }
        }

        private void TglLimitSrPlaylist_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.LimitSrToPlaylist = ((ToggleSwitch)sender).IsOn;
            CbSpotifySongLimitPlaylist.IsEnabled = Settings.LimitSrToPlaylist;
        }

        private void CbSpotifySongLimitPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            if ((((ComboBox)sender).SelectedItem as ComboBoxItem)?.Content is not UcPlaylistItem item)
                return;
            Settings.SpotifySongLimitPlaylist = item.Playlist.Id;
        }

        private void Chbx_BlockAllExplicit_Checked(object sender, RoutedEventArgs e)
        {
            Settings.BlockAllExplicitSongs = ((ToggleSwitch)sender).IsOn;
        }

        private void TbRequesterPrefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.RequesterPrefix = TbRequesterPrefix.Text;
        }

        private void TglUseDefaultBrowser_OnToggled(object sender, RoutedEventArgs e)
        {
            Settings.UseDefaultBrowser = ((ToggleSwitch)sender).IsOn;
        }

        private async void TglDonationReminder_OnToggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            if (Settings.DonationReminder == ((ToggleSwitch)sender).IsOn)
                return;

            Settings.DonationReminder = ((ToggleSwitch)sender).IsOn;
            if (!((ToggleSwitch)sender).IsOn) return;

            MessageDialogResult msgResult = await this.ShowMessageAsync("Hey 👋",
                "No more donation messages!\n\nRemember, our app stays free and accessible thanks to the support from people like you. If you ever feel like getting those warm, fuzzy feelings that come from supporting a good cause, you can \"Open Ko-Fi\" and donate right away.\n\nEnjoy your clutter-free experience! ✨",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Just Close 😞",
                    NegativeButtonText = "Open Ko-Fi ❤️"
                });
            switch (msgResult)
            {
                case MessageDialogResult.Negative:
                    Process.Start("https://ko-fi.com/overcodetv");
                    return;

                case MessageDialogResult.Affirmative:
                    break;

                case MessageDialogResult.Canceled:
                case MessageDialogResult.FirstAuxiliary:
                case MessageDialogResult.SecondAuxiliary:
                default:
                    return;
            }
        }

        private void BtnLogInTwitchAlt_Click(object sender, RoutedEventArgs e)
        {
            WindowManualTwitchLogin manualTwitchLogin = new(
                (sender is not Button button || button.Tag.ToString().Equals("main", StringComparison.CurrentCultureIgnoreCase))
                    ? Enums.TwitchAccount.Main
                    : Enums.TwitchAccount.Bot)
            {
                Owner = this
            };
            manualTwitchLogin.Show();
        }

        private void CbPauseOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            if ((Enums.PauseOptions)CbPauseOptions.SelectedIndex == Settings.PauseOption)
                return;
            Settings.PauseOption = (Enums.PauseOptions)CbPauseOptions.SelectedIndex;
        }

        private void ChbxSpacesSplitFiles_Checked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked != null)
                Settings.AppendSpacesSplitFiles = (bool)isChecked;
        }

        private void CooldownSpinner_OnValueChangedpinner_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded)
                return;
            if (NudCooldownPerUser.Value.HasValue)
            {
                Settings.TwSrPerUserCooldown = (int)NudCooldownPerUser.Value;
                int totalSeconds = (int)NudCooldownPerUser.Value.Value;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                UserCooldownDisplay.Text = $"({minutes:D2}:{seconds:D2})";
            }
        }

        private void Tgl_KeepCover_OnToggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            Settings.KeepAlbumCover = ((ToggleSwitch)sender).IsOn;
        }

        private void TglCanvas_OnToggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            Settings.DownloadCanvas = ((ToggleSwitch)sender).IsOn;
        }

        private async void Btn_YTMDesktopLink_Click(object sender, RoutedEventArgs e)
        {
            const string baseUrl = "http://localhost:9863/api/v1/";
            YtmdAuthentication auth = new(baseUrl);

            try
            {
                // Step 1: Request the auth code
                string appId = "songify";
                string appName = "Songify";
                string appVersion = FormatAppVersion(GlobalObjects.AppVersion);

                string authCode = await auth.RequestAuthCodeAsync(appId, appName, appVersion);

                TbYTMDesktopAuthcode.Text = $"AUTH CODE: {authCode}";
                PnlYTMDesktopAuthcode.Visibility = Visibility.Visible;
                Activate();

                // Step 2: Request the token using the auth code
                string token = await auth.RequestTokenAsync(appId, authCode);
                if (string.IsNullOrEmpty(token)) return;
                TbYTMDesktopToken.Password = token;
                Settings.YtmdToken = token;
                await ((MainWindow)Application.Current.MainWindow)?.StartYtmdSocketIoClient()!;
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static string FormatAppVersion(string appVersion)
        {
            if (string.IsNullOrWhiteSpace(appVersion))
                throw new ArgumentException("App version cannot be null or empty.");

            string[] parts = appVersion.Split('.'); // Split by dots
            if (parts.Length < 3)
                throw new ArgumentException("App version must have at least three components.");

            return string.Join(".", parts[0], parts[1], parts[2]); // Join the first three parts
        }

        private void BtnResponseParams_OnClick(object sender, RoutedEventArgs e)
        {
            _wRp ??= new Window_ResponseParams
            {
                Left = Left + Width,
                Top = Top,
                Owner = this,
                Height = Height
            };
            if (!_wRp.IsLoaded)
                _wRp = new Window_ResponseParams
                {
                    Left = Left + Width,
                    Top = Top,
                    Owner = this,
                    Height = Height
                };
            if (_wRp.IsVisible)
                _wRp.Hide();
            else
                _wRp.Show();
        }

        public void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            if (_wRp is not { } responseParams) return;
            responseParams.LocationChanged -= responseParams.Window_ResponseParams_OnLocationChanged;
            responseParams.Left = Left + Width;
            responseParams.Top = Top;
            responseParams.LocationChanged += responseParams.Window_ResponseParams_OnLocationChanged;

        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_wRp == null) return;
            _wRp.Height = Height;
            _wRp.LocationChanged -= _wRp.Window_ResponseParams_OnLocationChanged;
            _wRp.Left = Left + Width;
            _wRp.Top = Top;
            _wRp.LocationChanged += _wRp.Window_ResponseParams_OnLocationChanged;

        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void CbxUnlimitedRewardChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UnlimitedSrUserlevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsReward, value];
            Settings.UnlimitedSrUserlevelsReward = list;
        }

        private void CbxUnlimitedRewardUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UnlimitedSrUserlevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsReward];
            list.Remove(value);
            Settings.UnlimitedSrUserlevelsReward = list;
        }

        private void CbxUnlimitedCommandUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UnlimitedSrUserlevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsCommand];
            list.Remove(value);
            Settings.UnlimitedSrUserlevelsCommand = list;
        }

        private void CbxUnlimitedCommandChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UnlimitedSrUserlevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsCommand, value];
            Settings.UnlimitedSrUserlevelsCommand = list;
        }

        private void CbxSpotifyRedirectUri_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            Settings.SpotifyRedirectUri = ((ComboBox)sender).SelectedIndex switch
            {
                0 => "localhost",
                1 => "127.0.0.1",
                _ => Settings.SpotifyRedirectUri
            };
        }

        private async void TglsLongBadgeNames_OnToggled(object sender, RoutedEventArgs e)
        {
            Settings.LongBadgeNames = ((ToggleSwitch)sender).IsOn;
            await LoadCommands();
        }

        private void Tglsw_OnlyAddToPlaylist_OnToggled(object sender, RoutedEventArgs e)
        {
            Settings.AddSrtoPlaylistOnly = ((ToggleSwitch)sender).IsOn;
        }

        private void NudBits_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded) return;
            if (!NudBits.Value.HasValue) return;
            Settings.TwSrPerUserCooldown = (int)NudBits.Value;
            string imageName = GetImageNameForValue((int)NudBits.Value);
            string uri = $"pack://application:,,,/Resources/img/{imageName}.png";
            ImgBits.Source = new BitmapImage(new Uri(uri));
        }

        private static string GetImageNameForValue(int value)
        {
            return value switch
            {
                >= 10000 => "10000",
                >= 5000 => "5000",
                >= 1000 => "1000",
                >= 100 => "100",
                _ => "1"
            };
        }
    }
}