using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
        private string currentsong;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void themeToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if (themeToggleSwitch.IsChecked == true)
            {
                Settings.SetTheme("BaseDark");
            }
            else
            {
                Settings.SetTheme("BaseLight");
            }
            ThemeHandler.ApplyTheme();
        }

        private void ComboBox_Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(ComboBox_Color.SelectedValue.ToString());
            ThemeHandler.ApplyTheme();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in colors)
            {
                ComboBox_Color.Items.Add(s);
            }

            foreach (string s in ComboBox_Color.Items)
            {
                if (s == Settings.GetColor())
                {
                    ComboBox_Color.SelectedItem = s;
                    Settings.SetColor(s);
                }
            }
            if (Settings.GetTheme() == "BaseDark") { themeToggleSwitch.IsChecked = true; } else { themeToggleSwitch.IsChecked = false; }
            ThemeHandler.ApplyTheme();
            Txtbx_outputdirectory.Text = Settings.GetDirectory();
            startTimer(1000);
        }

        private void startTimer(int ms)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            timer.Interval = ms;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            getCurrentSong();
        }

        private void getCurrentSong()
        {
            var processes = Process.GetProcessesByName("Spotify");

            foreach (var process in processes)
            {
                if (process.ProcessName == "Spotify" && !String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    var id = process.Id;
                    var wintitle = process.MainWindowTitle;
                    if (wintitle != "Spotify")
                    {
                        if (currentsong != wintitle)
                        {
                            currentsong = wintitle;
                            Console.WriteLine(wintitle);
                            if (String.IsNullOrEmpty(Settings.GetDirectory()))
                            {
                                File.WriteAllText(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt", currentsong + "               ");
                            }
                            else
                            {
                                File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", currentsong + "               ");
                            }
                            txtblock_liveoutput.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { txtblock_liveoutput.Text = currentsong; }));
                        }
                    }
                }
            }
        }

        private void Btn_Outputdirectory_Click(object sender, RoutedEventArgs e)
        {
            fbd.Description = "Path where the text file will be located.";
            fbd.SelectedPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            Txtbx_outputdirectory.Text = fbd.SelectedPath;
            Settings.SetDirectory(fbd.SelectedPath);
        }

        private void chbx_autostart_Checked(object sender, RoutedEventArgs e)
        {
            RegisterInStartup((bool)chbx_autostart.IsChecked);
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("Songify", Assembly.GetEntryAssembly().Location);
            }
            else
            {
                registryKey.DeleteValue("Songify");
            }
        }
    }
}