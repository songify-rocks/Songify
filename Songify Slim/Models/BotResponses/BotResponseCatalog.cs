using System.Collections.Generic;
using System.Linq;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Models.BotResponses;

/// <summary>
/// Static catalog of song-request-related bot responses shown in <c>UcBotResponses</c>.
/// Order matches the previous hand-authored UI.
/// </summary>
public static class BotResponseCatalog
{
    private const string SuccessDefault =
        "{artist} - {title} requested by @{user} has been added to the queue.";

    public static IReadOnlyList<BotResponseItem> All { get; } =
    [
        new(
            "SongRequestSuccess",
            "window_botresponses_success",
            SuccessDefault,
            () => Settings.Commands.First(cmd => cmd.Name == "Song Request").Response,
            value => Settings.Commands.First(cmd => cmd.Name == "Song Request").Response = value),

        new(
            "SongInQueue",
            "window_botresponses_song_in_queue",
            "@{user} this song is already in the queue.",
            () => Settings.BotRespIsInQueue,
            value => Settings.BotRespIsInQueue = value),

        new(
            "MaxSongs",
            "window_botresponses_max_songs",
            "@{user} maximum number of songs in queue reached ({maxreq}).",
            () => Settings.BotRespMaxReq,
            value => Settings.BotRespMaxReq = value),

        new(
            "SongTooLong",
            "window_botresponses_song_too_long",
            "@{user} the song you requested exceeded the maximum song length ({maxlength}).",
            () => Settings.BotRespLength,
            value => Settings.BotRespLength = value),

        new(
            "UserLevelTooLowCommand",
            "window_botresponses_user_level_too_low_command",
            "Sorry, only {userlevel} or higher can request songs using the command.",
            () => Settings.BotRespUserlevelTooLowCommand,
            value => Settings.BotRespUserlevelTooLowCommand = value),

        new(
            "CommandDisabled",
            "window_botresponses_command_disabled",
            "@{user} the command {cmd} is not enabled.",
            () => Settings.BotRespCommandDisabled,
            value => Settings.BotRespCommandDisabled = value),

        new(
            "UserLevelTooLowReward",
            "window_botresponses_user_level_too_low_reward",
            "Sorry, only {userlevel} or higher can request songs using the reward.",
            () => Settings.BotRespUserlevelTooLowReward,
            value => Settings.BotRespUserlevelTooLowReward = value),

        new(
            "ArtistBlocked",
            "window_botresponses_artist_blocked",
            "@{user} the Artist: {artist} has been blocked by the broadcaster.",
            () => Settings.BotRespBlacklist,
            value => Settings.BotRespBlacklist = value),

        new(
            "SongBlocked",
            "window_botresponses_song_blocked",
            "@{user} the song: {song} has been blocked by the broadcaster.",
            () => Settings.BotRespBlacklistSong,
            value => Settings.BotRespBlacklistSong = value),

        new(
            "ExplicitSongs",
            "window_botresponses_explicit_songs",
            "This Song containts explicit content and is not allowed.",
            () => Settings.BotRespTrackExplicit,
            value => Settings.BotRespTrackExplicit = value),

        new(
            "CommandCooldown",
            "window_botresponses_command_on_cooldown",
            "The command is on cooldown. Try again in {cd} seconds.",
            () => Settings.BotRespCooldown,
            value => Settings.BotRespCooldown = value),

        new(
            "UserCooldown",
            "window_botresponses_command_on_user_cooldown",
            "@{user} you have to wait {cd} before you can request a song again.",
            () => Settings.BotRespUserCooldown,
            value => Settings.BotRespUserCooldown = value),

        new(
            "NotInPlaylist",
            "window_botresponses_song_not_in_playlist",
            "This song was not found in the allowed playlist.({playlist_name} {playlist_url})",
            () => Settings.BotRespPlaylist,
            value => Settings.BotRespPlaylist = value),

        new(
            "FetchError",
            "window_botresponses_fetch_error",
            "@{user} there was an error adding your Song to the queue. Error message: {errormsg}",
            () => Settings.BotRespError,
            value => Settings.BotRespError = value),

        new(
            "NoSong",
            "window_botresponses_no_song",
            "@{user} please specify a song to add to the queue.",
            () => Settings.BotRespNoSong,
            value => Settings.BotRespNoSong = value),

        new(
            "NoTrackFound",
            "window_botresponses_no_track_found",
            "No track found.",
            () => Settings.BotRespNoTrackFound,
            value => Settings.BotRespNoTrackFound = value),

        new(
            "Refund",
            "window_botresponses_refund",
            "Your points have been refunded.",
            () => Settings.BotRespRefund,
            value => Settings.BotRespRefund = value),
    ];
}
