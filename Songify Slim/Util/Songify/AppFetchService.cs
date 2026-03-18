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

    public static void Start()
    {
        if (_running) return;
        _running = true;
        RunGetCurrentSongAsync();
        SetTimer();
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

    private static void SetTimer()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
        catch { /* ignore */ }

        PlayerType player = (PlayerType)Settings.Player;
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
                intervalMs = MathUtils.Clamp(Settings.SpotifyFetchRate, 1, 30) * 1000;
                break;

            case PlayerType.BrowserCompanion:
            default:
                return;
        }

        _timer = new Timer(intervalMs);
        _timer.Elapsed += OnTimedEvent;
        _timer.Enabled = true;
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
        PlayerType player = (PlayerType)Settings.Player;
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
}