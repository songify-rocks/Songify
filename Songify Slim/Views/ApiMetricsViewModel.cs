using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Songify_Slim.Util.Spotify;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Songify_Slim.Views;

public sealed class ApiMetricsRow : INotifyPropertyChanged
{
    private string _key;
    private int _rpm;
    public string Key { get => _key; set { _key = value; OnPropertyChanged(); } }
    public int RequestsPerMinute { get => _rpm; set { _rpm = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public sealed class ApiMetricsViewModel : INotifyPropertyChanged, IDisposable
{
    public ObservableCollection<ApiMetricsRow> Rows { get; } = new();

    // ---- OxyPlot model bound in XAML ----
    public PlotModel PlotModel { get; } = CreatePlotModel();

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Dictionary<string, LineSeries> _seriesByKey = new();
    private const int Capacity = 60; // last 60 seconds

    public ApiMetricsViewModel()
    {
        _timer.Tick += (_, __) => Refresh();
        _timer.Start();
    }

    private static PlotModel CreatePlotModel()
    {
        var pm = new PlotModel
        {
            Title = "API calls per endpoint (last 60s)",
            TitleFont = "Segoe UI",
            TitleFontSize = 16,
            TitleColor = OxyColors.White,

            Background = OxyColors.Transparent,
            PlotAreaBackground = OxyColor.FromRgb(0x22, 0x22, 0x22),
            PlotAreaBorderColor = OxyColor.FromRgb(70, 70, 70),

            // This also affects legend text in 2.x
            TextColor = OxyColors.Gainsboro
        };

        // X axis
        pm.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = Capacity - 1,
            Title = "seconds (rolling)",
            TitleColor = OxyColors.Gainsboro,
            TextColor = OxyColors.Gainsboro,
            AxislineColor = OxyColors.Gray,
            AxislineThickness = 1,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(50, 50, 50),
            MinorGridlineStyle = LineStyle.None,
            TicklineColor = OxyColors.Gray,
            MajorStep = 5,
            MinorStep = 1,
            IsPanEnabled = false,
            IsZoomEnabled = false
        });

        // Y axis
        pm.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Title = "requests / min",
            TitleColor = OxyColors.Gainsboro,
            TextColor = OxyColors.Gainsboro,
            AxislineColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(45, 45, 45),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            TicklineColor = OxyColors.Gray,
            StringFormat = "0",
            MinimumPadding = 0,
            MaximumPadding = 0.05,
            IsPanEnabled = false,
            IsZoomEnabled = false
        });

        // Optional: soft cap line
        pm.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 60,
            Color = OxyColor.FromRgb(255, 165, 0),
            LineStyle = LineStyle.Dash,
            StrokeThickness = 1.5,
            Text = "soft cap 60/min",
            TextColor = OxyColors.Orange,
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
            TextMargin = 6
        });

        return pm;
    }
    private void Refresh()
    {
        // ---- table rows ----
        var snapshot = ApiCallMeter.GetAllCountsPerMinute(); // Dictionary<string,int>

        foreach (var kv in snapshot)
        {
            var row = Rows.FirstOrDefault(r => r.Key == kv.Key);
            if (row == null) Rows.Add(new ApiMetricsRow { Key = kv.Key, RequestsPerMinute = kv.Value });
            else row.RequestsPerMinute = kv.Value;
        }
        for (int i = Rows.Count - 1; i >= 0; i--)
            if (Rows[i].Key != "TOTAL" && !snapshot.ContainsKey(Rows[i].Key))
                Rows.RemoveAt(i);

        var total = snapshot.Values.Sum();
        var totalRow = Rows.FirstOrDefault(r => r.Key == "TOTAL");
        if (totalRow == null) Rows.Add(new ApiMetricsRow { Key = "TOTAL", RequestsPerMinute = total });
        else totalRow.RequestsPerMinute = total;

        // ---- chart series (one line per endpoint) ----
        foreach (var kv in snapshot.Where(k => k.Key != "TOTAL"))
        {
            if (!_seriesByKey.TryGetValue(kv.Key, out var series))
            {
                series = new LineSeries
                {
                    Title = kv.Key,
                    StrokeThickness = 2.5,
                    MarkerType = MarkerType.None,
                    Color = NextSeriesColor(),
                    TrackerFormatString = "{0}\n{1}: {2:0}s\n{3}: {4:0} rpm"
                    // InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline, // optional smoothing if your OxyPlot build has it
                };
                _seriesByKey[kv.Key] = series;
                PlotModel.Series.Add(series);
            }

            var pts = series.Points;
            if (pts.Count >= Capacity) pts.RemoveAt(0);
            pts.Add(new DataPoint(pts.Count, kv.Value));

            // normalize x to 0..n-1 so the axis stays 0..59
            for (int i = 0; i < pts.Count; i++)
                pts[i] = new DataPoint(i, pts[i].Y);
        }

        // remove vanished endpoints
        foreach (var gone in _seriesByKey.Keys.Where(k => !snapshot.ContainsKey(k)).ToList())
        {
            PlotModel.Series.Remove(_seriesByKey[gone]);
            _seriesByKey.Remove(gone);
        }

        PlotModel.InvalidatePlot(true);
        OnPropertyChanged(nameof(Rows));
    }

    private static readonly OxyColor[] SeriesColors =
    [
        OxyColor.FromRgb(156, 220, 254), // light blue
        OxyColor.FromRgb(86, 156, 214),  // blue
        OxyColor.FromRgb(181, 206, 168), // green
        OxyColor.FromRgb(197, 134, 192), // purple
        OxyColor.FromRgb(224, 108, 117), // red
        OxyColor.FromRgb(229, 192, 123) // yellow
    ];

    private int _seriesColorIndex = 0;
    private OxyColor NextSeriesColor() =>
        SeriesColors[_seriesColorIndex++ % SeriesColors.Length];

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= (_, __) => Refresh();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}