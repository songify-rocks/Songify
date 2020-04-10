using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        FileSystemWatcher watcher;

        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set the buttons to match the settings
                Tglbtn_Save.IsChecked = Settings.SaveHistory;
                Tglbtn_Upload.IsChecked = Settings.UploadHistory;

                if (Settings.SaveHistory)
                    Tglbtn_Save.Content = "Save ✔️";
                else
                    Tglbtn_Save.Content = "Save ❌";

                if (Settings.UploadHistory)
                    Tglbtn_Upload.Content = "Upload ✔️";
                else
                    Tglbtn_Upload.Content = "Upload ❌";

                // listen to changes made to the history.shr file
                watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location),
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "history.shr",
                    EnableRaisingEvents = true
                };

                watcher.Changed += new FileSystemEventHandler(OnChanged);

                LoadFile();

            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            LoadFile();
        }

        public void LoadFile()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    _doc = new XDocument(new XElement("History", new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy"))));
                    _doc.Save(_path);
                }

                //// Checks if the file is locked, if not the datagrids gets cleared and the file is read
                //if (IsFileLocked(new FileInfo(_path)))
                //    return;


                dgvHistorySongs.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,
                                new Action(() => { dgvHistorySongs.Items.Clear(); }));
                LbxHistory.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,
                                new Action(() => { LbxHistory.Items.Clear(); }));

                _doc = XDocument.Load(_path);
                List<DateTime> list = new List<DateTime>();
                List<string> dateList = new List<string>();

                if (_doc.Root != null)
                    foreach (XElement elem in _doc.Root.Elements())
                    {
                        dateList.AddRange(elem.Name.ToString().Replace("d_", "").Split('.'));
                        list.Add(new DateTime(int.Parse(dateList[2]), int.Parse(dateList[1]), int.Parse(dateList[0])));
                        dateList.Clear();
                    }

                IOrderedEnumerable<DateTime> orderedList = list.OrderByDescending(time => time.Date);
                foreach (DateTime time in orderedList)
                {
                    LbxHistory.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,
                                new Action(() => { LbxHistory.Items.Add(time.ToString("dd.MM.yyyy")); }));
                }

                if (LbxHistory.Items.Count > 0)
                {
                    LbxHistory.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,
                                new Action(() => { LbxHistory.SelectedIndex = 0; }));
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void LbxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (IsFileLocked(new FileInfo(_path)))
            //{
            //    watcher.EnableRaisingEvents = true;
            //    return;
            //}

            if (LbxHistory.SelectedIndex < 0)
            {
                return;
            }
            if (_doc == null)
                return;

            dgvHistorySongs.Items.Clear();
            XElement root = _doc.Descendants("d_" + LbxHistory.SelectedItem).FirstOrDefault();

            List<XElement> nodes = new List<XElement>();

            if (root != null) nodes.AddRange(root.Elements());

            nodes.Reverse();

            foreach (XElement node in nodes)
            {
                if (node.Name == "Song")
                {
                    Song data = new Song
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
            watcher.EnableRaisingEvents = false;
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (Exception ex)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                Logger.LogExc(ex);
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
            if (dgvHistorySongs.SelectedItem == null)
                return;

            Song sng = (Song)dgvHistorySongs.SelectedItem;

            long key = sng.UnixTimeStamp;

            XDocument xdoc = XDocument.Load(_path);
            xdoc.Element("History")
                ?.Element("d_" + LbxHistory.SelectedItem)
                ?.Elements("Song")
                .Where(x => (string)x.Attribute("Time") == key.ToString())
                .Remove();
            xdoc.Save(_path);
            LoadFile();
        }

        private void Tglbtn_Save_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            Settings.SaveHistory = (bool)Tglbtn_Save.IsChecked;

            if ((bool)Tglbtn_Save.IsChecked)
            {
                Tglbtn_Save.Content = "Save ✔️";
                Lbl_Status.Content = "History Save Enabled ✔️";

            }
            else
            {
                Tglbtn_Save.Content = "Save ❌";
                Lbl_Status.Content = "History Save Disabled ❌";

            }
        }

        private void Tglbtn_Upload_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            Settings.UploadHistory = (bool)Tglbtn_Upload.IsChecked;

            if ((bool)Tglbtn_Upload.IsChecked)
            {
                Tglbtn_Upload.Content = "Upload ✔️";
                Lbl_Status.Content = "History Upload Enabled ✔️";

            }
            else
            {
                Tglbtn_Upload.Content = "Upload ❌";
                Lbl_Status.Content = "History Upload Disabled ❌";
            }
        }

        private void Btn_CpyHistoryURL_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject("https://songify.rocks/history.php?id=" + Settings.Uuid);
            Lbl_Status.Content = "History URL copied to Clipboard";
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            watcher.Dispose();
        }
    }
    public class Song
    {
        public string Time { get; set; }
        public string Name { get; set; }
        public long UnixTimeStamp { get; set; }
    }
}
