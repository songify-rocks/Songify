using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Application-wide song fetch logic: runs SongFetcher on a timer and updates GlobalObjects.CurrentSong.
/// Use this when the main window is ShellWindow (or any context where MainWindow's fetch timer is not used).
/// </summary>
public static class AppFetchService
{
    private static readonly SongFetcher Sf = new();
    private static Timer _timer;
    private static bool _running;

    /// <summary>Raised when Spotify idle backoff stage/interval may have changed (UI should refresh).</summary>
    public static event Action IdleBackoffChanged;

    public static bool IsSpotifyIdleBackoffActive => Sf.IsSpotifyIdleBackoffActive;

    public static int GetEffectiveSpotifyFetchIntervalSeconds() =>
        Sf.GetEffectiveSpotifyFetchIntervalSeconds();

    public static void Start()
    {
        if (_running) return;
        _running = true;
        RunGetCurrentSongAsync();
        SetTimer();
        RaiseIdleBackoffChanged();
    }

    public static void Stop()
    {
        _running = false;
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
            case PlayerType.Pear:
                intervalMs = 1000;
                break;

            case PlayerType.Spotify:
                intervalMs = Sf.GetEffectiveSpotifyFetchIntervalMs();
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
            _timer.Interval = Sf.GetEffectiveSpotifyFetchIntervalMs();
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

                case PlayerType.Spotify:
                    await Sf.FetchSpotifyWeb();
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
