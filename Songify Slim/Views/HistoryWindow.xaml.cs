using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow
    {
        private FileSystemWatcher _watcher;
        private string _selectedDateKey;

        public HistoryWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await HistoryStore.MigrateLegacyIfNeededAsync(this);

                Tglbtn_Save.IsChecked = Settings.SaveHistory;
                Tglbtn_Save.Content = Settings.SaveHistory
                    ? $"{Properties.Resources.common_save} ✓"
                    : $"{Properties.Resources.common_save}";

                _watcher = new FileSystemWatcher
                {
                    Path = AppPaths.GetAppDirectory(),
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = Path.GetFileName(HistoryStore.FilePath),
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnChanged;

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
                HistoryStore.EnsureReady();

                dgvHistorySongs.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    () => { dgvHistorySongs.Items.Clear(); });
                LbxHistory.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    () => { LbxHistory.Items.Clear(); });

                foreach ((string dateKey, DateTime date, int _) in HistoryStore.GetDateSummaries())
                {
                    string display = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                    LbxHistory.Dispatcher.Invoke(
                        DispatcherPriority.Normal,
                        () => { LbxHistory.Items.Add(new HistoryDayListItem(dateKey, display)); });
                }

                if (LbxHistory.Items.Count > 0)
                    LbxHistory.Dispatcher.Invoke(
                        DispatcherPriority.Normal,
                        () => { LbxHistory.SelectedIndex = 0; });
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void LbxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbxHistory.SelectedItem is not HistoryDayListItem day)
            {
                _selectedDateKey = null;
                return;
            }

            _selectedDateKey = day.DateKey;
            dgvHistorySongs.Items.Clear();

            foreach (HistorySongRecord record in HistoryStore.GetSongsForDate(day.DateKey))
            {
                dgvHistorySongs.Items.Add(new Song
                {
                    Time = UnixTimeStampToDateTime(record.Time).ToLongTimeString(),
                    Name = record.Song,
                    UnixTimeStamp = record.Time
                });
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedDateKey))
                return;
            HistoryStore.DeleteDate(_selectedDateKey);
            LoadFile();
        }

        private void DgvItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvHistorySongs.SelectedItem is not Song sng || string.IsNullOrEmpty(_selectedDateKey))
                return;

            HistoryStore.DeleteSong(_selectedDateKey, sng.UnixTimeStamp);
            LoadFile();
        }

        private void Tglbtn_Save_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (Tglbtn_Save.IsChecked != null)
            {
                Settings.SaveHistory = (bool)Tglbtn_Save.IsChecked;

                if ((bool)Tglbtn_Save.IsChecked)
                {
                    Tglbtn_Save.Content = "Save ✓";
                    Lbl_Status.Content = "History Save Enabled ✓";
                }
                else
                {
                    Tglbtn_Save.Content = "Save";
                    Lbl_Status.Content = "History Save Disabled";
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _watcher?.Dispose();
        }

        private sealed class HistoryDayListItem(string dateKey, string display)
        {
            public string DateKey { get; } = dateKey;
            public override string ToString() => display;
        }
    }

    public class Song
    {
        public string Time { get; set; }
        public string Name { get; set; }
        public long UnixTimeStamp { get; set; }
    }
}
