using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Task = System.Threading.Tasks.Task;
using WebServer = Unosquare.Labs.EmbedIO.WebServer;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth
{
    /// <summary>
    /// <para>
    /// A version of <see cref="AuthorizationCodeAuth"/> that does not store your client secret, client ID or redirect URI, enforcing a secure authorization flow. Requires an exchange server that will return the authorization code to its callback server via GET request.
    /// </para>
    /// <para>
    /// It's recommended that you use <see cref="TokenSwapWebApiFactory"/> if you would like to use the TokenSwap method.
    /// </para>
    /// </summary>
    public class TokenSwapAuth : SpotifyAuthServer<AuthorizationCode>
    {
        private readonly string _exchangeServerUri;

        /// <summary>
        /// The HTML to respond with when the callback server (serverUri) is reached. The default value will close the window on arrival.
        /// </summary>
        public string HtmlResponse { get; set; } = "<script>window.close();</script>";

        /// <summary>
        /// If true, will time how long it takes for access to expire. On expiry, the <see cref="OnAccessTokenExpired"/> event fires.
        /// </summary>
        public bool TimeAccessExpiry { get; set; }

        public ProxyConfig ProxyConfig { get; set; }

        /// <param name="exchangeServerUri">The URI to an exchange server that will perform the key exchange.</param>
        /// <param name="serverUri">The URI to host the server at that your exchange server should return the authorization code to by GET request. (e.g. http://localhost:4002)</param>
        /// <param name="scope"></param>
        /// <param name="state">Stating none will randomly generate a state parameter.</param>
        /// <param name="htmlResponse">The HTML to respond with when the callback server (serverUri) is reached. The default value will close the window on arrival.</param>
        public TokenSwapAuth(string exchangeServerUri, string serverUri, Scope scope = Scope.None, string state = "",
          string htmlResponse = "") : base("code", "", "", serverUri, scope, state)
        {
            if (!string.IsNullOrEmpty(htmlResponse))
            {
                HtmlResponse = htmlResponse;
            }

            _exchangeServerUri = exchangeServerUri;
        }

        protected override void AdaptWebServer(WebServer webServer)
        {
            webServer.Module<WebApiModule>().RegisterController<TokenSwapAuthController>();
        }

        public override string GetUri()
        {
            StringBuilder builder = new(_exchangeServerUri);
            builder.Append(_exchangeServerUri.Contains("?") ? "&" : "?");
            builder.Append("response_type=code");
            builder.Append("&state=" + State);
            builder.Append("&scope=" + Scope.GetStringAttribute(" "));
            builder.Append("&show_dialog=" + ShowDialog);
            return Uri.EscapeUriString(builder.ToString());
        }

        /// <summary>
        /// The maximum amount of times to retry getting a token.
        /// <para/>
        /// A token get is attempted every time you <see cref="RefreshAuthAsync(string)"/> and <see cref="ExchangeCodeAsync(string)"/>.
        /// </summary>
        public int MaxGetTokenRetries { get; set; } = 10;

        /// <summary>
        /// Creates a HTTP request to obtain a token object.<para/>
        /// Parameter grantType can only be "refresh_token" or "authorization_code". authorizationCode and refreshToken are not mandatory, but at least one must be provided for your desired grant_type request otherwise an invalid response will be given and an exception is likely to be thrown.
        /// <para>
        /// Will re-attempt on error, on null or on no access token <see cref="MaxGetTokenRetries"/> times before finally returning null.
        /// </para>
        /// </summary>
        /// <param name="grantType">Can only be "refresh_token" or "authorization_code".</param>
        /// <param name="authorizationCode">This needs to be defined if "grantType" is "authorization_code".</param>
        /// <param name="refreshToken">This needs to be defined if "grantType" is "refresh_token".</param>
        /// <param name="currentRetries">Does not need to be defined. Used internally for retry attempt recursion.</param>
        /// <returns>Attempts to return a full <see cref="Token"/>, but after retry attempts, may return a <see cref="Token"/> with no <see cref="Token.AccessToken"/>, or null.</returns>
        private async Task<Token> GetToken(string grantType, string authorizationCode = "", string refreshToken = "",
          int currentRetries = 0)
        {
            FormUrlEncodedContent content = new(new Dictionary<string, string>
      { { "grant_type", grantType },
        { "code", authorizationCode },
        { "refresh_token", refreshToken }
      });

            try
            {
                HttpClientHandler handler = ProxyConfig.CreateClientHandler(ProxyConfig);
                HttpClient client = new(handler);
                HttpResponseMessage siteResponse = await client.PostAsync(_exchangeServerUri, content);
                if (siteResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    if (!siteResponse.RequestMessage.RequestUri.ToString().Contains(GlobalObjects.AuthUrl)) return null;
                    General.Logger.LogStr("SPOTIFY: Issues Authentication, trying alternate url");
                    await Task.Delay(5000);
                    await SpotifyApiHandler.DoAuthAsync(true);
                    return null;
                }

                Token token = JsonConvert.DeserializeObject<Token>(await siteResponse.Content.ReadAsStringAsync());
                // Don't need to check if it was null - if it is, it will resort to the catch block.
                if (!token.HasError() && !string.IsNullOrEmpty(token.AccessToken))
                {
                    return token;
                }
            }
            catch
            { }

            if (currentRetries >= MaxGetTokenRetries)
            {
                return null;
            }
            else
            {
                currentRetries++;
                // The reason I chose to implement the retries system this way is because a static or instance
                // variable keeping track would inhibit parallelism i.e. using this function on multiple threads/tasks.
                // It's not clear why someone would like to do that, but it's better to cater for all kinds of uses.
                return await GetToken(grantType, authorizationCode, refreshToken, currentRetries);
            }
        }

        private System.Timers.Timer _accessTokenExpireTimer;

        /// <summary>
        /// When Spotify authorization has expired. Will only trigger if <see cref="TimeAccessExpiry"/> is true.
        /// </summary>
        public event EventHandler OnAccessTokenExpired;

        /// <summary>
        /// If <see cref="TimeAccessExpiry"/> is true, sets a timer for how long access will take to expire.
        /// </summary>
        /// <param name="token"></param>
        private void SetAccessExpireTimer(Token token)
        {
            if (!TimeAccessExpiry) return;

            if (_accessTokenExpireTimer != null)
            {
                _accessTokenExpireTimer.Stop();
                _accessTokenExpireTimer.Dispose();
            }

            _accessTokenExpireTimer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = token.ExpiresIn * 1000,
                AutoReset = false
            };
            _accessTokenExpireTimer.Elapsed += (sender, e) => OnAccessTokenExpired?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Uses the authorization code to silently (doesn't open a browser) obtain both an access token and refresh token, where the refresh token would be required for you to use <see cref="RefreshAuthAsync(string)"/>.
        /// </summary>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        public async Task<Token> ExchangeCodeAsync(string authorizationCode)
        {
            Token token = await GetToken("authorization_code", authorizationCode: authorizationCode);
            if (token != null && !token.HasError() && !string.IsNullOrEmpty(token.AccessToken))
            {
                SetAccessExpireTimer(token);
            }

            return token;
        }

        /// <summary>
        /// Uses the refresh token to silently (doesn't open a browser) obtain a fresh access token, no refresh token is given however (as it does not change).
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public async Task<Token> RefreshAuthAsync(string refreshToken)
        {
            Token token = await GetToken("refresh_token", refreshToken: refreshToken);
            if (token != null && !token.HasError() && !string.IsNullOrEmpty(token.AccessToken))
            {
                SetAccessExpireTimer(token);
            }

            return token;
        }
    }

    internal class TokenSwapAuthController(IHttpContext context) : WebApiController(context)
    {
        [WebApiHandler(HttpVerbs.Get, "/auth")]
        public Task<bool> GetAuth()
        {
            string state = Request.QueryString["state"];
            SpotifyAuthServer<AuthorizationCode> auth = TokenSwapAuth.GetByState(state);

            string code = null;
            string error = Request.QueryString["error"];
            if (error == null)
            {
                code = Request.QueryString["code"];
            }

            Task.Factory.StartNew(() => auth?.TriggerAuth(new AuthorizationCode
            {
                Code = code,
                Error = error
            }));
            return HttpContext.HtmlResponseAsync(((TokenSwapAuth)auth).HtmlResponse);
        }
    }
}