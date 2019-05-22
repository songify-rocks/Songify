using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Xml;
using Path = System.IO.Path;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow
    {
        private readonly string _path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        private string[] files;
        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            files = System.IO.Directory.GetFiles(_path, "*.shr");
            LbxHistory.Items.Clear();
            foreach (var file in files)
            {
                string temp = "";
                int index = file.LastIndexOf("\\", StringComparison.Ordinal) + 1;
                // Remove everything after the last "-" int the string 
                // which is "- Youtube" and info that music is playing on this tab
                if (index > 0)
                    temp = file.Substring(index, file.Length - index);
                temp = temp.Trim().Replace(".shr", "");
                LbxHistory.Items.Add(temp);
            }
        }

        private void LbxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // reading the XML file, attributes get saved in Settings
            XmlDocument doc = new XmlDocument();
            doc.Load(files[LbxHistory.SelectedIndex]);
            dgvHistorySongs.Items.Clear();
            List<XmlNode> nodes = new List<XmlNode>();

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                nodes.Add(node);
            }

            nodes.Reverse();

            foreach (XmlNode node in nodes)
            {
                if (node.Name == "Song")
                {
                    if (node.Attributes != null)
                    {
                        var data = new Song
                        {
                            Time = UnixTimeStampToDateTime(double.Parse(node.Attributes["Time"]?.InnerText)).ToLongTimeString(),
                            Name = node.InnerText
                        };

                        dgvHistorySongs.Items.Add(data);
                    }
                }
            }
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
    public class Song
    {
        public string Time { get; set; }
        public string Name { get; set; }
    }
}
