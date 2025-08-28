using MahApps.Metro.IconPacks;
using Songify_Slim.Util.General;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Console.xaml
    /// </summary>
    public partial class WindowConsole
    {
        private readonly ApiMetricsViewModel _metricsVm = new();

        public WindowConsole()
        {
            InitializeComponent();
            DataContext = _metricsVm; // <-- makes {Binding Rows} work
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _metricsVm.Dispose(); // stop the timer if this window is ever actually closed
        }

        private void BtnLog_OnClick(object sender, RoutedEventArgs e)
        {
            TiLog.IsSelected = true;
        }

        private void BtnMetrics_OnClick(object sender, RoutedEventArgs e)
        {
            TiMetrics.IsSelected = true;
        }

        private void BtnToGraph_OnClick(object sender, RoutedEventArgs e)
        {
            TiGraph.IsSelected = true;
        }

        private void BtnToMetrics_OnClick(object sender, RoutedEventArgs e)
        {
            TiMetrics.IsSelected = true;
        }
    }
}
