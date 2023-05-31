using Microsoft.Win32;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Mutex _mutex;

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogExc(e.Exception);
        }

        private App()
        {
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml"))
                ConfigHandler.LoadConfig(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            else
                ConfigHandler.ReadConfig();
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            }
            catch (Exception e)
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            }
            //  Adding the RegKey for Songify in startup(autostart with windows)

            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Songify");
            if (registryKey != null && registryKey.GetValue("UUID") == null)
            {
                registryKey.SetValue("UUID", Settings.Uuid);
            }
            else
            {
                Settings.Uuid = registryKey?.GetValue("UUID").ToString();
            }
            if (registryKey?.GetValue("AccessKey") == null)
            {
                registryKey?.SetValue("AccessKey", Settings.AccessKey);
            }
            else
            {
                Settings.AccessKey = registryKey.GetValue("AccessKey").ToString();
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
            Logger.LogStr("Runtime terminating: {0}" + args.IsTerminating);
            Logger.LogStr("###############################");
            Logger.LogExc(e);

            if (!args.IsTerminating) return;
            if (MessageBox.Show("Would you like to open the log file directory?\n\nFeel free to submit the log file in our Discord.", "Songify just crashed :(",
                    MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                Process.Start(Logger.LogDirectoryPath);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
        }


    }
}