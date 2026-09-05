using Newtonsoft.Json;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    /// <summary>
    /// In-memory Songify API v3 session. The website-issued account token stays on disk;
    /// the JWT is RAM-only and is obtained via POST /auth.
    /// </summary>
    internal static class SongifyAuthService
    {
        private const int RefreshSkewSeconds = 60;
        private const int DefaultExpiresInSeconds = 3600;

        internal static readonly HttpClient HttpClient = new();

        private static readonly SemaphoreSlim AuthGate = new(1, 1);

        private static string _accessToken;
        private static DateTime _expiresAtUtc = DateTime.MinValue;
        private static bool _loggedMissingCredentials;
        private static bool _loggedAuthUnauthorized;

        public static void Invalidate()
        {
            _accessToken = null;
            _expiresAtUtc = DateTime.MinValue;
        }

        public static async Task<string> GetAccessTokenAsync()
        {
            if (HasValidToken())
                return _accessToken;

            return await EnsureAuthenticatedAsync().ConfigureAwait(false);
        }

        public static async Task<string> EnsureAuthenticatedAsync()
        {
            await AuthGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (HasValidToken())
                    return _accessToken;

                string accountToken = Settings.SongifyApiKey;
                string twitchId = Settings.TwitchUser?.Id;
                if (string.IsNullOrWhiteSpace(accountToken) || string.IsNullOrWhiteSpace(twitchId))
                {
                    if (!_loggedMissingCredentials)
                    {
                        _loggedMissingCredentials = true;
                        Logger.Info(LogSource.Api,
                            "Skipping Songify API /auth: Songify token or Twitch login is missing.");
                    }

                    Invalidate();
                    return null;
                }

                _loggedMissingCredentials = false;

                var body = new
                {
                    twitch_id = twitchId,
                    token = accountToken,
                    uuid = Settings.Uuid
                };

                string json = JsonConvert.SerializeObject(body);
                using HttpRequestMessage request = new(HttpMethod.Post, $"{GlobalObjects.ApiUrl}/auth");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SongifyAuthResponse auth = JsonConvert.DeserializeObject<SongifyAuthResponse>(responseJson);
                    if (string.IsNullOrWhiteSpace(auth?.AccessToken))
                    {
                        Logger.Warning(LogSource.Api, "Songify API /auth returned an empty access token.");
                        Invalidate();
                        return null;
                    }

                    int expiresIn = auth.ExpiresIn > 0 ? auth.ExpiresIn : DefaultExpiresInSeconds;
                    _accessToken = auth.AccessToken;
                    _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
                    _loggedAuthUnauthorized = false;
                    Logger.Info(LogSource.Api, "Songify API authenticated.");
                    return _accessToken;
                }

                Invalidate();
                LogAuthFailure(response.StatusCode, responseJson);
                return null;
            }
            catch (Exception ex)
            {
                Invalidate();
                Logger.Error(LogSource.Api, "Error calling Songify API /auth.", ex);
                return null;
            }
            finally
            {
                AuthGate.Release();
            }
        }

        private static bool HasValidToken()
        {
            return !string.IsNullOrEmpty(_accessToken) &&
                   DateTime.UtcNow < _expiresAtUtc - TimeSpan.FromSeconds(RefreshSkewSeconds);
        }

        private static void LogAuthFailure(HttpStatusCode statusCode, string responseJson)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    if (_loggedAuthUnauthorized)
                        return;
                    _loggedAuthUnauthorized = true;
                    Logger.Warning(LogSource.Api,
                        "Songify API /auth failed: invalid account token. Regenerate it on songify.rocks.");
                    return;

                case HttpStatusCode.BadRequest:
                    Logger.Warning(LogSource.Api, "Songify API /auth failed: bad request body.");
                    return;

                case HttpStatusCode.Conflict:
                    Logger.Warning(LogSource.Api,
                        "Songify API /auth failed: this UUID is owned by another account.");
                    return;

                case (HttpStatusCode)429:
                    Logger.Warning(LogSource.Api, "Songify API /auth failed: rate limited.");
                    return;

                default:
                    Logger.Warning(LogSource.Api,
                        $"Songify API /auth failed: {(int)statusCode} {statusCode}. {TrimForLog(responseJson)}");
                    return;
            }
        }

        private static string TrimForLog(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            text = text.Trim();
            return text.Length <= 200 ? text : text[..200];
        }

        private sealed class SongifyAuthResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
