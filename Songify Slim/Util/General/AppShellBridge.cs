namespace Songify_Slim.Util.General;

/// <summary>
/// Static bridge to the current application shell. Set by ShellWindow on Loaded; cleared on Closing.
/// Services (TwitchHandler, WebServer, SpotifyApiHandler, SongFetcher, IOManager, etc.) must use this
/// instead of casting Application.Current.MainWindow to a concrete window type.
/// </summary>
public static class AppShellBridge
{
    public static IAppShell Current { get; private set; }

    public static void Register(IAppShell shell)
    {
        Current = shell;
    }

    public static void Unregister(IAppShell shell)
    {
        if (Current == shell)
            Current = null;
    }
}