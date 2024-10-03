using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
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
            _mutex = new Mutex(true, appName, out bool createdNew);
            if (!createdNew)
            {
                _mutex = Mutex.OpenExisting(appName);
                if (_mutex != null)
                {
                    Window mainWindow = Current.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                }
                //app is already running! Exiting the application
                Current.Shutdown();
            }

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            base.OnStartup(e);
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
            Process.Start(ResourceAssembly.Location);
            Current.Shutdown();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow main = new()
            {
                Icon = IsBeta
                    ? new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"))
                    : new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"))
            };
            main.Show();
        }
    }
}