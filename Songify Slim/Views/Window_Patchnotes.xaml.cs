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

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class WindowPatchnotes
    {
        // Constructor to initialize the window
        public WindowPatchnotes()
        {
            InitializeComponent();
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
                // Get the selected markdown text
                string markdownTxt = (string)((ComboBox)sender).SelectedValue;
                markdownTxt = $"{markdownTxt.Split(new[] { "Checksum" }, StringSplitOptions.None)[0]}";

                // Convert markdown to XAML
                MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string xaml = Markdown.ToXaml(markdownTxt, pipeline);
                using (MemoryStream stream = new(Encoding.UTF8.GetBytes(xaml)))
                {
                    using XamlXmlReader reader = new(stream, new MyXamlSchemaContext());
                    if (XamlReader.Load(reader) is FlowDocument document)
                    {
                        RtbPatchnotes.Document = document;
                    }
                }

                // Set the foreground color of the document blocks
                foreach (Block documentBlock in RtbPatchnotes.Document.Blocks)
                {
                    Color themeForeground = (Color)Application.Current.FindResource("MahApps.Colors.ThemeForeground");
                    documentBlock.Foreground = new SolidColorBrush(themeForeground);
                }

                // Enable or disable the hyperlink based on the selected item's URL
                string uri = (((ComboBox)sender).SelectedItem as ReleaseObject)?.Url;
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
            catch
            {
                // ignore
            }
        }
    }
}