using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Views.WPFUI.ViewModels;

namespace Songify_Slim.Util.General;

/// <summary>
/// Reloads open blocklist UI after CSV sync / cloud restore.
/// </summary>
internal static class BlocklistUi
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<BlocklistViewModel>> ViewModels = [];

    public static void Register(BlocklistViewModel viewModel)
    {
        if (viewModel == null) return;
        lock (Gate)
        {
            Prune_NoLock();
            foreach (WeakReference<BlocklistViewModel> wr in ViewModels)
            {
                if (wr.TryGetTarget(out BlocklistViewModel existing) && ReferenceEquals(existing, viewModel))
                    return;
            }

            ViewModels.Add(new WeakReference<BlocklistViewModel>(viewModel));
        }
    }

    public static void Unregister(BlocklistViewModel viewModel)
    {
        if (viewModel == null) return;
        lock (Gate)
        {
            ViewModels.RemoveAll(wr =>
                !wr.TryGetTarget(out BlocklistViewModel existing) || ReferenceEquals(existing, viewModel));
        }
    }

    public static async Task RefreshArtistsAsync()
    {
        Application app = Application.Current;
        if (app?.Dispatcher == null)
            return;

        if (!app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(RefreshArtistsCoreAsync).Task.Unwrap();
            return;
        }

        await RefreshArtistsCoreAsync();
    }

    private static async Task RefreshArtistsCoreAsync()
    {
        foreach (BlocklistViewModel vm in GetLive())
        {
            try
            {
                await vm.ReloadAsync();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }
    }

    private static List<BlocklistViewModel> GetLive()
    {
        lock (Gate)
        {
            Prune_NoLock();
            List<BlocklistViewModel> result = [];
            foreach (WeakReference<BlocklistViewModel> wr in ViewModels)
            {
                if (wr.TryGetTarget(out BlocklistViewModel vm))
                    result.Add(vm);
            }

            return result;
        }
    }

    private static void Prune_NoLock()
    {
        ViewModels.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
}
