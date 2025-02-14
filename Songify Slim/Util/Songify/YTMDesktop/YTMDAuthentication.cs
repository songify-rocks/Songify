using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Songify_Slim.Util.Songify.YTMDesktop
{
    public class YtmdAuthentication(string baseUrl)
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _baseUrl = baseUrl.TrimEnd('/');

        /// <summary>
        /// Requests an authorization code.
        /// </summary>
        /// <param name="appId">The application ID.</param>
        /// <param name="appName">The name of the application.</param>
        /// <param name="appVersion">The version of the application.</param>
        /// <returns>The authorization code.</returns>
        public async Task<string> RequestAuthCodeAsync(string appId, string appName, string appVersion)
        {
            var payload = new
            {
                appId,
                appName,
                appVersion
            };

            string json = JsonSerializer.Serialize(payload);

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/auth/requestcode",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthCodeResponse>(responseBody);
            return result?.Code ?? throw new Exception("Failed to retrieve auth code.");
        }

        /// <summary>
        /// Requests a token using the authorization code.
        /// </summary>
        /// <param name="appId">The application ID.</param>
        /// <param name="code">The authorization code.</param>
        /// <returns>The token.</returns>
        public async Task<string> RequestTokenAsync(string appId, string code)
        {
            var payload = new
            {
                appId,
                code
            };

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/auth/request",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(responseBody);

            return result?.Token ?? throw new Exception("Failed to retrieve token.");
        }
    }

    /// <summary>
    /// Represents the response for the authorization code request.
    /// </summary>
    public class AuthCodeResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// Represents the response for the token request.
    /// </summary>
    public class TokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}