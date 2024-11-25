using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    public class YTMDApiClient(string baseUrl)
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> Get(string endpoint)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", Settings.Settings.YTMDToken);
                HttpResponseMessage response = await _httpClient.GetAsync($"{baseUrl}/{endpoint}");

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
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            return null;
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            try
            {
                UriBuilder builder = new($"{baseUrl}/{endpoint}")
                {
                    Query = $"api_key={Settings.Settings.AccessKey}"
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
    }
}
