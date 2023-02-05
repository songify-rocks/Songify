using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MdXaml;
using Octokit;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class Window_Patchnotes
    {
        Markdown engine = new Markdown();

        public Window_Patchnotes()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("SongifyInfo"));
            Task<IReadOnlyList<Release>> releases = client.Repository.Release.GetAll("songify-rocks", "Songify");
            foreach (Release release in releases.Result)
            {
                LbxVersions.Items.Add(new ReleaseObject { Version = release.TagName, Content = release.Body, URL = release.HtmlUrl});
            }

            LbxVersions.SelectedIndex = 0;
            LbxVersions.ScrollIntoView(LbxVersions.SelectedItem);
        }
        private void LbxVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string markdownTxt = (string)LbxVersions.SelectedValue;
            markdownTxt = markdownTxt.Split(new[] { "Checksum" }, StringSplitOptions.None)[0];
            FlowDocument document = engine.Transform(markdownTxt);
            engine.HyperlinkCommand.CanExecute(true);
            document.FontFamily = new FontFamily("Sogeo UI");
            RtbPatchnotes.Document = document;
            string uri = (LbxVersions.SelectedItem as ReleaseObject)?.URL;
            Hyperlink.NavigateUri = new Uri(uri);
        }

        private class ReleaseObject
        {
            public string Version { get; set; }
            public string Content { get; set; }
            public string URL { get; set; }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start((sender as Hyperlink).NavigateUri.ToString());
        }
    }
}
