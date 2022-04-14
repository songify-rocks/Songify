using System.Windows;
using System.Windows.Controls;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    ///     Interaktionslogik für UC_Setup_1.xaml
    /// </summary>
    public partial class UC_Setup_1 : UserControl
    {
        private Window _mW;

        public UC_Setup_1()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // assing mw to mainwindow for calling methods and setting texts etc
            foreach (Window window in Application.Current.Windows)
                if (window.GetType() == typeof(Window_GuidedSetup))
                    _mW = window;
        }

        private void sc_EULA_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                ((Window_GuidedSetup)_mW).btn_Next.IsEnabled = true;
        }
    }
}