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

    public static void OpenSpotifySetupGuide() => ShellHelper.OpenUrl(SpotifySetupWiki);

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
