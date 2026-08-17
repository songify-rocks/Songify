using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.ViewModels;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class HistoryPage : Page
{
    private HistoryViewModel _viewModel;
    private FileSystemWatcher _watcher;

    public HistoryPage()
    {
        _viewModel = new HistoryViewModel();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.DatesLoaded += OnDatesLoaded;
        _viewModel.DisplayMonthChanged += RefreshBlackoutDates;
        Loaded += HistoryPage_Loaded;
        Unloaded += HistoryPage_Unloaded;
    }

    private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (TxtTitle != null)
            TxtTitle.Text = Properties.Resources.window_history_title;

        Window owner = Window.GetWindow(this);
        await HistoryStore.MigrateLegacyIfNeededAsync(owner);

        _viewModel.ApplySettings();
        _viewModel.LoadFile();

        try
        {
            string dir = Path.GetDirectoryName(_viewModel.HistoryPath);
            if (string.IsNullOrEmpty(dir)) return;
            _watcher?.Dispose();
            _watcher = new FileSystemWatcher
            {
                Path = dir,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(_viewModel.HistoryPath),
                EnableRaisingEvents = true
            };
            _watcher.Changed += (_, _) =>
            {
                System.Threading.Thread.Sleep(500);
                _viewModel.LoadFromFile();
            };
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void HistoryPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    private void OnDatesLoaded()
    {
        RefreshBlackoutDates(_viewModel.DisplayDate);
    }

    private void HistoryCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
    {
        if (_viewModel == null || HistoryCalendar == null) return;

        DateTime month = (e.AddedDate ?? e.RemovedDate ?? HistoryCalendar.DisplayDate).Date;
        if (_viewModel.DisplayDate.Year != month.Year || _viewModel.DisplayDate.Month != month.Month)
            _viewModel.DisplayDate = month;
        else
            RefreshBlackoutDates(month);
    }

    private void HistoryDayButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CalendarDayButton button) return;

        button.DataContextChanged -= HistoryDayButton_DataContextChanged;
        button.DataContextChanged += HistoryDayButton_DataContextChanged;
        button.PreviewMouseRightButtonDown -= HistoryDayButton_PreviewMouseRightButtonDown;
        button.PreviewMouseRightButtonDown += HistoryDayButton_PreviewMouseRightButtonDown;
        ApplyDayButtonMarker(button);
    }

    private void HistoryDayButton_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not CalendarDayButton { DataContext: DateTime date } button)
            return;

        if (_viewModel?.HasHistoryOn(date.Date) != true || button.IsBlackedOut)
        {
            e.Handled = true;
            return;
        }

        // Select the day being right-clicked so delete targets it.
        _viewModel.CalendarSelectedDate = date.Date;
    }

    private void DeleteDayMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.DeleteDateCommand?.CanExecute(null) == true)
            _viewModel.DeleteDateCommand.Execute(null);
    }

    private void HistoryDayButton_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is CalendarDayButton button)
            ApplyDayButtonMarker(button);
    }

    private void ApplyDayButtonMarker(CalendarDayButton button)
    {
        if (button == null) return;
        button.ApplyTemplate();

        if (button.Template?.FindName("Dot", button) is not UIElement dot)
            return;

        if (button.DataContext is not DateTime date)
        {
            dot.Visibility = Visibility.Collapsed;
            button.ToolTip = null;
            button.ContextMenu = null;
            return;
        }

        bool hasHistory = _viewModel?.HasHistoryOn(date.Date) == true && !button.IsBlackedOut;
        dot.Visibility = hasHistory ? Visibility.Visible : Visibility.Collapsed;
        button.ContextMenu = hasHistory
            ? TryFindResource("HistoryDeleteDayMenu") as ContextMenu
            : null;

        if (hasHistory && _viewModel.TryGetSongCount(date.Date, out int count))
            button.ToolTip = count == 1 ? "1 song" : $"{count} songs";
        else
            button.ToolTip = null;
    }

    private void RefreshBlackoutDates(DateTime monthAnchor)
    {
        if (HistoryCalendar == null || _viewModel == null) return;

        DateTime? selected = HistoryCalendar.SelectedDate;
        try
        {
            // Avoid InvalidOperationException if selected day would clash while rebuilding.
            HistoryCalendar.SelectedDate = null;
            HistoryCalendar.BlackoutDates.Clear();

            // Cover the full visible month grid (incl. leading/trailing days from adjacent months).
            DateTime monthStart = new(monthAnchor.Year, monthAnchor.Month, 1);
            DayOfWeek firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            DateTime gridStart = monthStart;
            while (gridStart.DayOfWeek != firstDay)
                gridStart = gridStart.AddDays(-1);

            DateTime gridEnd = gridStart.AddDays(41);
            for (DateTime day = gridStart; day <= gridEnd; day = day.AddDays(1))
            {
                if (!_viewModel.HasHistoryOn(day))
                    HistoryCalendar.BlackoutDates.Add(new CalendarDateRange(day));
            }
        }
        finally
        {
            if (selected is DateTime sel && _viewModel.HasHistoryOn(sel))
                HistoryCalendar.SelectedDate = sel.Date;
            else if (_viewModel.CalendarSelectedDate is DateTime current && _viewModel.HasHistoryOn(current))
                HistoryCalendar.SelectedDate = current;
        }

        // Markers depend on blackout state — refresh after the grid settles.
        Dispatcher.BeginInvoke(UpdateAllDayButtonMarkers, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateAllDayButtonMarkers()
    {
        if (HistoryCalendar == null) return;
        foreach (CalendarDayButton button in FindVisualChildren<CalendarDayButton>(HistoryCalendar))
            ApplyDayButtonMarker(button);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;

            foreach (T nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
