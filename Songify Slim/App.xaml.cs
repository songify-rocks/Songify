﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Songify_Slim.Util.Settings;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ControlzEx.Theming;
using Songify_Slim.Views;
using System.Runtime.InteropServices;
using Common.Logging.Configuration;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex;
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en"); 
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
                    var mainWindow = Application.Current.MainWindow;
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