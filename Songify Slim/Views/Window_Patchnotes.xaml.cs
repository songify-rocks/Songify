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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xaml;
using Songify_Slim.Util.Settings;
using Application = System.Windows.Application;
using Markdown = Markdig.Wpf.Markdown;
using XamlReader = System.Windows.Markup.XamlReader;
using Windows.UI.Xaml.Controls;
using SelectionChangedEventArgs = Windows.UI.Xaml.Controls.SelectionChangedEventArgs;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class WindowPatchnotes
    {
        public WindowPatchnotes()
        {
            InitializeComponent();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GitHubClient client = new(new ProductHeaderValue("SongifyInfo"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("songify-rocks", "Songify");
            foreach (Release release in releases)
            {
                LbxVersions.Items.Add(new ReleaseObject { Version = release.TagName, Content = release.Body, Url = release.HtmlUrl });
            }

            if (App.IsBeta)
            {
                string patchnotes = await WebHelper.GetBetaPatchNotes($"{GlobalObjects.BaseUrl}/beta_update.md");

                LbxVersions.Items.Insert(0, new ReleaseObject
                {
                    Version = $"{GlobalObjects.AppVersion}_beta",
                    Content = patchnotes,
                    Url = ""
                });
            }

            LbxVersions.SelectedIndex = 0;
            LbxVersions.ScrollIntoView(LbxVersions.SelectedItem);
        }

        private void LbxVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string markdownTxt = (string)LbxVersions.SelectedValue;
            //markdownTxt = $"{markdownTxt.Split(new[] { "Checksum" }, StringSplitOptions.None)[0]}";
            //MarkdownPipeline pipeline = new MarkdownPipelineBuilder().Build();
            //string xaml = Markdown.ToXaml(markdownTxt, pipeline);
            //using (MemoryStream stream = new(Encoding.UTF8.GetBytes(xaml)))
            //{
            //    using (XamlXmlReader reader = new(stream, new MyXamlSchemaContext()))
            //    {
            //        if (XamlReader.Load(reader) is FlowDocument document)
            //        {
            //            RtbPatchnotes.Document = document;
            //        }
            //    }
            //}

            //foreach (Block documentBlock in RtbPatchnotes.Document.Blocks)
            //{
            //    Color themeForeground = (Color)Application.Current.FindResource("MahApps.Colors.ThemeForeground");
            //    documentBlock.Foreground = new SolidColorBrush(themeForeground);
            //}
            //string uri = (LbxVersions.SelectedItem as ReleaseObject)?.Url;
            //if (!string.IsNullOrWhiteSpace(uri))
            //{
            //    Hyperlink.IsEnabled = true;
            //    Hyperlink.NavigateUri = new Uri(uri);
            //}
            //else
            //{
            //    Hyperlink.IsEnabled = false;
            //}


        }

        private class ReleaseObject
        {
            public string Version { get; set; }
            public string Content { get; set; }
            public string Url { get; set; }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }

        class MyXamlSchemaContext : XamlSchemaContext
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

        private async void LbxVersions_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            string markdownTxt = (string)LbxVersions.SelectedValue;

            // Convert Markdown to HTML
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string htmlContent = Markdig.Markdown.ToHtml(markdownTxt, pipeline);

            // Load HTML Template and replace placeholder with content
            const string htmlTemplate = """
                                                <html>
                                            <head>
                                            <style>
                                                body {
                                                    font-family: Arial, sans-serif;
                                                    color: #e0e0e0; /* Light gray text for dark background */
                                                    background-color: #252525; /* Dark background for body */
                                                    line-height: 1.6;
                                                }
                                                code {
                                                    background-color: #2d2d2d; /* Slightly darker background for code elements */
                                                    padding: 2px 4px;
                                                    border-radius: 3px;
                                                    font-family: Consolas, 'Courier New', monospace;
                                                    color: #ff9d00; /* Slightly orange color for code text */
                                                }
                                                pre {
                                                    background-color: #2d2d2d; /* Dark background for preformatted text */
                                                    color: #f8f8f2; /* Light color for code */
                                                    padding: 10px;
                                                    border-radius: 5px;
                                                    overflow-x: auto;
                                                }
                                                a {
                                                    color: #4e9efc; /* Bright blue for links */
                                                    text-decoration: none;
                                                }
                                                a:hover {
                                                    text-decoration: underline;
                                                }
                                                h1, h2, h3, h4, h5, h6 {
                                                    color: #e6e6e6; /* Light color for headers */
                                                }
                                                blockquote {
                                                    border-left: 4px solid #757575; /* Light gray border for blockquotes */
                                                    padding-left: 10px;
                                                    color: #b0b0b0; /* Gray color for blockquote text */
                                                    margin-left: 0;
                                                    margin-right: 0;
                                                }
                                                ul, ol {
                                                    margin-left: 20px;
                                                    color: #d0d0d0; /* Light color for list items */
                                                }
                                                table {
                                                    width: 100%;
                                                    border-collapse: collapse;
                                                    margin-top: 20px;
                                                    background-color: #252525; /* Dark background for table */
                                                }
                                                th, td {
                                                    border: 1px solid #444444; /* Border for table cells */
                                                    padding: 8px;
                                                    color: #e0e0e0; /* Light color for text */
                                                }
                                                th {
                                                    background-color: #333333; /* Slightly lighter background for header cells */
                                                }
                                            </style>
                                        </head>
                                        <body>
                                            {{Content}}
                                        </body>
                                        </html>
                                        
                                        """;

            // Replace placeholder with HTML content
            string finalHtml = htmlTemplate.Replace("{{Content}}", htmlContent);
            // Ensure WebView2 is initialized
            await webView.EnsureCoreWebView2Async();
            // Navigate WebView to generated HTML
            webView.NavigateToString(finalHtml);

            // Handle hyperlink, if applicable
            string uri = (LbxVersions.SelectedItem as ReleaseObject)?.Url;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Hyperlink.IsEnabled = true;
                Hyperlink.NavigateUri = new Uri(uri);
            }
            else
            {
                Hyperlink.IsEnabled = false;
            }
        }
    }
}
