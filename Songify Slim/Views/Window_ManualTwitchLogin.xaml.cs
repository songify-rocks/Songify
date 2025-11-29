using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
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
            System.Diagnostics.Process.Start("https://v2.songify.rocks/auth/alt2/");
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

                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(Window_Settings)) continue;
                    await ((Window_Settings)window).SetControls();
                    await ((Window_Settings)window).ResetTwitchConnection();
                }
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