using System.Collections.Generic;

namespace Songify_Slim.Util.Songify.TwitchOAuth
{
    public static class ApplicationDetails
    {
        // The client ID you get from your Twitch developer console (https://dev.twitch.tv/).
        public static string TwitchClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";
        public static readonly List<int> RedirectPorts = new List<int> { 4003, 49180, 54321, 57689, 64567, 65100 };
        public static readonly List<int> FetchPorts = new List<int> { 4004, 49181, 54322, 57690, 64568, 65101 };

        // The URI you entered when registering your application in the twitch console.
        // Default is fine.
        public static string RedirectUri = $"http://localhost:{Settings.Settings.TwitchRedirectPort}/";

        // Any URI you want to fetch results on.
        // Default is fine.
        public static string FetchUri = $"http://localhost:{Settings.Settings.TwitchFetchPort}/";
    }
}