using MahApps.Metro.Controls;
using Songify_Slim.GuidedSetup;
using Songify_Slim.Util.Settings;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaktionslogik für Window_GuidedSetup.xaml
    /// </summary>
    public partial class Window_GuidedSetup
    {
        private readonly int _maxSteps = 5;

        // Steps: 0 = Welcome, 1 = EULA, 2 = Setup Yes/No, 3 = General Settings, 4 = Spotify Setup, 5 = Finish
        private int _step;


        public Window_GuidedSetup()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GuidedSetupStep(_step);
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
                    Title = "Songify Setup - EULA";
                    tsControl.Content = new UC_Setup_1();
                    break;
                case 1:
                    Title = "Songify Setup";
                    tsControl.Content = new UC_Setup_2();
                    break;
                case 2:
                    if (!Settings.GuidedSetup)
                    {
                        new MainWindow().Show();
                        Close();
                    }

                    tsControl.Content = new UC_Setup_3();
                    break;
                case 3:
                    tsControl.Content = new UC_Setup_4();
                    btn_Next.IsEnabled = false;
                    break;
                case 4:
                    tsControl.Content = new UC_Setup_5();
                    btn_Next.Content = "Finish";
                    break;
                case 5:
                    new MainWindow().Show();
                    Close();
                    break;
            }
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {
            if (_step >= _maxSteps) return;

            tsControl.Transition = TransitionType.Left;
            _step++;
            GuidedSetupStep(_step);
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            if (_step == 0)
            {
                Application.Current.Shutdown();
            }
            else
            {
                tsControl.Transition = TransitionType.Right;
                _step--;
                GuidedSetupStep(_step);
            }
        }
    }
}