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
        private readonly string baseURL;
        private readonly HttpClient httpClient;

        public ApiClient(string baseURL)
        {
            this.baseURL = baseURL;
            httpClient = new HttpClient();
        }

        public async Task<string> Get(string endpoint, string uuid)
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{baseURL}/{endpoint}?uuid={uuid}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"{baseURL}/{endpoint}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Patch(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod method = new HttpMethod("PATCH");
            HttpRequestMessage request = new HttpRequestMessage(method, $"{baseURL}/{endpoint}") { Content = content };
            HttpResponseMessage response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Clear(string endpoint, string payload)
        {
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpMethod mehtod = new HttpMethod("CLEAR");
            HttpRequestMessage request = new HttpRequestMessage(mehtod, $"{baseURL}/{endpoint}") { Content = content };
            HttpResponseMessage response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
