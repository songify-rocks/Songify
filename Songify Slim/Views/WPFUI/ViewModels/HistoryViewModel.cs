using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

public sealed class HistoryViewModel : INotifyPropertyChanged
{
    private string _selectedDate;
    private Song _selectedSong;
    private bool _saveHistory;
    private bool _uploadHistory;
    private string _statusMessage = "";
    private XDocument _doc;

    public HistoryViewModel()
    {
        HistoryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "", "history.shr");
        DateList = new ObservableCollection<string>();
        Songs = new ObservableCollection<Song>();

        RefreshCommand = new RelayCommand(Refresh);
        CopyHistoryUrlCommand = new RelayCommand(CopyHistoryUrl);
        DeleteDateCommand = new RelayCommand(DeleteSelectedDate, () => !string.IsNullOrEmpty(SelectedDate));
        DeleteSongCommand = new RelayCommand(DeleteSelectedSong, () => SelectedSong != null);
    }

    public string HistoryPath { get; }

    public ObservableCollection<string> DateList { get; }
    public ObservableCollection<Song> Songs { get; }

    public string SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value) return;
            _selectedDate = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
            LoadSongsForSelectedDate();
        }
    }

    public Song SelectedSong
    {
        get => _selectedSong;
        set
        {
            if (_selectedSong == value) return;
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
            OnPropertyChanged(nameof(SaveToggleContent));
            StatusMessage = value ? $"{Properties.Resources.s_Save} ✔️" : $"{Properties.Resources.s_Save} ❌";
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
            OnPropertyChanged(nameof(UploadToggleContent));
            StatusMessage = value ? $"{Properties.Resources.s_Upload} ✔️" : $"{Properties.Resources.s_Upload} ❌";
        }
    }

    public string SaveToggleContent => _saveHistory ? $"{Properties.Resources.s_Save} ✔️" : $"{Properties.Resources.s_Save} ❌";
    public string UploadToggleContent => _uploadHistory ? $"{Properties.Resources.s_Upload} ✔️" : $"{Properties.Resources.s_Upload} ❌";

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

            DateList.Clear();
            Songs.Clear();

            _doc = XDocument.Load(HistoryPath);
            var list = new System.Collections.Generic.List<DateTime>();
            var dateList = new System.Collections.Generic.List<string>();

            if (_doc.Root != null)
            {
                foreach (XElement elem in _doc.Root.Elements())
                {
                    dateList.AddRange(elem.Name.ToString().Replace("d_", "").Split('.'));
                    list.Add(new DateTime(int.Parse(dateList[2]), int.Parse(dateList[1]), int.Parse(dateList[0])));
                    dateList.Clear();
                }
            }

            foreach (DateTime time in list.OrderByDescending(t => t.Date))
                DateList.Add(time.ToString("dd.MM.yyyy"));

            if (DateList.Count > 0 && string.IsNullOrEmpty(SelectedDate))
                SelectedDate = DateList[0];
            else if (DateList.Count > 0 && SelectedDate != null && DateList.Contains(SelectedDate))
                LoadSongsForSelectedDate();
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
        if (string.IsNullOrEmpty(SelectedDate) || _doc == null) return;

        XElement root = _doc.Descendants("d_" + SelectedDate).FirstOrDefault();
        if (root == null) return;

        var nodes = root.Elements().Reverse().ToList();
        foreach (XElement node in nodes)
        {
            if (node.Name != "Song") continue;
            string timeVal = node.Attribute("Time")?.Value;
            if (string.IsNullOrEmpty(timeVal)) continue;
            var data = new Song
            {
                Time = UnixTimeStampToDateTime(double.Parse(timeVal)).ToLongTimeString(),
                Name = node.Value,
                UnixTimeStamp = long.Parse(timeVal)
            };
            Songs.Add(data);
        }
    }

    private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return dt.AddSeconds(unixTimeStamp).ToLocalTime();
    }

    private void Refresh()
    {
        LoadFile();
    }

    private void CopyHistoryUrl()
    {
        try
        {
            Clipboard.SetDataObject($"{GlobalObjects.BaseUrl}/history.php?id=" + Settings.Uuid);
            StatusMessage = "History URL copied to Clipboard";
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void DeleteSelectedDate()
    {
        if (string.IsNullOrEmpty(SelectedDate)) return;
        try
        {
            string key = "d_" + SelectedDate;
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
        if (SelectedSong == null || string.IsNullOrEmpty(SelectedDate)) return;
        try
        {
            long key = SelectedSong.UnixTimeStamp;
            var xdoc = XDocument.Load(HistoryPath);
            xdoc.Element("History")
                ?.Element("d_" + SelectedDate)
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
        OnPropertyChanged(nameof(SaveToggleContent));
        OnPropertyChanged(nameof(UploadToggleContent));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}