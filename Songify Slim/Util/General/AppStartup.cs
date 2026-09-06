using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
/// Call from ShellWindow so song fetcher and services start with the Fluent shell.
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

        if (!await WaitForInternetConnectionAsync())
        {
            if (!await RunInternetCheckDialogAsync(owner))
                return;
        }

        Task authTask = AuthenticateSongifyApiAsync();

        bool startTour = false;
        if (GuidedSetup.ShouldShowWizard())
            startTour = await GuidedSetup.ShowWizardAsync(owner);
        else
            await RunUseOwnAppDialogAsync();

        Logger.Info(LogSource.Spotify, "Starting Spotify init");
        await RunSpotifyInitAsync();
        Util.Spotify.SpotifyApiHandler.RefreshShellSpotifyIndicator();
        Logger.Info(LogSource.Spotify, "Spotify init done");

        Logger.Info(LogSource.Twitch, "Starting Twitch init");
        await RunTwitchInitAsync(useShellWindow, owner);
        Logger.Info(LogSource.Twitch, "Twitch init done");

        Logger.Info(LogSource.Core, "Starting Final Setup");
        await RunFinalSetupAsync(owner);
        Logger.Info(LogSource.Core, "Final Setup done");

        ArtistBlocklistSyncService.Start();

        try
        {
            await authTask;
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Api, "Songify API authentication did not finish before the Premium reminder.", ex);
        }

        if (owner is Views.WPFUI.ShellWindow shell)
        {
            Views.WPFUI.Pages.OverviewPage.RefreshChecklist();
            if (startTour)
                await shell.StartSetupTourAsync();
            await shell.TryShowPremiumReminderAsync();
        }
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

    private static async Task AuthenticateSongifyApiAsync()
    {
        try
        {
            await SongifyAuthService.EnsureAuthenticatedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Api, "Songify API authentication failed.", ex);
        }

        try
        {
            await SongifyPremiumService.RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Api, "Songify Premium status refresh failed.", ex);
        }
        finally
        {
            SongifyPremiumService.Start();
        }
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
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            if (await ProbeInternetAsync())
            {
                Logger.Info(LogSource.Core, "Internet Connection Established");
                return true;
            }

            if (attempt < 2)
                await Task.Delay(500);
        }

        Logger.Warning(LogSource.Core, "Internet connection check failed.");
        return false;
    }

    private static async Task<bool> ProbeInternetAsync()
    {
        string[] urlsToCheck =
        [
            "https://www.google.com",
            "https://www.cloudflare.com",
            "https://songify.rocks"
        ];

        using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

        List<Task<bool>> probes = urlsToCheck
            .Select(url => ProbeInternetUrlAsync(httpClient, url, cts.Token))
            .ToList();

        while (probes.Count > 0)
        {
            Task<bool> completed = await Task.WhenAny(probes);
            probes.Remove(completed);

            bool reached;
            try
            {
                reached = await completed;
            }
            catch
            {
                reached = false;
            }

            if (!reached)
                continue;

            await cts.CancelAsync();
            return true;
        }

        return false;
    }

    private static async Task<bool> ProbeInternetUrlAsync(
        HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            // Any HTTP response means the network is up (403/redirects still count).
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug(LogSource.Core, $"Internet probe failed for {url}: {ex.Message}");
            return false;
        }
    }

    /// <returns><c>true</c> if internet was confirmed and startup should continue.</returns>
    private static async Task<bool> RunInternetCheckDialogAsync(Window owner)
    {
        Window win = owner ?? Application.Current.MainWindow;
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
                return false;
            }

            if (await WaitForInternetConnectionAsync())
                return true;
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

    private static async Task RunTwitchInitAsync(bool useShellWindow, Window owner)
    {
        if (Settings.AutoStartWebServer)
            GlobalObjects.WebServer.StartWebServer(Settings.WebServerPort);

        if (useShellWindow && Settings.OpenQueueOnStartup && owner is Views.WPFUI.ShellWindow shell)
        {
            if (Settings.OpenQueuePopOutOnStartup)
                Views.WPFUI.QueueWindow.ShowOrActivate();
            else
                shell.NavigateToQueue();
        }

        if (Settings.TwAutoConnect)
            TwitchHandler.ConnectTwitchChatClient();

        if (Settings.AutoClearQueue)
        {
            GlobalObjects.ReqList.Clear();
            var payload = new { uuid = Settings.Uuid };
            await SongifyApi.ClearQueueAsync(Json.Serialize(payload));
        }

        if (!string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
            await TwitchHandler.InitializeApi(Enums.TwitchAccount.Main);
        if (!string.IsNullOrWhiteSpace(Settings.TwitchBotToken))
            await TwitchHandler.InitializeApi(Enums.TwitchAccount.Bot);
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

        AppActions.CheckForUpdates();
    }

    private static async Task SendTelemetryAsync()
    {
        try
        {
            dynamic telemetryPayload = new
            {
                uuid = Settings.Uuid,
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
        if (Application.Current.Windows.OfType<WindowPatchnotes>().FirstOrDefault() is { } existing)
        {
            existing.Focus();
            existing.Activate();
            return;
        }

        WindowPatchnotes wPn = new() { Owner = owner ?? Application.Current.MainWindow };
        wPn.Show();
        wPn.Activate();
    }
}