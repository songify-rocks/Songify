using System;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_ManualTwitchLogin.xaml
    /// </summary>
    public partial class WindowManualTwitchLogin
    {
        private readonly Enums.TwitchAccount _accountType;

        public WindowManualTwitchLogin(Enums.TwitchAccount accountType)
        {
            InitializeComponent();
            _accountType = accountType;
            Title = accountType switch
            {
                Enums.TwitchAccount.Main => "Twitch Account Linking: MAIN ACCOUNT",
                Enums.TwitchAccount.Bot => "Twitch Account Linking: BOT ACCOUNT",
                _ => Title
            };
        }

        private void Button_OpenTwitchLoginPage_Click(object sender, RoutedEventArgs e)
        {
            ShellHelper.OpenUrl("https://v2.songify.rocks/auth/alt2/");
        }

        private async void Button_LinkAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxTwitchCode.Password))
                return;
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

                    default:
                        break;
                }

                await SettingsUi.RefreshAsync(resetTwitch: true);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                Close();
            }
        }
    }
}
