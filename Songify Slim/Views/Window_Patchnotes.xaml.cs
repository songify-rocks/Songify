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
            string markdownTxt = (string)LbxVersions.SelectedValue;
            markdownTxt = $"{markdownTxt.Split(["Checksum"], StringSplitOptions.None)[0]}";
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

            foreach (Block documentBlock in RtbPatchnotes.Document.Blocks)
            {
                Color themeForeground = (Color)Application.Current.FindResource("MahApps.Colors.ThemeForeground");
                documentBlock.Foreground = new SolidColorBrush(themeForeground);
            }
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

            //// Define the plain text with links
            //string plainText = markdownTxt;

            //// Define the regular expression pattern to match URLs
            //const string urlPattern = @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";

            //// Convert plain text links to hyperlinks
            //var regex = new Regex(urlPattern, RegexOptions.IgnoreCase);
            //var matches = regex.Matches(plainText);
            //int index = 0;
            //var paragraph = new Paragraph();
            //foreach (Match match in matches)
            //{
            //    // Add plain text before the match
            //    paragraph.Inlines.Add(new Run(plainText.Substring(index, match.Index - index)));

            //    // Add hyperlink for the match
            //    var hyperlink = new Hyperlink(new Run(match.Value))
            //    {
            //        NavigateUri = new Uri(match.Value),
            //        TextDecorations = TextDecorations.Underline
            //    };
            //    hyperlink.RequestNavigate += (o, args) =>
            //    {
            //        Process.Start(args.Uri.ToString());
            //        args.Handled = true;
            //    };
            //    paragraph.Inlines.Add(hyperlink);

            //    // Update index for next iteration
            //    index = match.Index + match.Length;
            //}

            //// Add remaining plain text after the last match
            //if (index < plainText.Length)
            //{
            //    paragraph.Inlines.Add(new Run(plainText.Substring(index)));
            //}

            //// Set the paragraph as the content of the RichTextBox
            //RtbPatchnotes.Document.Blocks.Clear();
            //RtbPatchnotes.Document.Blocks.Add(paragraph);
            //RtbPatchnotes.IsDocumentEnabled = true;
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
    }
}