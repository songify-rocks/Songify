using Songify_Slim.Util.Settings;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    ///     Interaktionslogik für UC_Setup_3.xaml
    /// </summary>
    public partial class UC_Setup_3 : UserControl
    {
        public UC_Setup_3()
        {
            InitializeComponent();
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            // checkbox for autostart
            bool? chbxAutostartIsChecked = ChbxAutostart.IsOn;
            MainWindow.RegisterInStartup((bool)chbxAutostartIsChecked);
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            // enables / disbales minimize to systray
            bool? isChecked = ChbxMinimizeSystray.IsOn;
            Settings.Systray = (bool)isChecked;
        }

        private void ChbxTelemetry_IsCheckedChanged(object sender, EventArgs e)
        {
            // enables / disables telemetry
            Settings.Telemetry = (bool)ChbxTelemetry.IsOn;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Sets all the controls from settings
            ChbxAutostart.IsOn = Settings.Autostart;
            ChbxMinimizeSystray.IsOn = Settings.Systray;
            ChbxTelemetry.IsOn = Settings.Telemetry;
        }
    }
}