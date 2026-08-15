using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Songify_Slim.Util.UI;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class SettingsPage : Page
{
    private sealed record NavGroup(string SectionId, string HeaderResourceKey, SymbolRegular? FluentSymbol, string? BrandGeometryKey, string[] Tags);

    /// <summary>Modern IA: General → Music → Twitch → Network.</summary>
    private static readonly NavGroup[] NavGroups =
    [
        new("General", "window_settings_nav_general", SymbolRegular.Settings24, null, ["System", "Output", "Config"]),
        new("Music", "window_settings_nav_music", SymbolRegular.MusicNote224, null, ["Spotify", "Youtube"]),
        new("Twitch", "menu_twitch", null, AppIcons.Twitch,
        [
            "Twitch", "TwitchSongRequest", "TwitchRewards", "TwitchPolls", "TwitchCommands",
            "TwitchResponses"
        ]),
        new("Network", "window_settings_nav_network", SymbolRegular.Server24, null, ["WebServer"])
    ];

    private bool _navBuilt;

    public SettingsPage() => InitializeComponent();

    private void SettingsPage_OnLoaded(object sender, RoutedEventArgs e) => EnsureNavBuilt();

    /// <summary>
    /// Selects the first tab in the named top-level section (General / Music / Twitch / Network).
    /// </summary>
    public void SelectSection(string section)
    {
        EnsureNavBuilt();
        if (string.IsNullOrWhiteSpace(section) || NavList == null)
            return;

        NavGroup group = NavGroups.FirstOrDefault(g =>
            string.Equals(g.SectionId, section, StringComparison.OrdinalIgnoreCase));
        if (group == null)
            return;

        // Find the group header, then select the first real tab item after it.
        for (int i = 0; i < NavList.Items.Count; i++)
        {
            if (NavList.Items[i] is not ListBoxItem header)
                continue;
            if (header.Tag is not string headerSection ||
                !string.Equals(headerSection, group.SectionId, StringComparison.OrdinalIgnoreCase))
                continue;

            for (int j = i + 1; j < NavList.Items.Count; j++)
            {
                if (NavList.Items[j] is ListBoxItem { Tag: TabItem } item)
                {
                    NavList.SelectedItem = item;
                    return;
                }

                // Next header — section had no tabs
                if (NavList.Items[j] is ListBoxItem { Tag: string })
                    return;
            }

            return;
        }
    }

    private void EnsureNavBuilt()
    {
        if (_navBuilt || Panel?.TabCtrl == null)
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

        Dictionary<string, TabItem> byTag = Panel.TabCtrl.Items.OfType<TabItem>()
            .Where(t => t.Tag != null)
            .ToDictionary(t => t.Tag.ToString()!, t => t);

        HashSet<string> placed = [];
        foreach (NavGroup group in NavGroups)
        {
            NavList.Items.Add(CreateGroupHeader(group.SectionId, group.HeaderResourceKey, group.FluentSymbol, group.BrandGeometryKey));
            foreach (string tag in group.Tags)
            {
                if (!byTag.TryGetValue(tag, out TabItem tab))
                    continue;
                NavList.Items.Add(CreateNavItem(tab, tag));
                placed.Add(tag);
            }
        }

        // Any unexpected tabs still show (sorted) under "Other"
        List<TabItem> leftovers = byTag
            .Where(kv => !placed.Contains(kv.Key))
            .OrderBy(kv => HumanizeTag(kv.Key))
            .Select(kv => kv.Value)
            .ToList();
        if (leftovers.Count > 0)
        {
            NavList.Items.Add(CreateGroupHeader("Other", "window_settings_nav_other", SymbolRegular.MoreHorizontal24, null));
            foreach (TabItem tab in leftovers)
                NavList.Items.Add(CreateNavItem(tab, tab.Tag?.ToString() ?? "Tab"));
        }

        // Select first real nav item (skip group headers)
        for (int i = 0; i < NavList.Items.Count; i++)
        {
            if (NavList.Items[i] is ListBoxItem { Tag: TabItem })
            {
                NavList.SelectedIndex = i;
                break;
            }
        }

        _navBuilt = true;
    }

    private static ListBoxItem CreateGroupHeader(string sectionId, string headerResourceKey, SymbolRegular? fluent, string? brandKey)
    {
        // Use DynamicResource so headers follow theme switches (light/dark) like normal nav items.
        FrameworkElement? icon = null;
        if (!string.IsNullOrEmpty(brandKey))
            icon = AppIcons.TryBrand(brandKey, 12);
        else if (fluent is { } symbol)
            icon = AppIcons.Fluent(symbol, 12);

        var label = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(icon != null ? 8 : 8, 0, 0, 0)
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        // Bind via converter-less callback: uppercase at assign time from current culture; rebuild nav on language change via Settings refresh.
        string title = Application.Current?.TryFindResource(headerResourceKey) as string ?? headerResourceKey;
        label.Text = title.ToUpperInvariant();

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (icon != null)
            row.Children.Add(icon);
        row.Children.Add(label);

        return new ListBoxItem
        {
            Content = row,
            Tag = sectionId, // section name for SelectSection lookups
            // Keep enabled so DynamicResource foreground isn't coerced to GrayText in light theme.
            IsHitTestVisible = false,
            Focusable = false,
            Padding = new Thickness(4, 14, 4, 2),
            Margin = new Thickness(0, 4, 0, 0),
            Background = Brushes.Transparent
        };
    }

    private static ListBoxItem CreateNavItem(TabItem tab, string tag) => new()
    {
        Content = HumanizeTag(tag),
        Tag = tab
    };

    private static string HumanizeTag(string tag) => tag switch
    {
        "System" => "System",
        "Output" => "Output",
        "Twitch" => "Accounts",
        "TwitchRewards" => "Rewards",
        "TwitchPolls" => "Polls",
        "TwitchSongRequest" => "Song request",
        "TwitchCommands" => "Commands",
        "TwitchResponses" => "Responses",
        "Spotify" => "Spotify",
        "Youtube" => "YouTube",
        "WebServer" => "Web server",
        "Config" => "Backup & advanced",
        _ => tag
    };

    private void NavList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem item)
            return;

        // Group headers are not selectable destinations
        if (item.Tag is not TabItem tab)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ListBoxItem previous)
                NavList.SelectedItem = previous;
            return;
        }

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
