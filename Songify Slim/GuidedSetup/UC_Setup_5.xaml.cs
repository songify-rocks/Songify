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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    /// Interaktionslogik für UC_Setup_5.xaml
    /// </summary>
    public partial class UC_Setup_5 : UserControl
    {
        private DispatcherTimer _dispatcherTimer;

        public UC_Setup_5()
        {
            InitializeComponent();
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, EventArgs e)
        {
            if (Tglsw_Spotify.IsChecked != null)
            {
                tb_ClientID.IsEnabled = (bool)Tglsw_Spotify.IsChecked;
                tb_ClientSecret.IsEnabled = (bool)Tglsw_Spotify.IsChecked;
            }
            if (Tglsw_Spotify.IsChecked != null) Settings.UseOwnApp = (bool)Tglsw_Spotify.IsChecked;

        }

        private void btn_Link_Click(object sender, RoutedEventArgs e)
        {
            // Links Spotify
            Settings.RefreshToken = "";
            try
            {
                ApiHandler.DoAuthAsync();
            }

            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }


            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            _dispatcherTimer.Tick += DispatcherTimerOnTick;
            _dispatcherTimer.Start();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink source)
            {
                System.Diagnostics.Process.Start(source.NavigateUri.ToString());
            }
        }

        private void DispatcherTimerOnTick(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbl_Linked.Text))
            {
                try
                {
                    tbl_Linked.Text = Properties.Resources.sw_Integration_SpotifyLinked + " " +
                                      ApiHandler.Spotify.GetPrivateProfile().DisplayName;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb_ClientID.Text = Settings.ClientId;
            tb_ClientSecret.Password = Settings.ClientSecret;
            Tglsw_Spotify.IsChecked = Settings.UseOwnApp;

            tb_ClientID.IsEnabled = Settings.UseOwnApp;
            tb_ClientSecret.IsEnabled = Settings.UseOwnApp;
        }

        private void tb_ClientID_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            Settings.ClientSecret = tb_ClientSecret.Password;
        }

        private void tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientId = tb_ClientID.Text;
        }
    }
}
