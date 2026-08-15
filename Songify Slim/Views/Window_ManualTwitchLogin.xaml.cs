using System;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Views;

/// <summary>
/// Manual Twitch OAuth fallback: open login page, paste code, link account.
/// </summary>
public partial class WindowManualTwitchLogin
{
    private readonly Enums.TwitchAccount _accountType;
    private bool _isBusy;

    public WindowManualTwitchLogin(Enums.TwitchAccount accountType)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        _accountType = accountType;
        ApplyAccountTitle();
    }

    private void ApplyAccountTitle()
    {
        string key = _accountType switch
        {
            Enums.TwitchAccount.Main => "window_manualtwitch_title_main",
            Enums.TwitchAccount.Bot => "window_manualtwitch_title_bot",
            _ => "window_manualtwitch_title"
        };

        string title = TryFindResource(key) as string
                       ?? (_accountType == Enums.TwitchAccount.Bot
                           ? "Twitch Account Linking: Bot Account"
                           : "Twitch Account Linking: Main Account");

        Title = title;
        DlgTitleBar.Title = title;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_OpenTwitchLoginPage_Click(object sender, RoutedEventArgs e)
    {
        ShellHelper.OpenUrl("https://v2.songify.rocks/auth/alt2/");
    }

    private async void Button_LinkAccounts_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || string.IsNullOrEmpty(TextBoxTwitchCode.Password))
            return;

        _isBusy = true;
        IsEnabled = false;
        try
        {
            switch (_accountType)
            {
                case Enums.TwitchAccount.Main:
                    Settings.TwitchAccessToken = TextBoxTwitchCode.Password;
                    await TwitchHandler.InitializeApi(Enums.TwitchAccount.Main);
                    break;

                case Enums.TwitchAccount.Bot:
                    Settings.TwitchBotToken = TextBoxTwitchCode.Password;
                    await TwitchHandler.InitializeApi(Enums.TwitchAccount.Bot);
                    break;
            }

            await SettingsUi.RefreshAsync(resetTwitch: true);
        }
        catch (Exception exception)
        {
            Logger.LogExc(exception);
            throw;
        }
        finally
        {
            _isBusy = false;
            Close();
        }
    }
}
