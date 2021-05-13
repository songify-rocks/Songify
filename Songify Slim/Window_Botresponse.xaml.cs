using System.Windows;
using Songify_Slim.UserControls;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaktionslogik für Window_Botresponse.xaml
    /// </summary>
    public partial class Window_Botresponse
    {
        public Window_Botresponse()
        {
            InitializeComponent();
        }


        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Cctrl.Content = new UC_BotResponses();
        }
    }
}