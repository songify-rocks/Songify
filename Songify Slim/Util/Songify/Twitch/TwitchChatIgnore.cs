using System;
using System.Collections.Generic;
using Songify_Slim.Util.Configuration;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace Songify_Slim.Util.Songify.Twitch;

/// <summary>
/// Skips chat commands from names on the ignore list (including the broadcaster).
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

        return IsOnCustomIgnoreList(msg);
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
