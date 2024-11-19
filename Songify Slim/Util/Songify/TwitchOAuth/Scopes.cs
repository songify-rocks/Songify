/*NOTE FROM TWITCH:
 An application must request only the scopes required by the APIs that their app calls.
 If you request more scopes than is required to support your app�s functionality, Twitch may suspend your application�s access to the Twitch API.

 To see what each scope does, visit:
 https://dev.twitch.tv/docs/authentication/scopes
 If the list of scopes for some reason is outdated you can add more scopes manually by following the same syntax.
 Should you notice the list being outdated, please, do contact VonRiddarn.
 */

using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Songify_Slim.Util.Songify.TwitchOAuth
{
    public static class Scopes
    {
        public static string[] GetScopes()
        {
            List<string> s = new()
            {
                "channel:manage:redemptions",
                "channel:read:redemptions",
                "chat:edit",
                "chat:read",
                "moderator:manage:banned_users",
                "moderator:read:blocked_terms",
                "moderator:manage:blocked_terms",
                "moderator:manage:automod",
                "moderator:read:automod_settings",
                "moderator:manage:automod_settings",
                "moderator:read:chat_settings",
                "moderator:manage:chat_settings",
                "moderator:manage:announcements",
                "channel:moderate",
                "moderator:read:followers"
                //s.Add("user:read:follows");
                //s.Add("user:read:subscriptions");
                //s.Add("analytics:read:extensions");
                //s.Add("analytics:read:games");
                //s.Add("bits:read");
                //s.Add("channel:edit:commercial");
                //s.Add("channel:manage:broadcast");
                //s.Add("channel:manage:extensions");
                //s.Add("channel:manage:polls");
                //s.Add("channel:manage:predictions");
                //s.Add("channel:manage:raids");
                //s.Add("channel:manage:schedule");
                //s.Add("channel:manage:videos");
                //s.Add("channel:read:editors");
                //s.Add("channel:read:goals");
                //s.Add("channel:read:hype_train");
                //s.Add("channel:read:polls");
                //s.Add("channel:read:predictions");
                //s.Add("channel:read:stream_key");
                //s.Add("channel:read:subscriptions");
                //s.Add("clips:edit");
                //s.Add("moderation:read");
                ////s.Add("user:edit");
                //s.Add("user:edit:follows");
                //s.Add("user:manage:blocked_users");
                //s.Add("user:read:blocked_users");
                //s.Add("user:read:broadcast");
                //s.Add("user:read:email");
            };

            //s.Add("whispers:read");
            //s.Add("whispers:edit");

            return s.ToArray();
        }
    }
}