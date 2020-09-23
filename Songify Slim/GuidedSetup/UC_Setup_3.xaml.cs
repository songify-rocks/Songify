using Songify_Slim.Util.Settings;
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

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    /// Interaktionslogik für UC_Setup_3.xaml
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
            bool? chbxAutostartIsChecked = ChbxAutostart.IsChecked;
            MainWindow.RegisterInStartup(chbxAutostartIsChecked != null && (bool)chbxAutostartIsChecked);
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            // enables / disbales minimize to systray
            bool? isChecked = ChbxMinimizeSystray.IsChecked;
            Settings.Systray = isChecked != null && (bool)isChecked;
        }

        private void ChbxTelemetry_IsCheckedChanged(object sender, EventArgs e)
        {
            // enables / disables telemetry
            if (ChbxTelemetry.IsChecked == null) return;
            Settings.Telemetry = (bool)ChbxTelemetry.IsChecked;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Sets all the controls from settings
            ChbxAutostart.IsChecked = Settings.Autostart;
            ChbxMinimizeSystray.IsChecked = Settings.Systray;
            ChbxTelemetry.IsChecked = Settings.Telemetry;
        }
    }
}
