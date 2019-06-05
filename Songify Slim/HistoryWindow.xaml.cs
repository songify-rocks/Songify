using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow
    {
        private readonly string _path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "\\history.shr";
        private XDocument _doc;
        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        public void LoadFile()
        {
            dgvHistorySongs.Items.Clear();
            LbxHistory.Items.Clear();

            if (!File.Exists(_path))
                return;

            LbxHistory.Items.Clear();

            _doc = XDocument.Load(_path);
            List<DateTime> list = new List<DateTime>();
            List<string> dateList = new List<string>();

            if (_doc.Root != null)
                foreach (XElement elem in _doc.Root.Elements())
                {
                    dateList.AddRange(elem.Name.ToString().Replace("d_", "").Split('.'));
                    list.Add(new DateTime(int.Parse(dateList[2]), int.Parse(dateList[1]), int.Parse(dateList[0])));
                    dateList.Clear();
                    //LbxHistory.Items.Add(elem.Name.ToString().Replace("d_", ""));
                }

            var orderedList = list.OrderByDescending(time => time.Date);
            foreach (var time in orderedList)
            {
                LbxHistory.Items.Add(time.ToString("dd.MM.yyyy"));
            }

            if (LbxHistory.Items.Count > 0)
            {
                LbxHistory.SelectedIndex = 0;
            }
        }

        private void LbxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsFileLocked(new FileInfo(_path)))
                return;
            if (LbxHistory.SelectedIndex < 0)
                return;
            dgvHistorySongs.Items.Clear();
            _doc = XDocument.Load(_path);
            XElement root = _doc.Descendants("d_" + LbxHistory.SelectedItem).FirstOrDefault();

            List<XElement> nodes = new List<XElement>();

            if (root != null) nodes.AddRange(root.Elements());

            nodes.Reverse();

            foreach (XElement node in nodes)
            {
                if (node.Name == "Song")
                {
                    var data = new Song
                    {
                        Time = UnixTimeStampToDateTime(double.Parse(node.Attribute("Time")?.Value ?? throw new InvalidOperationException())).ToLongTimeString(),
                        Name = node.Value,
                        UnixTimeStamp = long.Parse(node.Attribute("Time")?.Value ?? throw new InvalidOperationException())
                    };

                    dgvHistorySongs.Items.Add(data);
                }
            }
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            string key = "d_" + LbxHistory.SelectedItem;
            XDocument xdoc = XDocument.Load(_path);
            xdoc.Descendants(key)
                .Remove();
            xdoc.Save(_path);
            LoadFile();
        }

        private void DgvItemDelete_Click(object sender, RoutedEventArgs e)
        {
            Song sng = (Song) dgvHistorySongs.SelectedItem;

            var key = sng.UnixTimeStamp;

            XDocument xdoc = XDocument.Load(_path);
            xdoc.Element("History")
                ?.Element("d_" + LbxHistory.SelectedItem)
                ?.Elements("Song")
                .Where(x => (string)x.Attribute("Time") == key.ToString())
                .Remove();
            xdoc.Save(_path);
            LoadFile();
        }
    }
    public class Song
    {
        public string Time { get; set; }
        public string Name { get; set; }
        public long UnixTimeStamp { get; set; }
    }
}
