using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage() => InitializeComponent();

    private void SettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Panel?.TabCtrl == null || NavList.Items.Count > 0)
            return;

        // Content-only host: left nav drives selection; hide the built-in tab strip.
        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetBinding(
            ContentPresenter.ContentProperty,
            new Binding(nameof(TabControl.SelectedContent))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
        Panel.TabCtrl.Template = new ControlTemplate(typeof(TabControl)) { VisualTree = contentFactory };

        foreach (TabItem tab in Panel.TabCtrl.Items.OfType<TabItem>())
        {
            NavList.Items.Add(new ListBoxItem
            {
                Content = HumanizeTag(tab.Tag?.ToString() ?? "Tab"),
                Tag = tab
            });
        }

        if (NavList.Items.Count > 0)
            NavList.SelectedIndex = 0;
    }

    private static string HumanizeTag(string tag) => tag switch
    {
        "System" => "System",
        "Output" => "Output",
        "Twitch" => "Twitch",
        "TwitchRewards" => "Rewards",
        "TwitchPolls" => "Polls",
        "TwitchSongRequest" => "Song request",
        "TwitchCommands" => "Commands",
        "TwitchResponses" => "Responses",
        "Spotify" => "Spotify",
        "Youtube" => "YouTube",
        "WebServer" => "Web server",
        "Config" => "Config",
        _ => tag
    };

    private void NavList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem { Tag: TabItem tab }) return;
        Panel.TabCtrl.SelectedItem = tab;
    }

    private async void SettingsPage_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible && Panel != null)
            await Panel.ConfirmCloseAsync();
    }

    private void BtnResponseParams_OnClick(object sender, RoutedEventArgs e)
        => Panel.OpenResponseParams();
}
