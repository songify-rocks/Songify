using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Songify_Slim.Util.Songify
{
    public static class WebTitleFetcher
    {
        public static async Task<string> GetWebsiteTitleAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            using HttpClient httpClient = new();
            // Optionally add headers if needed (User-Agent, etc.)
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C# WebTitleFetcher");

            // Get the HTML from the specified URL
            string html = await httpClient.GetStringAsync(url);

            // Load the HTML into an HtmlDocument
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Select the <title> node. 
            // Note: Sometimes it's inside <head> but //title will find it anywhere.
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//title");

            string rawTitle = titleNode?.InnerText?.Trim() ?? string.Empty;
            string decodedTitle = HtmlEntity.DeEntitize(rawTitle);
            decodedTitle = decodedTitle.Replace(" - YouTube", "");


            // Return the title text if found; otherwise an empty string or null
            return decodedTitle;
        }
    }
}
