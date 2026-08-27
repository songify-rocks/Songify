using Microsoft.Web.WebView2.Core;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Color = System.Drawing.Color;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>

    public partial class WindowPatchnotes
    {
        private const string BetaNotesRawUrl =
            "https://raw.githubusercontent.com/songify-rocks/Songify/refs/heads/feature/wpfui-shell/docs/releases/beta_update.md";

        private const string BetaNotesPageUrl =
            "https://github.com/songify-rocks/Songify/blob/feature/wpfui-shell/docs/releases/beta_update.md";

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
            ThemeHandler.ApplyTheme();
            // Set webview2 background color to #0d1117
            WebBrowser.DefaultBackgroundColor = Color.FromArgb(13, 17, 23);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CbxVersions.Items.Clear();

                Task<List<GitHubReleaseDto>> releasesTask = FetchGitHubReleasesHtmlAsync("songify-rocks", "Songify");
                Task<string> betaNotesTask = App.IsBeta
                    ? FetchBetaMarkdownAsync()
                    : Task.FromResult<string>(null);

                await Task.WhenAll(releasesTask, betaNotesTask);

                if (App.IsBeta && !string.IsNullOrWhiteSpace(betaNotesTask.Result))
                {
                    CbxVersions.Items.Add(new ReleaseObject
                    {
                        Version = "2.0.0 Beta",
                        Content = betaNotesTask.Result,
                        IsMarkdown = true,
                        Url = BetaNotesPageUrl
                    });
                }

                List<GitHubReleaseDto> releases = releasesTask.Result;
                if (!App.IsBeta)
                    releases.RemoveAll(r => r.IsPrelease);

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
                    await AppDialog.ShowAsync(
                        "Missing WebView2",
                        "WebView2 Runtime is not installed. Opening patch notes in your browser instead.");

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

        private static async Task<string> FetchBetaMarkdownAsync()
        {
            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SongifyInfo");
                string markdown = await client.GetStringAsync(BetaNotesRawUrl);
                return string.IsNullOrWhiteSpace(markdown) ? null : markdown;
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Patch notes: Error loading beta markdown", ex);
                return null;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ShellHelper.OpenUrl("https://github.com/songify-rocks/songify/releases");
        }
    }
}