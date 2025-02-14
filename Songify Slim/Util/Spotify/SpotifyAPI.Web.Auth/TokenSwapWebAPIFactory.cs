using System;
using System.Threading.Tasks;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth
{
    /// <summary>
    /// Returns a <see cref="SpotifyWebAPI"/> using the TokenSwapAuth process.
    /// </summary>
    public class TokenSwapWebApiFactory
    {
        /// <summary>
        /// Access provided by Spotify expires after 1 hour. If true, <see cref="TokenSwapAuth"/> will time the access tokens, and access will attempt to be silently (without opening a browser) refreshed automatically. This will not make <see cref="OnAccessTokenExpired"/> fire, see <see cref="TimeAccessExpiry"/> for that.
        /// </summary>
        public bool AutoRefresh { get; set; }

        /// <summary>
        /// If true when calling <see cref="GetWebApiAsync"/>, will time how long it takes for access to Spotify to expire. The event <see cref="OnAccessTokenExpired"/> fires when the timer elapses.
        /// </summary>
        public bool TimeAccessExpiry { get; set; }

        /// <summary>
        /// The maximum time in seconds to wait for a SpotifyWebAPI to be returned. The timeout is cancelled early regardless if an auth success or failure occured.
        /// </summary>
        public int Timeout { get; set; }

        public Scope Scope { get; set; }

        /// <summary>
        /// The URI (or URL) of the exchange server which exchanges the auth code for access and refresh tokens.
        /// </summary>
        public string ExchangeServerUri { get; set; }

        /// <summary>
        /// The URI (or URL) of where a callback server to receive the auth code will be hosted. e.g. http://localhost:4002
        /// </summary>
        public string HostServerUri { get; set; }

        /// <summary>
        /// Opens the user's browser and visits the exchange server for you, triggering the key exchange. This should be true unless you want to handle the key exchange in a nicer way.
        /// </summary>
        public bool OpenBrowser { get; set; }

        /// <summary>
        /// The HTML to respond with when the callback server has been reached. By default, it is set to close the window on arrival.
        /// </summary>
        public string HtmlResponse { get; set; }

        /// <summary>
        /// Whether or not to show a dialog saying "Is this you?" during the initial key exchange. It should be noted that this would allow a user the opportunity to change accounts.
        /// </summary>
        public bool ShowDialog { get; set; }

        /// <summary>
        /// The maximum amount of times to retry getting a token.
        /// <para/>
        /// A token get is attempted every time you <see cref="GetWebApiAsync"/> and <see cref="RefreshAuthAsync"/>. Increasing this may improve how often these actions succeed - although it won't solve any underlying problems causing a get token failure.
        /// </summary>
        public int MaxGetTokenRetries { get; set; } = 10;

        /// <summary>
        /// Returns a SpotifyWebAPI using the TokenSwapAuth process.
        /// </summary>
        /// <param name="exchangeServerUri">The URI (or URL) of the exchange server which exchanges the auth code for access and refresh tokens.</param>
        /// <param name="scope"></param>
        /// <param name="hostServerUri">The URI (or URL) of where a callback server to receive the auth code will be hosted. e.g. http://localhost:4002</param>
        /// <param name="timeout">The maximum time in seconds to wait for a SpotifyWebAPI to be returned. The timeout is cancelled early regardless if an auth success or failure occured.</param>
        /// <param name="autoRefresh">Access provided by Spotify expires after 1 hour. If true, access will attempt to be silently (without opening a browser) refreshed automatically.</param>
        /// <param name="openBrowser">Opens the user's browser and visits the exchange server for you, triggering the key exchange. This should be true unless you want to handle the key exchange in a nicer way.</param>
        public TokenSwapWebApiFactory(string exchangeServerUri, Scope scope = Scope.None, string hostServerUri = "http://localhost:4002", int timeout = 10, bool autoRefresh = false, bool openBrowser = true)
        {
            AutoRefresh = autoRefresh;
            Timeout = timeout;
            Scope = scope;
            ExchangeServerUri = exchangeServerUri;
            HostServerUri = hostServerUri;
            OpenBrowser = openBrowser;

            OnAccessTokenExpired += async (sender, e) =>
            {
                if (AutoRefresh)
                {
                    await RefreshAuthAsync();
                }
            };
        }

        private Token _lastToken;
        private SpotifyWebAPI _lastWebApi;
        private TokenSwapAuth _lastAuth;

        public class ExchangeReadyEventArgs : EventArgs
        {
            public string ExchangeUri { get; set; }
        }

        /// <summary>
        /// When the URI to get an authorization code is ready to be used to be visited. Not required if <see cref="OpenBrowser"/> is true as the exchange URI will automatically be visited for you.
        /// </summary>
        public event EventHandler<ExchangeReadyEventArgs> OnExchangeReady;

        /// <summary>
        /// Refreshes the access for a SpotifyWebAPI returned by this factory.
        /// </summary>
        /// <returns></returns>
        public async Task RefreshAuthAsync()
        {
            Token token = await _lastAuth.RefreshAuthAsync(_lastToken.RefreshToken);

            if (token == null)
            {
                OnAuthFailure?.Invoke(this, new AuthFailureEventArgs($"Token not returned by server."));
            }
            else if (token.HasError())
            {
                OnAuthFailure?.Invoke(this, new AuthFailureEventArgs($"{token.Error} {token.ErrorDescription}"));
            }
            else if (string.IsNullOrEmpty(token.AccessToken))
            {
                OnAuthFailure?.Invoke(this, new AuthFailureEventArgs("Token had no access token attached."));
            }
            else
            {
                _lastWebApi.AccessToken = token.AccessToken;
                OnAuthSuccess?.Invoke(this, new AuthSuccessEventArgs());
            }
        }

        // By defining empty EventArgs objects, you can specify additional information later on as you see fit and it won't
        // be considered a breaking change to consumers of this API.
        //
        // They don't even need to be constructed for their associated events to be invoked - just pass the static Empty property.
        public class AccessTokenExpiredEventArgs : EventArgs
        {
            public new static AccessTokenExpiredEventArgs Empty { get; } = new();

            public AccessTokenExpiredEventArgs()
            { }
        }

        /// <summary>
        /// When the authorization from Spotify expires. This will only occur if <see cref="AutoRefresh"/> is true.
        /// </summary>
        public event EventHandler<AccessTokenExpiredEventArgs> OnAccessTokenExpired;

        public class AuthSuccessEventArgs : EventArgs
        {
            public new static AuthSuccessEventArgs Empty { get; } = new();

            public AuthSuccessEventArgs()
            { }
        }

        /// <summary>
        /// When an authorization attempt succeeds and gains authorization.
        /// </summary>
        public event EventHandler<AuthSuccessEventArgs> OnAuthSuccess;

        public class AuthFailureEventArgs(string error) : EventArgs
        {
            public new static AuthFailureEventArgs Empty { get; } = new("");

            public string Error { get; } = error;
        }

        /// <summary>
        /// When an authorization attempt fails to gain authorization.
        /// </summary>
        public event EventHandler<AuthFailureEventArgs> OnAuthFailure;

        /// <summary>
        /// Manually triggers the timeout for any ongoing get web API request.
        /// </summary>
        public void CancelGetWebApiRequest()
        {
            if (_webApiTimeoutTimer == null) return;

            // The while loop in GetWebApiSync() will react and trigger the timeout.
            _webApiTimeoutTimer.Stop();
            _webApiTimeoutTimer.Dispose();
            _webApiTimeoutTimer = null;
        }

        private System.Timers.Timer _webApiTimeoutTimer;

        /// <summary>
        /// Gets an authorized and ready to use SpotifyWebAPI by following the SecureAuthorizationCodeAuth process with its current settings.
        /// </summary>
        /// <returns></returns>
        public async Task<SpotifyWebAPI> GetWebApiAsync()
        {
            return await Task<SpotifyWebAPI>.Factory.StartNew(() =>
            {
                bool currentlyAuthorizing = true;

                // Cancel any ongoing get web API requests
                CancelGetWebApiRequest();

                _lastAuth = new TokenSwapAuth(
            exchangeServerUri: ExchangeServerUri,
            serverUri: HostServerUri,
            scope: Scope,
            htmlResponse: HtmlResponse)
                {
                    ShowDialog = ShowDialog,
                    MaxGetTokenRetries = MaxGetTokenRetries,
                    TimeAccessExpiry = AutoRefresh || TimeAccessExpiry
                };
                _lastAuth.AuthReceived += async (sender, response) =>
          {
              if (!string.IsNullOrEmpty(response.Error) || string.IsNullOrEmpty(response.Code))
              {
                  // We only want one auth failure to be fired, if the request timed out then don't bother.
                  if (!_webApiTimeoutTimer.Enabled) return;

                  OnAuthFailure?.Invoke(this, new AuthFailureEventArgs(response.Error));
                  currentlyAuthorizing = false;
                  return;
              }

              _lastToken = await _lastAuth.ExchangeCodeAsync(response.Code);

              if (_lastToken == null || _lastToken.HasError() || string.IsNullOrEmpty(_lastToken.AccessToken))
              {
                  // We only want one auth failure to be fired, if the request timed out then don't bother.
                  if (!_webApiTimeoutTimer.Enabled) return;

                  OnAuthFailure?.Invoke(this, new AuthFailureEventArgs("Exchange token not returned by server."));
                  currentlyAuthorizing = false;
                  return;
              }

              _lastWebApi?.Dispose();
              _lastWebApi = new SpotifyWebAPI()
              {
                  TokenType = _lastToken.TokenType,
                  AccessToken = _lastToken.AccessToken
              };

              _lastAuth.Stop();

              OnAuthSuccess?.Invoke(this, AuthSuccessEventArgs.Empty);
              currentlyAuthorizing = false;
          };
                _lastAuth.OnAccessTokenExpired += async (sender, e) =>
          {
              if (TimeAccessExpiry)
              {
                  OnAccessTokenExpired?.Invoke(sender, AccessTokenExpiredEventArgs.Empty);
              }

              if (AutoRefresh)
              {
                  await RefreshAuthAsync();
              }
          };
                _lastAuth.Start();
                OnExchangeReady?.Invoke(this, new ExchangeReadyEventArgs { ExchangeUri = _lastAuth.GetUri() });
                if (OpenBrowser)
                {
                    _lastAuth.OpenBrowser();
                }

                _webApiTimeoutTimer = new System.Timers.Timer
                {
                    AutoReset = false,
                    Enabled = true,
                    Interval = Timeout * 1000
                };

                while (currentlyAuthorizing && _webApiTimeoutTimer.Enabled) ;

                // If a timeout occurred
                if (_lastWebApi == null && currentlyAuthorizing)
                {
                    OnAuthFailure?.Invoke(this, new AuthFailureEventArgs("Authorization request has timed out."));
                }

                return _lastWebApi;
            });
        }
    }
}