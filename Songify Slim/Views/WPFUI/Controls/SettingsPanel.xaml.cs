using Songify_Slim.Models.Pear;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Models.Twitch;
using Songify_Slim.UserControls;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Songify.TwitchOAuth;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Youtube.Youtube;
using Songify_Slim.Views;
using Songify_Slim.Views.WPFUI;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using static Songify_Slim.App;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using File = System.IO.File;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = System.Windows.Controls.MenuItem;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using TextBlock = System.Windows.Controls.TextBlock;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace Songify_Slim.Views.WPFUI.Controls
{
    // ReSharper disable once InconsistentNaming
    public partial class SettingsPanel
    {
        private static string Loc(string key, string fallback)
            => Application.Current?.TryFindResource(key) as string ?? fallback;

        private static string LocFormat(string key, string fallback, params object[] args)
        {
            try { return string.Format(Loc(key, fallback), args); }
            catch (FormatException) { return fallback; }
        }

        private Task<AppDialogResult> ShowMsgAsync(
            string title,
            string message,
            AppDialogStyle style = AppDialogStyle.Primary,
            AppDialogSettings settings = null)
            => AppDialog.ShowAsync(title, message, style, settings);

        public void OpenResponseParams() => BtnResponseParams_OnClick(null, null);

        private readonly Dictionary<Enums.RefundCondition, ToggleSwitch> _toggleMap = new();
        private readonly bool _appIdInitialValue = Settings.UseOwnApp;
        private readonly FolderBrowserDialog _fbd = new();
        private Window_ResponseParams _wRp;
        private bool _showPassword;
        private CancellationTokenSource _premiumRefreshCts;
        private bool _isSettingControls;
        private int _externalUiMutationDepth;
        private bool _rewardsLoadStarted;
        private bool _rewardsLoading;
        private BitmapImage? _defaultSongifyProfileImage;
        private bool _uiScalePointerActive;
        private bool _accentWheelUpdating;
        private DispatcherTimer _accentPreviewTimer;
        private Color _pendingAccentPreview;
        private bool _hasPendingAccentPreview;

        /// <summary>True while binding controls or before the window is ready - skip save/side-effect handlers.</summary>
        private bool IgnoreControlEvents => !IsLoaded || _isSettingControls || _externalUiMutationDepth > 0;

        /// <summary>Used by <see cref="SettingsUi"/> while theme/style changes may reset PasswordBoxes.</summary>
        public void BeginExternalUiMutation() => _externalUiMutationDepth++;

        public void EndExternalUiMutation(bool reloadSecrets)
        {
            if (_externalUiMutationDepth > 0)
                _externalUiMutationDepth--;

            if (reloadSecrets && _externalUiMutationDepth == 0 && IsLoaded)
                ReloadSecretFieldsFromSettings();
        }

        private void ReloadSecretFieldsFromSettings()
        {
            _isSettingControls = true;
            try
            {
                if (PasswordBox != null)
                    PasswordBox.Password = Settings.SongifyApiKey ?? "";
                if (TextBox != null)
                    TextBox.Text = Settings.SongifyApiKey ?? "";
                UpdateSongifyTokenStatus();
                if (PasswordBox_YoutubeApiKey != null)
                    PasswordBox_YoutubeApiKey.Password = Settings.YoutubeApiKey ?? "";
                if (PasswordBox_WebServer != null)
                    PasswordBox_WebServer.Password = Settings.WebServerPassword ?? "";
            }
            finally
            {
                _isSettingControls = false;
            }
        }

        private Dictionary<string, string> _supportedLanguages = LocalizationHelper.GetLanguages();

        private static Dictionary<Enums.RefundCondition, string> RefundConditionLabels => new()
        {
            {
                Enums.RefundCondition.UserLevelTooLow,
                Properties.Resources.window_settings_integration_refund_user_level_low
            },
            {
                Enums.RefundCondition.UserBlocked,
                Properties.Resources.window_settings_integration_refund_user_blocked
            },
            {
                Enums.RefundCondition.SpotifyNotConnected,
                Properties.Resources.window_settings_integration_refund_spotify_not_connected
            },
            {
                Enums.RefundCondition.SongUnavailable,
                Properties.Resources.window_settings_integration_refund_song_not_available
            },
            {
                Enums.RefundCondition.WrongPlayerRequested,
                Properties.Resources.window_settings_integration_refund_wrong_player_requested
            },
            {
                Enums.RefundCondition.SongBlocked,
                Properties.Resources.window_settings_integration_refund_song_blocked
            },
            {
                Enums.RefundCondition.ArtistBlocked,
                Properties.Resources.window_settings_integration_refund_artist_blocked
            },
            {
                Enums.RefundCondition.SongTooLong,
                Properties.Resources.window_settings_integration_refund_song_too_long
            },
            {
                Enums.RefundCondition.SongAlreadyInQueue,
                Properties.Resources.window_settings_integration_refund_song_already_in_queue
            },
            {
                Enums.RefundCondition.QueueLimitReached,
                Properties.Resources.window_settings_integration_refund_queue_limit
            },
            {
                Enums.RefundCondition.NoSongFound,
                Properties.Resources.window_settings_integration_refund_no_song_found
            },
            {
                Enums.RefundCondition.SongAddedButError,
                Properties.Resources.window_settings_integration_refund_song_added
            },
            {
                Enums.RefundCondition.TrackIsExplicit,
                Properties.Resources.window_settings_integration_refund_track_explicit
            },
            {
                Enums.RefundCondition.OnSuccess,
                Properties.Resources.window_settings_integration_refund_always
            },
        };

        public SettingsPanel()
        {
            InitializeComponent();
        }

        private void OnPremiumStatusChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(UpdateSongifyTokenStatus);
                return;
            }

            UpdateSongifyTokenStatus();
        }

        public async Task SetControls()
        {
            if (_isSettingControls)
                return;

            Stopwatch sw = Stopwatch.StartNew();

            _isSettingControls = true;
            SetLoadingState(true);

            try
            {
                await Task.Yield();
                LogStep(sw, "Start");

                EnsureSettingsDefaults();
                LogStep(sw, "EnsureSettingsDefaults");

                await LoadCommands();
                LogStep(sw, "LoadCommands");

                InitializeUserLevelComboboxes();
                LogStep(sw, "InitializeUserLevelComboboxes");

                ApplyPollSettings();
                LogStep(sw, "ApplyPollSettings");

                ApplyGeneralSettings();
                LogStep(sw, "ApplyGeneralSettings");

                InitializePortComboboxes();
                LogStep(sw, "InitializePortComboboxes");

                InitializeBotResponsesControl();
                LogStep(sw, "InitializeBotResponsesControl");

                ApplyUserLevelCheckboxes();
                LogStep(sw, "ApplyUserLevelCheckboxes");

                GenerateRefundConditionToggles();
                LogStep(sw, "GenerateRefundConditionToggles");

                await LoadSpotifySectionAsync();
                LogStep(sw, "LoadSpotifySectionAsync");

                ApplyLanguageSettings();
                LogStep(sw, "ApplyLanguageSettings");

                ApplyTwitchAccountsUi();
                LogStep(sw, "ApplyTwitchAccountsUi");

                // Refund condition toggles are local UI - do not wait on Twitch rewards here.
                ApplyRefundConditions();
                LogStep(sw, "ApplyRefundConditions");

                LogStep(sw, "END");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error in Setting controls.", e);
            }
            finally
            {
                SetLoadingState(false);
                _isSettingControls = false;
            }

            // Rewards are fetched lazily when the Rewards tab is selected (UI virtualization).
            _rewardsLoadStarted = false;
            if (TabCtrl.SelectedItem is TabItem { Tag: "TwitchRewards" })
                _ = EnsureRewardsLoadedAsync();
        }

        private async void TabCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Nested TabControls also raise SelectionChanged (bubbling); ignore those.
            if (!ReferenceEquals(e.Source, TabCtrl))
                return;
            if (!IsLoaded || _isSettingControls)
                return;
            if (TabCtrl.SelectedItem is not TabItem { Tag: "TwitchRewards" })
                return;

            await EnsureRewardsLoadedAsync().ConfigureAwait(true);
        }

        private async Task EnsureRewardsLoadedAsync()
        {
            if (_rewardsLoadStarted || _rewardsLoading)
                return;

            _rewardsLoadStarted = true;
            try
            {
                if (BtnUpdateRewards != null)
                    BtnUpdateRewards.IsEnabled = false;
                await LoadRewardsSectionAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _rewardsLoadStarted = false;
                Logger.Error(LogSource.Twitch, "Lazy reward load failed.", ex);
            }
            finally
            {
                if (BtnUpdateRewards != null)
                    BtnUpdateRewards.IsEnabled = true;
            }
        }

        private static void LogStep(Stopwatch sw, string step)
        {
            Logger.Log(LogLevel.Debug, LogSource.Core,
                $"[SetControls] {step,-30} | {sw.ElapsedMilliseconds,6} ms");
        }

        private void SetLoadingState(bool isLoading)
        {
            GridLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            TabCtrl.IsEnabled = !isLoading;
        }

        private static void EnsureSettingsDefaults()
        {
            Settings.TwitchPollSettings ??= new TwitchPollSettings();
            Settings.UserLevelsCommand ??= [];
            Settings.UserLevelsReward ??= [];
            Settings.UserLevelsExplicitSongs ??= [];
            Settings.UnlimitedSrUserlevelsCommand ??= [];
            Settings.UnlimitedSrUserlevelsReward ??= [];
        }

        private void InitializeUserLevelComboboxes()
        {
            CbxUserLevelsMaxReq.SelectionChanged -= CbxUserLevelsMaxReq_SelectionChanged;

            CbxUserLevels.Items.Clear();
            CbxUserLevelsMaxReq.Items.Clear();

            Array values = Enum.GetValues(typeof(Enums.TwitchUserLevels));
            foreach (object value in values)
            {
                if (value.ToString() == "Broadcaster")
                    continue;

                CbxUserLevels.Items.Add(value);
                CbxUserLevelsMaxReq.Items.Add(value);
            }

            if (CbxUserLevelsMaxReq.Items.Count > 0)
                CbxUserLevelsMaxReq.SelectedIndex = 0;

            CbxUserLevelsMaxReq.SelectionChanged += CbxUserLevelsMaxReq_SelectionChanged;
        }

        private void ApplyPollSettings()
        {
            TextBoxPollTitle.Text = Settings.TwitchPollSettings.Title;
            TextBoxPollAnswer1.Text = Settings.TwitchPollSettings.Choices.First();
            TextBoxPollAnswer2.Text = Settings.TwitchPollSettings.Choices.Last();
            ToggleSwitchPollAdditionalVotes.IsChecked = Settings.TwitchPollSettings.AdditionalVotesEnabled;
            NumericUpDownPollChannelPointsPerVote.Value = Settings.TwitchPollSettings.ChannelPointsPerVote;
            NumericUpDownPollDuration.Value = Settings.TwitchPollSettings.Duration;

            if ((string)RadioButtonPollAnswer1.Content == Settings.TwitchPollSettings.Choices.First())
                RadioButtonPollAnswer1.IsChecked = true;
            else if ((string)RadioButtonPollAnswer2.Content == Settings.TwitchPollSettings.Choices.Last())
                RadioButtonPollAnswer2.IsChecked = true;
        }

        private void ApplyGeneralSettings()
        {
            NudMaxReq.Value = Settings.TwSrMaxReqEveryone;
            ThemeToggleSwitch.IsChecked = Settings.Theme == "BaseDark" || Settings.Theme == "Dark";

            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;

            Nb_MinimumMessagesBetweenAnnounces.Value = Settings.MinimumMessagesBetweenAnnounces;
            ChbxAutoClear.IsChecked = Settings.AutoClearQueue;
            ChbxTwAutoconnect.IsChecked = Settings.TwAutoConnect;
            ChbxTwReward.IsChecked = Settings.TwSrReward;
            ChbxAutostart.IsChecked = Settings.Autostart;
            TglCanvas.IsChecked = Settings.DownloadCanvas;
            CbPauseOptions.SelectedIndex = (int)Settings.PauseOption;
            ChbxMinimizeSystray.IsChecked = Settings.Systray;
            ChbxOpenQueueOnStartup.IsChecked = Settings.OpenQueueOnStartup;
            if (TglOpenQueuePopOutOnStartup != null)
                TglOpenQueuePopOutOnStartup.IsChecked = Settings.OpenQueuePopOutOnStartup;
            UpdateOpenQueuePopOutVisibility();
            if (TglOverruleShellMinWidth != null)
                TglOverruleShellMinWidth.IsChecked = Settings.OverruleShellMinWidth;
            BindNavigationPaneCombo();
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            ChbxSpacesSplitFiles.IsChecked = Settings.AppendSpacesSplitFiles;
            ChbxSplit.IsChecked = Settings.SplitOutput;
            ChbxUpload.IsChecked = Settings.Upload;
            NudSpaces.Value = Settings.SpaceCount;
            NudCooldown.Value = Settings.TwSrCooldown;
            NudCooldownPerUser.Value = Settings.TwSrPerUserCooldown;
            NudMaxlength.Value = Settings.MaxSongLength;
            TbClientId.Text = Settings.ClientId;
            TbClientSecret.Password = Settings.ClientSecret;
            TglAnnounceInChat.IsChecked = Settings.AnnounceInChat;
            TglswSpotify.IsChecked = true;
            TglUseDefaultBrowser.IsChecked = Settings.UseDefaultBrowser;
            Tglsw_OnlyAddToPlaylist.IsChecked = Settings.AddSrtoPlaylistOnly;
            TglSharedChat.IsChecked = Settings.SharedChatEnabled;
            RefreshIgnoredChatUsers();
            TextBox.Text = Settings.SongifyApiKey;
            PasswordBox.Password = Settings.SongifyApiKey;
            UpdateSongifyTokenStatus();
            PasswordBox_YoutubeApiKey.Password = Settings.YoutubeApiKey;
            NudBits.Value = Settings.MinimumBitsForSr;
            TbBitsKeyword.Text = Settings.SrForBitsKeyWord;
            TxtbxTwChannel.Text = Settings.TwChannel;
            TxtbxTwOAuth.Password = Settings.TwOAuth;
            TxtbxTwUser.Text = Settings.TwAcc;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            TxtbxOutputformat2.Text = Settings.OutputString2;
            CbxUserLevels.SelectedIndex = Settings.TwSrUserLevel == -1 ? 0 : Settings.TwSrUserLevel;
            NudServerPort.Value = Settings.WebServerPort;
            tgl_KeepCover.IsChecked = Settings.KeepAlbumCover;
            TglAutoStartWebserver.IsChecked = Settings.AutoStartWebServer;
            TglWebServerPassword.IsChecked = Settings.WebServerPasswordEnabled;
            PasswordBox_WebServer.Password = Settings.WebServerPassword ?? "";
            TbWebServerAdminWarning.Visibility = WebServer.IsRunningAsAdministrator()
                ? Visibility.Visible
                : Visibility.Collapsed;
            CbxReleaseChannel.SelectedIndex = (int)Settings.ReleaseChannel;
            TglOnlyWorkWhenLive.IsChecked = Settings.BotOnlyWorkWhenLive;
            TglInformChat.IsEnabled = Settings.BotOnlyWorkWhenLive;
            ToggleSwitchUnlimitedSr.IsChecked = Settings.TwSrUnlimitedSr;
            Tglsw_BitsForSr.IsChecked = Settings.SrForBits;
            TglInformChat.IsChecked = Settings.ChatLiveStatus;
            TglAddToPlaylist.IsChecked = Settings.AddSrToPlaylist;
            Tglsw_BlockAllExplicitSongs.IsChecked = Settings.BlockAllExplicitSongs;
            CbxAllowedUserLevelsExplicit.IsEnabled = Settings.BlockAllExplicitSongs;
            NudSpotifyFetchRate.Value = Settings.SpotifyFetchRate;
            TglBypassSpotifyFetchGate.IsChecked = Settings.BypassSpotifyFetchGate;
            TglShowSpotifyToasts.IsChecked = Settings.ShowSpotifyToasts;
            LoadArtistBlocklistSyncControls();
            TbRequesterPrefix.Text = Settings.RequesterPrefix;
            TglDonationReminder.IsChecked = Settings.DonationReminder;
            TglsLongBadgeNames.IsChecked = Settings.LongBadgeNames;
            TglDebugLogging.IsChecked = Settings.DebugLogging;
            NudLogFileRetention.Value = Settings.LogFileRetentionCount;
            TglOnlySkipNonSrRewards.IsChecked = Settings.SkipOnlyNonSrSongs;

            BtnWebserverStart.Content = GlobalObjects.WebServer.Run
                ? Properties.Resources.window_settings_webserver_stop
                : Properties.Resources.window_settings_webserver_start;
        }

        private void InitializePortComboboxes()
        {
            ComboboxRedirectPort.SelectionChanged -= ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged -= ComboboxfetchPort_SelectionChanged;

            ComboboxRedirectPort.Items.Clear();
            ComboboxfetchPort.Items.Clear();

            ApplicationDetails.RedirectPorts.ForEach(i => ComboboxRedirectPort.Items.Add(i));
            ApplicationDetails.FetchPorts.ForEach(i => ComboboxfetchPort.Items.Add(i));

            ComboboxRedirectPort.SelectedItem = Settings.TwitchRedirectPort;
            ComboboxfetchPort.SelectedItem = Settings.TwitchFetchPort;

            ComboboxRedirectPort.SelectionChanged += ComboboxRedirectPort_SelectionChanged;
            ComboboxfetchPort.SelectionChanged += ComboboxfetchPort_SelectionChanged;
        }

        private void InitializeBotResponsesControl()
        {
            Cctrl.Content = new UcBotResponses();
        }

        private void ApplyUserLevelCheckboxes()
        {
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

            ChckUlExplicitViewer.IsChecked = Settings.UserLevelsExplicitSongs.Contains(0);
            ChckUlExplicitFollower.IsChecked = Settings.UserLevelsExplicitSongs.Contains(1);
            ChckUlExplicitSub.IsChecked = Settings.UserLevelsExplicitSongs.Contains(2);
            ChckUlExplicitSubT2.IsChecked = Settings.UserLevelsExplicitSongs.Contains(3);
            ChckUlExplicitSubT3.IsChecked = Settings.UserLevelsExplicitSongs.Contains(4);
            ChckUlExplicitVip.IsChecked = Settings.UserLevelsExplicitSongs.Contains(5);
            ChckUlExplicitMod.IsChecked = Settings.UserLevelsExplicitSongs.Contains(6);

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

            RefreshUserLevelComboSummaries();
        }

        private static readonly string[] UserLevelShortNames =
        [
            "Viewer", "Follower", "Sub", "Sub T2", "Sub T3", "VIP", "Mod"
        ];

        private static string FormatUserLevelsSummary(IReadOnlyCollection<int> levels, string emptyFallback)
        {
            List<int> sorted = (levels ?? Array.Empty<int>())
                .Where(i => i >= 0 && i < UserLevelShortNames.Length)
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            if (sorted.Count == 0)
                return emptyFallback;
            if (sorted.Count >= UserLevelShortNames.Length)
                return "All";

            string text = string.Join(", ", sorted.Select(i => UserLevelShortNames[i]));
            return text.Length > 32 ? $"{sorted.Count} selected" : text;
        }

        private void RefreshUserLevelComboSummaries()
        {
            try
            {
                string none = Properties.Resources.window_settings_user_levels_none;
                if (CbiAllowedUserLevelsRewardSummary != null)
                    CbiAllowedUserLevelsRewardSummary.Content =
                        FormatUserLevelsSummary(Settings.UserLevelsReward, none);
                if (CbiAllowedUserLevelsCommandSummary != null)
                    CbiAllowedUserLevelsCommandSummary.Content =
                        FormatUserLevelsSummary(Settings.UserLevelsCommand, none);
                if (CbiAllowedUserLevelsExplicitSummary != null)
                    CbiAllowedUserLevelsExplicitSummary.Content =
                        FormatUserLevelsSummary(Settings.UserLevelsExplicitSongs, none);
                if (CbiUnlimitedCommandSummary != null)
                    CbiUnlimitedCommandSummary.Content =
                        FormatUserLevelsSummary(Settings.UnlimitedSrUserlevelsCommand, none);
                if (CbiUnlimitedRewardSummary != null)
                    CbiUnlimitedRewardSummary.Content =
                        FormatUserLevelsSummary(Settings.UnlimitedSrUserlevelsReward, none);
            }
            catch
            {
                // ignored - summaries are cosmetic
            }
        }

        private async Task LoadSpotifySectionAsync()
        {
            if (SpotifyApiHandler.Client == null)
                return;

            PrivateUser? profile = null;
            try
            {
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                profile = Settings.SpotifyProfile ?? await SpotifyApiHandler.GetUser(cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }

            if (profile == null)
            {
                ImgSpotifyProfile.ImageSource = null;
                return;
            }

            LblSpotifyAcc.Content =
                $"{Properties.Resources.window_settings_integration_spotify_linked} {profile.DisplayName ?? "(unknown)"}";

            try
            {
                if (profile.Images is { Count: > 0 } && !string.IsNullOrEmpty(profile.Images[0].Url))
                {
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(profile.Images[0].Url, UriKind.Absolute);
                    bitmap.EndInit();

                    ImgSpotifyProfile.ImageSource = bitmap;
                }
                else
                {
                    ImgSpotifyProfile.ImageSource = null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                ImgSpotifyProfile.ImageSource = null;
            }

            try
            {
                await LoadSpotifyPlaylists();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void ApplyTwitchAccountsUi()
        {
            CbAccountSelection.SelectionChanged -= CbAccountSelection_SelectionChanged;
            CbAccountSelection.Items.Clear();

            ApplyMainTwitchAccountUi();
            ApplyBotTwitchAccountUi();
            SelectCurrentAccount();

            CbAccountSelection.SelectionChanged += CbAccountSelection_SelectionChanged;
        }

        private void ApplyMainTwitchAccountUi()
        {
            if (Settings.TwitchUser != null)
            {
                BtnTwitchLogout.Visibility = Visibility.Visible;
                BtnTwitchRefreshMain.Visibility = Visibility.Collapsed;

                UpdateTwitchUserUi(Settings.TwitchUser, ImgTwitchProfile, LblTwitchName, BtnLogInTwitch, 0,
                    BtnLogInTwitchAlt);

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
                BtnTwitchLogout.Visibility = Visibility.Collapsed;
                BtnTwitchRefreshMain.Visibility = Visibility.Collapsed;
                LblTwitchName.Content = Loc("window_settings_main_account", "Main Account:");

                ImgTwitchProfile.ImageSource = GetDefaultSongifyProfileImage();
            }
        }

        private void ApplyBotTwitchAccountUi()
        {
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
                BtnTwitchBotLogout.Visibility = Visibility.Collapsed;
                BtnTwitchRefreshBot.Visibility = Visibility.Collapsed;
                LblTwitchBotName.Content = Loc("window_settings_bot_account", "Bot Account:");

                ImgTwitchBotProfile.ImageSource = GetDefaultSongifyProfileImage();
            }
        }

        private BitmapImage GetDefaultSongifyProfileImage()
        {
            if (_defaultSongifyProfileImage != null)
                return _defaultSongifyProfileImage;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("pack://application:,,,/Resources/songify.ico", UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _defaultSongifyProfileImage = bitmap;
            return _defaultSongifyProfileImage;
        }

        private void SelectCurrentAccount()
        {
            if (string.IsNullOrEmpty(Settings.TwAcc))
            {
                if (CbAccountSelection.Items.Count > 0)
                    CbAccountSelection.SelectedIndex = 0;

                return;
            }

            CbAccountSelection.SelectedItem = CbAccountSelection.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item =>
                    ((UcAccountItem)item.Content).Username != null &&
                    ((UcAccountItem)item.Content).Username == Settings.TwAcc);
        }

        private async Task LoadRewardsSectionAsync()
        {
            if (TwitchHandler.TwitchApi == null)
                return;

            await LoadRewards();
        }

        private void ApplyRefundConditions()
        {
            List<Enums.RefundCondition> selected = Settings.RefundConditons ?? [];
            foreach (KeyValuePair<Enums.RefundCondition, ToggleSwitch> kvp in _toggleMap)
                kvp.Value.IsChecked = selected.Contains(kvp.Key);
        }

        private void ApplyLanguageSettings()
        {
            CbxLanguage.SelectionChanged -= ComboBox_SelectionChanged;
            CbxLanguage.ItemsSource = LocalizationHelper.GetLanguages();
            CbxLanguage.SelectedValue = Settings.Language;
            CbxLanguage.SelectionChanged += ComboBox_SelectionChanged;
        }

        public async Task LoadCommands()
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

            lbl.Content = (lbl.Tag.ToString() == "main"
                    ? Loc("window_settings_main_account", "Main Account:")
                    : Loc("window_settings_bot_account", "Bot Account:")) + "\n";

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
                    LblMainExpiry.Content = LocFormat("window_settings_expires_on", "Expires on {0}", Settings.TwitchAccessTokenExpiryDate);
                    break;

                case 1:
                    LblBotExpiry.Content = LocFormat("window_settings_expires_on", "Expires on {0}", Settings.BotAccessTokenExpiryDate);
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
                Logger.Info(LogSource.Twitch, $"Couldn't load profile picture for {(account == 0 ? "Main" : "Bot")}");
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

        private async void Btn_ExportConfig_Click(object sender, RoutedEventArgs e)
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
            fbd.Description = Loc("window_settings_folder_save_config", "Select a folder to save the config file");
            fbd.ShowNewFolderButton = true;
            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
            if (fbd.ShowDialog() != DialogResult.OK) return;
            ConfigHandler.WriteAllConfig(Settings.Export(), fbd.SelectedPath);
            await ShowMsgAsync(Loc("common_success", "Success"), Loc("window_settings_config_saved", "Config file saved successfully"));
        }

        private async void Btn_ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // Open a dialog to select a folder to import the config files
            using FolderBrowserDialog fbd = new();
            fbd.Description = Loc("window_settings_folder_import_config", "Select the folder containing the config files");
            fbd.ShowNewFolderButton = false; // Optional, prevents creating new folders
            // set the apps directory as the default directory
            fbd.SelectedPath = AppPaths.GetAppDirectory();
            if (fbd.ShowDialog() != DialogResult.OK) return;
            string selectedFolder = fbd.SelectedPath;
            if (!ConfigHandler.HasImportableFiles(selectedFolder))
            {
                await ShowMsgAsync(
                    Loc("common_error", "Error"),
                    Loc("window_settings_config_import_empty", "No Songify config files were found in that folder."));
                return;
            }

            Configuration incoming = ConfigHandler.LoadForImport(selectedFolder);
            Configuration local = ConfigHandler.SnapshotLocalForCompare();
            Window_CloudImportPreview preview = new(local, incoming, ImportPreviewKind.Backup)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (preview.DiffCount == 0)
            {
                preview.ShowDialog();
                return;
            }

            preview.ShowDialog();
            if (!preview.IsConfirmed)
                return;

            try
            {
                await Settings.ApplySelectedImport(incoming, preview.SelectedPaths, preserveSecrets: false);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error importing config", ex);
            }
        }

        private void Btn_OwnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            AccountLinking.OpenSpotifySetupGuide();
        }

        private async void Btn_ResetConfig_Click(object sender, RoutedEventArgs e)
        {
            AppDialogResult msgResult = await ShowMsgAsync("Warning",
                Loc("window_settings_reset_confirm", "Are you sure you want to reset all settings?"), AppDialogStyle.PrimaryAndSecondary,
                new AppDialogSettings
                {
                    PrimaryButtonText = Loc("dialog_yes", "Yes"),
                    NegativeButtonText = Loc("dialog_no", "No")
                });
            if (msgResult != AppDialogResult.Primary) return;
            File.Delete(AppPaths.GetAppDirectory() + "/config.xml");
            File.Delete(AppPaths.GetAppDirectory() + "/AppConfig.yaml");
            File.Delete(AppPaths.GetAppDirectory() + "/BotConfig.yaml");
            File.Delete(AppPaths.GetAppDirectory() + "/TwitchCredentials.yaml");
            File.Delete(AppPaths.GetAppDirectory() + "/SpotifyCredentials.yaml");
            Settings.ResetConfig();
            ShellHelper.OpenPath(AppPaths.GetExecutablePath());
            Application.Current.Shutdown();
        }

        private async void Btn_spotifyLink_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.ClientId))
            {
                AppDialogResult result = await ShowMsgAsync(
                    "Error",
                    Properties.Resources.common_fill_client_id_secret,
                    AppDialogStyle.PrimaryAndSecondary, new AppDialogSettings
                    {
                        PrimaryButtonText = Properties.Resources.common_ok,
                        NegativeButtonText = "How to get Client ID and Secret"
                    });
                if (result == AppDialogResult.Secondary)
                    AccountLinking.OpenSpotifySetupGuide();

                return;
            }

            SpotifyLinkResult linkResult = await AccountLinking.LinkSpotifyAsync();
            if (linkResult == SpotifyLinkResult.Started)
                await SetControls();
        }

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            // Copies the txt path to the clipboard and shows a notification
            if (string.IsNullOrEmpty(Settings.Directory))
                Clipboard.SetDataObject(Path.Combine(AppPaths.GetAppDirectory(), "Songify.txt"));
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
            _fbd.Description = Loc("window_settings_folder_song_output", "Path where the text file will be located.");
            _fbd.SelectedPath = AppPaths.GetAppDirectory();

            if (_fbd.ShowDialog() == DialogResult.Cancel)
                return;
            TxtbxOutputdirectory.Text = _fbd.SelectedPath;
            Settings.Directory = _fbd.SelectedPath;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Opens twitchapps to generate a TMI oAuth Token
            ShellHelper.OpenUrl("https://twitchtokengenerator.com/");
        }

        private void Chbx_AutoClear_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // Sets wether to clear the queue on startup or not
            Settings.AutoClearQueue = ChbxAutoClear.IsChecked == true;
        }

        private void Chbx_TwAutoconnect_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // Sets wether to autoconnect or not
            Settings.TwAutoConnect = ChbxTwAutoconnect.IsChecked == true;
        }

        private void Chbx_TwReward_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.TwSrReward = ChbxTwReward.IsChecked == true;
            _ = TwitchHandler.SetTwitchSrRewardsEnabledState(ChbxTwReward.IsChecked == true);
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            bool? chbxAutostartIsChecked = ChbxAutostart.IsChecked == true;
            AutostartHelper.RegisterInStartup((bool)chbxAutostartIsChecked);
        }

        private void TglCanvas_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            bool enabled = ((ToggleSwitch)sender).IsChecked == true;
            Settings.DownloadCanvas = enabled;

            if (!enabled)
            {
                AppShellBridge.Current?.StopCanvas();
                return;
            }

            if (GlobalObjects.Canvas is { Item1: true } && !string.IsNullOrEmpty(GlobalObjects.Canvas.Item2))
            {
                IoManager.DownloadCanvas(GlobalObjects.Canvas.Item2, Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4"));
                return;
            }

            string existing = Path.Combine(GlobalObjects.RootDirectory, "canvas.mp4");
            if (File.Exists(existing))
                AppShellBridge.Current?.SetCanvas(existing);
        }

        //private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        //{
        //    Settings.CustomPauseTextEnabled = ChbxCustomPause.IsChecked == true;
        //    TxtbxCustompausetext.IsEnabled = ChbxCustomPause.IsChecked == true;
        //}

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // enables / disbales minimize to systray
            bool isChecked = ChbxMinimizeSystray.IsChecked == true;
            Settings.Systray = isChecked;
        }

        private void ChbxOpenQueueOnStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.OpenQueueOnStartup = ((ToggleSwitch)sender).IsChecked == true;
            UpdateOpenQueuePopOutVisibility();
        }

        private void TglOpenQueuePopOutOnStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.OpenQueuePopOutOnStartup = TglOpenQueuePopOutOnStartup.IsChecked == true;
        }

        private void UpdateOpenQueuePopOutVisibility()
        {
            if (TglOpenQueuePopOutOnStartup == null)
                return;
            TglOpenQueuePopOutOnStartup.Visibility = ChbxOpenQueueOnStartup.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ChbxSpaces_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (ChbxSpaces.IsChecked != null) Settings.AppendSpaces = (bool)ChbxSpaces.IsChecked;
        }

        private void ChbxSplit_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // enables / disables telemetry
            Settings.SplitOutput = ChbxSplit.IsChecked == true;
        }

        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // enables / disables upload
            Settings.Upload = ChbxUpload.IsChecked == true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (CbxLanguage.SelectedValue is not string selectedLanguageCode) return;
            //// Update the current UI culture and settings
            //Settings.Language = selectedLanguageCode;
            //// Restart the application to apply the language change
            //ShellHelper.OpenPath(Environment.ProcessPath ?? Application.ResourceAssembly.Location);
            //Application.Current.Shutdown();

            if (CbxLanguage.SelectedValue is not string selectedLanguageCode)
                return;

            LocalizationHelper.Apply(selectedLanguageCode);
            _supportedLanguages = LocalizationHelper.GetLanguages();
            CbxLanguage.ItemsSource = _supportedLanguages;
            CbxLanguage.SelectedValue = selectedLanguageCode;

            _wRp?.LoadItems();

            GenerateRefundConditionToggles();
            UpdateUiScaleLabel(Settings.UiScale);
        }

        private void CbxWindowBackdrop_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (CbxWindowBackdrop.SelectedItem is not ComboBoxItem { Content: string name })
                return;

            Settings.WindowBackdrop = name;
            ThemeHandler.ApplyTheme();
        }

        private void BuildAccentSwatches()
        {
            if (PnlAccentSwatches == null)
                return;

            PnlAccentSwatches.Children.Clear();
            string current = Settings.AccentColor ?? "";
            List<string> recents = Settings.RecentAccentColors;
            bool hasRecents = recents is { Count: > 0 };
            if (TbAccentRecent != null)
                TbAccentRecent.Visibility = hasRecents ? Visibility.Visible : Visibility.Collapsed;

            if (hasRecents)
            {
                foreach (string hex in recents)
                {
                    if (!ThemeHandler.TryParseHex(hex, out Color parsed))
                        continue;

                    Button btn = new()
                    {
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(0, 0, 8, 8),
                        Tag = hex,
                        Focusable = false,
                        ToolTip = hex,
                        Background = new SolidColorBrush(parsed),
                        BorderThickness = IsCurrentAccent(hex, current)
                            ? new Thickness(3)
                            : new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
                    };
                    btn.PreviewMouseLeftButtonDown += AccentSwatch_OnPreviewMouseDown;
                    PnlAccentSwatches.Children.Add(btn);
                }
            }

            SyncAccentHexText(current);
            SyncAccentWheel(current);
            UpdateAccentPreviewSwatch();
            UpdateSystemAccentChrome();
        }

        private void BtnAccentSystem_OnClick(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            ApplyAccentChoice("");
        }

        private void UpdateAccentPreviewSwatch()
        {
            Color fill = ResolveAccentColor(Settings.AccentColor);
            if (BdAccentPreview != null)
                BdAccentPreview.Background = new SolidColorBrush(fill);
            if (BdAccentSystem != null)
                BdAccentSystem.Background = new SolidColorBrush(ResolveAccentColor(""));
            if (IcoAccentSystem != null)
                IcoAccentSystem.Visibility = string.IsNullOrEmpty(Settings.AccentColor)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void UpdateSystemAccentChrome()
        {
            if (BdAccentSystemFrame == null)
                return;
            bool isSystem = string.IsNullOrEmpty(Settings.AccentColor);
            BdAccentSystemFrame.BorderThickness = new Thickness(isSystem ? 2 : 1);
            BdAccentSystemFrame.BorderBrush = new SolidColorBrush(
                isSystem ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x60, 0x60, 0x60));
        }

        private void BtnAccentPicker_OnClick(object sender, RoutedEventArgs e)
        {
            if (PopAccentPicker == null)
                return;
            if (PopAccentPicker.IsOpen)
            {
                PopAccentPicker.IsOpen = false;
                return;
            }

            SyncAccentWheel(Settings.AccentColor);
            // Open after this click so StaysOpen=False does not close the popup on the same mouse up.
            Dispatcher.BeginInvoke(() =>
            {
                if (PopAccentPicker != null)
                    PopAccentPicker.IsOpen = true;
            }, DispatcherPriority.Input);
        }

        private void PopAccentPicker_OnOpened(object sender, EventArgs e)
            => SyncAccentWheel(Settings.AccentColor);

        private static bool IsCurrentAccent(string hex, string current)
            => string.Equals(hex ?? "", current ?? "", StringComparison.OrdinalIgnoreCase);

        private void SyncAccentHexText(string current, bool force = false)
        {
            if (TbAccentHex == null)
                return;
            if (!force && TbAccentHex.IsKeyboardFocusWithin)
                return;
            string shown = current ?? "";
            if (!string.Equals(TbAccentHex.Text, shown, StringComparison.Ordinal))
                TbAccentHex.Text = shown;
        }

        private void SyncAccentWheel(string current)
        {
            if (AccentWheel == null)
                return;
            _accentWheelUpdating = true;
            try
            {
                AccentWheel.SetColor(ResolveAccentColor(current));
            }
            finally
            {
                _accentWheelUpdating = false;
            }
        }

        private Color ResolveAccentColor(string current)
        {
            if (ThemeHandler.TryParseHex(current, out Color parsed))
                return parsed;
            if (TryFindResource("SystemAccentColor") is Color system)
                return system;
            return Color.FromRgb(0x00, 0x78, 0xD4);
        }

        private void AccentWheel_OnColorChanged(object sender, Color color)
        {
            if (IgnoreControlEvents || _accentWheelUpdating)
                return;

            SyncAccentHexText(ThemeHandler.ToHex(color), force: true);
            if (BdAccentPreview != null)
                BdAccentPreview.Background = new SolidColorBrush(color);
            if (IcoAccentSystem != null)
                IcoAccentSystem.Visibility = Visibility.Collapsed;
            _pendingAccentPreview = color;
            _hasPendingAccentPreview = true;
            _accentPreviewTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _accentPreviewTimer.Tick -= AccentPreviewTimer_OnTick;
            _accentPreviewTimer.Tick += AccentPreviewTimer_OnTick;
            _accentPreviewTimer.Stop();
            _accentPreviewTimer.Start();
        }

        private void AccentPreviewTimer_OnTick(object sender, EventArgs e)
        {
            _accentPreviewTimer?.Stop();
            if (!_hasPendingAccentPreview)
                return;
            ThemeHandler.PreviewAccent(_pendingAccentPreview);
        }

        private void AccentWheel_OnPickingCompleted(object sender, EventArgs e)
        {
            if (IgnoreControlEvents || _accentWheelUpdating || AccentWheel == null)
                return;
            _accentPreviewTimer?.Stop();
            _hasPendingAccentPreview = false;
            ApplyAccentChoice(ThemeHandler.ToHex(AccentWheel.SelectedColor));
        }

        private void AccentSwatch_OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (sender is not Button { Tag: string hex })
                return;
            ApplyAccentChoice(hex);
        }

        private void ApplyAccentChoice(string hex)
        {
            string value = string.IsNullOrEmpty(hex) ? "" : hex.ToUpperInvariant();
            Settings.AccentColor = value;
            // Always overwrite the hex box so LostFocus cannot write the previous custom value back.
            SyncAccentHexText(value, force: true);
            ThemeHandler.ApplyTheme(force: true);
            BuildAccentSwatches();
        }

        private void TbAccentHex_OnLostFocus(object sender, RoutedEventArgs e)
            => CommitAccentHex();

        private void TbAccentHex_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;
            e.Handled = true;
            CommitAccentHex();
        }

        private void CommitAccentHex()
        {
            if (IgnoreControlEvents || TbAccentHex == null)
                return;

            string text = (TbAccentHex.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
            {
                if (string.IsNullOrEmpty(Settings.AccentColor))
                    return;
                Settings.AccentColor = "";
                ThemeHandler.ApplyTheme(force: true);
                BuildAccentSwatches();
                return;
            }

            if (!text.StartsWith('#'))
                text = "#" + text;

            if (!ThemeHandler.TryParseHex(text, out _))
                return;

            string normalized = text.ToUpperInvariant();
            if (string.Equals(Settings.AccentColor ?? "", normalized, StringComparison.OrdinalIgnoreCase))
                return;

            Settings.AccentColor = normalized;
            ThemeHandler.ApplyTheme(force: true);
            BuildAccentSwatches();
        }

        private void BindUiScaleSlider()
        {
            if (SliderUiScale == null)
                return;

            SliderUiScale.ValueChanged -= SliderUiScale_OnValueChanged;
            double scale = Settings.UiScale;
            SliderUiScale.Value = scale;
            UpdateUiScaleLabel(scale);
            SliderUiScale.ValueChanged += SliderUiScale_OnValueChanged;
        }

        private void SliderUiScale_OnPreviewPointerDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _uiScalePointerActive = true;
        }

        private void SliderUiScale_OnPreviewPointerUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CommitUiScaleFromPointer();
        }

        private void SliderUiScale_OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CommitUiScaleFromPointer();
        }

        private void SliderUiScale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IgnoreControlEvents)
                return;

            double scale = UiScaleHandler.Clamp(e.NewValue);
            UpdateUiScaleLabel(scale);

            // Don't apply while dragging: scaling this window moves the slider under the cursor.
            if (_uiScalePointerActive || SliderUiScale.IsMouseCaptureWithin)
                return;

            CommitUiScale(scale);
        }

        private void CommitUiScaleFromPointer()
        {
            if (!_uiScalePointerActive && SliderUiScale is { IsMouseCaptureWithin: false })
                return;

            _uiScalePointerActive = false;
            if (IgnoreControlEvents || SliderUiScale == null)
                return;

            CommitUiScale(UiScaleHandler.Clamp(SliderUiScale.Value));
        }

        private void CommitUiScale(double scale)
        {
            scale = UiScaleHandler.Clamp(scale);
            UpdateUiScaleLabel(scale);
            if (Math.Abs(Settings.UiScale - scale) > 0.001)
                Settings.UiScale = scale;
            UiScaleHandler.Apply(scale);
        }

        private void TglOverruleShellMinWidth_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            Settings.OverruleShellMinWidth = TglOverruleShellMinWidth.IsChecked == true;
            if (Application.Current?.Windows == null)
                return;
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ShellWindow shell)
                    shell.ApplyMinSizeOverride();
            }
        }

        private void BindNavigationPaneCombo()
        {
            if (CbxNavigationPane == null)
                return;

            CbxNavigationPane.SelectionChanged -= CbxNavigationPane_OnSelectionChanged;
            CbxNavigationPane.Items.Clear();
            (string Mode, string Key, string Fallback)[] modes =
            [
                ("Left", "window_settings_appearance_nav_left", "Expanded"),
                ("Top", "window_settings_appearance_nav_top", "Top"),
                ("Bottom", "window_settings_appearance_nav_bottom", "Bottom")
            ];
            foreach ((string mode, string key, string fallback) in modes)
            {
                CbxNavigationPane.Items.Add(new ComboBoxItem
                {
                    Content = Loc(key, fallback),
                    Tag = mode
                });
            }

            string current = Settings.NavigationPaneDisplayMode;
            foreach (ComboBoxItem item in CbxNavigationPane.Items)
            {
                if (!string.Equals(item.Tag as string, current, StringComparison.OrdinalIgnoreCase))
                    continue;
                CbxNavigationPane.SelectedItem = item;
                break;
            }

            if (CbxNavigationPane.SelectedItem == null)
            {
                foreach (ComboBoxItem item in CbxNavigationPane.Items)
                {
                    if (!string.Equals(item.Tag as string, "Left", StringComparison.OrdinalIgnoreCase))
                        continue;
                    CbxNavigationPane.SelectedItem = item;
                    break;
                }
            }

            CbxNavigationPane.SelectionChanged += CbxNavigationPane_OnSelectionChanged;
        }

        private void CbxNavigationPane_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (CbxNavigationPane?.SelectedItem is not ComboBoxItem { Tag: string mode })
                return;
            if (string.Equals(Settings.NavigationPaneDisplayMode, mode, StringComparison.OrdinalIgnoreCase))
                return;
            Settings.NavigationPaneDisplayMode = mode;
            if (string.Equals(mode, "Left", StringComparison.OrdinalIgnoreCase))
                Settings.NavigationPaneOpen = true;
            AppShellBridge.Current?.ApplyNavigationChrome();
        }

        private void UpdateUiScaleLabel(double scale)
        {
            if (TbUiScaleValue == null)
                return;

            int percent = (int)Math.Round(scale * 100);
            TbUiScaleValue.Text = LocFormat("window_settings_appearance_scale_value", "{0}%", percent);
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

        /// <summary>Persist settings. Returns false if close should be cancelled (restart required).</summary>
        public async Task<bool> ConfirmCloseAsync()
        {
            ConfigHandler.WriteAllConfig(Settings.Export());
            if (_appIdInitialValue == Settings.UseOwnApp) return true;

            Settings.SpotifyAccessToken = "";
            Settings.SpotifyRefreshToken = "";
            string temp = _appIdInitialValue == false
                ? "You switched from Songify's internal app-ID to your own. This is great because you won't get throttled by rate limits! \n\nIn order to use it though, Songify needs to be restarted and you have to relink with your Spotify account!"
                : "You switched from your own app-ID to Songify's internal one. This is bad and you will likely encounter problems. The API only allows a certain amount of requests done through an app. We have been exceeding this amount by a lot. Please use your own app-ID instead!\n\nSongify needs a restart and you have to link your Spotify account again.";

            AppDialogResult msgResult = await ShowMsgAsync("Warning", temp, AppDialogStyle.Primary,
                new AppDialogSettings { PrimaryButtonText = "Restart" });
            if (msgResult != AppDialogResult.Primary) return false;
            ShellHelper.OpenPath(AppPaths.GetExecutablePath());
            Application.Current.Shutdown();
            return false;
        }

        private void Nud_Spaces_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (NudSpaces.Value != null) Settings.SpaceCount = (int)NudSpaces.Value;
        }

        private void NudChrome_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            // Chrome fetch UI removed; setting retained in config for compatibility.
        }

        private void NudCooldown_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // Sets command cooldown
            if (NudCooldown.Value == null)
                return;
            Settings.TwSrCooldown = (int)NudCooldown.Value;
            if (!NudCooldown.Value.HasValue) return;
            int totalSeconds = (int)NudCooldown.Value.Value;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            GlobalCooldownDisplay.Text = $"({minutes:D2}:{seconds:D2})";
        }

        private void NudMaxlength_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (NudMaxlength.Value != null) Settings.MaxSongLength = (int)NudMaxlength.Value;
        }

        private void NudMaxReq_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
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

        private async void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsUi.Register(this);
            SongifyPremiumService.StatusChanged -= OnPremiumStatusChanged;
            SongifyPremiumService.StatusChanged += OnPremiumStatusChanged;
            _ = SongifyPremiumService.RefreshAsync();

            // Theme + window backdrop (WPF-UI system accent; no custom accent color list)
            ThemeToggleSwitch.IsChecked = Settings.Theme is "BaseDark" or "Dark";

            CbxWindowBackdrop.SelectionChanged -= CbxWindowBackdrop_OnSelectionChanged;
            CbxWindowBackdrop.Items.Clear();
            foreach (string name in new[] { "Mica", "Acrylic", "Tabbed", "Auto", "None" })
                CbxWindowBackdrop.Items.Add(new ComboBoxItem { Content = name });

            string currentBackdrop = Settings.WindowBackdrop;
            foreach (ComboBoxItem item in CbxWindowBackdrop.Items)
            {
                if ((string)item.Content != currentBackdrop) continue;
                CbxWindowBackdrop.SelectedItem = item;
                break;
            }
            if (CbxWindowBackdrop.SelectedItem == null)
                CbxWindowBackdrop.SelectedIndex = 0;
            CbxWindowBackdrop.SelectionChanged += CbxWindowBackdrop_OnSelectionChanged;

            BuildAccentSwatches();

            BindUiScaleSlider();

            await SetControls();
        }

        private void SettingsPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            SongifyPremiumService.StatusChanged -= OnPremiumStatusChanged;
            _premiumRefreshCts?.Cancel();
            if (_accentPreviewTimer != null)
            {
                _accentPreviewTimer.Stop();
                _accentPreviewTimer.Tick -= AccentPreviewTimer_OnTick;
            }
            if (PopAccentPicker != null)
                PopAccentPicker.IsOpen = false;
        }

        private void Tb_ClientID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.ClientId = TbClientId.Text;
        }

        private void Tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.ClientSecret = TbClientSecret.Password;
        }

        private void Tgl_AnnounceInChat_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.AnnounceInChat = TglAnnounceInChat.IsChecked == true;
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, EventArgs e)
        {
            //Settings.UseOwnApp = TglswSpotify.IsChecked == true;
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

        private void ThemeToggleSwitchIsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.Theme = ThemeToggleSwitch.IsChecked == true ? "Dark" : "Light";

            ThemeHandler.ApplyTheme();
        }

        private void Txtbx_twChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            ((TextBox)sender).Text = ((TextBox)sender).Text.ToLower().Trim();
            // Sets the twitch channel
            Settings.TwChannel = TxtbxTwChannel.Text.Trim();
        }

        private void Txtbx_twOAuth_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // Sets the twitch oauth token
            Settings.TwOAuth = TxtbxTwOAuth.Password;
        }

        private void Txtbx_twUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // Sets the twitch acc
            Settings.TwAcc = TxtbxTwUser.Text.Trim();
        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // write CustomPausetext to settings
            Settings.CustomPauseText = TxtbxCustompausetext.Text;
        }

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            // write custom output format to settings
            if (TxtbxOutputformat.Text == Settings.OutputString)
                return;
            Settings.OutputString = TxtbxOutputformat.Text;
            GlobalObjects.ForceUpdate = true;
        }

        private void TxtbxOutputformat2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (TxtbxOutputformat2.Text == Settings.OutputString2)
                return;
            Settings.OutputString2 = ((TextBox)sender).Text;
        }

        private void CbxUserLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.TwSrUserLevel = CbxUserLevels.SelectedIndex;
        }

        private void CbxUserLevelsMaxReq_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
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
            Pages.SettingsPage.Instance?.SelectTab("TwitchRewards");
        }

        private async void BtnUpdateRewards_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            _rewardsLoadStarted = true;
            await LoadRewards();
            ((Button)sender).IsEnabled = true;
        }

        public async Task LoadRewards()
        {
            if (TwitchHandler.TwitchApi == null)
                return;
            if (TwitchHandler.TokenCheck == null)
                return;
            if (_rewardsLoading)
                return;

            _rewardsLoading = true;
            BtnCreateNewReward.IsEnabled = true;
            try
            {
                // Helix I/O off the UI thread; only assign ItemsSource on the dispatcher.
                Task<List<CustomReward>> manageableTask = TwitchApiHelper.GetChannelRewards(true);
                Task<List<CustomReward>> rewardsTask = TwitchApiHelper.GetChannelRewards(false);
                await Task.WhenAll(manageableTask, rewardsTask).ConfigureAwait(false);

                List<CustomReward> managableRewards = await manageableTask.ConfigureAwait(false) ?? [];
                List<CustomReward> rewards = await rewardsTask.ConfigureAwait(false);
                if (rewards == null)
                    return;

                HashSet<string> manageableIds = new(managableRewards.Select(r => r.Id));
                List<TwitchRewardListItem> items = rewards
                    .OrderBy(o => o.Cost)
                    .Select(r => new TwitchRewardListItem(r, manageableIds.Contains(r.Id)))
                    .ToList();

                await Dispatcher.InvokeAsync(() =>
                {
                    List<string> idsToRemove = Settings.TwRewardId.Where(s => rewards.All(o => o.Id != s)).ToList();
                    foreach (string s in idsToRemove)
                        Settings.TwRewardId.Remove(s);

                    ListboxRewards.ItemsSource = null;
                    ListboxRewards.ItemsSource = items;
                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Twitch, "Error loading rewards.", e);
            }
            finally
            {
                _rewardsLoading = false;
            }
        }

        private void BtnLogInTwitch_Click(object sender, RoutedEventArgs e)
        {
            AccountLinking.LoginTwitchMain();
        }

        private void ToggleSwitchPrivacy_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsChecked == true)
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
                ? Properties.Resources.window_settings_webserver_stop
                : Properties.Resources.window_settings_webserver_start;
        }

        private void NudServerPort_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;
            int? value = (int?)((NumberBox)sender).Value;
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
            if (IgnoreControlEvents)
                return;
            Settings.AutoStartWebServer = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void TglWebServerPassword_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.WebServerPasswordEnabled = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void PasswordBox_WebServer_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (sender is not PasswordBox pb) return;
            string pwd = pb.Password ?? "";
            if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(Settings.WebServerPassword))
                return;
            Settings.WebServerPassword = pwd;
        }

        private void BtnCreateNewReward_Click(object sender, RoutedEventArgs e)
        {
            WindowCreateCustomReward createCustomReward = new()
            {
                Owner = Window.GetWindow(this)
            };
            createCustomReward.ShowDialog();
        }

        private void CbxReleaseChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (CbxReleaseChannel.SelectedIndex < 0)
                return;
            Enums.ReleaseChannel channel = (Enums.ReleaseChannel)CbxReleaseChannel.SelectedIndex;
            if (channel == Settings.ReleaseChannel)
                return;
            Settings.ReleaseChannel = channel;
        }

        private void BtnWebserverOpenUrl_OnClick(object sender, RoutedEventArgs e)
        {
            if (GlobalObjects.WebServer.Run)
                ShellHelper.OpenUrl($"http://localhost:{Settings.WebServerPort}");
        }

        private void Tgl_OnlyWorkWhenLive_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.BotOnlyWorkWhenLive = TglOnlyWorkWhenLive.IsChecked == true;
            TglInformChat.IsEnabled = TglOnlyWorkWhenLive.IsChecked == true;
            if (TglOnlyWorkWhenLive.IsChecked == true) return;
            TglInformChat.IsChecked = false;
        }

        private void ToggleSwitchUnlimitedSR_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.TwSrUnlimitedSr = ToggleSwitchUnlimitedSr.IsChecked == true;
        }

        private async void BtnTwitchLogout_OnClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Tag.ToString().ToLower())
            {
                case "main":
                    Settings.TwitchAccessToken = "";
                    Settings.TwitchUser = null;
                    TwitchHandler.TwitchApi = null;
                    TwitchHandler.ResetTwitchSetting(Enums.TwitchAccount.Main);
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
            if (IgnoreControlEvents)
                return;
            Settings.ChatLiveStatus = TglInformChat.IsChecked == true;
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

        private async void Cb_SpotifyPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if ((((ComboBox)sender).SelectedItem as ComboBoxItem)?.Content is not UcPlaylistItem item) return;
            if (item.Playlist.Id == Settings.SpotifyPlaylistId.PlaylistId)
                return;
            Settings.SpotifyPlaylistId = new PlaylistSnapshot
            {
                PlaylistId = item.Playlist.Id,
                Snapshot = item.Playlist.SnapshotId
            };
        }

        private async void CbAccountSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded || _isSettingControls)
                    return;
                await ResetTwitchConnection();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public Task ResetTwitchConnection()
        {
            Settings.TwAcc = ((UcAccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).Username;
            Settings.TwOAuth = ((UcAccountItem)((ComboBoxItem)CbAccountSelection.SelectedItem).Content).OAuth;
            TwitchHandler.ConnectTwitchChatClient();
            _ = SetControls();
            return Task.CompletedTask;
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
            RefreshUserLevelComboSummaries();
        }

        private void CbxUserLevelsRewardUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsReward];
            list.Remove(value);
            Settings.UserLevelsReward = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUserLevelsCommandChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsCommand, value];
            Settings.UserLevelsCommand = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUserLevelsCommandUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsCommand];
            list.Remove(value);
            Settings.UserLevelsCommand = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUserLevelsExplicitChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UserLevelsExplicitSongs.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsExplicitSongs, value];
            Settings.UserLevelsExplicitSongs = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUserLevelsExplicitUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UserLevelsExplicitSongs.Contains(value)) return;
            List<int> list = [.. Settings.UserLevelsExplicitSongs];
            list.Remove(value);
            Settings.UserLevelsExplicitSongs = list;
            RefreshUserLevelComboSummaries();
        }

        private void TglAddToPlaylist_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.AddSrToPlaylist = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void TglBypassSpotifyFetchGate_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.BypassSpotifyFetchGate = ((ToggleSwitch)sender).IsChecked == true;
            AppFetchService.NotifySpotifyRelatedActivity("bypass Spotify fetch gate toggled");
        }

        private void TglShowSpotifyToasts_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.ShowSpotifyToasts = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void LoadArtistBlocklistSyncControls()
        {
            if (TbArtistBlocklistSyncUrl != null)
                TbArtistBlocklistSyncUrl.Text = Settings.ArtistBlocklistSyncUrl;

            if (TglArtistBlocklistSyncEnabled != null)
                TglArtistBlocklistSyncEnabled.IsChecked = Settings.ArtistBlocklistSyncEnabled;

            SeedArtistBlocklistColumnCombo(
                CbxArtistBlocklistSyncNameColumn,
                Settings.ArtistBlocklistSyncNameColumn,
                includeNone: false);
            SeedArtistBlocklistColumnCombo(
                CbxArtistBlocklistSyncIdColumn,
                Settings.ArtistBlocklistSyncIdColumn,
                includeNone: true);

            UpdateArtistBlocklistSyncStatusLabel();
        }

        private static void SeedArtistBlocklistColumnCombo(ComboBox combo, string selectedHeader, bool includeNone)
        {
            if (combo == null)
                return;

            List<ArtistCsvColumnOption> options = [];
            if (includeNone)
            {
                options.Add(new ArtistCsvColumnOption
                {
                    Index = -1,
                    Header = ArtistCsvImport.NoneColumn,
                    Display = ArtistCsvImport.NoneColumn
                });
            }

            if (!string.IsNullOrWhiteSpace(selectedHeader) &&
                !options.Any(o => string.Equals(o.Header, selectedHeader, StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new ArtistCsvColumnOption
                {
                    Index = 0,
                    Header = selectedHeader.Trim(),
                    Display = selectedHeader.Trim()
                });
            }

            combo.ItemsSource = options;
            if (!string.IsNullOrWhiteSpace(selectedHeader))
                combo.SelectedValue = selectedHeader.Trim();
            else if (includeNone)
                combo.SelectedValue = ArtistCsvImport.NoneColumn;
        }

        private void UpdateArtistBlocklistSyncStatusLabel()
        {
            if (TbArtistBlocklistSyncStatus == null)
                return;

            string last = Settings.ArtistBlocklistSyncLastUtc;
            if (string.IsNullOrWhiteSpace(last))
            {
                TbArtistBlocklistSyncStatus.Text =
                    Application.Current.TryFindResource("window_settings_last_sync_never") as string
                    ?? "Last sync: never";
                return;
            }

            string format =
                Application.Current.TryFindResource("window_settings_last_sync") as string
                ?? "Last sync: {0}";

            if (DateTime.TryParse(last, null, DateTimeStyles.RoundtripKind, out DateTime utc))
            {
                DateTime local = utc.ToLocalTime();
                TbArtistBlocklistSyncStatus.Text = string.Format(format, local.ToString("g"));
            }
            else
            {
                TbArtistBlocklistSyncStatus.Text = string.Format(format, last);
            }
        }

        private void TbArtistBlocklistSyncUrl_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Persist on LostFocus to avoid writing config on every keystroke.
        }

        private void TbArtistBlocklistSyncUrl_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.ArtistBlocklistSyncUrl = TbArtistBlocklistSyncUrl.Text?.Trim() ?? "";
        }

        private void CbxArtistBlocklistSyncColumn_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            if (CbxArtistBlocklistSyncNameColumn?.SelectedValue is string nameHeader)
                Settings.ArtistBlocklistSyncNameColumn = nameHeader;

            if (CbxArtistBlocklistSyncIdColumn?.SelectedValue is string idHeader)
                Settings.ArtistBlocklistSyncIdColumn = idHeader;
        }

        private void TglArtistBlocklistSyncEnabled_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.ArtistBlocklistSyncEnabled = TglArtistBlocklistSyncEnabled.IsChecked == true;
        }

        private async void BtnArtistBlocklistDetectColumns_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            string url = TbArtistBlocklistSyncUrl.Text?.Trim() ?? "";
            Settings.ArtistBlocklistSyncUrl = url;

            Button btn = sender as Button;
            if (btn != null)
                btn.IsEnabled = false;

            try
            {
                TbArtistBlocklistSyncStatus.Text = Loc("window_settings_detecting_columns", "Detecting columns…");
                string csv = await ArtistCsvImport.DownloadCsvAsync(url);
                if (!ArtistCsvImport.TryParse(csv, out List<string> headers, out _, out string error))
                {
                    TbArtistBlocklistSyncStatus.Text = error;
                    await ShowMsgAsync(Loc("window_settings_detect_columns_title", "Detect columns"), error);
                    return;
                }

                List<ArtistCsvColumnOption> nameOptions = headers
                    .Select((h, i) => new ArtistCsvColumnOption { Index = i, Header = h, Display = $"{i + 1}: {h}" })
                    .ToList();

                List<ArtistCsvColumnOption> idOptions =
                [
                    new ArtistCsvColumnOption
                    {
                        Index = -1,
                        Header = ArtistCsvImport.NoneColumn,
                        Display = ArtistCsvImport.NoneColumn
                    }
                ];
                idOptions.AddRange(nameOptions);

                _isSettingControls = true;
                CbxArtistBlocklistSyncNameColumn.ItemsSource = nameOptions;
                CbxArtistBlocklistSyncIdColumn.ItemsSource = idOptions;

                int nameIdx = ArtistCsvImport.ResolveColumnIndex(headers, Settings.ArtistBlocklistSyncNameColumn, ArtistCsvImport.NameColumnHints);
                if (nameIdx < 0)
                    nameIdx = ArtistCsvImport.GuessColumnIndex(headers, ArtistCsvImport.NameColumnHints);
                if (nameIdx < 0 && headers.Count > 0)
                    nameIdx = 0;

                int idIdx = ArtistCsvImport.ResolveColumnIndex(headers, Settings.ArtistBlocklistSyncIdColumn, ArtistCsvImport.IdColumnHints);
                if (idIdx < 0 && string.IsNullOrWhiteSpace(Settings.ArtistBlocklistSyncIdColumn))
                    idIdx = ArtistCsvImport.GuessColumnIndex(headers, ArtistCsvImport.IdColumnHints);

                CbxArtistBlocklistSyncNameColumn.SelectedValue = nameIdx >= 0 ? headers[nameIdx] : null;
                CbxArtistBlocklistSyncIdColumn.SelectedValue = idIdx >= 0 ? headers[idIdx] : ArtistCsvImport.NoneColumn;
                _isSettingControls = false;

                if (CbxArtistBlocklistSyncNameColumn.SelectedValue is string nameHeader)
                    Settings.ArtistBlocklistSyncNameColumn = nameHeader;
                if (CbxArtistBlocklistSyncIdColumn.SelectedValue is string idHeader)
                    Settings.ArtistBlocklistSyncIdColumn = idHeader;

                TbArtistBlocklistSyncStatus.Text = LocFormat("window_settings_detected_columns", "Detected {0} column(s). Mapping saved.", headers.Count);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to detect artist blocklist CSV columns", ex);
                TbArtistBlocklistSyncStatus.Text = Loc("window_settings_detect_failed", "Detect failed.");
                await ShowMsgAsync(Loc("window_settings_detect_columns_title", "Detect columns"), Loc("window_settings_detect_csv_error", "Could not load the CSV. Check the URL and try again."));
            }
            finally
            {
                _isSettingControls = false;
                if (btn != null)
                    btn.IsEnabled = true;
            }
        }

        private async void BtnArtistBlocklistSyncNow_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            Settings.ArtistBlocklistSyncUrl = TbArtistBlocklistSyncUrl.Text?.Trim() ?? "";
            if (CbxArtistBlocklistSyncNameColumn?.SelectedValue is string nameHeader)
                Settings.ArtistBlocklistSyncNameColumn = nameHeader;
            if (CbxArtistBlocklistSyncIdColumn?.SelectedValue is string idHeader)
                Settings.ArtistBlocklistSyncIdColumn = idHeader;

            Button btn = sender as Button;
            if (btn != null)
                btn.IsEnabled = false;

            try
            {
                TbArtistBlocklistSyncStatus.Text = "Syncing…";
                ArtistCsvSyncResult result = await ArtistCsvImport.SyncFromSettingsAsync();
                UpdateArtistBlocklistSyncStatusLabel();
                if (!result.Success)
                {
                    TbArtistBlocklistSyncStatus.Text = result.Message;
                    await ShowMsgAsync(Loc("window_settings_artist_sync_title", "Artist blocklist sync"), result.Message);
                    return;
                }

                // Refresh open blocklist UI if present
                await BlocklistUi.RefreshArtistsAsync();

                await ShowMsgAsync(Loc("window_settings_artist_sync_title", "Artist blocklist sync"), result.Message);
                UpdateArtistBlocklistSyncStatusLabel();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Manual artist blocklist sync failed", ex);
                await ShowMsgAsync(Loc("window_settings_artist_sync_title", "Artist blocklist sync"), Loc("window_settings_artist_sync_failed", "Sync failed. Check the logs for details."));
            }
            finally
            {
                if (btn != null)
                    btn.IsEnabled = true;
            }
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
                        using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(5)))
                        {
                            GlobalObjects.SpotifyProfile ??= await SpotifyApiHandler.GetUser(cts.Token);
                        }

                        if (GlobalObjects.SpotifyProfile == null) return;

                        CbSpotifyPlaylist.Items.Clear();

                        CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                        {
                            Content = new UcPlaylistItem(new SpotifyPlaylistCache()
                            {
                                Id = "-1",
                                Images = ["https://misc.scdn.co/liked-songs/liked-songs-640.png"],
                                Items = [],
                                Name = "Liked Songs",
                                Owner = Settings.SpotifyProfile.DisplayName,
                                SnapshotId = null,
                                Url = null
                            })
                        });

                        Paging<FullPlaylist> playlists =
                            await SpotifyApiHandler.GetUserPlaylists();
                        if (playlists == null) return;
                        List<SpotifyPlaylistCache> playlistCache = [];
                        CbSpotifyPlaylist.SelectionChanged -= Cb_SpotifyPlaylist_SelectionChanged;

                        foreach (FullPlaylist playlist in playlists.Items
                                     .Where(playlist =>
                                         playlist?.Owner?.Id != null &&
                                         playlist.Owner.Id == GlobalObjects.SpotifyProfile?.Id))
                        {
                            SpotifyPlaylistCache cache = new SpotifyPlaylistCache
                            {
                                Id = playlist.Id,
                                Name = playlist.Name,
                                Owner = playlist.Owner?.DisplayName,
                                Url = playlist.Uri,
                                SnapshotId = playlist.SnapshotId,
                                Images = [playlist.Images?.First().Url],
                                Items = null
                            };
                            CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                            { Content = new UcPlaylistItem(cache) });
                            playlistCache.Add(cache);
                        }

                        CbSpotifyPlaylist.SelectionChanged += Cb_SpotifyPlaylist_SelectionChanged;
                        Settings.SpotifyPlaylistCache = playlistCache;

                        if (!string.IsNullOrEmpty(Settings.SpotifyPlaylistId.PlaylistId))
                            CbSpotifyPlaylist.SelectedItem = CbSpotifyPlaylist.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id ==
                                    Settings.SpotifyPlaylistId.PlaylistId);
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
                        CbSpotifyPlaylist.Items.Add(new ComboBoxItem
                        {
                            Content = new UcPlaylistItem(new SpotifyPlaylistCache
                            {
                                Id = "-1",
                                Name = "Liked Songs",
                                Owner = null,
                                Url = null,
                                SnapshotId = null,
                                Images = ["https://misc.scdn.co/liked-songs/liked-songs-640.png"],
                                Items = null
                            })
                        });
                        foreach (SpotifyPlaylistCache playlist in Settings.SpotifyPlaylistCache)
                        {
                            CbSpotifyPlaylist.Items.Add(new ComboBoxItem { Content = new UcPlaylistItem(playlist) });
                        }

                        if (!string.IsNullOrEmpty(Settings.SpotifyPlaylistId.PlaylistId))
                            CbSpotifyPlaylist.SelectedItem = CbSpotifyPlaylist.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item =>
                                    ((UcPlaylistItem)item.Content).Playlist != null &&
                                    ((UcPlaylistItem)item.Content).Playlist.Id ==
                                    Settings.SpotifyPlaylistId.PlaylistId);
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

        private void Chbx_BlockAllExplicit_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            bool enabled = ((ToggleSwitch)sender).IsChecked == true;
            Settings.BlockAllExplicitSongs = enabled;
            CbxAllowedUserLevelsExplicit.IsEnabled = enabled;
        }

        private void TbRequesterPrefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.RequesterPrefix = TbRequesterPrefix.Text;
        }

        private void TglUseDefaultBrowser_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.UseDefaultBrowser = ((ToggleSwitch)sender).IsChecked == true;
        }

        private async void TglDonationReminder_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (Settings.DonationReminder == ((ToggleSwitch)sender).IsChecked == true)
                return;

            Settings.DonationReminder = ((ToggleSwitch)sender).IsChecked == true;
            if (!((ToggleSwitch)sender).IsChecked == true) return;

            AppDialogResult msgResult = await ShowMsgAsync(
                Loc("window_settings_premium_reminder_title", "Premium reminders off"),
                Loc("window_settings_premium_reminder_body",
                    "Songify stays free. Premium adds stream recap, top songs and requesters, cloud sync, and extra widgets.\n\nYou can still open Songify Premium any time from Home or About."),
                AppDialogStyle.PrimaryAndSecondary,
                new AppDialogSettings
                {
                    PrimaryButtonText = Loc("common_close", "Close"),
                    NegativeButtonText = Loc("cta_premium", "Songify Premium")
                });
            switch (msgResult)
            {
                case AppDialogResult.Secondary:
                    AccountLinking.OpenPremium();
                    return;

                case AppDialogResult.Primary:
                    break;

                default:
                    return;
            }
        }

        private void BtnLogInTwitchAlt_Click(object sender, RoutedEventArgs e)
        {
            WindowManualTwitchLogin manualTwitchLogin = new(
                (sender is not Button button ||
                 button.Tag.ToString().Equals("main", StringComparison.CurrentCultureIgnoreCase))
                    ? Enums.TwitchAccount.Main
                    : Enums.TwitchAccount.Bot)
            {
                Owner = Window.GetWindow(this)
            };
            manualTwitchLogin.Show();
        }

        private void CbPauseOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if ((Enums.PauseOptions)CbPauseOptions.SelectedIndex == Settings.PauseOption)
                return;
            Settings.PauseOption = (Enums.PauseOptions)CbPauseOptions.SelectedIndex;
        }

        private void ChbxSpacesSplitFiles_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            bool? isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked != null)
                Settings.AppendSpacesSplitFiles = (bool)isChecked;
        }

        private void CooldownSpinner_OnValueChangedpinner_ValueChanged(object sender,
            NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (!NudCooldownPerUser.Value.HasValue) return;
            Settings.TwSrPerUserCooldown = (int)NudCooldownPerUser.Value;
            int totalSeconds = (int)NudCooldownPerUser.Value.Value;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            UserCooldownDisplay.Text = $"({minutes:D2}:{seconds:D2})";
        }

        private void Tgl_KeepCover_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.KeepAlbumCover = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void BtnResponseParams_OnClick(object sender, RoutedEventArgs e)
        {
            Window owner = Window.GetWindow(this);
            double left = (owner?.Left ?? 0) + (owner?.ActualWidth ?? ActualWidth);
            double top = owner?.Top ?? 0;
            double height = owner?.ActualHeight ?? ActualHeight;

            _wRp ??= new Window_ResponseParams
            {
                Left = left,
                Top = top,
                Owner = owner,
                Height = height
            };
            if (!_wRp.IsLoaded)
                _wRp = new Window_ResponseParams
                {
                    Left = left,
                    Top = top,
                    Owner = owner,
                    Height = height
                };
            if (_wRp.IsVisible)
                _wRp.Hide();
            else
                _wRp.Show();
        }

        public void SyncResponseParamsPosition()
        {
            Window owner = Window.GetWindow(this);
            if (_wRp is not { } responseParams || owner == null) return;
            responseParams.LocationChanged -= responseParams.Window_ResponseParams_OnLocationChanged;
            responseParams.Left = owner.Left + owner.ActualWidth;
            responseParams.Top = owner.Top;
            responseParams.LocationChanged += responseParams.Window_ResponseParams_OnLocationChanged;
        }

        public void SyncResponseParamsSize()
        {
            Window owner = Window.GetWindow(this);
            if (_wRp == null || owner == null) return;
            _wRp.Height = owner.ActualHeight;
            _wRp.LocationChanged -= _wRp.Window_ResponseParams_OnLocationChanged;
            _wRp.Left = owner.Left + owner.ActualWidth;
            _wRp.Top = owner.Top;
            _wRp.LocationChanged += _wRp.Window_ResponseParams_OnLocationChanged;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void CbxUnlimitedRewardChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UnlimitedSrUserlevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsReward, value];
            Settings.UnlimitedSrUserlevelsReward = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUnlimitedRewardUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UnlimitedSrUserlevelsReward.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsReward];
            list.Remove(value);
            Settings.UnlimitedSrUserlevelsReward = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUnlimitedCommandUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (!Settings.UnlimitedSrUserlevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsCommand];
            list.Remove(value);
            Settings.UnlimitedSrUserlevelsCommand = list;
            RefreshUserLevelComboSummaries();
        }

        private void CbxUnlimitedCommandChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;
            int value = Convert.ToInt32(checkBox.Tag);
            if (Settings.UnlimitedSrUserlevelsCommand.Contains(value)) return;
            List<int> list = [.. Settings.UnlimitedSrUserlevelsCommand, value];
            Settings.UnlimitedSrUserlevelsCommand = list;
            RefreshUserLevelComboSummaries();
        }

        private async void TglsLongBadgeNames_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            try
            {
                Settings.LongBadgeNames = ((ToggleSwitch)sender).IsChecked == true;
                await LoadCommands();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error setting badge length", ex);
            }
        }

        private async void Tglsw_OnlyAddToPlaylist_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            try
            {
                if (sender is not ToggleSwitch toggleSwitch)
                    return;

                if (!toggleSwitch.IsChecked == true)
                {
                    Settings.AddSrtoPlaylistOnly = false;
                    return;
                }

                AppDialogSettings dialogSettings = new AppDialogSettings
                {
                    AnimateHide = false,
                    AnimateShow = true
                };

                AppDialogResult result = await ShowMsgAsync(
                    "Warning",
                    "Turning this option on makes it so that song requests will ONLY be " +
                    "added to the \"Liked Songs\" playlist selected in the Spotify tab.\n\n" +
                    "They WILL NOT end up in your queue!",
                    AppDialogStyle.PrimaryAndSecondary,
                    dialogSettings);

                if (result == AppDialogResult.Primary)
                {
                    Settings.AddSrtoPlaylistOnly = true;
                }
                else
                {
                    toggleSwitch.IsChecked = false;
                    Settings.AddSrtoPlaylistOnly = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Error setting AddSrtoPlaylistOnly", ex);
            }
        }

        private void NudBits_OnValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;
            if (!NudBits.Value.HasValue) return;
            Settings.MinimumBitsForSr = (int)NudBits.Value;
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

        private void PwbSongifyToken_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (sender is not PasswordBox pb)
                return;

            string pwd = pb.Password ?? "";
            if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(Settings.SongifyApiKey))
                return;

            Settings.SongifyApiKey = pwd;
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            // Theme/style refresh can momentarily clear PasswordBox; never wipe a stored token that way.
            string pwd = PasswordBox.Password ?? "";
            if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(Settings.SongifyApiKey) && !_showPassword)
                return;

            if (TextBox.Text != pwd)
                Settings.SongifyApiKey = pwd;
            if (!_showPassword)
                TextBox.Text = pwd;
            NotifySongifyTokenChanged();
        }

        private void ShowHideButton_OnClick(object sender, RoutedEventArgs e)
        {
            _showPassword = !_showPassword;

            if (_showPassword)
            {
                TextBox.Text = PasswordBox.Password;
                TextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                if (EyeIcon != null)
                    EyeIcon.Symbol = SymbolRegular.Eye24;
            }
            else
            {
                PasswordBox.Password = TextBox.Text;
                TextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                if (EyeIcon != null)
                    EyeIcon.Symbol = SymbolRegular.EyeOff24;
            }
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            // Only update if not a pure visibility toggle
            if (PasswordBox.Password != TextBox.Text)
                Settings.SongifyApiKey = TextBox.Text;

            if (_showPassword)
                PasswordBox.Password = TextBox.Text;
            NotifySongifyTokenChanged();
        }

        private void GenerateRefundConditionToggles()
        {
            RefundSwitchesPanel.Children.Clear();
            RefundSwitchesPanel.RowDefinitions.Clear();
            RefundSwitchesPanel.ColumnDefinitions.Clear();
            _toggleMap.Clear();

            const int columns = 2;
            int totalItems = RefundConditionLabels.Count;
            int rows = (int)Math.Ceiling(totalItems / (double)columns);

            // Add columns
            for (int i = 0; i < columns; i++)
            {
                RefundSwitchesPanel.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Add rows
            for (int i = 0; i < rows; i++)
            {
                RefundSwitchesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            int index = 0;
            foreach (KeyValuePair<Enums.RefundCondition, string> kvp in RefundConditionLabels)
            {
                Enums.RefundCondition condition = kvp.Key;

                ToggleSwitch toggle = new()
                {
                    Tag = condition,
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = new TextBlock
                    {
                        Text = kvp.Value,
                        MaxWidth = 250,
                        TextWrapping = TextWrapping.Wrap
                    },
                    FontWeight = condition == Enums.RefundCondition.OnSuccess ? FontWeights.Bold : FontWeights.Normal
                };

                toggle.IsChecked = (Settings.RefundConditons ?? []).Contains(condition);
                toggle.Checked += RefundCondition_Toggled;
                toggle.Unchecked += RefundCondition_Toggled;

                _toggleMap[condition] = toggle;

                int row = index / columns;
                int col = index % columns;

                Grid.SetRow(toggle, row);
                Grid.SetColumn(toggle, col);
                RefundSwitchesPanel.Children.Add(toggle);

                index++;
            }
        }

        private void RefundCondition_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents) return;

            if (sender is not ToggleSwitch { Tag: Enums.RefundCondition conditionValue } toggle) return;
            List<Enums.RefundCondition> current = [..(Settings.RefundConditons ?? [])];
            if (toggle.IsChecked == true)
            {
                if (!current.Contains(conditionValue))
                    current.Add(conditionValue);
            }
            else
            {
                current.Remove(conditionValue);
            }

            Settings.RefundConditons = current;
        }

        private async void BtnSaveCloudSettings_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.SongifyApiKey))
                {
                    TblError.Text = Loc("window_settings_cloud_need_api_key", "Please enter your Songify API key.");
                    return;
                }

                if (Settings.TwitchUser == null)
                {
                    TblError.Text = Loc("window_settings_cloud_need_twitch", "Please log in to your Twitch account.");
                    return;
                }

                if (string.IsNullOrEmpty(Settings.TwitchUser.Id))
                {
                    TblError.Text = Loc("window_settings_cloud_need_twitch", "Please log in to your Twitch account.");
                    return;
                }

                Tuple<bool, HttpStatusCode> result = await ConfigHandler.CloudSaveSettings(
                    Settings.TwitchUser.Id, Settings.CurrentConfig);

                if (result.Item1)
                {
                    TblError.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    TblError.Text = Loc("window_settings_cloud_saved", "Successfully saved settings in the cloud");
                }
                else
                {
                    TblError.Foreground = new SolidColorBrush(Colors.Red);

                    switch (result.Item2)
                    {
                        case HttpStatusCode.Unauthorized:
                            TblError.Text = Loc("window_settings_cloud_unauthorized", "Unauthorized access. Please check your API token.");
                            return;

                        case HttpStatusCode.Forbidden:
                            TblError.Text = Loc("window_settings_cloud_forbidden_premium",
                                "Cloud sync is included with Songify Premium.");
                            return;

                        case HttpStatusCode.InternalServerError:
                            TblError.Text = Loc("window_settings_cloud_server_error", "Internal server error. Please try again later.");
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private async void BtnRestoreCloudSettings_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.SongifyApiKey))
                {
                    TblError.Text = Loc("window_settings_cloud_need_api_key", "Please enter your Songify API key.");
                    return;
                }

                if (Settings.TwitchUser == null)
                {
                    TblError.Text = Loc("window_settings_cloud_need_twitch", "Please log in to your Twitch account.");
                    return;
                }

                if (string.IsNullOrEmpty(Settings.TwitchUser.Id))
                {
                    TblError.Text = Loc("window_settings_cloud_need_twitch", "Please log in to your Twitch account.");
                    return;
                }

                Tuple<bool, HttpStatusCode> result =
                    await ConfigHandler.CloudRestoreSettings(Settings.TwitchUser.Id);

                if (result.Item1)
                {
                    TblError.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    TblError.Text = "Successfully restored settings from the cloud";
                }
                else
                {
                    TblError.Foreground = new SolidColorBrush(Colors.Red);

                    switch (result.Item2)
                    {
                        case HttpStatusCode.Unauthorized:
                            TblError.Text = Loc("window_settings_cloud_unauthorized", "Unauthorized access. Please check your API token.");
                            return;

                        case HttpStatusCode.Forbidden:
                            TblError.Text = Loc("window_settings_cloud_forbidden_premium",
                                "Cloud sync is included with Songify Premium.");
                            return;

                        case HttpStatusCode.InternalServerError:
                            TblError.Text = Loc("window_settings_cloud_server_error", "Internal server error. Please try again later.");
                            return;

                        case HttpStatusCode.NotModified:
                            TblError.Foreground = new SolidColorBrush(Colors.LawnGreen);
                            TblError.Text = "No changes have been detected, keeping local settings.";
                            return;

                        case HttpStatusCode.NotAcceptable:
                            TblError.Text = "Cancelled by user.";
                            break;

                        case HttpStatusCode.ServiceUnavailable:
                            TblError.Text = "Error connecting to Songify service.";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void RefundSwitchesPanel_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double totalWidth = RefundSwitchesPanel.ActualWidth;
            double columnWidth = (totalWidth / 2) - 100; // Subtract some padding

            foreach (ToggleSwitch toggle in _toggleMap.Values)
            {
                if (toggle.Content is TextBlock tb)
                {
                    tb.MaxWidth = columnWidth;
                }
            }
        }

        private void TglOnlySkipNonSrRewards_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.SkipOnlyNonSrSongs = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void BtnRewardsSegmentChannel_Click(object sender, RoutedEventArgs e)
            => SetRewardsSegment(showChannelRewards: true);

        private void BtnRewardsSegmentRefund_Click(object sender, RoutedEventArgs e)
            => SetRewardsSegment(showChannelRewards: false);

        private void SetRewardsSegment(bool showChannelRewards)
        {
            BtnRewardsSegmentChannel.Appearance = showChannelRewards
                ? ControlAppearance.Primary
                : ControlAppearance.Secondary;
            BtnRewardsSegmentRefund.Appearance = showChannelRewards
                ? ControlAppearance.Secondary
                : ControlAppearance.Primary;

            ListboxRewards.Visibility = showChannelRewards ? Visibility.Visible : Visibility.Collapsed;
            PnlRefundConditions.Visibility = showChannelRewards ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Tglsw_BitsForSr_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.SrForBits = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void NudSpotifyFetchRate_OnValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (!NudSpotifyFetchRate.Value.HasValue) return;
            Settings.SpotifyFetchRate = (int)NudSpotifyFetchRate.Value;
        }

        private void BtnApiToken_OnClick(object sender, RoutedEventArgs e)
        {
            AccountLinking.OpenSongifyTokenFaq();
        }

        private void BtnGetSongifyToken_OnClick(object sender, RoutedEventArgs e)
        {
            AccountLinking.OpenSongifyTokenPage();
        }

        private async void BtnRefreshPremium_OnClick(object sender, RoutedEventArgs e)
        {
            if (BtnRefreshPremium != null)
                BtnRefreshPremium.IsEnabled = false;
            SongifyAuthService.Invalidate();
            await SongifyPremiumService.RefreshAsync();
            UpdateSongifyTokenStatus();
        }

        private void NotifySongifyTokenChanged()
        {
            UpdateSongifyTokenStatus();
            Pages.OverviewPage.RefreshChecklist();
            SchedulePremiumRefresh();
        }

        private void SchedulePremiumRefresh()
        {
            _premiumRefreshCts?.Cancel();
            _premiumRefreshCts = new CancellationTokenSource();
            CancellationToken token = _premiumRefreshCts.Token;
            _ = RefreshPremiumDebouncedAsync(token);
        }

        private static async Task RefreshPremiumDebouncedAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(800, token);
                SongifyAuthService.Invalidate();
                await SongifyPremiumService.RefreshAsync();
            }
            catch (TaskCanceledException)
            {
                // A newer token edit replaced this refresh.
            }
        }

        private void UpdateSongifyTokenStatus()
        {
            if (TblSongifyTokenStatus == null)
                return;

            if (BtnRefreshPremium != null)
                BtnRefreshPremium.IsEnabled = !SongifyPremiumService.IsRefreshing;

            if (SongifyPremiumService.IsRefreshing)
            {
                TblSongifyTokenStatus.Text = Loc("setup_token_checking_premium",
                    "Checking Songify Premium status…");
                TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.Gray);
                return;
            }

            if (!AccountLinking.HasSongifyApiToken())
            {
                TblSongifyTokenStatus.Text = Loc("setup_token_missing",
                    "No token yet. Song data and queue uploads will not work until you add one.");
                TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.Orange);
                return;
            }

            switch (SongifyPremiumService.Current)
            {
                case SongifyPremiumState.Active:
                    TblSongifyTokenStatus.Text = Loc("setup_token_premium_active",
                        "Token saved. Songify Premium is active.");
                    TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    return;
                case SongifyPremiumState.Inactive:
                    TblSongifyTokenStatus.Text = Loc("setup_token_premium_inactive",
                        "Token saved. Link Ko-fi on your account page to unlock recap, stats, and cloud sync.");
                    TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.DarkOrange);
                    return;
                case SongifyPremiumState.InvalidToken:
                    TblSongifyTokenStatus.Text = Loc("setup_token_invalid",
                        "This token is invalid. Generate a new one on your account page.");
                    TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    return;
                default:
                    TblSongifyTokenStatus.Text = Loc("setup_token_present",
                        "Token saved. Generate a new one on your account page if you need to replace it.");
                    TblSongifyTokenStatus.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    return;
            }
        }

        private void TglDebugLogging_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.DebugLogging = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void NudLogFileRetention_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents || NudLogFileRetention.Value == null)
                return;
            Settings.LogFileRetentionCount = (int)NudLogFileRetention.Value;
        }

        private void PasswordBox_YoutubeApiKey_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            string pwd = PasswordBox_YoutubeApiKey.Password ?? "";
            if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(Settings.YoutubeApiKey))
                return;
            Settings.YoutubeApiKey = pwd;
        }

        private void BtnTestYoutubeApi_OnClick(object sender, RoutedEventArgs e)
        {
            //TODO: Youtube API test
            TestYoutubeApi();
        }

        private async Task TestYoutubeApi()
        {
            try
            {
                PearSearch result = await YouTubeDataApiClient.GetMetaAsync(Settings.YoutubeApiKey, "dQw4w9WgXcQ");
                if (result != null)
                {
                    TextBlock_YoutubeApiResult.Text = $"Title: {result.Title}\n" +
                                                      $"Video ID: {result.VideoId}\n" +
                                                      $"Artists: {string.Join(", ", result.Artists)}\n" +
                                                      $"Album: {result.Album}\n" +
                                                      $"Duration: {result.Duration}\n" +
                                                      $"Views: {result.Views}";
                }
                else
                {
                    TextBlock_YoutubeApiResult.Text = "No data was returned. Please check your API key and try again.";
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void TextBoxPollTitle_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;
            Settings.TwitchPollSettings.Title = ((TextBox)sender).Text;
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void TextBoxPollAnswer1_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;

            Settings.TwitchPollSettings.Choices[0] = ((TextBox)sender).Text;
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void TextBoxPollAnswer2_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;

            Settings.TwitchPollSettings.Choices[1] = ((TextBox)sender).Text;
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void ToggleSwitchPollAdditionalVotes_OnToggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents) return;

            Settings.TwitchPollSettings.AdditionalVotesEnabled = ((ToggleSwitch)sender).IsChecked == true;
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void NumericUpDownPollChannelPointsPerVote_OnValueChanged(object sender,
            NumberBoxValueChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;

            double? value = ((NumberBox)sender).Value;
            if (value == null)
                return;
            Settings.TwitchPollSettings.ChannelPointsPerVote = (int)value;
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void RadioButtonPollAnswer1_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents) return;
            Settings.TwitchPollSettings.WinningChoice = ((RadioButton)sender).Content.ToString();
            Settings.TwitchPollSettings = Settings.TwitchPollSettings;
        }

        private void NumericUpDownPollDuration_OnValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            double? value = ((NumberBox)sender).Value;
            if (value != null)
            {
                Settings.TwitchPollSettings.Duration = (int)value;

                int totalSeconds = (int)value;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                TextBlockPollDuration.Text = $"({minutes:D2}m {seconds:D2}s)";
            }
        }

        private void Tgl_SharedChat_Toggled(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            Settings.SharedChatEnabled = ((ToggleSwitch)sender).IsChecked == true;
        }

        private void TbIgnoreChatUser_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;
            e.Handled = true;
            AddIgnoredChatUserFromInput();
        }

        private void BtnAddIgnoredChatUser_Click(object sender, RoutedEventArgs e)
            => AddIgnoredChatUserFromInput();

        private void BtnAddKnownBots_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;

            List<string> list = [.. Settings.IgnoredChatUsers ?? []];
            if (TwitchChatIgnore.MergeKnownBots(list) == 0)
                return;

            Settings.IgnoredChatUsers = list;
            RefreshIgnoredChatUsers();
        }

        private void AddIgnoredChatUserFromInput()
        {
            if (IgnoreControlEvents)
                return;
            string login = TwitchChatIgnore.NormalizeIgnoreName(TbIgnoreChatUser?.Text);
            if (login.Length == 0)
                return;

            List<string> list = [.. Settings.IgnoredChatUsers ?? []];
            if (list.Any(u => string.Equals(TwitchChatIgnore.NormalizeIgnoreName(u), login, StringComparison.OrdinalIgnoreCase)))
            {
                TbIgnoreChatUser.Text = "";
                return;
            }

            list.Add(login);
            Settings.IgnoredChatUsers = list;
            TbIgnoreChatUser.Text = "";
            RefreshIgnoredChatUsers();
        }

        private void BtnRemoveIgnoredChatUser_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if (sender is not System.Windows.Controls.Button { Tag: string login })
                return;

            List<string> list = [.. Settings.IgnoredChatUsers ?? []];
            list.RemoveAll(u => string.Equals(TwitchChatIgnore.NormalizeIgnoreName(u), login, StringComparison.OrdinalIgnoreCase));
            Settings.IgnoredChatUsers = list;
            RefreshIgnoredChatUsers();
        }

        private void BtnRemoveAllIgnoredChatUsers_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoreControlEvents)
                return;
            if ((Settings.IgnoredChatUsers ?? []).Count == 0)
                return;

            Settings.IgnoredChatUsers = [];
            RefreshIgnoredChatUsers();
        }

        private void RefreshIgnoredChatUsers()
        {
            if (IcIgnoredChatUsers == null)
                return;
            IcIgnoredChatUsers.ItemsSource = null;
            IcIgnoredChatUsers.ItemsSource = (Settings.IgnoredChatUsers ?? []).ToList();
            if (BtnRemoveAllIgnoredChatUsers != null)
                BtnRemoveAllIgnoredChatUsers.IsEnabled = (Settings.IgnoredChatUsers ?? []).Count > 0;
        }

        private void TbBitsKeyword_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreControlEvents) return;
            Settings.SrForBitsKeyWord = ((TextBox)sender).Text;
        }

        public void SelectTab(string tabName, string elementName = "")
        {
            foreach (TabItem tab in TabCtrl.Items)
            {
                if (tab.Tag?.ToString().Equals(tabName, StringComparison.CurrentCultureIgnoreCase) != true)
                    continue;
                TabCtrl.SelectedItem = tab;
                if (!string.IsNullOrWhiteSpace(elementName))
                    _ = FocusNamedElementAsync(elementName);
                break;
            }
        }

        private async Task FocusNamedElementAsync(string elementName)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            await Task.Delay(50);

            FrameworkElement element = FindName(elementName) as FrameworkElement;
            if (element == null)
                return;

            element.UpdateLayout();
            element.BringIntoView();
            HighlightElement(element);

            if (ReferenceEquals(element, CardSongifyApiToken) && PasswordBox != null)
                PasswordBox.Focus();
        }

        private DispatcherTimer _focusHighlightTimer;
        private FrameworkElement _focusHighlightTarget;
        private Brush _focusHighlightOriginalBrush;
        private Thickness _focusHighlightOriginalThickness;

        private void HighlightElement(FrameworkElement element)
        {
            ClearFocusHighlight();

            Brush accent = TryFindResource("AccentFillColorDefaultBrush") as Brush
                           ?? new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));

            _focusHighlightTarget = element;
            _focusHighlightOriginalBrush = element.GetValue(BorderBrushProperty) as Brush;
            _focusHighlightOriginalThickness = element is System.Windows.Controls.Control control
                ? control.BorderThickness
                : new Thickness(1);

            element.SetValue(BorderBrushProperty, accent);
            element.SetValue(BorderThicknessProperty, new Thickness(2));

            _focusHighlightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _focusHighlightTimer.Tick += (_, _) => ClearFocusHighlight();
            _focusHighlightTimer.Start();
        }

        private void ClearFocusHighlight()
        {
            _focusHighlightTimer?.Stop();
            _focusHighlightTimer = null;
            if (_focusHighlightTarget == null)
                return;

            _focusHighlightTarget.SetValue(BorderBrushProperty, _focusHighlightOriginalBrush);
            _focusHighlightTarget.SetValue(BorderThicknessProperty, _focusHighlightOriginalThickness);
            _focusHighlightTarget = null;
            _focusHighlightOriginalBrush = null;
        }

        private void MinimumMessagesBetweenAnnounces_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            Settings.MinimumMessagesBetweenAnnounces = (int)(args.NewValue ?? 0);
        }
    }
}