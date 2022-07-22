using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MdXaml;
using Octokit;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for Window_Patchnotes.xaml
    /// </summary>
    public partial class Window_Patchnotes
    {
        GitHubClient client;
        List<ReleaseObject> _releaseList = new List<ReleaseObject>();
        Markdown engine = new Markdown();
        
        public Window_Patchnotes()
        {
            InitializeComponent();
            client = new GitHubClient(new ProductHeaderValue("SongifyInfo"));
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task<IReadOnlyList<Release>> releases = client.Repository.Release.GetAll("songify-rocks", "Songify");
            foreach (Release release in releases.Result)
            {
                LbxVersions.Items.Add(new ReleaseObject() { Version = release.TagName, Content = release.Body });
            }

            LbxVersions.SelectedIndex = 0;
        }
        private void LbxVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string markdownTxt = (string)LbxVersions.SelectedValue;
            markdownTxt = markdownTxt.Split(new[] { "Checksum" }, StringSplitOptions.None)[0];
            FlowDocument document = engine.Transform(markdownTxt);
            engine.HyperlinkCommand.CanExecute(true);
            document.FontFamily = new FontFamily("Sogeo UI");
            RtbPatchnotes.Document = document;
        }

        private class ReleaseObject
        {
            public string Version { get; set; }
            public string Content { get; set; }

        }
    }
}
