using System;
using System.Collections.Generic;
using TwitchLib.Client.Models;

namespace Songify_Slim.Util.Songify.Twitch;

public static class SharedChatExtensions
{
    /// <summary>
    /// Returns true if this chat message originated in the current channel (not a relayed shared-chat copy).
    /// </summary>
    public static bool IsLocalOrigin(this ChatMessage msg)
    {
        // Undocumented / new tag for shared chat origin channel
        // TwitchLib stores tags in a dictionary (case-insensitive) - adjust if needed
        Dictionary<string, string> tags = msg?.UndocumentedTags;

        // Try to get the source-room-id tag
        if (tags != null && tags.TryGetValue("source-room-id", out string sourceRoomIdValue))
        {
            // If present and different from the current room, it's a shared (foreign) message
            return string.Equals(sourceRoomIdValue, msg.RoomId, StringComparison.Ordinal);
        }

        // Tag absent -> not a shared duplicate (original)
        return true;
    }

    /// <summary>
    /// Returns true if this message was *relayed* from another channel in a Shared Chat session.
    /// </summary>
    public static bool IsRelayedSharedChat(this ChatMessage msg) => !msg.IsLocalOrigin();

    /// <summary>
    /// If relayed, returns the origin room id; else returns the local RoomId.
    /// </summary>
    public static string GetOriginRoomId(this ChatMessage msg)
    {
        Dictionary<string, string> tags = msg?.UndocumentedTags;
        if (tags != null && tags.TryGetValue("source-room-id", out string sourceRoomIdValue)
                         && !string.IsNullOrEmpty(sourceRoomIdValue))
        {
            return sourceRoomIdValue;
        }
        return msg.RoomId;
    }

    /// <summary>
    /// Returns true if the message is marked as source-only (will not be duplicated to other channels).
    /// </summary>
    public static bool IsSourceOnly(this ChatMessage msg)
    {
        Dictionary<string, string> tags = msg?.UndocumentedTags;
        return tags != null && tags.ContainsKey("source-only");
    }
}