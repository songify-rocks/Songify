using Newtonsoft.Json;
using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    public class ApiClient(string baseUrl)
    {
        private readonly HttpClient _httpClient = SongifyAuthService.HttpClient;

        public async Task<string> GetCanvas(string songId)
        {
            try
            {
                using HttpResponseMessage response = await SendAsync(
                    HttpMethod.Get, $"canvas/{songId}", requireAuth: false).ConfigureAwait(false);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error in API client processing.", e);
            }

            return null;
        }

        public async Task<string> Get(string endpoint, string uuid)
        {
            try
            {
                string query = string.IsNullOrEmpty(uuid) ? null : $"uuid={Uri.EscapeDataString(uuid)}";
                using HttpResponseMessage response = await SendAsync(
                    HttpMethod.Get, endpoint, requireAuth: false, query: query).ConfigureAwait(false);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error in GET request", e);
            }

            return null;
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            try
            {
                using HttpResponseMessage response = await SendAsync(
                    HttpMethod.Post, endpoint, payload, requireAuth: true).ConfigureAwait(false);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        switch (endpoint)
                        {
                            case "song":
                                Logger.Info(LogSource.Api, "Upload Song: success");
                                break;

                            case "telemetry":
                                Logger.Info(LogSource.Api, "Telemetry: success");
                                break;
                        }
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, $"Error posting to {endpoint}", e);
            }
            return null;
        }

        public async Task<string> Patch(string endpoint, string payload)
        {
            try
            {
                using HttpResponseMessage response = await SendAsync(
                    new HttpMethod("PATCH"), endpoint, payload, requireAuth: true).ConfigureAwait(false);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error in patch request.", e);
            }
            return null;
        }

        public async Task<string> Clear(string endpoint, string payload)
        {
            try
            {
                using HttpResponseMessage response = await SendAsync(
                    HttpMethod.Post, endpoint, payload, requireAuth: true).ConfigureAwait(false);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error in Clear request", e);
            }
            return null;
        }

        public async Task<string> PostYtEndpoint(string videoId)
        {
            var payload = new
            {
                videoId = videoId
            };

            string json = JsonConvert.SerializeObject(payload);
            using HttpResponseMessage resp = await SendAsync(
                HttpMethod.Post, "youtube/meta", json, requireAuth: true).ConfigureAwait(false);
            string respJson = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Songify API error {(int)resp.StatusCode}: {respJson}");
            }

            return respJson;
        }

        internal Task<HttpResponseMessage> GetAuthenticated(string endpoint, string query)
        {
            return SendAsync(HttpMethod.Get, endpoint, requireAuth: true, query: query);
        }

        internal Task<HttpResponseMessage> PostAuthenticated(string endpoint, string payload)
        {
            return SendAsync(HttpMethod.Post, endpoint, payload, requireAuth: true);
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string endpoint,
            string payload = null,
            bool requireAuth = false,
            string query = null,
            bool isRetry = false)
        {
            string url = string.IsNullOrEmpty(query)
                ? $"{baseUrl}/{endpoint}"
                : $"{baseUrl}/{endpoint}?{query}";

            using HttpRequestMessage request = new(method, url);
            if (payload != null)
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            if (requireAuth)
            {
                string token = await SongifyAuthService.GetAccessTokenAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (requireAuth && response.StatusCode == HttpStatusCode.Unauthorized && !isRetry)
            {
                response.Dispose();
                SongifyAuthService.Invalidate();
                await SongifyAuthService.EnsureAuthenticatedAsync().ConfigureAwait(false);
                return await SendAsync(method, endpoint, payload, requireAuth, query, isRetry: true)
                    .ConfigureAwait(false);
            }

            return response;
        }
    }
}
