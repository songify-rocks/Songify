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
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_ManualTwitchLogin.xaml
    /// </summary>
    public partial class Window_ManualTwitchLogin
    {
        private TwitchHandler.TwitchAccount _accountType;
        public Window_ManualTwitchLogin(TwitchHandler.TwitchAccount accountType)
        {
            InitializeComponent();
            this._accountType = accountType;
            switch (accountType)
            {
                case TwitchHandler.TwitchAccount.Main:
                    Title = "Twitch Account Linking: MAIN ACCOUNT";
                    break;
                case TwitchHandler.TwitchAccount.Bot:
                    Title = "Twitch Account Linking: BOT ACCOUNT";
                    break;
            }
        }

        private void Button_OpenTwitchLoginPage_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://songify.overcode.tv/auth/alt2/");
        }

        private async void Button_LinkAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxTwitchCode.Password))
                return;
            this.IsEnabled = false;
            try
            {
                switch (_accountType)
                {
                    case TwitchHandler.TwitchAccount.Main:
                        Settings.TwitchAccessToken = TextBoxTwitchCode.Password;
                        await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Main);
                        break;
                    case TwitchHandler.TwitchAccount.Bot:
                        Settings.TwitchBotToken = TextBoxTwitchCode.Password;
                        await TwitchHandler.InitializeApi(TwitchHandler.TwitchAccount.Bot);
                        break;
                    default:
                        break;
                }

                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(Window_Settings)) continue;
                    await ((Window_Settings)window).SetControls();
                    ((Window_Settings)window).ResetTwitchConnection();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}
