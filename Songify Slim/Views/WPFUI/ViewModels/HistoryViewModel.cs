using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
    private string _statusMessage = "";

    public HistoryViewModel()
    {
        HistoryPath = HistoryStore.FilePath;
        DateList = new ObservableCollection<HistoryDateItem>();
        Songs = new ObservableCollection<HistorySongItem>();

        RefreshCommand = new RelayCommand(Refresh);
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

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value ?? ""; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
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
            HistoryStore.EnsureReady();

            string keepKey = _selectedDate?.DateKey;
            DateList.Clear();
            Songs.Clear();

            foreach ((string key, DateTime dt, int count) in HistoryStore.GetDateSummaries())
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
                ? FirstDateOrDefault(keepKey)
                : null;
            SelectedDate = restore ?? DateList[0];
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            StatusMessage = ex.Message;
        }
    }

    private HistoryDateItem FirstDateOrDefault(string keepKey)
    {
        foreach (HistoryDateItem d in DateList)
        {
            if (d.DateKey == keepKey)
                return d;
        }

        return null;
    }

    private void LoadSongsForSelectedDate()
    {
        Songs.Clear();
        SelectedSong = null;
        if (SelectedDate == null)
        {
            OnPropertyChanged(nameof(HasSongs));
            OnPropertyChanged(nameof(HasNoSongs));
            return;
        }

        foreach (HistorySongRecord record in HistoryStore.GetSongsForDate(SelectedDate.DateKey))
        {
            string fullName = record.Song ?? "";
            SplitTrackName(fullName, out string title, out string artist);

            var source = new Song
            {
                Time = UnixTimeStampToDateTime(record.Time).ToLongTimeString(),
                Name = fullName,
                UnixTimeStamp = record.Time
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

    private void DeleteSelectedDate()
    {
        if (SelectedDate == null) return;
        try
        {
            HistoryStore.DeleteDate(SelectedDate.DateKey);
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
            HistoryStore.DeleteSong(SelectedDate.DateKey, SelectedSong.UnixTimeStamp);
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
        OnPropertyChanged(nameof(SaveHistory));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
