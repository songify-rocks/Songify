using System;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Util.Spotify;

namespace Songify_Slim.Util.General;

internal enum SpotifyLinkResult
{
    Started,
    MissingClientId,
    Failed
}

/// <summary>
/// Shared Spotify / Twitch account-link entry points for Settings and the setup wizard.
/// </summary>
internal static class AccountLinking
{
    public const string SpotifySetupWiki =
        "https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup";

    public const string SongifyTokenFaqUrl =
        "https://songify.rocks/faq/what-is-the-songify-api-token";

    public const string SongifyAccountUrl = "https://songify.rocks/account";

    /// <summary>
    /// Website route that should log in with Twitch, mint a token, and redirect to
    /// <c>songify://import-token?token=...</c>. Point <see cref="OpenSongifyTokenPage"/> at this
    /// when that page exists.
    /// </summary>
    public const string SongifyDesktopImportTokenUrl = "https://songify.rocks/token-import";

    public static void OpenSpotifySetupGuide() => ShellHelper.OpenUrl(SpotifySetupWiki);

    public static bool HasSongifyApiToken() =>
        !string.IsNullOrWhiteSpace(Settings.SongifyApiKey);

    /// <summary>Opens the account page to generate a token. Switch to <see cref="SongifyDesktopImportTokenUrl"/> for one-click import.</summary>
    public static void OpenSongifyTokenPage() => ShellHelper.OpenUrl(SongifyDesktopImportTokenUrl);

    public static void OpenSongifyTokenFaq() => ShellHelper.OpenUrl(SongifyTokenFaqUrl);

    public static async Task<SpotifyLinkResult> LinkSpotifyAsync()
    {
        if (string.IsNullOrWhiteSpace(Settings.ClientId))
            return SpotifyLinkResult.MissingClientId;

        Settings.UseOwnApp = true;
        Settings.SpotifyRedirectUri = "127.0.0.1";
        SpotifyApiHandler.ResetSpotifyAuthState();

        try
        {
            await SpotifyApiHandler.Auth();
            return SpotifyLinkResult.Started;
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Spotify, "Error linking Spotify.", ex);
            return SpotifyLinkResult.Failed;
        }
    }

    public static void LoginTwitchMain() => TwitchHandler.ApiConnect(Enums.TwitchAccount.Main);

    public static bool IsSpotifyLinked() =>
        !string.IsNullOrWhiteSpace(Settings.ClientId) &&
        (!string.IsNullOrWhiteSpace(Settings.SpotifyAccessToken) ||
         !string.IsNullOrWhiteSpace(Settings.SpotifyRefreshToken));

    public static bool IsTwitchMainLinked() =>
        !string.IsNullOrWhiteSpace(Settings.TwitchAccessToken);
}