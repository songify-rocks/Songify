using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.Songify
{
    public class ApiClient(string baseUrl)
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetCanvas(string songId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{baseUrl}/canvas/{songId}");
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
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
                HttpResponseMessage response = await _httpClient.GetAsync($"{baseUrl}/{endpoint}?uuid={uuid}");
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
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
                UriBuilder builder = new($"{baseUrl}/{endpoint}")
                {
                    Query = $"api_key={Settings.AccessKey}"
                };
                StringContent content = new(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(builder.ToString(), content);
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
                        return await response.Content.ReadAsStringAsync();
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
                UriBuilder builder = new($"{baseUrl}/{endpoint}")
                {
                    Query = $"api_key={Settings.AccessKey}"
                };
                StringContent content = new(payload, Encoding.UTF8, "application/json");
                HttpMethod method = new("PATCH");
                HttpRequestMessage request = new(method, builder.ToString()) { Content = content };
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
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
                UriBuilder builder = new($"{baseUrl}/{endpoint}")
                {
                    Query = $"api_key={Settings.AccessKey}"
                };
                StringContent content = new(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(builder.ToString(), content);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;

                    case HttpStatusCode.ServiceUnavailable:
                        return null;

                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error in Clear request", e);
            }
            return null;
        }
    }
}