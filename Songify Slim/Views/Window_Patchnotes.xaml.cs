using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Octokit;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.APIs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xaml;
using Windows.UI.Xaml.Controls;
using Color = System.Drawing.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>

    public partial class WindowPatchnotes
    {
        // One template for both GitHub HTML and beta markdown
        private readonly string htmlTemplate = """
                                               <!DOCTYPE html>
                                               <html>
                                               <head>
                                                 <meta charset="utf-8" />
                                                 <title>Patch Notes</title>

                                                 <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/github-markdown-css/github-markdown-dark.min.css">
                                                 <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>

                                                 <style>
                                                   body {
                                                     background-color: #0d1117 !important;
                                                     color: #c9d1d9 !important;
                                                     font-family: system-ui, sans-serif !important;
                                                     padding: 2rem !important;
                                                   }
                                                   .markdown-body {
                                                     max-width: 900px;
                                                     margin: 0 auto;
                                                   }
                                                   pre, code {
                                                     background-color: #1e1e1e;
                                                     color: #f5f5f5;
                                                   }
                                                   a { color: #58a6ff; }
                                                 </style>
                                               </head>
                                               <body>
                                                 <article class="markdown-body" id="content">Loading patch notes...</article>

                                                 <script id="payload" type="application/json">
                                                   {{PAYLOAD_JSON}}
                                                 </script>

                                                 <script>
                                                   const payload = JSON.parse(document.getElementById("payload").textContent);
                                                   const el = document.getElementById("content");

                                                   if (payload.isMarkdown) {
                                                     el.innerHTML = marked.parse(payload.content || "");
                                                   } else {
                                                     // GitHub already provides sanitized HTML in body_html
                                                     el.innerHTML = payload.content || "";
                                                   }
                                                 </script>
                                               </body>
                                               </html>
                                               """;

        public WindowPatchnotes()
        {
            InitializeComponent();
            // Set webview2 background color to #0d1117
            WebBrowser.DefaultBackgroundColor = Color.FromArgb(13, 17, 23);
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Optional: clear in case window is reused
                CbxVersions.Items.Clear();

                // Fetch releases with body_html
                List<GitHubReleaseDto> releases = await FetchGitHubReleasesHtmlAsync("songify-rocks", "Songify");

                // if App.IsBeta is false, filter out pre-releases
                if (!App.IsBeta)
                {
                    releases.RemoveAll(r => r.IsPrelease);
                }

                foreach (GitHubReleaseDto r in releases)
                {
                    CbxVersions.Items.Add(new ReleaseObject
                    {
                        Version = r.TagName,
                        Content = r.BodyHtml ?? "",
                        IsMarkdown = false,
                        Url = r.HtmlUrl ?? ""
                    });
                }

                // Select first
                if (CbxVersions.Items.Count > 0)
                    CbxVersions.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Patch notes: Error loading patch notes list", ex);
            }
        }

        private async void CbxVersions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CbxVersions.SelectedItem is not ReleaseObject ro)
                    return;

                var payload = new
                {
                    isMarkdown = ro.IsMarkdown,
                    content = ro.Content
                };

                string html = htmlTemplate.Replace("{{PAYLOAD_JSON}}", JsonSerializer.Serialize(payload));

                try
                {
                    await WebBrowser.EnsureCoreWebView2Async(null);
                    WebBrowser.NavigateToString(html);
                }
                catch (WebView2RuntimeNotFoundException)
                {
                    MessageBox.Show(
                        "WebView2 Runtime is not installed. Opening patch notes in your browser instead.",
                        "Missing WebView2",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Fallback: prefer the actual GitHub release page if we have it
                    string url = string.IsNullOrWhiteSpace(ro.Url)
                        ? "https://github.com/songify-rocks/Songify/releases"
                        : ro.Url;

                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Patch notes: Error displaying patch notes", ex);
            }
        }

        private class ReleaseObject
        {
            public string Version { get; set; }
            public string Content { get; set; } // HTML or Markdown depending on IsMarkdown
            public bool IsMarkdown { get; set; }
            public string Url { get; set; }

            public override string ToString() => Version; // makes ComboBox show Version by default
        }

        // Minimal DTO for GitHub API (releases endpoint)
        private sealed class GitHubReleaseDto
        {
            [JsonPropertyName("tag_name")] public string TagName { get; set; }

            [JsonPropertyName("html_url")] public string HtmlUrl { get; set; }

            [JsonPropertyName("body_html")] public string BodyHtml { get; set; }

            [JsonPropertyName("prerelease")] public bool IsPrelease { get; set; }
        }

        // Fetch list of releases including body_html
        private static async Task<List<GitHubReleaseDto>> FetchGitHubReleasesHtmlAsync(string owner, string repo)
        {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(15);

            // GitHub requires a User-Agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SongifyInfo");

            // This media type returns body_html in the response
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3.html+json");

            string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
            string json = await client.GetStringAsync(url);

            List<GitHubReleaseDto> releases = JsonSerializer.Deserialize<List<GitHubReleaseDto>>(json) ??
                                              new List<GitHubReleaseDto>();

            // Keep same ordering as GitHub returns (usually newest first)
            return releases;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/songify-rocks/songify/releases");
        }
    }
}