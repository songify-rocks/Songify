using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.ViewModels;
using Songify_Slim.Views;

namespace Songify_Slim.Views.WPFUI.ViewModels;

public sealed class HistoryDateItem
{
    public string DateKey { get; init; }
    public string Day { get; init; }
    public string Month { get; init; }
    public string Weekday { get; init; }
    public int SongCount { get; init; }
    public string CountLabel => SongCount == 1 ? "1 song" : $"{SongCount} songs";
}

public sealed class HistorySongItem
{
    public string Time { get; init; }
    public string Title { get; init; }
    public string Artist { get; init; }
    public string FullName { get; init; }
    public long UnixTimeStamp { get; init; }
    public Song Source { get; init; }
}

public sealed class HistoryViewModel : INotifyPropertyChanged
{
    private HistoryDateItem _selectedDate;
    private HistorySongItem _selectedSong;
    private bool _saveHistory;
    private bool _uploadHistory;
    private string _statusMessage = "";
    private XDocument _doc;

    public HistoryViewModel()
    {
        HistoryPath = Path.Combine(AppPaths.GetAppDirectory(), "history.shr");
        DateList = new ObservableCollection<HistoryDateItem>();
        Songs = new ObservableCollection<HistorySongItem>();

        RefreshCommand = new RelayCommand(Refresh);
        CopyHistoryUrlCommand = new RelayCommand(CopyHistoryUrl);
        DeleteDateCommand = new RelayCommand(DeleteSelectedDate, () => SelectedDate != null);
        DeleteSongCommand = new RelayCommand(DeleteSelectedSong, () => SelectedSong != null);
    }

    public string HistoryPath { get; }

    public ObservableCollection<HistoryDateItem> DateList { get; }
    public ObservableCollection<HistorySongItem> Songs { get; }

    public bool HasDates => DateList.Count > 0;
    public bool HasSongs => Songs.Count > 0;
    public bool HasNoSongs => SelectedDate != null && Songs.Count == 0;
    public string SelectedDateTitle => SelectedDate == null
        ? (Application.Current?.TryFindResource("window_history_select_day") as string ?? "Select a day")
        : $"{SelectedDate.Weekday}, {SelectedDate.Day} {SelectedDate.Month}";

    public HistoryDateItem SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (ReferenceEquals(_selectedDate, value)) return;
            _selectedDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDateTitle));
            OnPropertyChanged(nameof(HasNoSongs));
            CommandManager.InvalidateRequerySuggested();
            LoadSongsForSelectedDate();
        }
    }

    public HistorySongItem SelectedSong
    {
        get => _selectedSong;
        set
        {
            if (ReferenceEquals(_selectedSong, value)) return;
            _selectedSong = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool SaveHistory
    {
        get => _saveHistory;
        set
        {
            if (_saveHistory == value) return;
            _saveHistory = value;
            Settings.SaveHistory = value;
            OnPropertyChanged();
            StatusMessage = value
                ? (Application.Current?.TryFindResource("window_history_saving_on") as string ?? "Saving history locally")
                : (Application.Current?.TryFindResource("window_history_saving_off") as string ?? "Local history saving is off");
        }
    }

    public bool UploadHistory
    {
        get => _uploadHistory;
        set
        {
            if (_uploadHistory == value) return;
            _uploadHistory = value;
            Settings.UploadHistory = value;
            OnPropertyChanged();
            StatusMessage = value
                ? (Application.Current?.TryFindResource("window_history_upload_on") as string ?? "Uploading history")
                : (Application.Current?.TryFindResource("window_history_upload_off") as string ?? "History upload is off");
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value ?? ""; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CopyHistoryUrlCommand { get; }
    public ICommand DeleteDateCommand { get; }
    public ICommand DeleteSongCommand { get; }

    public void LoadFromFile()
    {
        Application.Current?.Dispatcher.Invoke(() => LoadFile());
    }

    public void LoadFile()
    {
        try
        {
            if (!File.Exists(HistoryPath))
            {
                _doc = new XDocument(new XElement("History",
                    new XElement("d_" + DateTime.Now.ToString("dd.MM.yyyy"))));
                _doc.Save(HistoryPath);
            }

            string keepKey = _selectedDate?.DateKey;
            DateList.Clear();
            Songs.Clear();

            _doc = XDocument.Load(HistoryPath);
            var dates = new List<(DateTime Dt, string Key, int Count)>();

            if (_doc.Root != null)
            {
                foreach (XElement elem in _doc.Root.Elements())
                {
                    string key = elem.Name.ToString().Replace("d_", "");
                    string[] parts = key.Split('.');
                    if (parts.Length != 3) continue;
                    if (!int.TryParse(parts[0], out int day) ||
                        !int.TryParse(parts[1], out int month) ||
                        !int.TryParse(parts[2], out int year))
                        continue;

                    int count = elem.Elements("Song").Count(s => !string.IsNullOrEmpty(s.Attribute("Time")?.Value));
                    dates.Add((new DateTime(year, month, day), key, count));
                }
            }

            foreach ((DateTime dt, string key, int count) in dates.OrderByDescending(t => t.Dt.Date))
            {
                DateList.Add(new HistoryDateItem
                {
                    DateKey = key,
                    Day = dt.Day.ToString("00"),
                    Month = dt.ToString("MMM", CultureInfo.CurrentCulture),
                    Weekday = dt.ToString("ddd", CultureInfo.CurrentCulture),
                    SongCount = count
                });
            }

            OnPropertyChanged(nameof(HasDates));

            if (DateList.Count == 0)
            {
                SelectedDate = null;
                return;
            }

            HistoryDateItem restore = keepKey != null
                ? DateList.FirstOrDefault(d => d.DateKey == keepKey)
                : null;
            SelectedDate = restore ?? DateList[0];
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            StatusMessage = ex.Message;
        }
    }

    private void LoadSongsForSelectedDate()
    {
        Songs.Clear();
        SelectedSong = null;
        if (SelectedDate == null || _doc == null)
        {
            OnPropertyChanged(nameof(HasSongs));
            OnPropertyChanged(nameof(HasNoSongs));
            return;
        }

        XElement root = _doc.Descendants("d_" + SelectedDate.DateKey).FirstOrDefault();
        if (root == null)
        {
            OnPropertyChanged(nameof(HasSongs));
            OnPropertyChanged(nameof(HasNoSongs));
            return;
        }

        foreach (XElement node in root.Elements().Reverse())
        {
            if (node.Name != "Song") continue;
            string timeVal = node.Attribute("Time")?.Value;
            if (string.IsNullOrEmpty(timeVal)) continue;
            if (!double.TryParse(timeVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double unix) &&
                !double.TryParse(timeVal, out unix))
                continue;

            string fullName = node.Value ?? "";
            SplitTrackName(fullName, out string title, out string artist);

            var source = new Song
            {
                Time = UnixTimeStampToDateTime(unix).ToLongTimeString(),
                Name = fullName,
                UnixTimeStamp = (long)unix
            };

            Songs.Add(new HistorySongItem
            {
                Time = source.Time,
                Title = title,
                Artist = artist,
                FullName = fullName,
                UnixTimeStamp = source.UnixTimeStamp,
                Source = source
            });
        }

        OnPropertyChanged(nameof(HasSongs));
        OnPropertyChanged(nameof(HasNoSongs));
    }

    private static void SplitTrackName(string fullName, out string title, out string artist)
    {
        title = fullName?.Trim() ?? "";
        artist = "";
        if (string.IsNullOrEmpty(title))
            return;

        int sep = title.IndexOf(" - ", StringComparison.Ordinal);
        if (sep <= 0)
            return;

        artist = title[..sep].Trim();
        title = title[(sep + 3)..].Trim();
        if (string.IsNullOrEmpty(title))
        {
            title = fullName.Trim();
            artist = "";
        }
    }

    private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return dt.AddSeconds(unixTimeStamp).ToLocalTime();
    }

    private void Refresh() => LoadFile();

    private void CopyHistoryUrl()
    {
        try
        {
            Clipboard.SetDataObject($"{GlobalObjects.BaseUrl}/history.php?id=" + Settings.Uuid);
            StatusMessage = Application.Current?.TryFindResource("window_history_url_copied") as string
                            ?? "History URL copied to clipboard";
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void DeleteSelectedDate()
    {
        if (SelectedDate == null) return;
        try
        {
            string key = "d_" + SelectedDate.DateKey;
            var xdoc = XDocument.Load(HistoryPath);
            xdoc.Descendants(key).Remove();
            xdoc.Save(HistoryPath);
            LoadFile();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void DeleteSelectedSong()
    {
        if (SelectedSong == null || SelectedDate == null) return;
        try
        {
            long key = SelectedSong.UnixTimeStamp;
            var xdoc = XDocument.Load(HistoryPath);
            xdoc.Element("History")
                ?.Element("d_" + SelectedDate.DateKey)
                ?.Elements("Song")
                .Where(x => (string)x.Attribute("Time") == key.ToString())
                .Remove();
            xdoc.Save(HistoryPath);
            LoadFile();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    public void ApplySettings()
    {
        _saveHistory = Settings.SaveHistory;
        _uploadHistory = Settings.UploadHistory;
        OnPropertyChanged(nameof(SaveHistory));
        OnPropertyChanged(nameof(UploadHistory));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
