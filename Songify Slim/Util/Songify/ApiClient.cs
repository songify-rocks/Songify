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
            var url = $"{baseURL}/{endpoint}?uuid={uuid}";
            //Debug.WriteLine(url);
            var response = await httpClient.GetAsync($"{baseURL}/{endpoint}?uuid={uuid}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Post(string endpoint, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{baseURL}/{endpoint}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Patch(string endpoint, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, $"{baseURL}/{endpoint}") { Content = content };
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Clear(string endpoint, string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var mehtod = new HttpMethod("CLEAR");
            var request = new HttpRequestMessage(mehtod, $"{baseURL}/{endpoint}") { Content = content };
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
