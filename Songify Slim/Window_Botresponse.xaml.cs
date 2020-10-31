using System;
using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Util.Settings;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für Window_Botresponse.xaml
    /// </summary>
    public partial class Window_Botresponse
    {
        public Window_Botresponse()
        {
            InitializeComponent();
        }


        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Cctrl.Content = new UserControls.UC_BotResponses();
        }
    }
}
