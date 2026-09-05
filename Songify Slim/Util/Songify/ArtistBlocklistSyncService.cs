using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Timer = System.Timers.Timer;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Hourly (or forced) download of the configured artist CSV into the Spotify artist blocklist.
/// </summary>
internal static class ArtistBlocklistSyncService
{
    private static readonly Timer SyncTimer = new(TimeSpan.FromHours(1).TotalMilliseconds);
    private static int _running;
    private static bool _started;

    public static void Start()
    {
        if (_started)
            return;

        _started = true;
        SyncTimer.Elapsed += async (_, _) => await TryRunAsync(force: false, reason: "hourly timer");
        SyncTimer.AutoReset = true;
        SyncTimer.Start();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(8000);
                await TryRunAsync(force: true, reason: "startup");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Startup artist blocklist sync failed", ex);
            }
        });
    }

    public static void Stop()
    {
        SyncTimer.Stop();
        _started = false;
    }

    public static async Task TryRunAsync(bool force, string reason = null)
    {
        string trigger = string.IsNullOrWhiteSpace(reason) ? (force ? "forced" : "scheduled") : reason;

        if (!Settings.ArtistBlocklistSyncEnabled)
        {
            if (force)
                Logger.Info(LogSource.Spotify, $"Artist blocklist sync skipped ({trigger}): hourly sync is disabled in Settings → Spotify.");
            else
                Logger.Debug(LogSource.Spotify, $"Artist blocklist sync skipped ({trigger}): hourly sync is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Settings.ArtistBlocklistSyncUrl))
        {
            Logger.Info(LogSource.Spotify, $"Artist blocklist sync skipped ({trigger}): no CSV URL configured.");
            return;
        }

        if (!force)
        {
            string last = Settings.ArtistBlocklistSyncLastUtc;
            if (!string.IsNullOrWhiteSpace(last) &&
                DateTime.TryParse(last, null, DateTimeStyles.RoundtripKind, out DateTime lastUtc) &&
                DateTime.UtcNow - lastUtc.ToUniversalTime() < TimeSpan.FromHours(1))
            {
                Logger.Debug(LogSource.Spotify,
                    $"Artist blocklist sync skipped ({trigger}): last sync was less than 1 hour ago ({last}).");
                return;
            }
        }

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            Logger.Debug(LogSource.Spotify, $"Artist blocklist sync skipped ({trigger}): already running.");
            return;
        }

        try
        {
            Logger.Info(LogSource.Spotify,
                $"Artist blocklist sync starting ({trigger}): {Settings.ArtistBlocklistSyncUrl}");

            ArtistCsvSyncResult result = await ArtistCsvImport.SyncFromSettingsAsync();
            if (!result.Success)
            {
                Logger.Warning(LogSource.Spotify, $"Artist blocklist sync failed ({trigger}): {result.Message}");
                return;
            }

            Logger.Info(LogSource.Spotify, $"Artist blocklist sync finished ({trigger}): {result.Message}");

            await BlocklistUi.RefreshArtistsAsync();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, LogSource.Core, $"Artist blocklist sync failed ({trigger})", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
