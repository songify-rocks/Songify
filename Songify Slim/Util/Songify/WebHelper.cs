using Songify_Slim.Util.General;
using Swan.Formatters;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Songify_Slim.Util.Songify.APIs;

namespace Songify_Slim.Util.Songify;

internal static class WebHelper
{
    /// <summary>
    ///     This Class is a helper class to reduce repeatedly used code across multiple classes
    /// </summary>

    public static async Task<string> GetBetaPatchNotes(string url)
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return content;
    }
}