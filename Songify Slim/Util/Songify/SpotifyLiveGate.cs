using System;
using System.Windows;
using System.Windows.Threading;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Spotify live/Test Mode gating: when enabled, now-playing fetch runs only while live on Twitch
/// or while Test Mode is on. Test Mode is a temporary override that turns itself off.
/// </summary>
public static class SpotifyLiveGate
{
    public static readonly TimeSpan TestModeDuration = TimeSpan.FromMinutes(2);

    private static DispatcherTimer _testModeTimer;

    /// <summary>Raised when the gate, Test Mode, or related UI visibility should refresh.</summary>
    public static event Action Changed;

    /// <summary>Settings → Spotify “Enable live gate / test mode”.</summary>
    public static bool IsEnabled => Settings.EnableSpotifyLiveGate;

    /// <summary>Test Mode control belongs on the shell when Spotify is selected and the gate is on.</summary>
    public static bool ShouldShowTestMode =>
        Settings.Player == PlayerType.Spotify && Settings.EnableSpotifyLiveGate;

    /// <summary>
    /// When the player is Spotify and the gate is on, fetch only if live or Test Mode is on.
    /// Other players are unaffected.
    /// </summary>
    public static bool AllowsSpotifyFetch =>
        !Settings.EnableSpotifyLiveGate
        || Settings.IsLive
        || GlobalObjects.TestMode;

    public static bool IsFetchPaused =>
        Settings.Player == PlayerType.Spotify && !AllowsSpotifyFetch;

    public static void SetTestMode(bool enabled)
    {
        void Apply()
        {
            if (enabled)
            {
                if (!ShouldShowTestMode)
                    return;

                GlobalObjects.TestMode = true;
                RestartTestModeTimer();
                AppFetchService.NotifySpotifyRelatedActivity("test mode enabled");
                _ = AppFetchService.ForceFetchSpotifyAsync(true);
            }
            else
            {
                StopTestModeTimer();
                GlobalObjects.TestMode = false;
            }

            RaiseChanged();
        }

        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            Apply();
        else
            dispatcher.Invoke(Apply);
    }

    /// <summary>Clear Test Mode without fetching. Used when the gate hides or the player is no longer Spotify.</summary>
    public static void ClearTestMode()
    {
        if (!GlobalObjects.TestMode && _testModeTimer == null)
            return;

        SetTestMode(false);
    }

    /// <summary>Call after the live-gate setting or player source changes.</summary>
    public static void OnSettingsOrPlayerChanged()
    {
        if (!ShouldShowTestMode)
        {
            StopTestModeTimer();
            GlobalObjects.TestMode = false;
        }

        RaiseChanged();
    }

    private static void RestartTestModeTimer()
    {
        StopTestModeTimer();
        Dispatcher dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        _testModeTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TestModeDuration
        };
        _testModeTimer.Tick += TestModeTimerOnTick;
        _testModeTimer.Start();
    }

    private static void TestModeTimerOnTick(object sender, EventArgs e)
    {
        SetTestMode(false);
    }

    private static void StopTestModeTimer()
    {
        if (_testModeTimer == null)
            return;

        _testModeTimer.Tick -= TestModeTimerOnTick;
        _testModeTimer.Stop();
        _testModeTimer = null;
    }

    private static void RaiseChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }
}
