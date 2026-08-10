using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Newtonsoft.Json;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class AboutPage : Page
{
    public AboutPage() => InitializeComponent();

    private void AboutPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadBrand();
        LoadThirdPartyLibraries();
        PlayIntro();
    }

    private void LoadBrand()
    {
        string version = string.IsNullOrWhiteSpace(GlobalObjects.AppVersion)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?"
            : GlobalObjects.AppVersion;
        TxtVersion.Text = App.IsBeta ? $"v{version}  ·  BETA" : $"v{version}  ·  songify.rocks";

        try
        {
            string iconPath = App.IsBeta
                ? "pack://application:,,,/Resources/songifyBeta.ico"
                : "pack://application:,,,/Resources/songify.ico";
            ImgLogo.Source = new BitmapImage(new Uri(iconPath));
        }
        catch
        {
            // logo is decorative
        }
    }

    private void PlayIntro()
    {
        if (Resources["IntroStoryboard"] is Storyboard sb)
            sb.Begin(this);
    }

    private void LoadThirdPartyLibraries()
    {
        const string resourceName = "Songify_Slim.Resources.thirdparty.json";
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null) return;

        using StreamReader reader = new(stream, Encoding.UTF8);
        string json = reader.ReadToEnd();

        ThirdPartyLibrary[] items = JsonConvert.DeserializeObject<ThirdPartyLibrary[]>(json);
        if (items == null) return;

        ThirdPartyItems.Items.Clear();
        foreach (ThirdPartyLibrary item in items)
            ThirdPartyItems.Items.Add(item);
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    private void BtnDiscord_Click(object sender, RoutedEventArgs e)
        => OpenUrl("https://songify.rocks/discord");

    private void BtnWebsite_Click(object sender, RoutedEventArgs e)
        => OpenUrl("https://songify.rocks");

    private void BtnSupport_Click(object sender, RoutedEventArgs e)
        => OpenUrl("https://ko-fi.com/overcodetv");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }
}
