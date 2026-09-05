using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Songify_Slim.Models.BotResponses;
using Songify_Slim.Util.General;

namespace Songify_Slim.UserControls;

/// <summary>
/// Catalog-driven editor for song-request bot responses.
/// </summary>
public partial class UcBotResponses
{
    public ObservableCollection<BotResponseItem> Responses { get; } = [];

    public UcBotResponses()
    {
        InitializeComponent();
        foreach (BotResponseItem item in BotResponseCatalog.All)
            Responses.Add(item);
    }

    private static void SetPreview(TextBox tb)
    {
        string response;

        if (tb != null)
        {
            response = tb.Text;
            Dictionary<string, string> replacements = new()
            {
                { "{user}", "SomeUser" },
                { "{artist}", "Rick Astley" },
                { "{single_artist}", "Rick Astley" },
                { "{title}", "Never Gonna Give You Up" },
                { "{userreq}", "1" },
                { "{maxreq}", "5" },
                { "{errormsg}", "Couldn't find a song matching your request." },
                { "{maxlength}", "300" },
                { "{votes}", "3/5" },
                { "{song}", "Rick Astley - Never Gonna Give You Up" },
                { "{req}", "John Doe" },
                { "{{", "" },
                { "}}", "" },
                { "{url}", "https://open.spotify.com/track/4cOdK2wGLETKBW3PvgPWqT?si=0633b850641d4bce" },
                { "{playlist_name}", "My Super Cool Playlist" },
                { "{playlist_url}", "https://open.spotify.com/playlist/2wKHJy4vO0pA1gXfACW8Qh?si=30184b3f0854459c" },
                { "{cd}", "5" },
                { "{userlevel}", "subscribers" },
                { "{ttp}", "1m 37s" },
                { "{cmd}", "!ssr" },
            };
            response = replacements.Aggregate(response, (current, pair) => current.Replace(pair.Key, pair.Value));
        }
        else
            response = "";

        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            SettingsUi.SetBotResponsePreview(response);
        }));
    }

    private void ResponseTitle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock title || title.DataContext is not BotResponseItem item)
            return;

        title.SetResourceReference(TextBlock.TextProperty, item.TitleResourceKey);
    }

    private void ResponseTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not BotResponseItem item)
            return;

        textBox.Text = item.Get() ?? string.Empty;
        AttachContextMenu(textBox, item);
    }

    private static void AttachContextMenu(TextBox textBox, BotResponseItem item)
    {
        ContextMenu contextMenu = new();

        MenuItem cutItem = new() { Header = "Cut", Command = ApplicationCommands.Cut, CommandTarget = textBox };
        MenuItem copyItem = new() { Header = "Copy", Command = ApplicationCommands.Copy, CommandTarget = textBox };
        MenuItem pasteItem = new() { Header = "Paste", Command = ApplicationCommands.Paste, CommandTarget = textBox };

        MenuItem resetItem = new() { Header = "Reset to default" };
        resetItem.Click += (_, _) => { textBox.Text = item.DefaultText; };

        contextMenu.Items.Add(cutItem);
        contextMenu.Items.Add(copyItem);
        contextMenu.Items.Add(pasteItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(resetItem);

        contextMenu.Opened += (_, _) =>
        {
            bool hasSelection = !string.IsNullOrEmpty(textBox.SelectedText);
            cutItem.IsEnabled = hasSelection;
            copyItem.IsEnabled = hasSelection;
            pasteItem.IsEnabled = Clipboard.ContainsText();
        };

        textBox.ContextMenu = contextMenu;
    }

    private void Tb_GotFocus(object sender, RoutedEventArgs e)
    {
        SetPreview(sender as TextBox);
    }

    private void Tb_LostFocus(object sender, RoutedEventArgs e)
    {
        SetPreview(null);
    }

    private void Tb_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not BotResponseItem item)
            return;

        item.Set(textBox.Text);
        SetPreview(textBox);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Values are applied per TextBox in ResponseTextBox_Loaded.
        // Reload from Settings in case the control is re-hosted after a settings refresh.
        foreach (TextBox textBox in GlobalObjects.FindVisualChildren<TextBox>(this))
        {
            if (textBox.DataContext is BotResponseItem item)
                textBox.Text = item.Get() ?? string.Empty;
        }
    }
}
