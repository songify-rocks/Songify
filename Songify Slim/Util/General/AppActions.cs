using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Views;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.General;

/// <summary>
/// Shared shell/menu actions for ShellWindow (and tray).
/// </summary>
internal static class AppActions
{
    public static void OpenWidget() => OpenWidgetGallery();

    public static void OpenWidgetGallery()
    {
        GuidedSetup.EnableWidgetUpload();
        ShellHelper.OpenUrl(GuidedSetup.WidgetGalleryUrl);
    }

    public static void OpenWidgetGenerator()
    {
        GuidedSetup.EnableWidgetUpload();
        ShellHelper.OpenUrl(GuidedSetup.WidgetGeneratorUrl());
    }

    public static void OpenPatchNotes()
    {
        WindowPatchnotes existing = Application.Current.Windows.OfType<WindowPatchnotes>().FirstOrDefault();
        if (existing != null)
        {
            existing.Focus();
            existing.Activate();
            return;
        }

        WindowPatchnotes wPn = new()
        {
            Owner = Application.Current.MainWindow
        };
        wPn.Show();
        wPn.Activate();
    }

    private static bool _notifyIfCurrent;

    /// <param name="notifyIfCurrent">
    /// True for Help / menu: tell the user when they already have the latest version.
    /// False for the silent startup check.
    /// </param>
    public static void CheckForUpdates(bool notifyIfCurrent = false)
    {
        _notifyIfCurrent = notifyIfCurrent;
        AutoUpdater.Mandatory = false;
        AutoUpdater.UpdateMode = Mode.Normal;
        AutoUpdater.AppTitle = "Songify";
        AutoUpdater.RunUpdateAsAdmin = false;
        AutoUpdater.ReportErrors = false;
        AutoUpdater.ApplicationExitEvent -= AutoUpdaterOnApplicationExit;
        AutoUpdater.ApplicationExitEvent += AutoUpdaterOnApplicationExit;
        AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdate;
        AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdate;
        if (Application.Current?.MainWindow != null)
            AutoUpdater.SetOwner(Application.Current.MainWindow);
        Logger.Info(LogSource.Core, $"Checking for update ({Settings.ReleaseChannel})...");
        AutoUpdater.Start(Settings.GetUpdateFeedUrl());
    }

    private static void AutoUpdaterOnCheckForUpdate(UpdateInfoEventArgs args)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            HandleUpdateCheckResult(args);
        else
            dispatcher.Invoke(() => HandleUpdateCheckResult(args));
    }

    private static void HandleUpdateCheckResult(UpdateInfoEventArgs args)
    {
        if (args?.Error != null)
        {
            Logger.Error(LogSource.Core, "Update check failed.", args.Error);
            if (_notifyIfCurrent)
                _ = AppDialog.ShowAsync(
                    "Update check failed",
                    "Couldn't reach the update server. Check your internet connection and try again.");
            return;
        }

        if (args is { IsUpdateAvailable: true })
        {
            AutoUpdater.ShowUpdateForm(args);
            return;
        }

        Logger.Info(LogSource.Core, "Songify is up to date.");
        if (!_notifyIfCurrent)
            return;

        string version = GlobalObjects.AppVersion
                         ?? args?.InstalledVersion?.ToString()
                         ?? "";
        _ = AppDialog.ShowAsync(
            "You're up to date",
            string.IsNullOrEmpty(version)
                ? "You already have the latest version of Songify."
                : $"You already have the latest version of Songify ({version}).");
    }

    /// <summary>
    /// AutoUpdater.NET skips its own shutdown when this event has subscribers, so we must
    /// persist <see cref="Settings.UpdateRequired"/> and exit ourselves. That flag is what
    /// shows the changelog prompt after the new version starts.
    /// </summary>
    private static void AutoUpdaterOnApplicationExit()
    {
        try
        {
            string versionFolder = (GlobalObjects.AppVersion ?? "unknown").Replace(".", "_");
            string backupDir = Path.Combine(GlobalObjects.RootDirectory, "Backup", versionFolder);
            Directory.CreateDirectory(backupDir);
            ConfigHandler.WriteAllConfig(Settings.Export(), backupDir, true);
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Core, "Failed to backup config before applying an update.", ex);
        }

        Settings.UpdateRequired = true;
        Logger.Info(LogSource.Core, "Update accepted; showing patch notes on next launch.");
        ExitApplication();
    }

    public static void OpenFaq() =>
        ShellHelper.OpenUrl($"{GlobalObjects.BaseUrl}/faq.html");

    public static void OpenGitHubIssues() =>
        ShellHelper.OpenUrl("https://github.com/songify-rocks/Songify/issues");

    public static void OpenDiscord() =>
        ShellHelper.OpenUrl("https://discordapp.com/invite/H8nd4T4");

    public static void OpenLogFolder() =>
        ShellHelper.OpenPath(Logger.LogDirectoryPath);

    public static void OpenAppFolder() =>
        ShellHelper.OpenPath(Directory.GetCurrentDirectory());

    public static void OpenWebServerUrl()
    {
        if (GlobalObjects.WebServer.Run)
            ShellHelper.OpenUrl($"http://localhost:{Settings.WebServerPort}");
    }

    public static void OpenQueueInBrowser() =>
        ShellHelper.OpenUrl($"{GlobalObjects.BaseUrl}/queue.php?id=" + Settings.Uuid);

    public static void TwitchLoginMain() => AccountLinking.LoginTwitchMain();

    /// <summary>Same as main-window Twitch → Connect: start/restart EventSub host.</summary>
    public static async Task TwitchConnectAsync()
    {
        try
        {
            await TwitchHandler.StartOrRestartAsync();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    /// <summary>Tray / explicit stop. Main menu uses Connect only (ForceDisconnect path).</summary>
    public static async Task TwitchDisconnectAsync()
    {
        try
        {
            TwitchHandler.ForceDisconnect = true;
            await TwitchHandler.StopAsync();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    /// <summary>Returns whether the channel is live after refreshing <see cref="Settings.IsLive"/>.</summary>
    public static async Task<bool> CheckTwitchOnlineStatusAsync()
    {
        try
        {
            Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
            Logger.Info(LogSource.Twitch, Settings.IsLive
                ? $"Stream is Live (id {Settings.StreamId})"
                : "Stream is Offline");
            AppShellBridge.Current?.SetStatusText(Settings.IsLive ? "Stream is Up!" : "Stream is offline.");
            if (Settings.IsLive)
                AppShellBridge.Current?.ClearTwitchCommandsPausedOffline();
            return Settings.IsLive;
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            return false;
        }
    }

    public static void ExitApplication()
    {
        if (Application.Current.MainWindow is Views.WPFUI.ShellWindow shell)
            shell.RequestForceClose();
        Application.Current.Shutdown();
    }
}
