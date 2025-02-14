using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    public class YtmdApiClient(string baseUrl)
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> Get(string endpoint)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", Settings.Settings.YtmdToken);
                HttpResponseMessage response = await _httpClient.GetAsync($"{baseUrl}/{endpoint}");

                return response.StatusCode switch
                {
                    HttpStatusCode.InternalServerError => null,
                    HttpStatusCode.ServiceUnavailable => null,
                    HttpStatusCode.OK => await response.Content.ReadAsStringAsync(),
                    _ => null
                };
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            return null;
        }

        public async Task<string> Post(string payload)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", Settings.Settings.YtmdToken);
                StringContent content = new(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync($"{baseUrl}/command", content);
                return response.StatusCode switch
                {
                    HttpStatusCode.InternalServerError => null,
                    HttpStatusCode.ServiceUnavailable => null,
                    HttpStatusCode.OK => await response.Content.ReadAsStringAsync(),
                    _ => null
                };
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
            return null;
        }
    }
}