using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}?uuid={uuid}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Patch(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod method = new HttpMethod("PATCH");
            HttpRequestMessage request = new HttpRequestMessage(method, $"{_baseUrl}/{endpoint}") { Content = content };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Clear(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod mehtod = new HttpMethod("CLEAR");
            HttpRequestMessage request = new HttpRequestMessage(mehtod, $"{_baseUrl}/{endpoint}") { Content = content };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
