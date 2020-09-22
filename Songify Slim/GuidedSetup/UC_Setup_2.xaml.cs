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
using Songify_Slim.Util.Settings;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    /// Interaktionslogik für UC_Setup_2.xaml
    /// </summary>
    public partial class UC_Setup_2 : UserControl
    {
        private Window _mW;

        public UC_Setup_2()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            rbtn_Yes.IsChecked = Settings.GuidedSetup;
        }

        private void rbtn_Yes_Checked(object sender, RoutedEventArgs e)
        {
            Settings.GuidedSetup = true;
        }

        private void rbtn_No_Checked(object sender, RoutedEventArgs e)
        {
            Settings.GuidedSetup = false;
        }
    }
}
