using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace Songify_Slim.Util.General;

/// <summary>
/// Abstraction for the main application shell (MainWindow or ShellWindow).
/// Service/worker code must use this via <see cref="AppShellBridge"/> instead of referencing MainWindow directly,
/// so the app works when either MainWindow or ShellWindow is the active main window.
/// </summary>
public interface IAppShell
{
    /// <summary>Show a message dialog. Returns the user's choice.</summary>
    Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null);

    /// <summary>Set the main status line text (e.g. "Stream is Up!", "Error uploading...").</summary>
    void SetStatusText(string text);

    /// <summary>Twitch API connection indicator: Connected, Error, or neutral.</summary>
    void SetTwitchApiState(ConnectionIndicatorState state);

    /// <summary>Twitch Bot/chat connection indicator.</summary>
    void SetTwitchBotState(ConnectionIndicatorState state);

    /// <summary>Web server running indicator.</summary>
    void SetWebServerRunning(bool running);

    /// <summary>Spotify connection indicator: Disconnected, Premium, or Free.</summary>
    void SetSpotifyState(SpotifyIndicatorState state);

    /// <summary>Set album cover image from local file path.</summary>
    void SetCoverImage(string coverPath);

    /// <summary>Set the live output / preview text.</summary>
    void SetTextPreview(string text);

    /// <summary>Start canvas video from path.</summary>
    void SetCanvas(string path);

    /// <summary>Stop canvas video.</summary>
    void StopCanvas();

    /// <summary>Current song display string (e.g. "Artist - Title") for services that need to show it.</summary>
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