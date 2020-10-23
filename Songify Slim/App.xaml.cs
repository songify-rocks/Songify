using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using Songify_Slim.Util.Settings;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        App()
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Settings.Language);
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en"); 
        }

        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "Songify";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                //app is already running! Exiting the application
                Application.Current.Shutdown();
            }

            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml"))
            {
                ConfigHandler.LoadConfig(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml");
            }

            MainWindow main = new MainWindow();
            main.Show();

            //if (Settings.Uuid != "")
            //{
            //    MainWindow main = new MainWindow();
            //    main.Show();
            //}
            //else
            //{
            //    Window_GuidedSetup guidedSetup = new Window_GuidedSetup();
            //    guidedSetup.Show();
            //}
        }
    }
}
