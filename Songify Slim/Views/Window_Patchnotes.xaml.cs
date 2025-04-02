using Markdig;
using Octokit;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xaml;
using CefSharp;
using CefSharp.Wpf;
using Application = System.Windows.Application;
using XamlReader = System.Windows.Markup.XamlReader;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class WindowPatchnotes
    {
        private string htmlTemplate = """
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
                                    max-width: 768px;
                                    margin: 0 auto;
                                  }
                                  pre, code {
                                    background-color: #1e1e1e;
                                    color: #f5f5f5;
                                  }
                                </style>
                              </head>
                              <body>
                              <article class="markdown-body" id="markdown-content">Loading patch notes...</article>
                              <script id="md" type="application/json">
                                {{MARKDOWN_CONTENT}}
                              </script>
                              
                              <script>
                                const raw = JSON.parse(document.getElementById("md").textContent);
                                document.getElementById("markdown-content").innerHTML = marked.parse(raw);
                              </script>
                              
                              </body>
                              </html>

                              """;


        // Constructor to initialize the window
        public WindowPatchnotes()
        {
            InitializeComponent();
            WebBrowser.BrowserSettings = new CefSharp.BrowserSettings
            {
                WindowlessFrameRate = 60 // smooth resizing & scrolling
            };

        }

        // Event handler for when the window is loaded
        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a GitHub client to fetch release information
            GitHubClient client = new(new ProductHeaderValue("SongifyInfo"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("songify-rocks", "Songify");

            // Add each release to the ComboBox
            foreach (Release release in releases)
            {
                CbxVersions.Items.Add(new ReleaseObject() { Version = release.TagName, Content = release.Body, Url = release.HtmlUrl });
            }

            // If the application is in beta, fetch and add beta patch notes
            if (App.IsBeta)
            {
                string patchnotes = await WebHelper.GetBetaPatchNotes($"{GlobalObjects.BaseUrl}/beta_update.md");
                CbxVersions.Items.Insert(0, new ReleaseObject
                {
                    Version = $"{GlobalObjects.AppVersion}_beta",
                    Content = patchnotes,
                    Url = ""
                });
            }

            // Select the first item in the ComboBox
            CbxVersions.SelectedIndex = 0;
        }

        // Class to represent a release object with version, content, and URL
        private class ReleaseObject
        {
            public string Version { get; set; }
            public string Content { get; set; }
            public string Url { get; set; }
        }

        // Event handler for when a hyperlink is clicked
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }

        // Command handler to open a hyperlink
        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }

        // Custom XAML schema context to handle namespace compatibility
        private class MyXamlSchemaContext : XamlSchemaContext
        {
            public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
            {
                if (xamlNamespace.Equals("clr-namespace:Markdig.Wpf", StringComparison.Ordinal))
                {
                    compatibleNamespace = $"clr-namespace:Markdig.Wpf;assembly={Assembly.GetAssembly(typeof(Markdig.Wpf.Styles)).FullName}";
                    return true;
                }
                return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
            }
        }

        // Event handler for when the selected item in the ComboBox changes
        private void CbxVersions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string markdown = (string)((ComboBox)sender).SelectedValue;
                string html = htmlTemplate.Replace("{{MARKDOWN_CONTENT}}", JsonSerializer.Serialize(markdown));
                WebBrowser.LoadHtml(html, "http://local.songify/");
            }
            catch (Exception exception)
            {
                Logger.LogStr("PATCHNOTES: Error displaying patch notes");
                Logger.LogExc(exception);
            }
        }
    }
}