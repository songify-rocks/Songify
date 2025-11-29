using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    public static class HttpClientPatchExtensions
    {
        private static readonly HttpMethod PatchMethod = new("PATCH");

        public static Task<HttpResponseMessage> PatchAsync(
            this HttpClient client, string requestUri, HttpContent content)
        {
            HttpRequestMessage req = new(PatchMethod, requestUri) { Content = content };
            return client.SendAsync(req);
        }
    }
}