using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    ///     Interaktionslogik für UC_Setup_5.xaml
    /// </summary>
    public partial class UC_Setup_5 : UserControl
    {
        private DispatcherTimer _dispatcherTimer;

        public UC_Setup_5()
        {
            InitializeComponent();
        }

        private void btn_Link_Click(object sender, RoutedEventArgs e)
        {
            // Links Spotify
            Settings.RefreshToken = "";
            Settings.ClientId = tb_ClientID.Text;
            Settings.ClientSecret = tb_ClientSecret.Password;
            try
            {
                ApiHandler.DoAuthAsync();
            }

            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }

            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal) {Interval = TimeSpan.FromSeconds(1)};
            _dispatcherTimer.Tick += DispatcherTimerOnTick;
            _dispatcherTimer.Start();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink source) Process.Start(source.NavigateUri.ToString());
        }

        private void DispatcherTimerOnTick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbl_Linked.Text) || !ApiHandler.Authed) return;
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb_ClientID.Text = Settings.ClientId;
            tb_ClientSecret.Password = Settings.ClientSecret;
            Tglsw_Spotify.IsOn = Settings.UseOwnApp;

            tb_ClientID.IsEnabled = Settings.UseOwnApp;
            tb_ClientSecret.IsEnabled = Settings.UseOwnApp;
        }

        private void tb_ClientID_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            Settings.ClientId = tb_ClientSecret.Password;
        }

        private void tb_ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ClientSecret = tb_ClientSecret.Password;
        }

        private void Tglsw_Spotify_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            tb_ClientID.IsEnabled = Tglsw_Spotify.IsOn;
            tb_ClientSecret.IsEnabled = Tglsw_Spotify.IsOn;

            Settings.UseOwnApp = Tglsw_Spotify.IsOn;
        }
    }
}