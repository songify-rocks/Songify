using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Views;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        BtnOpenSettings.Content = Properties.Resources.menu_file_settings;
    }

    private void BtnOpenSettings_Click(object sender, RoutedEventArgs e)
    {
        new Window_Settings { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}