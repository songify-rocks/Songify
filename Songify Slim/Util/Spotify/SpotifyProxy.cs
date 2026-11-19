using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Spotify
{
    /// <summary>
    /// Single entry point for all HTTP traffic to Spotify (Web API, accounts/OAuth, open.spotify.com).
    /// Routes requests through the user-configured proxy when enabled. Proxy changes in settings are
    /// picked up on the next request, so existing clients do not need to be recreated.
    /// </summary>
    public static class SpotifyProxy
    {
        private static readonly object Gate = new();
        private static HttpMessageInvoker _invoker;
        private static string _signature;

        /// <summary>Shared client for every Spotify request. Never dispose it.</summary>
        public static readonly HttpClient HttpClient = new(new SwitchingHandler(), disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(100)
        };

        /// <summary>
        /// Handler for callers that need their own HttpClient (base address, default headers) but must
        /// still follow the proxy settings. Create the HttpClient with disposeHandler: false.
        /// </summary>
        public static HttpMessageHandler CreateHandler() => new SwitchingHandler();

        public static SpotifyClientConfig CreateClientConfig() =>
            SpotifyClientConfig.CreateDefault().WithHTTPClient(new NetHttpClient(HttpClient));

        public static OAuthClient CreateOAuthClient() => new(CreateClientConfig());

        /// <summary>
        /// Parses a proxy address such as "127.0.0.1:8080", "http://host:3128" or "socks5://127.0.0.1:1080".
        /// Returns null when the address is empty or invalid.
        /// </summary>
        public static Uri ParseAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            string value = address.Trim();
            if (!value.Contains("://"))
                value = "http://" + value;

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                return null;

            if (string.IsNullOrEmpty(uri.Host))
                return null;

            return uri.Scheme.ToLowerInvariant() switch
            {
                "http" or "https" or "socks4" or "socks4a" or "socks5" => uri,
                _ => null
            };
        }

        /// <summary>
        /// Sends a lightweight request to the Spotify Web API through the current proxy settings.
        /// Returns a human readable result.
        /// </summary>
        public static async Task<(bool Success, string Message)> TestAsync(CancellationToken ct = default)
        {
            if (Settings.SpotifyProxyEnabled && ParseAddress(Settings.SpotifyProxyAddress) == null)
                return (false, "Invalid proxy address. Use host:port, http://host:port or socks5://host:port.");

            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, "https://api.spotify.com/v1/me");
                if (!string.IsNullOrWhiteSpace(Settings.SpotifyAccessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.SpotifyAccessToken);

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(15));

                using HttpResponseMessage response = await HttpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int status = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                    return (true, $"Connected to Spotify ({status}).");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return (true, $"Spotify reachable ({status}). Link your Spotify account to finish the check.");

                if (body.Length > 200)
                    body = body.Substring(0, 197) + "...";
                return (false, $"Spotify answered {status}: {body}");
            }
            catch (Exception ex)
            {
                Exception inner = ex.GetBaseException();
                return (false, "Connection failed: " + inner.Message);
            }
        }

        private static HttpMessageInvoker GetInvoker()
        {
            bool enabled = Settings.SpotifyProxyEnabled;
            string address = Settings.SpotifyProxyAddress ?? "";
            string user = Settings.SpotifyProxyUsername ?? "";
            string password = Settings.SpotifyProxyPassword ?? "";
            string signature = enabled ? $"{address}\n{user}\n{password}" : "";

            HttpMessageInvoker invoker = _invoker;
            if (invoker != null && signature == _signature)
                return invoker;

            lock (Gate)
            {
                if (_invoker != null && signature == _signature)
                    return _invoker;

                HttpMessageInvoker old = _invoker;
                _invoker = new HttpMessageInvoker(CreateHandler(enabled, address, user, password), disposeHandler: true);
                _signature = signature;

                if (old != null)
                {
                    // Let in-flight requests on the previous handler finish before releasing its connections.
                    _ = Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(_ => old.Dispose(), TaskScheduler.Default);
                }

                return _invoker;
            }
        }

        private static SocketsHttpHandler CreateHandler(bool enabled, string address, string user, string password)
        {
            SocketsHttpHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            if (!enabled)
                return handler; // System proxy, same as before this setting existed.

            Uri proxyUri = ParseAddress(address);
            if (proxyUri == null)
            {
                Logger.Warning(LogSource.Spotify,
                    $"Spotify proxy is enabled but the address '{address}' is invalid. Connecting without proxy.");
                handler.UseProxy = false;
                return handler;
            }

            WebProxy proxy = new(proxyUri) { BypassProxyOnLocal = false };
            if (!string.IsNullOrEmpty(user))
                proxy.Credentials = new NetworkCredential(user, password);

            handler.Proxy = proxy;
            handler.UseProxy = true;

            Logger.Info(LogSource.Spotify, $"Spotify requests are routed through proxy {proxyUri.Scheme}://{proxyUri.Host}:{proxyUri.Port}");
            return handler;
        }

        private sealed class SwitchingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                GetInvoker().SendAsync(request, cancellationToken);
        }
    }
}
