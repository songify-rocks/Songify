using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Views;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.General;

/// <summary>
/// Shared shell/menu actions so ShellWindow (and tray) do not depend on MainWindow handlers.
/// </summary>
internal static class AppActions
{
    public static void OpenWidget()
    {
        if (!Settings.Upload)
            Settings.Upload = true;
        ShellHelper.OpenUrl("https://widget.songify.rocks/" + Settings.Uuid);
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

    public static void CheckForUpdates()
    {
        AutoUpdater.Mandatory = false;
        AutoUpdater.UpdateMode = Mode.Normal;
        AutoUpdater.AppTitle = "Songify";
        AutoUpdater.RunUpdateAsAdmin = false;
        Logger.Info(LogSource.Core, "Checking for update...");
        AutoUpdater.Start(Settings.BetaUpdates
            ? $"{GlobalObjects.BaseUrl}/update-beta.xml"
            : $"{GlobalObjects.BaseUrl}/update.xml");
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

    public static void OpenHistoryInBrowser() =>
        ShellHelper.OpenUrl($"{GlobalObjects.BaseUrl}/history.php?id=" + Settings.Uuid);

    public static void TwitchLoginMain() =>
        TwitchHandler.ApiConnect(TwitchAccount.Main);

    public static void TwitchConnect()
    {
        try
        {
            TwitchHandler.ConnectTwitchChatClient();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

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

    public static async Task CheckTwitchOnlineStatusAsync()
    {
        try
        {
            Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
            Logger.Info(LogSource.Twitch, $"Stream is {(Settings.IsLive ? "Live" : "Offline")}");
            AppShellBridge.Current?.SetStatusText(Settings.IsLive ? "Stream is Up!" : "Stream is offline.");
            await AppDialog.ShowAsync(
                "Twitch status",
                Settings.IsLive ? "Stream is live." : "Stream is offline.");
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    public static void ExitApplication()
    {
        if (Application.Current.MainWindow is Views.WPFUI.ShellWindow shell)
            shell.RequestForceClose();
        Application.Current.Shutdown();
    }
}
