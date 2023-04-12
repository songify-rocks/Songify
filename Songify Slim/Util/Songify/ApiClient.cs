using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    public class ApiClient
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public ApiClient(string baseUrl)
        {
            this._baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        public async Task<string> Get(string endpoint, string uuid)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}.php?uuid={uuid}");
            switch (response.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    return null;
                case HttpStatusCode.ServiceUnavailable:
                    return null;
                case HttpStatusCode.OK:
                    return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            try
            {
                var builder = new UriBuilder($"{_baseUrl}/{endpoint}.php")
                {
                    Query = $"api_key={Settings.Settings.AccessKey}"
                };
                StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(builder.ToString(), content);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        return null;
                    case HttpStatusCode.ServiceUnavailable:
                        return null;
                    case HttpStatusCode.OK:
                        switch (endpoint)
                        {
                            case "song":
                                Logger.LogStr("API: Upload Song: success");
                                break;
                            case "telemetry":
                                Logger.LogStr("API: Telemetry: success");
                                break;
                        }
                        return await response.Content.ReadAsStringAsync();
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            return null;
        }

        public async Task<string> Patch(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod method = new HttpMethod("PATCH");
            HttpRequestMessage request = new HttpRequestMessage(method, $"{_baseUrl}/{endpoint}.php") { Content = content };
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
            return null;
        }

        public async Task<string> Clear(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod mehtod = new HttpMethod("CLEAR");
            HttpRequestMessage request = new HttpRequestMessage(mehtod, $"{_baseUrl}/{endpoint}.php") { Content = content };
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
            return null;
        }
    }
}
