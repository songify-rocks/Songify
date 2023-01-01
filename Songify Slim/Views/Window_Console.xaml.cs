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
using MahApps.Metro.IconPacks;
using Songify_Slim.Util.General;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Console.xaml
    /// </summary>
    public partial class Window_Console
    {
        public Window_Console()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            richTextBox.Document = GlobalObjects.ConsoleDocument;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void richTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            richTextBox.ScrollToEnd();
        }

        private void MetroWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnAttachDetach_OnClick(object sender, RoutedEventArgs e)
        {
            GlobalObjects.DetachConsole = !GlobalObjects.DetachConsole;
            IconDetach.Kind = GlobalObjects.DetachConsole ? PackIconBootstrapIconsKind.Fullscreen : PackIconBootstrapIconsKind.LayoutSidebar;
            IsWindowDraggable = GlobalObjects.DetachConsole;
            if (GlobalObjects.DetachConsole) return;
            this.Left = Owner.Left + Owner.Width;
            this.Top = Owner.Top;
        }
    }
}
