using Songify.Classes;
using System.Windows;
using System.Windows.Threading;

namespace Songify
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        readonly Log logger = new Log();
        public App()
        {
            logger.Start();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            logger.Add(e.Exception.Message, Models.MessageType.Error);
        }
    }
}
