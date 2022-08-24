using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace VonRiddarn.Twitch.ImplicitOAuth
{
	public class ImplicitOAuth
	{

		#region Variables
		// Privates
		string twitchAuthUrl = "https://id.twitch.tv/oauth2/authorize";
		int salt = 0;

		// Events
		public delegate void UpdatedValuesEvent(string state, string token);
		/// <summary>
		/// Called when the HttpListeners has recieved all data needed.
		/// </summary>
		/// <remarks>
		/// This returns the state and the token recieved from the server.
		/// Make sure to compare the state before trusting the token.
		/// </remarks>
		public event UpdatedValuesEvent OnRevcievedValues;
		#endregion

		#region Public Methods
		public ImplicitOAuth(int stateSalt = 42)
		{
			salt = stateSalt;
		}

		/// <summary>
		/// Request the user to Authorize the application through twitch.
		/// </summary>
		/// <remarks>
		/// Open the users browser and ask them to authorize this application.
		/// The method will return a state string you can use to prevent CSRF attacks.
		/// </remarks>
		public string RequestClientAuthorization()
		{
			// Create a "random" number to use as verification.
			string authStateVerify = ((Int64)(DateTime.UtcNow.AddYears(salt).Subtract(new DateTime(1939, 11, 30))).TotalSeconds).ToString();

			// Assign value to string
			string queryParams =
			"response_type=token" + "&" +
			"client_id=" + ApplicationDetails.twitchClientId + "&" +
			"redirect_uri=" + ApplicationDetails.redirectUri + "&" +
			"state=" + authStateVerify + "&" +
			"scope=" + string.Join("+", Scopes.GetScopes());
			// End

			// Start a local webserver that twitch can redirect us back to after authentication.
			InitializeLocalWebServers();

			// Open the browser and send the user to the implicit autentication page on Twitch.
			System.Diagnostics.Process.Start($"{twitchAuthUrl}?{queryParams}");

			return authStateVerify;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Start 2 local HttpListeners and have them wait for data
		/// </summary>
		void InitializeLocalWebServers()
		{
			if (ApplicationDetails.redirectUri == null || ApplicationDetails.redirectUri.Length == 0)
			{
				Console.WriteLine("URI may not be empty!");
				return;
			}

			// Listener for twitch redirect.
			HttpListener redirectListener = new HttpListener();
			redirectListener.Prefixes.Add(ApplicationDetails.redirectUri);
			redirectListener.Start();
			redirectListener.BeginGetContext(new AsyncCallback(IncommingTwitchRequest), redirectListener);

			// Listener for fetching info from the redirect listener.
			HttpListener fetchListeneer = new HttpListener();
			fetchListeneer.Prefixes.Add(ApplicationDetails.fetchUri);
			fetchListeneer.Start();
			fetchListeneer.BeginGetContext(new AsyncCallback(IncommingLocalRequest), fetchListeneer);
		}

		/// <summary>
		/// Recieve the data from the other HttpListener and send it through the OnJsonObjectUpdate event.
		/// </summary>
		void IncommingLocalRequest(IAsyncResult result)
		{
			HttpListener httpListener = (HttpListener)result.AsyncState;
			HttpListenerContext httpContext = httpListener.EndGetContext(result);
			HttpListenerRequest httpRequest = httpContext.Request;

			string jsonObjectString = null;
			var reader = new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding);
			jsonObjectString = reader.ReadToEnd();

			// Fix errors in the string and send it through
			// Probably caused by the fetch not knowing that they are supposed to send JSON data.
			jsonObjectString = jsonObjectString.Replace("\\", null);
			jsonObjectString = jsonObjectString.Remove(jsonObjectString.Length - 1);
			jsonObjectString = jsonObjectString.Remove(0, 1);
			JObject jo = JObject.Parse(jsonObjectString);

			OnRevcievedValues?.Invoke(jo.GetValue("state").ToString(), jo.GetValue("access_token").ToString());

			httpListener.Stop();
		}

		/// <summary>
		/// Get the data from the URL hash and send it to the other listener.
		/// </summary>
		void IncommingTwitchRequest(IAsyncResult result)
		{
			HttpListener httpListener = (HttpListener)result.AsyncState;
			HttpListenerContext httpContext = httpListener.EndGetContext(result);
			HttpListenerRequest httpRequest = httpContext.Request;
			HttpListenerResponse httpResponse = httpContext.Response;

			string responseString = "";

			// Create manual html and js code in here to get the URL, scrape the jsondata, then send it back to us via "IncommingLocalRequest".
			// TODO: Separate this data into it's own file.
			responseString =
			@"
				<html>
					<body>
						<h1>
							Authentication complete!
						</h1>
						<p>You are adviced not to show this page on stream.<p>
						<p>This window closes in 5 seconds.<p>

						<script>
							let values = 
							{
								access_token:""TOKEN"",
								state: ""STATE""
							};

							const url = new URLSearchParams(""?"" + window.location.hash.substring(1))
							window.history.replaceState(null, '', '/');
							values.access_token = url.get('access_token');
							values.state = url.get('state');

							jsonData = JSON.stringify(values);

							fetch('VARIABLE_FETCHURI', { method: ""POST"", body: JSON.stringify(jsonData)})
                            setTimeout(function () {
                                window.close();
                                }, 5000);
                            
						</script>
					</body>
				</html>
			";
			responseString = responseString.Replace("VARIABLE_FETCHURI", ApplicationDetails.fetchUri);


			// Save html to buffer and send to browser
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			httpResponse.ContentLength64 = buffer.Length;
			Stream output = httpResponse.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();


			// Stop the local webserver.
			httpListener.Stop();
		}
		#endregion
	}
}