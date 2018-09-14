using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Songify_Slim
{
    public partial class MainWindow : MetroWindow
    {
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        public NotifyIcon notifyIcon = new NotifyIcon();
        private System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
        System.Windows.Forms.MenuItem menuItem1 = new System.Windows.Forms.MenuItem();
        System.Windows.Forms.MenuItem menuItem2 = new System.Windows.Forms.MenuItem();
        private string currentsong;
        public static string version;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ThemeToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
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
            menuItem1.Text = "Exit";
            menuItem1.Click += new EventHandler(MenuItem1_Click);

            menuItem2.Text = "Show";
            menuItem2.Click += new EventHandler(MenuItem2_Click);

            contextMenu.MenuItems.AddRange(
        new System.Windows.Forms.MenuItem[] { menuItem2, menuItem1 });

            notifyIcon.Icon = Properties.Resources.songify;
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += new EventHandler(MenuItem2_Click);
            notifyIcon.Text = "Songify";




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
            Txtbx_outputdirectory.Text = Assembly.GetEntryAssembly().Location;
            if (!String.IsNullOrEmpty(Settings.GetDirectory()))
                Txtbx_outputdirectory.Text = Settings.GetDirectory();

            chbx_autostart.IsChecked = (bool)Settings.GetAutostart();
            chbx_minimizeSystray.IsChecked = (bool)Settings.GetSystray();

            if (WindowState == WindowState.Minimized)
                MinimizeToSysTray();

            CheckForUpdates();


            StartTimer(1000);
        }

        private void CheckForUpdates()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            try
            {
                Updater.checkForUpdates(new Version(version));
            }
            catch
            {
                lbl_status.Content = "Unable to check for newer version.";
            }
        }

        private void StartTimer(int ms)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Interval = ms;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            GetCurrentSong();
        }

        private void GetCurrentSong()
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
                                File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt", currentsong + "               ");
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
            fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            Txtbx_outputdirectory.Text = fbd.SelectedPath;
            Settings.SetDirectory(fbd.SelectedPath);
        }

        private void Chbx_autostart_Checked(object sender, RoutedEventArgs e)
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

            Settings.SetAutostart(isChecked);
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    break;
                case WindowState.Minimized:
                    MinimizeToSysTray();
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    break;
            }
        }

        private void MinimizeToSysTray()
        {
            if (Settings.GetSystray())
            {
                Hide();
            }
        }

        private void MenuItem2_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        private void MenuItem1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Chbx_minimizeSystray_Checked(object sender, RoutedEventArgs e)
        {
            Settings.SetSystray((bool)chbx_minimizeSystray.IsChecked);
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();

        }

        private void Btn_updates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();

        }

        private void Btn_Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/inzaniity");

        }

        private void Btn_Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discordapp.com/invite/H8nd4T4");

        }

        private void Btn_GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Inzaniity/Songify");

        }

        private void Btn_About_Click(object sender, RoutedEventArgs e)
        {
            flyout_About.IsOpen = (flyout_About.IsOpen) ? !true : !false;
        }
    }
}