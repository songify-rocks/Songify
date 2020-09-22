using Songify_Slim.GuidedSetup;
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
using Songify_Slim.Properties;
using Settings = Songify_Slim.Util.Settings.Settings;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für Window_GuidedSetup.xaml
    /// </summary>
    public partial class Window_GuidedSetup
    {
        private int step = 0;
        public Window_GuidedSetup()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GuidedSetupStep(step);
        }

        private void GuidedSetupStep(int i)
        {
            btn_Back.Content = "Back";
            btn_Next.Content = "Next";
            switch (i)
            {
                case 0:
                    btn_Back.Content = "Cancel";
                    btn_Next.Content = "Accept";
                    tsControl.Content = new UC_Setup_1();
                    break;
                case 1:
                    tsControl.Content = new UC_Setup_2();
                    break;
                case 2:
                    if (!Settings.GuidedSetup)
                    {
                        MainWindow main = new MainWindow();
                        main.Show();
                        this.Close();
                    }

                    //tsControl.Content = new UC_Setup_2();

                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
            }
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {
            step++;
            GuidedSetupStep(step);
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            if (step == 0)
            {
                Application.Current.Shutdown();
            }
            else
            {
                step--;
                GuidedSetupStep(step);
            }
        }
    }
}
