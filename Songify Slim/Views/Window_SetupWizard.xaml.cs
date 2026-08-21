using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Views.WPFUI.Pages;
using static Songify_Slim.Util.General.Enums;
using Clipboard = System.Windows.Clipboard;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using PasswordBox = System.Windows.Controls.PasswordBox;
using TextBox = System.Windows.Controls.TextBox;

namespace Songify_Slim.Views;

public partial class WindowSetupWizard
{
    private enum WizardStep
    {
        Welcome,
        Player,
        Spotify,
        Twitch,
        Token,
        Output,
        Done
    }

    private readonly FolderBrowserDialog _folderBrowser = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private List<WizardStep> _steps = [];
    private int _index;
    private bool _languageComboReady;
    private bool _playerComboReady;

    public bool StartTourRequested { get; private set; }

    public WindowSetupWizard()
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        Loaded += WindowSetupWizard_Loaded;
        Closed += WindowSetupWizard_Closed;
        Activated += (_, _) => RefreshLinkStatus();
    }

    private string Loc(string key, string fallback)
        => TryFindResource(key) as string ?? fallback;

    private void WindowSetupWizard_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsUi.Refreshed += OnAccountsRefreshed;
        _statusTimer.Tick += (_, _) => RefreshLinkStatus();
        _statusTimer.Start();
        BindLanguageCombo();
        BindPlayerCombo();
        TbClientId.Text = Settings.ClientId ?? "";
        if (!string.IsNullOrEmpty(Settings.SongifyApiKey))
            PwbToken.Password = Settings.SongifyApiKey;
        RefreshOutputPath();
        RebuildSteps(keepCurrent: false);
        ShowCurrentStep();
    }

    private void WindowSetupWizard_Closed(object sender, EventArgs e)
    {
        SettingsUi.Refreshed -= OnAccountsRefreshed;
        _statusTimer.Stop();
        GuidedSetup.MarkCompleted();
        OverviewPage.RefreshChecklist();
    }

    private void OnAccountsRefreshed()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RefreshLinkStatus);
            return;
        }

        RefreshLinkStatus();
    }

    private void BindLanguageCombo()
    {
        _languageComboReady = false;
        CbxLanguage.ItemsSource = LocalizationHelper.GetLanguages();
        CbxLanguage.SelectedValue = string.IsNullOrWhiteSpace(Settings.Language) ? "en" : Settings.Language;
        _languageComboReady = true;
    }

    private void BindPlayerCombo()
    {
        _playerComboReady = false;
        var items = Enum.GetValues(typeof(PlayerType))
            .Cast<PlayerType>()
            .Select(p => new { Value = p, Name = EnumHelper.GetDescription(p) })
            .ToList();
        CbxPlayer.ItemsSource = items;
        CbxPlayer.DisplayMemberPath = "Name";
        CbxPlayer.SelectedValuePath = "Value";
        CbxPlayer.SelectedValue = Settings.Player;
        _playerComboReady = true;
    }

    private void RebuildSteps(bool keepCurrent)
    {
        WizardStep current = _steps.Count > 0 && _index >= 0 && _index < _steps.Count
            ? _steps[_index]
            : WizardStep.Welcome;

        _steps =
        [
            WizardStep.Welcome,
            WizardStep.Player
        ];
        if (Settings.Player == PlayerType.Spotify)
            _steps.Add(WizardStep.Spotify);
        _steps.Add(WizardStep.Twitch);
        _steps.Add(WizardStep.Token);
        _steps.Add(WizardStep.Output);
        _steps.Add(WizardStep.Done);

        if (!keepCurrent)
        {
            _index = 0;
            return;
        }

        int found = _steps.IndexOf(current);
        _index = found >= 0 ? found : Math.Min(_index, _steps.Count - 1);
    }

    private void ShowCurrentStep()
    {
        if (_index < 0) _index = 0;
        if (_index >= _steps.Count) _index = _steps.Count - 1;

        WizardStep step = _steps[_index];
        StepWelcome.Visibility = step == WizardStep.Welcome ? Visibility.Visible : Visibility.Collapsed;
        StepPlayer.Visibility = step == WizardStep.Player ? Visibility.Visible : Visibility.Collapsed;
        StepSpotify.Visibility = step == WizardStep.Spotify ? Visibility.Visible : Visibility.Collapsed;
        StepTwitch.Visibility = step == WizardStep.Twitch ? Visibility.Visible : Visibility.Collapsed;
        StepToken.Visibility = step == WizardStep.Token ? Visibility.Visible : Visibility.Collapsed;
        StepOutput.Visibility = step == WizardStep.Output ? Visibility.Visible : Visibility.Collapsed;
        StepDone.Visibility = step == WizardStep.Done ? Visibility.Visible : Visibility.Collapsed;

        TxtStepLabel.Text = string.Format(
            Loc("setup_step_label", "Step {0} of {1}"),
            _index + 1,
            _steps.Count);

        BtnBack.Visibility = _index > 0 ? Visibility.Visible : Visibility.Collapsed;
        bool last = step == WizardStep.Done;
        BtnTour.Visibility = last ? Visibility.Visible : Visibility.Collapsed;
        BtnNext.Content = last
            ? Loc("setup_finish", "Finish")
            : Loc("setup_next", "Next");

        if (step is WizardStep.Spotify or WizardStep.Twitch or WizardStep.Token or WizardStep.Done)
            RefreshLinkStatus();
        if (step == WizardStep.Done)
            RebuildStatusChips();
    }

    private void RefreshLinkStatus()
    {
        if (TxtSpotifyStatus != null)
        {
            if (AccountLinking.IsSpotifyLinked())
            {
                string name = Settings.SpotifyProfile?.DisplayName
                              ?? Loc("setup_spotify_linked_unknown", "Linked");
                TxtSpotifyStatus.Text = string.Format(
                    Loc("setup_spotify_linked", "Linked as {0}"),
                    name);
            }
            else
            {
                TxtSpotifyStatus.Text = Loc("setup_spotify_not_linked", "Not linked yet. You can do this later in Settings.");
            }
        }

        if (TxtTwitchStatus != null)
        {
            if (AccountLinking.IsTwitchMainLinked() && Settings.TwitchUser != null)
            {
                TxtTwitchStatus.Text = string.Format(
                    Loc("setup_twitch_linked", "Linked as {0}"),
                    Settings.TwitchUser.DisplayName ?? Settings.TwitchUser.Login);
            }
            else if (AccountLinking.IsTwitchMainLinked())
            {
                TxtTwitchStatus.Text = Loc("setup_twitch_linked_unknown", "Twitch account linked.");
            }
            else
            {
                TxtTwitchStatus.Text = Loc("setup_twitch_not_linked", "Optional if you only need a now-playing overlay.");
            }
        }

        if (TxtTokenStatus != null)
        {
            TxtTokenStatus.Text = AccountLinking.HasSongifyApiToken()
                ? Loc("setup_token_present", "Token saved. Generate a new one on your account page if you need to replace it.")
                : Loc("setup_token_missing", "No token yet. Song data and queue uploads will not work until you add one.");
        }

        if (StepDone.Visibility == Visibility.Visible)
            RebuildStatusChips();
    }

    private void RebuildStatusChips()
    {
        PnlStatusChips.Children.Clear();
        AddStatusRow(
            Loc("setup_checklist_spotify", "Link Spotify"),
            Settings.Player != PlayerType.Spotify || AccountLinking.IsSpotifyLinked());
        AddStatusRow(
            Loc("setup_checklist_twitch", "Link Twitch (for song requests)"),
            AccountLinking.IsTwitchMainLinked());
        AddStatusRow(
            Loc("setup_checklist_token", "Add Songify API token"),
            AccountLinking.HasSongifyApiToken());
        AddStatusRow(
            Loc("setup_checklist_output", "Song output file (OBS)"),
            GuidedSetup.IsOutputReady());
    }

    private void AddStatusRow(string label, bool done)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };
        row.Children.Add(new TextBlock
        {
            Text = done ? "✓" : "○",
            Width = 22,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)(TryFindResource(done ? "AccentTextFillColorPrimaryBrush" : "TextFillColorTertiaryBrush")
                                 ?? Brushes.Gray)
        });
        row.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)(TryFindResource("TextFillColorPrimaryBrush") ?? Brushes.White)
        });
        PnlStatusChips.Children.Add(row);
    }

    private void CbxLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_languageComboReady || CbxLanguage.SelectedValue is not string code)
            return;
        LocalizationHelper.Apply(code);
        BindLanguageCombo();
        ShowCurrentStep();
    }

    private async void CbxPlayer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_playerComboReady || CbxPlayer.SelectedValue is not PlayerType selected)
            return;
        if (Settings.Player == selected)
            return;

        PlayerType previous = Settings.Player;
        Settings.Player = selected;
        try
        {
            await AppFetchService.ApplyPlayerSourceAsync(previous, selected);
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }

        RebuildSteps(keepCurrent: true);
        ShowCurrentStep();
    }

    private void TbClientId_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box)
            Settings.ClientId = box.Text?.Trim() ?? "";
    }

    private void BtnSpotifyHelp_OnClick(object sender, RoutedEventArgs e)
        => AccountLinking.OpenSpotifySetupGuide();

    private async void BtnSpotifyLink_OnClick(object sender, RoutedEventArgs e)
    {
        SpotifyLinkResult result = await AccountLinking.LinkSpotifyAsync();
        if (result == SpotifyLinkResult.MissingClientId)
        {
            AppDialogResult dialog = await AppDialog.ShowAsync(
                Loc("setup_spotify_title", "Spotify"),
                Loc("common_fill_client_id_secret", "Please fill in your Spotify Client ID first."),
                AppDialogStyle.PrimaryAndSecondary,
                new AppDialogSettings
                {
                    PrimaryButtonText = Loc("common_ok", "OK"),
                    SecondaryButtonText = Loc("setup_spotify_open_guide", "How to get a Client ID")
                });
            if (dialog == AppDialogResult.Secondary)
                AccountLinking.OpenSpotifySetupGuide();
            return;
        }

        RefreshLinkStatus();
    }

    private void BtnTwitchLogin_OnClick(object sender, RoutedEventArgs e)
        => AccountLinking.LoginTwitchMain();

    private void BtnGetToken_OnClick(object sender, RoutedEventArgs e)
        => AccountLinking.OpenSongifyTokenPage();

    private void PwbToken_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box)
            return;
        string pwd = box.Password ?? "";
        if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(Settings.SongifyApiKey))
            return;
        Settings.SongifyApiKey = pwd;
        RefreshLinkStatus();
        OverviewPage.RefreshChecklist();
    }

    private void BtnBrowseOutput_OnClick(object sender, RoutedEventArgs e)
    {
        _folderBrowser.Description = Loc("window_settings_folder_song_output", "Path where the text file will be located.");
        _folderBrowser.SelectedPath = string.IsNullOrEmpty(Settings.Directory)
            ? AppPaths.GetAppDirectory()
            : Settings.Directory;
        if (_folderBrowser.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        Settings.Directory = _folderBrowser.SelectedPath;
        RefreshOutputPath();
    }

    private void BtnCopyOutput_OnClick(object sender, RoutedEventArgs e)
    {
        Clipboard.SetDataObject(GuidedSetup.DefaultOutputFilePath());
    }

    private void RefreshOutputPath()
    {
        TxtOutputDirectory.Text = GuidedSetup.DefaultOutputFilePath();
    }

    private void BtnBack_OnClick(object sender, RoutedEventArgs e)
    {
        if (_index <= 0) return;
        _index--;
        ShowCurrentStep();
    }

    private void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (_steps[_index] == WizardStep.Done)
        {
            Close();
            return;
        }

        _index++;
        ShowCurrentStep();
    }

    private void BtnSkip_OnClick(object sender, RoutedEventArgs e) => Close();

    private void BtnTour_OnClick(object sender, RoutedEventArgs e)
    {
        StartTourRequested = true;
        Close();
    }
}
