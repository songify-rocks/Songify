using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Songify_Slim.Util.General;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.UI;

internal sealed class PlayerOption
{
    public PlayerType Value { get; init; }
    public string Name { get; init; } = "";
    public string Group { get; init; } = "";
}

/// <summary>Shared player picker: grouped by song-request support, Browser Companion hidden unless already selected.</summary>
internal static class PlayerComboBinder
{
    public static bool SupportsSongRequests(PlayerType player)
        => player is PlayerType.Spotify or PlayerType.Pear;

    public static void Bind(System.Windows.Controls.ComboBox combo, PlayerType selected)
    {
        if (combo == null)
            return;

        combo.DisplayMemberPath = nameof(PlayerOption.Name);
        combo.SelectedValuePath = nameof(PlayerOption.Value);
        combo.ItemsSource = CreateItems(selected);
        EnsureGroupStyle(combo);
        VirtualizingPanel.SetIsVirtualizing(combo, false);
        combo.SelectedValue = selected;
    }

    private static ICollectionView CreateItems(PlayerType selected)
    {
        string songRequests = Loc("player_group_song_requests", "Song requests");
        string nowPlaying = Loc("player_group_now_playing", "Now playing only");

        List<PlayerOption> items = Enum.GetValues<PlayerType>()
            .Where(p => p != PlayerType.BrowserCompanion || p == selected)
            .Select(p =>
            {
                bool supports = SupportsSongRequests(p);
                return new PlayerOption
                {
                    Value = p,
                    Name = EnumHelper.GetDescription(p),
                    Group = supports ? songRequests : nowPlaying
                };
            })
            .OrderByDescending(p => SupportsSongRequests(p.Value))
            .ThenBy(p => (int)p.Value)
            .ToList();

        ICollectionView view = CollectionViewSource.GetDefaultView(items);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PlayerOption.Group)));
        return view;
    }

    private static void EnsureGroupStyle(System.Windows.Controls.ComboBox combo)
    {
        if (combo.GroupStyle.Count > 0)
            return;

        DataTemplate template = combo.TryFindResource("PlayerComboGroupHeader") as DataTemplate
                                ?? Application.Current?.TryFindResource("PlayerComboGroupHeader") as DataTemplate;
        if (template == null)
            return;

        combo.GroupStyle.Add(new GroupStyle { HeaderTemplate = template });
    }

    private static string Loc(string key, string fallback)
        => Application.Current?.TryFindResource(key) as string ?? fallback;
}
