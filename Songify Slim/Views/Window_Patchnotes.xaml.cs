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

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class WindowPatchnotes
    {
        Markdown _engine = new Markdown();

        public WindowPatchnotes()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("SongifyInfo"));
            Task<IReadOnlyList<Release>> releases = client.Repository.Release.GetAll("songify-rocks", "Songify");
            foreach (Release release in releases.Result)
            {
                LbxVersions.Items.Add(new ReleaseObject { Version = release.TagName, Content = release.Body, Url = release.HtmlUrl});
            }

            LbxVersions.SelectedIndex = 0;
            LbxVersions.ScrollIntoView(LbxVersions.SelectedItem);
        }
        private void LbxVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string markdownTxt = (string)LbxVersions.SelectedValue;
            markdownTxt = markdownTxt.Split(new[] { "Checksum" }, StringSplitOptions.None)[0];
            FlowDocument document = _engine.Transform(markdownTxt);
            _engine.HyperlinkCommand?.CanExecute(true);
            document.FontFamily = new FontFamily("Sogeo UI");
            RtbPatchnotes.Document = document;
            string uri = (LbxVersions.SelectedItem as ReleaseObject)?.Url;
            if (uri != null) Hyperlink.NavigateUri = new Uri(uri);
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
    }
}
