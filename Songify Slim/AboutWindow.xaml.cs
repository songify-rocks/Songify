using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }


        private void BtnDonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/inzaniity");
        }

        private void BtnDiscordClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Inzaniity/Songify");
        }
    }
}
