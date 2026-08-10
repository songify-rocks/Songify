using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Views;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        BtnOpenAbout.Content = Properties.Resources.menu_help_about;
    }

    private void BtnOpenAbout_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}