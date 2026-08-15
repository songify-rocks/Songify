using System.Threading.Tasks;

namespace Songify_Slim.Util.General;

/// <summary>
/// Abstraction for the main application shell (ShellWindow).
/// Service/worker code must use this via <see cref="AppShellBridge"/> instead of referencing a concrete window type.
/// </summary>
public interface IAppShell
{
    /// <summary>Show a message dialog. Returns the user's choice.</summary>
    Task<AppDialogResult> ShowMessageAsync(
        string title,
        string message,
        AppDialogStyle style = AppDialogStyle.Primary,
        AppDialogSettings settings = null);

    void SetStatusText(string text);
    void SetTwitchApiState(ConnectionIndicatorState state);
    void SetTwitchBotState(ConnectionIndicatorState state);
    void SetWebServerRunning(bool running);
    void SetSpotifyState(SpotifyIndicatorState state);
    void SetCoverImage(string coverPath);
    void SetTextPreview(string text);
    void SetCanvas(string path);
    void StopCanvas();
    string GetCurrentSongDisplayString();
}

public enum ConnectionIndicatorState
{
    Unknown,
    Connected,
    Error
}

public enum SpotifyIndicatorState
{
    Disconnected,
    Premium,
    Free
}
