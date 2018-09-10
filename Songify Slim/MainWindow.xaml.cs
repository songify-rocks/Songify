using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private NotifyIcon notifyIcon = new NotifyIcon();
        private System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
        System.Windows.Forms.MenuItem menuItem1 = new System.Windows.Forms.MenuItem();
        System.Windows.Forms.MenuItem menuItem2 = new System.Windows.Forms.MenuItem();
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
            Txtbx_outputdirectory.Text = Assembly.GetEntryAssembly().Location;
            if (!String.IsNullOrEmpty(Settings.GetDirectory()))
                Txtbx_outputdirectory.Text = Settings.GetDirectory();

            chbx_autostart.IsChecked = (bool)Settings.GetAutostart();
            chbx_minimizeSystray.IsChecked = (bool)Settings.GetSystray();
            

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

            Settings.SetAutostart(isChecked);
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                    break;
                case WindowState.Minimized:
                    if (Settings.GetSystray())
                    {
                        menuItem1.Text = "Exit";
                        menuItem1.Click += new EventHandler(this.MenuItem1_Click);

                        menuItem2.Text = "Show";
                        menuItem2.Click += new EventHandler(this.MenuItem2_Click);

                        this.contextMenu.MenuItems.AddRange(
                    new System.Windows.Forms.MenuItem[] { this.menuItem2, this.menuItem1 });

                        notifyIcon.Icon = Properties.Resources.songify;
                        //notifyIcon.BalloonTipText = "Songify is minimized to the system tray.";
                        //notifyIcon.BalloonTipTitle = "Songify";
                        notifyIcon.ContextMenu = contextMenu;
                        notifyIcon.Visible = true;
                        //notifyIcon.ShowBalloonTip(500);
                        notifyIcon.DoubleClick += new EventHandler(this.MenuItem2_Click);
                        this.Hide();
                    }
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    break;
            }
        }

        private void MenuItem2_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void MenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chbx_minimizeSystray_Checked(object sender, RoutedEventArgs e)
        {
            Settings.SetSystray((bool)chbx_minimizeSystray.IsChecked);
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();

        }
    }
}