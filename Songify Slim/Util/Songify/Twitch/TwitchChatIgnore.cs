using System;
using System.Collections.Generic;
using Songify_Slim.Util.Configuration;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace Songify_Slim.Util.Songify.Twitch;

/// <summary>
/// Skips chat commands from names on the ignore list (including the broadcaster)
/// and from the linked Songify bot account.
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

        if (IsOnCustomIgnoreList(msg))
            return true;

        // The connected Songify bot account posts announcements, not viewer requests.
        if (IsLinkedSongifyBot(msg) && !msg.IsBroadcaster)
            return true;

        return false;
    }

    /// <summary>Adds known bot logins that are not already on the list. Returns how many were added.</summary>
    internal static int MergeKnownBots(List<string> list)
    {
        if (list == null)
            return 0;

        int added = 0;
        foreach (string bot in DefaultBotLogins)
        {
            if (list.Exists(u => string.Equals(NormalizeIgnoreName(u), bot, StringComparison.OrdinalIgnoreCase)))
                continue;
            list.Add(bot);
            added++;
        }

        return added;
    }

    private static bool IsLinkedSongifyBot(ChannelChatMessage msg)
    {
        if (Settings.TwitchBotUser == null)
            return false;

        if (!string.IsNullOrEmpty(Settings.TwitchBotUser.Id) &&
            string.Equals(msg.ChatterUserId, Settings.TwitchBotUser.Id, StringComparison.Ordinal))
            return true;

        return string.Equals(msg.ChatterUserLogin, Settings.TwitchBotUser.Login, StringComparison.OrdinalIgnoreCase);
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
