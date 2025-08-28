using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Songify.TwitchOAuth
{
    public class ImplicitOAuth(int stateSalt = 42)
    {
        #region Variables

        // Privates
        private const string TwitchAuthUrl = "https://id.twitch.tv/oauth2/authorize";

        // Listener for twitch redirect.
        private readonly HttpListener _redirectListener = new();

        // Listener for fetching info from the redirect listener.
        private readonly HttpListener _fetchListener = new();

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

        #endregion Variables

        #region Public Methods

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
            string authStateVerify = ((Int64)(DateTime.UtcNow.AddYears(stateSalt).Subtract(new DateTime(1939, 11, 30))).TotalSeconds).ToString();

            // Assign value to string
            string queryParams =
            "response_type=token" + "&" +
            "client_id=" + ApplicationDetails.TwitchClientId + "&" +
            "redirect_uri=" + ApplicationDetails.RedirectUri + "&" +
            "state=" + authStateVerify + "&" +
            "scope=" + string.Join("+", Scopes.GetScopes()) + "&" +
            "force_verify=true";
            // End

            // Start a local webserver that twitch can redirect us back to after authentication.
            InitializeLocalWebServers();

            // Open the browser and send the user to the implicit autentication page on Twitch.
            try
            {
                if (File.Exists(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe") && !Settings.Settings.UseDefaultBrowser)
                {
                    Process process = new()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                            Arguments = $"--inprivate {TwitchAuthUrl}?{queryParams}"
                        }
                    };
                    process.Start();
                }
                else
                {
                    Process.Start($"{TwitchAuthUrl}?{queryParams}");
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }

            return authStateVerify;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Start 2 local HttpListeners and have them wait for data
        /// </summary>
        private void InitializeLocalWebServers()
        {
            if (string.IsNullOrEmpty(ApplicationDetails.RedirectUri))
            {
                Console.WriteLine(@"URI may not be empty!");
                return;
            }

            try
            {
                if (!_redirectListener.IsListening)
                {
                    _redirectListener.Prefixes.Add(ApplicationDetails.RedirectUri);
                    _redirectListener.Start();
                    _redirectListener.BeginGetContext(IncommingTwitchRequest, _redirectListener);
                }

                if (!_fetchListener.IsListening)
                {
                    _fetchListener.Prefixes.Add(ApplicationDetails.FetchUri);
                    _fetchListener.Start();
                    _fetchListener.BeginGetContext(IncommingLocalRequest, _fetchListener);
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        /// <summary>
        /// Recieve the data from the other HttpListener and send it through the OnJsonObjectUpdate event.
        /// </summary>
        private void IncommingLocalRequest(IAsyncResult result)
        {
            HttpListener httpListener = (HttpListener)result.AsyncState;
            HttpListenerContext httpContext = httpListener.EndGetContext(result);
            HttpListenerRequest httpRequest = httpContext.Request;

            StreamReader reader = new(httpRequest.InputStream, httpRequest.ContentEncoding);
            string jsonObjectString = reader.ReadToEnd();

            // Fix errors in the string and send it through
            // Probably caused by the fetch not knowing that they are supposed to send JSON data.
            jsonObjectString = jsonObjectString.Replace("\\", null);
            jsonObjectString = jsonObjectString.Remove(jsonObjectString.Length - 1);
            jsonObjectString = jsonObjectString.Remove(0, 1);
            JObject jo = JObject.Parse(jsonObjectString);

            OnRevcievedValues?.Invoke(jo.GetValue("state")?.ToString(), jo.GetValue("access_token")?.ToString());

            httpListener.Stop();
        }

        /// <summary>
        /// Get the data from the URL hash and send it to the other listener.
        /// </summary>
        private void IncommingTwitchRequest(IAsyncResult result)
        {
            HttpListener httpListener = (HttpListener)result.AsyncState;
            HttpListenerContext httpContext = httpListener.EndGetContext(result);
            HttpListenerResponse httpResponse = httpContext.Response;

            string responseString =
                // Create manual html and js code in here to get the URL, scrape the jsondata, then send it back to us via "IncommingLocalRequest".
                // TODO: Separate this data into it's own file.
                """
                <html>
                <style>
                  @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap');
                  @import url('https://fonts.googleapis.com/css2?family=Pacifico&display=swap');
                  
                  body {
                    text-align: center;
                    min-height: 100vh;
                    margin: 0;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    background-color: #1a1a1a;
                    background-image: radial-gradient(#1f1f1f 1px, transparent 1px);
                    background-size: 20px 20px;
                    font-family: 'Inter', sans-serif;
                  }
                
                  h1 {
                    color: #1ed760;
                    font-weight: 700;
                    font-size: 2.5rem;
                    margin: 1.5rem 0 1rem;
                    opacity: 0;
                    transform: translateY(20px);
                  }
                
                  h1.animate {
                    animation: fadeUp 0.6s ease-out forwards 0.5s;
                  }
                
                  p {
                    color: #e2e8f0;
                    font-size: 1.125rem;
                    line-height: 1.6;
                    margin: 0;
                    opacity: 0;
                    transform: translateY(20px);
                  }
                
                  p.animate {
                    animation: fadeUp 0.6s ease-out forwards 0.7s;
                  }
                
                  .songify-text {
                    font-family: 'Pacifico', cursive;
                    color: #1ed760;
                  }
                
                  .content {
                    height: 200px;
                    width: 200px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    margin: 0 auto;
                    position: relative;
                    opacity: 0;
                    transform: scale(0.9);
                  }
                
                  .content.animate {
                    animation: popIn 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards;
                  }
                
                  .content img {
                    height: 70;
                    transition: transform 0.3s ease;
                  }
                
                  .content img:hover {
                    transform: scale(1.1);
                  }
                
                  .plus-sign {
                    margin: 0 1.5rem;
                    color: #e2e8f0;
                    font-size: 1.5rem;
                    font-weight: bold;
                  }
                
                  .card {
                    background: #2b2b2b;
                    padding: 3rem;
                    border-radius: 16px;
                    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                    max-width: 90%;
                    width: 440px;
                    margin: 2rem;
                    position: relative;
                    overflow: hidden;
                    border: 1px solid #363636;
                  }
                
                  .card::before {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: 0;
                    right: 0;
                    height: 4px;
                    background: linear-gradient(90deg, #1ed760, #9147ff);
                  }
                
                  @keyframes fadeUp {
                    to {
                      opacity: 1;
                      transform: translateY(0);
                    }
                  }
                
                  @keyframes popIn {
                    to {
                      opacity: 1;
                      transform: scale(1);
                    }
                  }
                </style>

                <body>
                  <div class="card">
                <div class="content">
                  <img src="https://songify.overcode.tv/api/images/logo.png" alt="Songify Logo" />
                  <span class="plus-sign">+</span>
                  <img src="https://songify.overcode.tv/api/images/TwitchGlitchPurple.png" alt="Twitch Logo" />
                </div>
                    <h1>Success!</h1>
                    <p><span class="songify-text">Songify</span> and Twitch are now connected!<br />You can close this page.</p>
                  </div>
                
                  <script>
                    // Wait for logo to load before starting animations
                    window.addEventListener('load', function() {
                      const songifyLogo = document.querySelector('img[src*="songify.rocks"]');
                      
                      function startAnimations() {
                        document.querySelector('.content').classList.add('animate');
                        document.querySelector('h1').classList.add('animate');
                        document.querySelector('p').classList.add('animate');
                      }
                
                      if (songifyLogo.complete) {
                        startAnimations();
                      } else {
                        songifyLogo.addEventListener('load', startAnimations);
                      }
                    });
                
                    let values = {
                      access_token: "TOKEN",
                      state: "STATE"
                    };
                
                    const url = new URLSearchParams("?" + window.location.hash.substring(1))
                    window.history.replaceState(null, '', '/');
                    values.access_token = url.get('access_token');
                    values.state = url.get('state');
                
                    jsonData = JSON.stringify(values);
                
                    fetch('VARIABLE_FETCHURI', {
                      method: "POST",
                      body: JSON.stringify(jsonData)
                    })
                
                    setTimeout(function() {
                      window.close();
                    }, 5000);
                  </script>
                </body>
                </html>
                """;
            responseString = responseString.Replace("VARIABLE_FETCHURI", ApplicationDetails.FetchUri);

            // Save html to buffer and send to browser
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            httpResponse.ContentLength64 = buffer.Length;
            Stream output = httpResponse.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            // Stop the local webserver.
            httpListener.Stop();
        }

        #endregion Private Methods
    }
}