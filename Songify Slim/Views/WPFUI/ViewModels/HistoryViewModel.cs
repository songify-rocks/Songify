using System;
using System.Collections.Generic;
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
    public DateTime Date { get; init; }
    public string YearLabel { get; init; }
    public string MonthLabel { get; init; }
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
    private readonly Dictionary<DateTime, HistoryDateItem> _byDate = new();
    private HistoryDateItem _selectedDate;
    private HistorySongItem _selectedSong;
    private DateTime? _calendarSelectedDate;
    private DateTime _displayDate = DateTime.Today;
    private DateTime? _displayDateStart;
    private DateTime? _displayDateEnd;
    private bool _saveHistory;
    private string _statusMessage = "";
    private bool _hasDates;

    public HistoryViewModel()
    {
        HistoryPath = HistoryStore.FilePath;
        Songs = new ObservableCollection<HistorySongItem>();

        RefreshCommand = new RelayCommand(Refresh);
        DeleteDateCommand = new RelayCommand(DeleteSelectedDate, () => SelectedDate != null);
        DeleteSongCommand = new RelayCommand(DeleteSelectedSong, () => SelectedSong != null);
    }

    public string HistoryPath { get; }
    public ObservableCollection<HistorySongItem> Songs { get; }

    public bool HasDates
    {
        get => _hasDates;
        private set
        {
            if (_hasDates == value) return;
            _hasDates = value;
            OnPropertyChanged();
        }
    }

    public bool HasSongs => Songs.Count > 0;
    public bool HasNoSongs => SelectedDate != null && Songs.Count == 0;

    public string SelectedDateTitle => SelectedDate == null
        ? (Application.Current?.TryFindResource("window_history_select_day") as string ?? "Select a day")
        : SelectedDate.Date.ToString("D", CultureInfo.CurrentCulture);

    public DateTime DisplayDate
    {
        get => _displayDate;
        set
        {
            DateTime normalized = value.Date;
            if (_displayDate == normalized) return;
            bool monthChanged = _displayDate.Year != normalized.Year || _displayDate.Month != normalized.Month;
            _displayDate = normalized;
            OnPropertyChanged();
            if (monthChanged)
                DisplayMonthChanged?.Invoke(normalized);
        }
    }

    public DateTime? DisplayDateStart
    {
        get => _displayDateStart;
        private set
        {
            if (_displayDateStart == value) return;
            _displayDateStart = value;
            OnPropertyChanged();
        }
    }

    public DateTime? DisplayDateEnd
    {
        get => _displayDateEnd;
        private set
        {
            if (_displayDateEnd == value) return;
            _displayDateEnd = value;
            OnPropertyChanged();
        }
    }

    public DateTime? CalendarSelectedDate
    {
        get => _calendarSelectedDate;
        set
        {
            DateTime? normalized = value?.Date;
            if (_calendarSelectedDate == normalized) return;

            if (normalized is DateTime dt && !_byDate.ContainsKey(dt))
            {
                // Ignore clicks on days without history (blackouts should already prevent this).
                OnPropertyChanged();
                return;
            }

            _calendarSelectedDate = normalized;
            OnPropertyChanged();
            SelectedDate = normalized is DateTime selected && _byDate.TryGetValue(selected, out HistoryDateItem item)
                ? item
                : null;
        }
    }

    public HistoryDateItem SelectedDate
    {
        get => _selectedDate;
        private set
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

    public event Action DatesLoaded;
    public event Action<DateTime> DisplayMonthChanged;

    public bool HasHistoryOn(DateTime date) => _byDate.ContainsKey(date.Date);

    public bool TryGetSongCount(DateTime date, out int count)
    {
        if (_byDate.TryGetValue(date.Date, out HistoryDateItem item))
        {
            count = item.SongCount;
            return true;
        }

        count = 0;
        return false;
    }

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
            _byDate.Clear();
            Songs.Clear();

            HistoryDateItem firstDay = null;
            HistoryDateItem restore = null;
            DateTime? min = null;
            DateTime? max = null;

            foreach ((string key, DateTime dt, int count) in HistoryStore.GetDateSummaries())
            {
                DateTime date = dt.Date;
                var item = new HistoryDateItem
                {
                    DateKey = key,
                    Date = date,
                    YearLabel = date.Year.ToString(CultureInfo.InvariantCulture),
                    MonthLabel = date.ToString("MMMM", CultureInfo.CurrentCulture),
                    Day = date.Day.ToString("00"),
                    Month = date.ToString("MMM", CultureInfo.CurrentCulture),
                    Weekday = date.ToString("ddd", CultureInfo.CurrentCulture),
                    SongCount = count
                };

                _byDate[date] = item;
                firstDay ??= item;
                if (keepKey != null && key == keepKey)
                    restore = item;

                if (min == null || date < min) min = date;
                if (max == null || date > max) max = date;
            }

            HasDates = _byDate.Count > 0;

            DateTime today = DateTime.Today;
            if (HasDates && min is DateTime earliest && max is DateTime latest)
            {
                // Show full months: leading/trailing empty days stay visible but blacked out.
                DisplayDateStart = new DateTime(earliest.Year, earliest.Month, 1);
                DateTime endMonth = latest > today ? latest : today;
                DisplayDateEnd = new DateTime(
                    endMonth.Year,
                    endMonth.Month,
                    DateTime.DaysInMonth(endMonth.Year, endMonth.Month));
            }
            else
            {
                DisplayDateStart = new DateTime(today.Year, today.Month, 1);
                DisplayDateEnd = new DateTime(
                    today.Year,
                    today.Month,
                    DateTime.DaysInMonth(today.Year, today.Month));
            }

            if (!HasDates)
            {
                _calendarSelectedDate = null;
                OnPropertyChanged(nameof(CalendarSelectedDate));
                SelectedDate = null;
                DisplayDate = DateTime.Today;
                DatesLoaded?.Invoke();
                return;
            }

            HistoryDateItem select = restore ?? firstDay;
            DisplayDate = select.Date;
            _calendarSelectedDate = select.Date;
            OnPropertyChanged(nameof(CalendarSelectedDate));
            SelectedDate = select;
            DatesLoaded?.Invoke();
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
