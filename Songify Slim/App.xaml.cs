using System;
using System.Collections.Generic;
using System.Diagnostics;
using Songify_Slim.Util.Settings;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Songify_Slim.Views;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
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
            //ConfigHandler.ConvertConfig(new Config()
            //    {
            //        AccessToken = "",
            //        AnnounceInChat = false,
            //        AppendSpaces = false,
            //        ArtistBlacklist = new List<string>(),
            //        AutoClearQueue = false,
            //        Autostart = false,
            //        BotCmdNext = false,
            //        BotCmdPos = false,
            //        BotCmdSkip = false,
            //        BotCmdSkipVote = false,
            //        BotCmdSkipVoteCount = 5,
            //        BotCmdSong = false,
            //        BotRespBlacklist = "@{artist} has been blacklisted by the broadcaster.",
            //        BotRespError = "@{user} there was an error adding your Song to the queue. Error message: {errormsg}",
            //        BotRespIsInQueue = "@{user} this song is already in the queue.",
            //        BotRespLength = "@{user} the song you requested exceeded the maximum song length ({maxlength}).",
            //        BotRespMaxReq = "@{user} maximum number of songs in queue reached ({maxreq}).",
            //        BotRespModSkip = "@{user} skipped the current song.",
            //        BotRespNoSong = "@{user} please specify a song to add to the queue.",
            //        BotRespSuccess = "{artist} - {title} requested by @{user} has been added to the queue.",
            //        BotRespVoteSkip = "@{user} voted to skip the current song. ({votes})",
            //        ClientId = "",
            //        ClientSecret = "",
            //        Color = "Blue",
            //        CustomPauseText = "",
            //        CustomPauseTextEnabled = false,
            //        Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location),
            //        DownloadCover = false,
            //        Language = "en",
            //        MaxSongLength = 10,
            //        MsgLoggingEnabled = false,
            //        OpenQueueOnStartup = false,
            //        OutputString = "{artist} - {title} {extra}",
            //        OutputString2 = "{artist} - {title} {extra}",
            //        PosX = 100,
            //        PosY = 100,
            //        RefreshToken = "",
            //        SaveHistory = false,
            //        SpaceCount = 10,
            //        SplitOutput = false,
            //        SpotifyDeviceId = "",
            //        Systray = false,
            //        Telemetry = false,
            //        Theme = "Dark",
            //        TwAcc = "",
            //        TwAutoConnect = false,
            //        TwChannel = "",
            //        TwOAuth = "",
            //        TwRewardId = "",
            //        TwSrCommand = false,
            //        TwSrCooldown = 5,
            //        TwSrMaxReq = 1,
            //        TwSrMaxReqBroadcaster = 1,
            //        TwSrMaxReqEveryone = 1,
            //        TwSrMaxReqModerator = 1,
            //        TwSrMaxReqSubscriber = 1,
            //        TwSrMaxReqVip = 1,
            //        TwSrReward = false,
            //        TwSrUserLevel = 1,
            //        Upload = false,
            //        UploadHistory = false,
            //        UseOwnApp = false,
            //        UserBlacklist = new List<string>(),
            //        Uuid = ""
            //    });
            

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en"); 
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "Songify";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
                //app is already running! Exiting the application
                Current.Shutdown();

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