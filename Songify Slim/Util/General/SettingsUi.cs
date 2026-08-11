using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Views.WPFUI.Controls;

namespace Songify_Slim.Util.General;

/// <summary>
/// Reloads open settings UI after account login, imports, reward changes, etc.
/// Covers both the legacy <c>Window_Settings</c> host and the in-shell <see cref="SettingsPanel"/>.
/// </summary>
internal static class SettingsUi
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<SettingsPanel>> Panels = [];

    public static void Register(SettingsPanel panel)
    {
        if (panel == null) return;
        lock (Gate)
        {
            Prune_NoLock();
            foreach (WeakReference<SettingsPanel> wr in Panels)
            {
                if (wr.TryGetTarget(out SettingsPanel existing) && ReferenceEquals(existing, panel))
                    return;
            }

            Panels.Add(new WeakReference<SettingsPanel>(panel));
        }
    }

    public static void Unregister(SettingsPanel panel)
    {
        if (panel == null) return;
        lock (Gate)
        {
            Panels.RemoveAll(wr =>
                !wr.TryGetTarget(out SettingsPanel existing) || ReferenceEquals(existing, panel));
        }
    }

    /// <param name="resetTwitch">Also re-run Twitch connection UI (manual login / OAuth).</param>
    /// <param name="loadRewards">Refresh channel rewards list only (skips full SetControls when true alone).</param>
    /// <param name="loadCommands">Refresh Twitch commands UI only.</param>
    /// <param name="fullReload">Call SetControls (default). Set false when only loadRewards/loadCommands is needed.</param>
    public static async Task RefreshAsync(
        bool resetTwitch = false,
        bool loadRewards = false,
        bool loadCommands = false,
        bool fullReload = true)
    {
        Application app = Application.Current;
        if (app?.Dispatcher == null)
            return;

        if (!app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() =>
                RefreshCoreAsync(resetTwitch, loadRewards, loadCommands, fullReload)).Task.Unwrap();
            return;
        }

        await RefreshCoreAsync(resetTwitch, loadRewards, loadCommands, fullReload);
    }

    private static async Task RefreshCoreAsync(
        bool resetTwitch,
        bool loadRewards,
        bool loadCommands,
        bool fullReload)
    {
        List<SettingsPanel> live = GetLivePanels();
        foreach (SettingsPanel panel in live)
        {
            try
            {
                if (fullReload)
                    await panel.SetControls();
                if (resetTwitch)
                    await panel.ResetTwitchConnection();
                if (loadRewards)
                    await panel.LoadRewards();
                if (loadCommands)
                    await panel.LoadCommands();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }
    }

    private static List<SettingsPanel> GetLivePanels()
    {
        lock (Gate)
        {
            Prune_NoLock();
            List<SettingsPanel> result = [];
            foreach (WeakReference<SettingsPanel> wr in Panels)
            {
                if (wr.TryGetTarget(out SettingsPanel panel))
                    result.Add(panel);
            }

            return result;
        }
    }

    private static void Prune_NoLock()
    {
        Panels.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
}
