using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using Songify_Slim.Models.Responses;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Fetches server PSAs/MOTDs, tracks read state, and raises UI refresh events.
/// Owned by the shell (ShellWindow); independent of legacy MainWindow.
/// </summary>
internal static class PsaManager
{
    private static readonly object Sync = new();
    private static DispatcherTimer _timer;
    private static bool _toastHooked;
    private static bool _refreshing;
    private static List<Psa> _psas = [];

    public static IReadOnlyList<Psa> Current
    {
        get
        {
            lock (Sync)
                return _psas.ToList();
        }
    }

    /// <summary>Raised on the UI thread after the PSA list or read state changes.</summary>
    public static event Action Changed;

    /// <summary>Raised on the UI thread after a successful fetch replaces the PSA list.</summary>
    public static event Action ListUpdated;

    public static void Start()
    {
        EnsureToastHook();

        if (_timer == null)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _timer.Tick += (_, _) => _ = RefreshAsync();
        }

        if (!_timer.IsEnabled)
            _timer.Start();

        _ = RefreshAsync();
    }

    public static void Stop()
    {
        _timer?.Stop();
    }

    public static async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            List<Psa> fetched = await PsaService.GetPsaAsync().ConfigureAwait(true);
            lock (Sync)
                _psas = fetched ?? [];

            MaybeShowHighSeverityToast();
            RaiseListUpdated();
            RaiseChanged();
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Api, "Error refreshing PSAs", ex);
        }
        finally
        {
            _refreshing = false;
        }
    }

    public static int GetUnreadCount()
    {
        List<int> readIds = Settings.ReadNotificationIds ?? [];
        lock (Sync)
            return _psas.Count(p => !readIds.Contains(p.Id));
    }

    public static bool HasAny()
    {
        lock (Sync)
            return _psas.Count > 0;
    }

    public static bool HasUnread() => GetUnreadCount() > 0;

    /// <summary>Badge color from highest severity present (matches legacy MainWindow behavior).</summary>
    public static Brush GetSeverityBadgeBrush()
    {
        lock (Sync)
        {
            if (_psas.Any(p => p.Severity == "High"))
                return new SolidColorBrush(Colors.IndianRed);
            if (_psas.Any(p => p.Severity == "Medium"))
                return new SolidColorBrush(Colors.Orange);
            return new SolidColorBrush(Colors.DarkGray);
        }
    }

    public static void MarkAsRead(int id)
    {
        List<int> readIds = Settings.ReadNotificationIds ?? [];
        if (readIds.Contains(id))
            return;

        readIds.Add(id);
        Settings.ReadNotificationIds = readIds;
        RaiseChanged();
    }

    public static void MarkAllAsRead()
    {
        List<int> readIds = Settings.ReadNotificationIds ?? [];
        lock (Sync)
        {
            foreach (Psa psa in _psas)
            {
                if (!readIds.Contains(psa.Id))
                    readIds.Add(psa.Id);
            }
        }

        Settings.ReadNotificationIds = readIds;
        RaiseChanged();
    }

    public static void ShowPsaDialog(Psa psa)
    {
        if (psa == null)
            return;

        void Show()
        {
            WindowUniversalDialog dialog = new(psa, "Notification")
            {
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontSize = 14
            };
            dialog.Show();
        }

        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        if (dispatcher.CheckAccess())
            Show();
        else
            dispatcher.Invoke(Show);
    }

    private static void MaybeShowHighSeverityToast()
    {
        Psa high;
        lock (Sync)
            high = _psas.FirstOrDefault(p => p.Severity == "High");

        if (high == null || Settings.LastShownMotdId == high.Id)
            return;

        string msg = high.MessageText ?? string.Empty;
        if (msg.Length > 190)
            msg = msg[..190] + "...";

        try
        {
            new ToastContentBuilder()
                .AddArgument("msgId", high.Id)
                .AddText($"{high.Author} from Songify")
                .AddText(msg)
                .AddAttributionText(high.CreatedAtDateTime.ToString())
                .Show();
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }
        finally
        {
            Settings.LastShownMotdId = high.Id;
        }
    }

    private static void EnsureToastHook()
    {
        if (_toastHooked)
            return;

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
        _toastHooked = true;
    }

    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        Dictionary<string, string> args = ParseArguments(e.Argument);
        if (!args.TryGetValue("msgId", out string value) || !int.TryParse(value, out int id))
            return;

        Psa psa;
        lock (Sync)
            psa = _psas.FirstOrDefault(p => p.Id == id);

        if (psa == null)
            return;

        ShowPsaDialog(psa);
    }

    private static Dictionary<string, string> ParseArguments(string arguments)
    {
        Dictionary<string, string> result = new();
        if (string.IsNullOrEmpty(arguments))
            return result;

        foreach (string pair in arguments.Split('&'))
        {
            string[] keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
                result[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
        }

        return result;
    }

    private static void RaiseChanged()
    {
        InvokeOnUi(() => Changed?.Invoke());
    }

    private static void RaiseListUpdated()
    {
        InvokeOnUi(() => ListUpdated?.Invoke());
    }

    private static void InvokeOnUi(Action action)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            action();
            return;
        }

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}
