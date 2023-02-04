using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms.VisualStyles;
using Microsoft.Win32;
using Songify_Slim.Views;

namespace VonRiddarn.Twitch.ImplicitOAuth
{
    public class ImplicitOAuth
    {


        #region Variables
        // Privates
        string twitchAuthUrl = "https://id.twitch.tv/oauth2/authorize";
        int salt = 0;

        // Listener for twitch redirect.
        HttpListener redirectListener = new HttpListener();
        // Listener for fetching info from the redirect listener.
        HttpListener fetchListeneer = new HttpListener();

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
            //Process.Start($"{twitchAuthUrl}?{queryParams}");
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Arguments = $"--inprivate {twitchAuthUrl}?{queryParams}"
            };
            process.Start();
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


            if (!redirectListener.IsListening)
            {
                redirectListener.Prefixes.Add(ApplicationDetails.redirectUri);
                redirectListener.Start();
                redirectListener.BeginGetContext(new AsyncCallback(IncommingTwitchRequest), redirectListener);
            }

            if (!fetchListeneer.IsListening)
            {
                fetchListeneer.Prefixes.Add(ApplicationDetails.fetchUri);
                fetchListeneer.Start();
                fetchListeneer.BeginGetContext(new AsyncCallback(IncommingLocalRequest), fetchListeneer);
            }
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
                    <style>
                        body {
                          text-align: center;
                          padding: 40px 0;
                          background: #EBF0F5;
                        }
                        h1 {
                            color: #1ed760;
                            font-family: ""Nunito Sans"", ""Helvetica Neue"", sans-serif;
                            font-weight: 900;
                            font-size: 40px;
                            margin-bottom: 10px;
                        }
                        p {
                            color: #404F5E;
                            font-family: ""Nunito Sans"", ""Helvetica Neue"", sans-serif;
                            font-size:20px;
                            margin: 0;
                        }
                        .content {
                            font-style: normal !important;
                            color: #9ABC66;
                            font-size: 50px;
                            line-height: 200px;
                            display: flex;
                            align-items: center;
                            justify-content: space-evenly;
                        }
                        .card {
                          background: white;
                          padding: 60px;
                          border-radius: 4px;
                          box-shadow: 0 2px 3px #C8D0D8;
                          display: inline-block;
                          margin: 0 auto;
                        }
                      </style>
                      <body>
                        <div class=""card"">
                        <div style=""border-radius:200px; height:200px; width:200px; background: #F8FAF5; margin:0 auto;"">
                          <div class=""content"">
                              <img src=""https://songify.overcode.tv/img/ms-icon-310x310.png"" style="" height: 50px;""/> + <img src=""https://songify.overcode.tv/img/TwitchGlitchPurple.png"" style="" height: 50px;""/>
                          </div>
                        </div>
                          <h1>Success</h1> 
                          <p>Twitch and Songify are now connected!<br/>This page closes itself in 5 seconds.</p>
                        </div>

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