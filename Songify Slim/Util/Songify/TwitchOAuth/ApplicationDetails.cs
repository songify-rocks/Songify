namespace VonRiddarn.Twitch.ImplicitOAuth
{
	public static class ApplicationDetails
	{
		// The client ID you get from your Twitch developer console (https://dev.twitch.tv/).
		public static string twitchClientId = "sgiysnqpffpcla6zk69yn8wmqnx56o";

		// The URI you entered when registering your application in the twitch console.
		// Default is fine.
		public static string redirectUri = "http://localhost:4003/";

		// Any URI you want to fetch results on.
		// Default is fine.
		public static string fetchUri = "http://localhost:4004/";
	}
}