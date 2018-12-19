using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Input;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        private Window mW;

        private readonly string[] _colors = new string[]
                                       {
                                                   "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
                                                   "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
                                                   "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve",
                                                   "Taupe", "Sienna"
                                       };

        private readonly FolderBrowserDialog _fbd = new FolderBrowserDialog();

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.GetDirectory()))
            {
                System.Windows.Clipboard.SetDataObject(Assembly.GetEntryAssembly().Location + "\\Songify.txt");
            }
            else
            {
                System.Windows.Clipboard.SetDataObject(Settings.GetDirectory() + "\\Songify.txt");
            }
            (mW as MainWindow).LblStatus.Content = @"Path copied to clipboard.";
        }

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void BtnUpdatesClick(object sender, RoutedEventArgs e)
        {
            MainWindow.CheckForUpdates();
        }

        private void TxtbxCustompausetext_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SetCustomPauseText(TxtbxCustompausetext.Text);
        }

        private void ChbxCustompauseChecked(object sender, RoutedEventArgs e)
        {
            Settings.SetCustomPauseTextEnabled((bool)ChbxCustomPause.IsChecked);
            if (!(bool)ChbxCustomPause.IsChecked)
            {
                TxtbxCustompausetext.IsEnabled = false;
            }
            else
            {
                TxtbxCustompausetext.IsEnabled = true;
            }
        }

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            Settings.SetTheme(this.ThemeToggleSwitch.IsChecked == true ? "BaseDark" : "BaseLight");
            ThemeHandler.ApplyTheme();
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(this.ComboBoxColor.SelectedValue.ToString());
            ThemeHandler.ApplyTheme();
            if (Settings.GetColor() != "Yellow")
            {
                (mW as MainWindow).LblStatus.Foreground = Brushes.White;
                (mW as MainWindow).LblCopyright.Foreground = Brushes.White;
            }
            else
            {
                (mW as MainWindow).LblStatus.Foreground = Brushes.Black;
                (mW as MainWindow).LblCopyright.Foreground = Brushes.Black;
            }
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            var isChecked = this.ChbxMinimizeSystray.IsChecked;
            Settings.SetSystray(isChecked != null && (bool)isChecked);
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            this._fbd.Description = @"Path where the text file will be located.";
            this._fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (this._fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            this.TxtbxOutputdirectory.Text = this._fbd.SelectedPath;
            Settings.SetDirectory(this._fbd.SelectedPath);
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            var chbxAutostartIsChecked = this.ChbxAutostart.IsChecked;
            MainWindow.RegisterInStartup(chbxAutostartIsChecked != null && (bool)chbxAutostartIsChecked);
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    mW = window;
                }
            }

            foreach (var s in _colors)
            {
                ComboBoxColor.Items.Add(s);
            }

            foreach (string s in ComboBoxColor.Items)
            {
                if (s != Settings.GetColor()) continue;
                ComboBoxColor.SelectedItem = s;
                Settings.SetColor(s);
            }

            ThemeToggleSwitch.IsChecked = Settings.GetTheme() == "BaseDark";
            TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly().Location;
            if (!string.IsNullOrEmpty(Settings.GetDirectory()))
                TxtbxOutputdirectory.Text = Settings.GetDirectory();

            ChbxAutostart.IsChecked = Settings.GetAutostart();
            ChbxMinimizeSystray.IsChecked = Settings.GetSystray();
            ChbxCustomPause.IsChecked = Settings.GetCustomPauseTextEnabled();
            TxtbxCustompausetext.Text = Settings.GetCustomPauseText();
            TxtbxOutputformat.Text = Settings.GetOutputString();
        }

        private void TxtbxOutputformat_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SetOutputString(TxtbxOutputformat.Text);
        }

        private void MenuBtnArtist_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{artist}");
        }

        private void MenuBtnTitle_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{title}");

        }

        private void MenuBtnExtra_Click(object sender, RoutedEventArgs e)
        {
            AppendText(TxtbxOutputformat, "{extra}");
        }

        private void AppendText(System.Windows.Controls.TextBox tb, string text)
        {
            tb.AppendText(text);
            tb.Select(TxtbxOutputformat.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }
    }
}