using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Views;
using Swan;
using Swan.Formatters;

namespace Songify_Slim.Util.General;

/// <summary>
/// Runs application startup logic (config checks, dialogs, Spotify/Twitch init, fetch timer).
/// Call from ShellWindow or MainWindow so song fetcher and services start regardless of which window is used.
/// </summary>
public static class AppStartup
{
    /// <summary>
    /// Run the full startup sequence. Use <paramref name="useShellWindow"/> true when the main window is ShellWindow.
    /// </summary>
    public static async Task RunAsync(Window owner, bool useShellWindow)
    {
        EnsureAppVersionSet();
        if (!await CheckAndNotifyConfigurationIssuesAsync())
            return;

        await RunUseOwnAppDialogAsync();
        bool internetAvailable = await WaitForInternetConnectionAsync();
        if (!internetAvailable)
        {
            await RunInternetCheckDialogAsync(owner);
            return;
        }

        Logger.Info(LogSource.Spotify, "Starting Spotify init");
        await RunSpotifyInitAsync();
        Util.Spotify.SpotifyApiHandler.RefreshShellSpotifyIndicator();
        Logger.Info(LogSource.Spotify, "Spotify init done");

        Logger.Info(LogSource.Twitch, "Starting Twitch init");
        await RunTwitchInitAsync(useShellWindow);
        Logger.Info(LogSource.Twitch, "Twitch init done");

        Logger.Info(LogSource.Core, "Starting Final Setup");
        await RunFinalSetupAsync(owner);
        Logger.Info(LogSource.Core, "Final Setup done");
    }

    /// <returns><c>false</c> if startup should abort (app shutting down).</returns>
    private static async Task<bool> CheckAndNotifyConfigurationIssuesAsync()
    {
        Logger.Info(LogSource.Core, $"LOCATION: {AppPaths.GetAppDirectory()}");

        string assemblyLocation = AppPaths.GetAppDirectory();
        if (!string.IsNullOrEmpty(assemblyLocation) && assemblyLocation.Contains(".zip"))
        {
            await AppDialog.ShowAsync(
                "Warning",
                "Please extract Songify to a directory. The app can't save the config when run directly from the zip file.\nWe suggest a folder on the Desktop or in Documents.");
            Application.Current.Shutdown();
            return false;
        }

        if (string.IsNullOrEmpty(assemblyLocation) ||
            (!assemblyLocation.Contains(@"C:\Program Files") &&
             !assemblyLocation.Contains(@"C:\Program Files (x86)") &&
             !assemblyLocation.Contains(@"C:\ProgramData")))
            return true;

        try
        {
            File.WriteAllText(Path.Combine(assemblyLocation, "test.txt"), "test");
            File.Delete(Path.Combine(assemblyLocation, "test.txt"));
            return true;
        }
        catch (Exception)
        {
            await AppDialog.ShowAsync(
                "Warning",
                "Please move Songify to a different directory. The app can't save the config when run from this directory.\nWe suggest a folder on the Desktop or in Documents.");
            Application.Current.Shutdown();
            return false;
        }
    }

    private static void EnsureAppVersionSet()
    {
        if (!string.IsNullOrEmpty(GlobalObjects.AppVersion)) return;
        GlobalObjects.AppVersion = AppPaths.GetFileVersionThreePart() ?? "?";
    }

    private static async Task RunUseOwnAppDialogAsync()
    {
        if (Settings.UseOwnApp) return;

        AppDialogResult result = await AppDialog.ShowAsync(
            "Warning",
            "Songify now needs your own Spotify credentials (Client ID and Secret). Please follow the linked guide to set them up. This will help you avoid Spotify rate limits and ensure faster updates.",
            AppDialogStyle.PrimaryAndSecondary,
            new AppDialogSettings
            {
                PrimaryButtonText = "Open guide",
                SecondaryButtonText = "OK"
            });

        if (result == AppDialogResult.Primary)
            Process.Start(new ProcessStartInfo("https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup") { UseShellExecute = true });

        Settings.UseOwnApp = true;
    }

    private static async Task<bool> WaitForInternetConnectionAsync()
    {
        string[] urlsToCheck = { "https://www.google.com", "https://www.cloudflare.com", "https://songify.rocks" };
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var tasks = urlsToCheck.Select(url => httpClient.GetAsync(url)).ToList();
            var completed = await Task.WhenAny(tasks);
            if (completed != null && (await completed).IsSuccessStatusCode)
            {
                Logger.Info(LogSource.Core, "Internet Connection Established");
                return true;
            }
        }
        catch { /* ignore */ }

        return false;
    }

    private static async Task RunInternetCheckDialogAsync(Window owner)
    {
        var win = owner ?? Application.Current.MainWindow;
        while (true)
        {
            AppDialogResult result = await AppDialog.ShowAsync(
                "No Internet Connection",
                "It seems that no internet connection could be established.\n\nDo you want to retry or close Songify?",
                AppDialogStyle.PrimaryAndSecondary,
                new AppDialogSettings
                {
                    PrimaryButtonText = "Retry",
                    SecondaryButtonText = "Close"
                });

            if (result != AppDialogResult.Primary)
            {
                win?.Close();
                return;
            }

            if (await WaitForInternetConnectionAsync())
                return;
        }
    }

    private static async Task RunSpotifyInitAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(Settings.SpotifyAccessToken) || !string.IsNullOrEmpty(Settings.SpotifyRefreshToken))
                await SpotifyApiHandler.Auth();
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }
    }

    private static async Task RunTwitchInitAsync(bool useShellWindow)
    {
        if (Settings.AutoStartWebServer)
            GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);

        if (!useShellWindow && Settings.OpenQueueOnStartup)
            OpenQueueLegacy();

        if (Settings.TwAutoConnect)
            TwitchHandler.ConnectTwitchChatClient();

        if (Settings.AutoClearQueue)
        {
            GlobalObjects.ReqList.Clear();
            var payload = new { uuid = Settings.Uuid, key = Settings.AccessKey };
            await SongifyApi.ClearQueueAsync(Json.Serialize(payload));
        }

        if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
            await TwitchHandler.InitializeApi(Enums.TwitchAccount.Main);
        if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
            await TwitchHandler.InitializeApi(Enums.TwitchAccount.Bot);
    }

    private static void OpenQueueLegacy()
    {
        if (Application.Current.Windows.OfType<WindowQueue>().Any()) return;
        new WindowQueue().Show();
    }

    private static async Task RunFinalSetupAsync(Window owner)
    {
        try
        {
            await SendTelemetryAsync();
            Logger.Info(LogSource.Core, "Check Stream up");
            Settings.IsLive = await TwitchHandler.CheckStreamIsUp();
            Logger.Info(LogSource.Twitch, "Check Stream up done");

            Logger.Info(LogSource.Core, "SetFetchTimer");
            AppFetchService.Start();
            Logger.Info(LogSource.Core, "SetFetchTimer done");

            if (Settings.UpdateRequired)
            {
                AppDialogResult result = await AppDialog.ShowAsync(
                    "Songify just updated",
                    "Would you like to read the changelog? (recommended)\n\nYou can always find the changelog from the navigation.",
                    AppDialogStyle.PrimaryAndSecondary,
                    new AppDialogSettings
                    {
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "No"
                    });

                if (result == AppDialogResult.Primary)
                    OpenPatchNotes(owner);

                Settings.UpdateRequired = false;
            }
        }
        catch (Exception e)
        {
            Logger.LogExc(e);
        }

        CheckForUpdates();
    }

    private static async Task SendTelemetryAsync()
    {
        try
        {
            dynamic telemetryPayload = new
            {
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                tst = DateTime.Now.ToUnixEpochDate(),
                twitch_id = Settings.TwitchUser == null ? "" : Settings.TwitchUser.Id,
                twitch_name = Settings.TwitchUser == null ? "" : Settings.TwitchUser.DisplayName,
                vs = GlobalObjects.AppVersion,
                playertype = GlobalObjects.GetReadablePlayer(),
            };
            await SongifyApi.PostTelemetryAsync(Json.Serialize(telemetryPayload));
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private static void OpenPatchNotes(Window owner)
    {
        if (Application.Current.Windows.OfType<WindowPatchnotes>().FirstOrDefault() is WindowPatchnotes existing)
        {
            existing.Focus();
            existing.Activate();
            return;
        }

        var wPn = new WindowPatchnotes { Owner = owner ?? Application.Current.MainWindow };
        wPn.Show();
        wPn.Activate();
    }

    private static void CheckForUpdates()
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
}
