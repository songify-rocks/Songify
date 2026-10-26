using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Youtube.Pear;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Application-wide song fetch logic: runs SongFetcher on a timer and updates GlobalObjects.CurrentSong.
/// Owns the song-fetch timer for the Fluent shell.
/// </summary>
public static class AppFetchService
{
    private static readonly SongFetcher Sf = new();
    private static Timer _timer;
    private static bool _running;
    private static bool _forceSpotifyUpdate;

    /// <summary>SMTC tick: local progress, fallback poll scheduling. No API call per tick.</summary>
    private const int SmtcTickIntervalMs = 1000;

    /// <summary>Non-null while Spotify now-playing is SMTC-driven (default on Windows).</summary>
    private static NowPlayingCoordinator _nowPlaying;

    /// <summary>Bumped on every Start/Stop so a slow SMTC start cannot attach to a stopped run.</summary>
    private static int _runGeneration;

    /// <summary>Raised when Spotify idle backoff stage/interval may have changed (UI should refresh).</summary>
    public static event Action IdleBackoffChanged;

    /// <summary>Raised after the active player source changes (e.g. Spotify ↔ Pear).</summary>
    public static event Action PlayerSourceChanged;

    /// <summary>Idle backoff only applies to legacy polling; SMTC mode has no poll interval to restore.</summary>
    public static bool IsSpotifyIdleBackoffActive => _nowPlaying == null && Sf.IsSpotifyIdleBackoffActive;

    /// <summary>True while Spotify now-playing is driven by Windows media controls instead of polling.</summary>
    public static bool IsSmtcNowPlayingActive => _nowPlaying != null;

    public static int GetEffectiveSpotifyFetchIntervalSeconds() =>
        Sf.GetEffectiveSpotifyFetchIntervalSeconds();

    public static TimeSpan? GetPearConnectBackoffRemaining() =>
        Sf.GetPearConnectBackoffRemaining();

    public static Task ForceFetchSpotifyAsync(bool forceUpdate = true) =>
        Sf.FetchSpotifyWeb(forceUpdate);

    public static Task ForceFetchPearAsync(bool forceNow = true) =>
        Sf.FetchPear(forceNow);

    public static Task<bool> SkipPearAsync() =>
        Sf.SkipPearAsync();

    public static Task RefreshPearNowPlayingAsync() =>
        Sf.RefreshPearNowPlayingAsync();

    public static Task NotifyPearPlayerInactiveAsync() =>
        Sf.NotifyPearPlayerInactiveAsync();

    public static void Start()
    {
        if (_running) return;
        _running = true;
        int generation = ++_runGeneration;
        SyncPearAutoConnect();
        RunGetCurrentSongAsync();
        SetTimer();
        RaiseIdleBackoffChanged();

        if (Settings.Player == PlayerType.Spotify && !Settings.SpotifyLegacyApiPolling)
            _ = StartSmtcNowPlayingAsync(generation);
    }

    /// <summary>Stops and starts the fetch loop, e.g. after the "Legacy API polling" setting changed.</summary>
    public static void Restart()
    {
        Stop();
        Start();
    }

    public static void Stop()
    {
        _running = false;
        _runGeneration++;
        StopSmtcNowPlaying();
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    /// <summary>
    /// Apply player-source side effects (Pear WebSocket auto-connect / cleanup) then restart the fetch timer.
    /// </summary>
    public static async Task ApplyPlayerSourceAsync(PlayerType previous, PlayerType selected)
    {
        if (previous == PlayerType.Pear && selected != PlayerType.Pear)
            await Sf.NotifyPearPlayerInactiveAsync().ConfigureAwait(true);

        PearWebSocketClient.AutoConnectEnabled = selected == PlayerType.Pear;

        if (selected == PlayerType.Spotify)
            await SpotifyApiHandler.Auth().ConfigureAwait(true);
        else
            SpotifyApiHandler.ReleaseInactivePlayerSession();

        Stop();
        Start();
        SpotifyLiveGate.OnSettingsOrPlayerChanged();
        RaisePlayerSourceChanged();
    }

    /// <summary>
    /// Clears Spotify idle fetch backoff and restarts the fetch timer at the settings interval.
    /// Used when Twitch commands/rewards or the status-bar chip indicate activity.
    /// </summary>
    public static void NotifySpotifyRelatedActivity(string reason = "Twitch command or reward")
    {
        if (Settings.Player != PlayerType.Spotify)
            return;

        Sf.RestoreSpotifyFetchRate(reason);
        ApplySpotifyFetchTimerInterval();
        RaiseIdleBackoffChanged();
    }

    /// <summary>
    /// Switches Spotify to SMTC-driven now-playing. Until this completes (or if SMTC is unavailable)
    /// the legacy API poll keeps running.
    /// </summary>
    private static async Task StartSmtcNowPlayingAsync(int generation)
    {
        Application app = Application.Current;
        if (app == null)
            return;

        if (!app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() => StartSmtcNowPlayingAsync(generation)).Task.Unwrap();
            return;
        }

        NowPlayingCoordinator coordinator = new(Sf, new SpotifySmtcWatcher(app.Dispatcher));
        try
        {
            await coordinator.StartAsync();
        }
        catch (Exception ex)
        {
            coordinator.Dispose();
            Logger.Warning(LogSource.Spotify,
                "Windows media controls (SMTC) are unavailable. Falling back to legacy Spotify API polling.", ex);
            return;
        }

        if (!_running || generation != _runGeneration || Settings.Player != PlayerType.Spotify)
        {
            coordinator.Dispose();
            return;
        }

        _nowPlaying = coordinator;
        ApplySpotifyFetchTimerInterval();
    }

    private static void StopSmtcNowPlaying()
    {
        NowPlayingCoordinator coordinator = _nowPlaying;
        _nowPlaying = null;
        try
        {
            coordinator?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private static void SyncPearAutoConnect()
    {
        PearWebSocketClient.AutoConnectEnabled = Settings.Player == PlayerType.Pear;
    }

    private static void RaisePlayerSourceChanged()
    {
        try
        {
            PlayerSourceChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private static void SetTimer()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
        catch { /* ignore */ }

        PlayerType player = Settings.Player;
        int intervalMs;
        switch (player)
        {
            case PlayerType.WindowsPlayback:
            case PlayerType.Vlc:
            case PlayerType.FooBar2000:
            case PlayerType.Qobuz:
            case PlayerType.Pear:
                intervalMs = 1000;
                break;

            case PlayerType.Spotify:
                intervalMs = _nowPlaying != null ? SmtcTickIntervalMs : Sf.GetEffectiveSpotifyFetchIntervalMs();
                break;

            case PlayerType.BrowserCompanion:
            default:
                return;
        }

        _timer = new Timer(intervalMs);
        _timer.Elapsed += OnTimedEvent;
        _timer.Enabled = true;
    }

    private static void ApplySpotifyFetchTimerInterval()
    {
        if (_timer == null || Settings.Player != PlayerType.Spotify)
            return;

        try
        {
            // Changing Interval while Enabled restarts the countdown with the restored rate.
            _timer.Interval = _nowPlaying != null ? SmtcTickIntervalMs : Sf.GetEffectiveSpotifyFetchIntervalMs();
        }
        catch (ObjectDisposedException)
        {
            // Timer may be mid-recreate during source switches.
        }
        catch (ArgumentException)
        {
            // Interval must be > 0.
        }
    }

    private static async void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        if (!_running || _timer == null) return;
        try
        {
            _timer.Enabled = false;
            _timer.Elapsed -= OnTimedEvent;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await RunGetCurrentSongAsync();
                }
                finally
                {
                    if (_running && _timer != null)
                    {
                        if (Settings.Player == PlayerType.Spotify)
                        {
                            ApplySpotifyFetchTimerInterval();
                            RaiseIdleBackoffChanged();
                        }

                        _timer.Elapsed += OnTimedEvent;
                        _timer.Enabled = true;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            if (_running && _timer != null)
            {
                _timer.Elapsed += OnTimedEvent;
                _timer.Enabled = true;
            }
        }
    }

    private static async Task RunGetCurrentSongAsync()
    {
        PlayerType player = Settings.Player;
        try
        {
            switch (player)
            {
                case PlayerType.BrowserCompanion:
                    await Sf.FetchYoutubeData();
                    break;

                case PlayerType.Vlc:
                    await Sf.FetchDesktopPlayer("vlc");
                    break;

                case PlayerType.FooBar2000:
                    await Sf.FetchDesktopPlayer("foobar2000");
                    break;

                case PlayerType.Qobuz:
                    await Sf.FetchDesktopPlayer("qobuz");
                    break;

                case PlayerType.Spotify:
                    if (_nowPlaying != null)
                    {
                        // Handles the live gate itself and republishes when it reopens.
                        await _nowPlaying.TickAsync();
                        break;
                    }

                    if (!SpotifyLiveGate.AllowsSpotifyFetch)
                    {
                        _forceSpotifyUpdate = true;
                        break;
                    }

                    await Sf.FetchSpotifyWeb(_forceSpotifyUpdate);
                    _forceSpotifyUpdate = false;
                    break;

                case PlayerType.Pear:
                    await Sf.FetchPear();
                    break;

                case PlayerType.WindowsPlayback:
                    await Sf.FetchWindowsApi();
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private static void RaiseIdleBackoffChanged()
    {
        try
        {
            IdleBackoffChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }
}
