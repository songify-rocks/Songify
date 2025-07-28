using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Mutex _mutex;
        public static bool IsBeta = false;
        private const string PipeName = "SongifyPipe";
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogExc(e.Exception);
        }

        private App()
        {
            ConfigHandler.ReadConfig();
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                Logger.LogStr("SYSTEM: Couldn't set language, reverting to english");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            }

            if (string.IsNullOrEmpty(Settings.Uuid))
            {
                Settings.Uuid = Guid.NewGuid().ToString();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "Songify";

            // Check if restart argument exists
            bool isRestart = e.Args.Contains("--restart");

            // Mutex logic: bypass if it's a restart
            if (!isRestart)
            {
                _mutex = new Mutex(true, appName, out bool createdNew);
                if (!createdNew)
                {
                    // Mutex exists: app is already running
                    _mutex = Mutex.OpenExisting(appName);
                    if (_mutex != null)
                    {
                        SingleInstanceHelper.NotifyFirstInstance();
                        Environment.Exit(0);
                        //Window mainWindow = Current.MainWindow;
                        //if (mainWindow != null)
                        //{
                        //    mainWindow.Show();
                        //    mainWindow.Activate();
                        //}
                    }
                    Current.Shutdown();
                    return;
                }
            }

            // Register global unhandled exception handler
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            base.OnStartup(e);

            string exePath = Assembly.GetEntryAssembly()?.Location;

            //AddFirewallException(appName, exePath);

            // Override the Markdig CodeStyleKey at runtime
            if (Current.Resources.Contains(Markdig.Wpf.Styles.CodeStyleKey))
            {
                Style newStyle = new Style(typeof(Run));

                newStyle.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Colors.White)));
                newStyle.Setters.Add(new Setter(TextElement.BackgroundProperty, new SolidColorBrush(Colors.Black)));
                newStyle.Setters.Add(new Setter(TextElement.FontFamilyProperty, new FontFamily("Consolas")));
                newStyle.Setters.Add(new Setter(TextElement.FontSizeProperty, 14.0));

                // Override the existing Markdig Code Style
                Current.Resources[Markdig.Wpf.Styles.CodeStyleKey] = newStyle;
            }

            // Determine the default culture. You can use CultureInfo.CurrentUICulture or a fixed one like "en".
            CultureInfo defaultCulture = CultureInfo.CurrentUICulture;
            // Or for a fixed default, for example:
            // CultureInfo defaultCulture = new CultureInfo("en");

            // Create a localization dictionary from your RESX file.
            ResourceDictionary defaultLocalizationDict = ResxToDictionaryHelper.CreateResourceDictionary(defaultCulture);

            // Add it to the merged dictionaries so that your UI has access to the keys from the start.
            Current.Resources.MergedDictionaries.Add(defaultLocalizationDict);

            StartPipeServer();
        }


        public static class ResxToDictionaryHelper
        {
            public static ResourceDictionary CreateResourceDictionary(CultureInfo culture)
            {
                ResourceDictionary dict = new ResourceDictionary();
                ResourceManager rm = Songify_Slim.Properties.Resources.ResourceManager;
                // Retrieve the resource set for the specified culture.
                ResourceSet resourceSet = rm.GetResourceSet(culture, true, true);
                foreach (DictionaryEntry entry in resourceSet)
                {
                    // Add each key/value pair to the dictionary.
                    dict.Add(entry.Key, entry.Value);
                }
                return dict;
            }
        }


        private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.LogStr("##### Unhandled Exception #####");
            Logger.LogStr("MyHandler caught : " + e.Message);
            Logger.LogStr("Stack Trace: " + e.StackTrace);

            if (e.InnerException != null)
            {
                Logger.LogStr("Inner Exception: " + e.InnerException.Message);
                Logger.LogStr("Inner Exception Stack Trace: " + e.InnerException.StackTrace);
            }

            Logger.LogStr("Runtime terminating: " + args.IsTerminating);
            Logger.LogStr("###############################");
            Logger.LogExc(e);

            if (!args.IsTerminating) return;
            if (MessageBox.Show("Would you like to open the log file directory?\n\nFeel free to submit the log file in our Discord.", "Songify just crashed :(",
                    MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                Process.Start(Logger.LogDirectoryPath);
            }

            if (MessageBox.Show("Restart Songify?", "Songify", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
                MessageBoxResult.Yes) return;
            // Pass an argument to indicate this is a restart
            ProcessStartInfo startInfo = new()
            {
                FileName = Assembly.GetExecutingAssembly().Location,
                Arguments = "--restart", // Custom argument
                UseShellExecute = false
            };

            // Start the new process
            Process.Start(startInfo);

            // Shutdown the current instance
            Current.Shutdown();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check for the --restart flag
            bool isRestart = e.Args.Contains("--restart");

            // Optionally log or handle restart-specific behavior
            if (isRestart)
            {
                // Perform any specific actions for restarted instance, if needed
                Console.WriteLine("Restarting Songify...");
            }

            // Initialize and show the main window
            MainWindow main = new()
            {
                Icon = IsBeta
                    ? new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"))
                    : new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"))
            };

            main.Show();
        }

        private void StartPipeServer()
        {
            Thread pipeThread = new(() =>
            {
                while (true)
                {
                    try
                    {
                        using NamedPipeServerStream server = new(PipeName, PipeDirection.In);
                        // Wait for a connection (blocking)
                        server.WaitForConnection();

                        using StreamReader reader = new(server);
                        string message = reader.ReadLine();
                        if (message == "SHOW")
                        {
                            // Use the dispatcher to interact with UI elements.
                            Current.Dispatcher.Invoke(RestoreWindow);
                        }
                    }
                    catch
                    {
                        // Handle exceptions if needed (for example, log them)
                    }
                }
            })
            {
                IsBackground = true
            };

            pipeThread.Start();
        }

        public static void AddFirewallException(string appName, string exePath)
        {
            string args =
                $"advfirewall firewall add rule name=\"{appName}\" dir=in action=allow program=\"{exePath}\" enable=yes";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                Verb = "runas", // <--- This prompts for admin
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex); // or show MessageBox
            }
        }

        private static void RestoreWindow()
        {
            // Your logic to restore the window from the tray.
            Window win = Current.MainWindow;

            if (win is MainWindow)
            {
                // For example:
                win.Show();
                win.WindowState = WindowState.Normal;
                Thread.Sleep(1000);
                win.Activate();
            }
        }
    }
}