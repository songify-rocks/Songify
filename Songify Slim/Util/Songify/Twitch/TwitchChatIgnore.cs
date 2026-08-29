using System;
using System.Linq;
using Songify_Slim.Util.Configuration;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace Songify_Slim.Util.Songify.Twitch;

/// <summary>
/// Skips chat commands from known bots, the linked Songify bot account, and a user-defined ignore list.
/// Automatic bot detection never ignores the broadcaster. Names on the custom list are always skipped.
/// </summary>
internal static class TwitchChatIgnore
{
    internal static readonly string[] DefaultBotLogins =
    [
        "nightbot",
        "streamelements",
        "streamlabs",
        "fossabot",
        "moobot",
        "mooobot",
        "wizebot",
        "stay_hydrated_bot",
        "sery_bot",
        "soundalerts",
        "kofistreambot",
        "phantombot",
        "ankhbot",
        "coebot",
        "deepbot",
        "botisimo",
        "commanderroot",
        "streamelementsbot",
        "pretzelrocks",
        "vivbot",
        "supibot"
    ];

    public static bool ShouldIgnore(ChannelChatMessage msg)
    {
        if (msg == null)
            return false;

        // Explicit ignore list always wins — including the broadcaster.
        if (IsOnCustomIgnoreList(msg))
            return true;

        if (!Settings.IgnoreBotMessages)
            return false;

        // Don't let automatic bot detection lock the streamer out of their own commands.
        if (msg.IsBroadcaster)
            return false;

        string login = msg.ChatterUserLogin;
        if (string.IsNullOrWhiteSpace(login))
            return false;

        if (Settings.TwitchBotUser != null)
        {
            if (!string.IsNullOrEmpty(Settings.TwitchBotUser.Id) &&
                string.Equals(msg.ChatterUserId, Settings.TwitchBotUser.Id, StringComparison.Ordinal))
                return true;

            if (string.Equals(login, Settings.TwitchBotUser.Login, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (DefaultBotLogins.Any(b => string.Equals(b, login, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (msg.Badges != null &&
            msg.Badges.Any(b => string.Equals(b.SetId, "bot", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private static bool IsOnCustomIgnoreList(ChannelChatMessage msg)
    {
        if (Settings.IgnoredChatUsers == null || Settings.IgnoredChatUsers.Count == 0)
            return false;

        foreach (string entry in Settings.IgnoredChatUsers)
        {
            string name = NormalizeIgnoreName(entry);
            if (name.Length == 0)
                continue;

            if (string.Equals(name, NormalizeIgnoreName(msg.ChatterUserLogin), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, NormalizeIgnoreName(msg.ChatterUserName), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, msg.ChatterUserId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal static string NormalizeIgnoreName(string value)
        => (value ?? "").Trim().TrimStart('@');
}
