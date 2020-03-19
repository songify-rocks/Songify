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

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für Window_Queue.xaml
    /// </summary>
    public partial class Window_Queue
    {
        public Window_Queue()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() != typeof(MainWindow))
                    continue;

                dgv_Queue.ItemsSource = (window as MainWindow).ReqList;
                dgv_Queue.Columns[0].Visibility = Visibility.Hidden;
            }
        }
    }
}
