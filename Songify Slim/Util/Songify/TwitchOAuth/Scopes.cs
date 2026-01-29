/*NOTE FROM TWITCH:
 An application must request only the scopes required by the APIs that their app calls.
 If you request more scopes than is required to support your app�s functionality, Twitch may suspend your application�s access to the Twitch API.

 To see what each scope does, visit:
 https://dev.twitch.tv/docs/authentication/scopes
 If the list of scopes for some reason is outdated you can add more scopes manually by following the same syntax.
 Should you notice the list being outdated, please, do contact VonRiddarn.
 */

using System.Collections.Generic;

namespace Songify_Slim.Util.Songify.TwitchOAuth
{
    public static class Scopes
    {
        public static string[] GetScopes()
        {
            List<string> s =
            [
                "bits:read",
                "channel:bot",
                "channel:manage:polls",
                "channel:manage:redemptions",
                "channel:moderate",
                "channel:read:polls",
                "channel:read:redemptions",
                "channel:read:subscriptions",
                "channel:read:vips",
                "chat:edit",
                "chat:read",
                "moderation:read",
                "moderator:manage:announcements",
                "moderator:manage:automod",
                "moderator:manage:automod_settings",
                "moderator:manage:banned_users",
                "moderator:manage:blocked_terms",
                "moderator:manage:chat_settings",
                "moderator:read:automod_settings",
                "moderator:read:blocked_terms",
                "moderator:read:chat_settings",
                "moderator:read:chatters",
                "moderator:read:followers",
                "user:bot",
                "user:read:chat",
                "user:write:chat",
                //"user:read:follows"
                //"user:read:subscriptions"
                //"analytics:read:extensions"
                //"analytics:read:games"
                //"bits:read"
                //"channel:edit:commercial"
                //"channel:manage:broadcast"
                //"channel:manage:extensions"
                //"channel:manage:predictions"
                //"channel:manage:raids"
                //"channel:manage:schedule"
                //"channel:manage:videos"
                //"channel:read:editors"
                //"channel:read:goals"
                //"channel:read:hype_train"
                //"channel:read:predictions"
                //"channel:read:stream_key"
                //"channel:read:subscriptions"
                //"clips:edit"
                //"moderation:read"
                //"user:edit"
                //"user:edit:follows"
                //"user:manage:blocked_users"
                //"user:read:blocked_users"
                //"user:read:broadcast"
                //"user:read:email"
            ];
            return [.. s];
        }
    }
}