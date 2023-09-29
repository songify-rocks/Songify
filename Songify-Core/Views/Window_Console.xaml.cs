using MahApps.Metro.IconPacks;
using Songify_Core.Util.General;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Songify_Core.Views
{
    /// <summary>
    /// Interaction logic for Window_Console.xaml
    /// </summary>
    public partial class WindowConsole
    {
        public WindowConsole()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            richTextBox.Document = GlobalObjects.ConsoleDocument;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
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
            Left = Owner.Left + Owner.Width;
            Top = Owner.Top;
        }

        private void BtnClearConsole_OnClick(object sender, RoutedEventArgs e)
        {
            GlobalObjects.ConsoleDocument.Blocks.Clear();
        }
    }
}
