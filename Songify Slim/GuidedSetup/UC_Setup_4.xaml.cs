using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Songify_Slim.Util.Settings;
using UserControl = System.Windows.Controls.UserControl;

namespace Songify_Slim.GuidedSetup
{
    /// <summary>
    /// Interaction logic for UC_Setup_4.xaml
    /// </summary>
    public partial class UC_Setup_4 : UserControl
    {
        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();
        private Window _mW;

        public UC_Setup_4()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            // assing mw to mainwindow for calling methods and setting texts etc
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() == typeof(Window_GuidedSetup))
                {
                    _mW = window;
                }
            }

            // Sets all the controls from settings
            if (!string.IsNullOrEmpty(Settings.Directory))
                TxtbxOutputdirectory.Text = Settings.Directory;
            ChbxCustomPause.IsChecked = Settings.CustomPauseTextEnabled;
            TxtbxCustompausetext.Text = Settings.CustomPauseText;
            TxtbxOutputformat.Text = Settings.OutputString;
            ChbxUpload.IsChecked = Settings.Upload;
            ChbxCover.IsChecked = Settings.DownloadCover;
            ChbxSplit.IsChecked = Settings.SplitOutput;
            ChbxSplit.IsChecked = Settings.SplitOutput;
            ChbxSpaces.IsChecked = Settings.AppendSpaces;
            nud_Spaces.Value = Settings.SpaceCount;
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            // Where the user wants the text file to be saved in
            _fbd.Description = @"Path where the text file will be located.";
            _fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (_fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            TxtbxOutputdirectory.Text = _fbd.SelectedPath;
            Settings.Directory = _fbd.SelectedPath;
        }


        private void AppendText(System.Windows.Controls.TextBox tb, string text)
        {
            // Appends Rightclick-Text from the output text box (parameters)
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            if (tb.ContextMenu != null) tb.ContextMenu.IsOpen = false;
        }


        private void MenuBtnArtist_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{artist}");
        }

        private void MenuBtnExtra_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{extra}");
        }

        private void MenuBtnTitle_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{title}");
        }
        private void MenuBtnReq_Click(object sender, RoutedEventArgs e)
        {
            // appends text
            AppendText(TxtbxOutputformat, "{{requested by {req}}}");
        }

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write custom output format to settings
            Settings.OutputString = TxtbxOutputformat.Text;
        }
        private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        {
            // enables / disables custom pause
            if (ChbxCustomPause.IsChecked == null) return;
            Settings.CustomPauseTextEnabled = (bool)ChbxCustomPause.IsChecked;
            if (!(bool)ChbxCustomPause.IsChecked)
            {
                TxtbxCustompausetext.IsEnabled = false;
            }
            else
            {
                TxtbxCustompausetext.IsEnabled = true;
            }
        }
        private void ChbxUpload_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables upload
            if (ChbxUpload.IsChecked != null)
                Settings.Upload = (bool)ChbxUpload.IsChecked;
        }

        private void ChbxCover_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            if (ChbxCover.IsChecked == null) return;
            Settings.DownloadCover = (bool)ChbxCover.IsChecked;
        }
        private void ChbxSplit_Checked(object sender, RoutedEventArgs e)
        {
            // enables / disables telemetry
            if (ChbxSplit.IsChecked == null) return;
            if (ChbxCover.IsChecked != null) Settings.SplitOutput = (bool)ChbxCover.IsChecked;
        }

        private void ChbxSpaces_Checked(object sender, RoutedEventArgs e)
        {
            if (ChbxSpaces.IsChecked != null) Settings.AppendSpaces = (bool)ChbxSpaces.IsChecked;
        }
        private void nud_Spaces_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nud_Spaces.Value != null) Settings.SpaceCount = (int)nud_Spaces.Value;
        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            // write CustomPausetext to settings
            Settings.CustomPauseText = TxtbxCustompausetext.Text;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            var scrollViewer = (ScrollViewer) sender;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                ((Window_GuidedSetup) _mW).btn_Next.IsEnabled = true;
        }
    }
}
