using Newtonsoft.Json;
using Songify_Slim.Util.General;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Navigation;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Interaktionslogik für AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void LoadThirdPartyLibraries()
        {
            //string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            // Change namespace + path to match your project
            string resourceName = "Songify_Slim.Resources.thirdparty.json";

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return;

            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            string json = reader.ReadToEnd();

            ThirdPartyLibrary[] items = JsonConvert.DeserializeObject<ThirdPartyLibrary[]>(json);
            if (items == null) return;

            ThirdPartyItems.Items.Clear();
            foreach (ThirdPartyLibrary item in items)
                ThirdPartyItems.Items.Add(item);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadThirdPartyLibraries();
        }
    }
}