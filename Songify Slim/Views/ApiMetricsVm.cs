using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Songify_Slim.Util.Spotify;
using Songify_Slim.Util.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using SkiaSharp;
using static Songify_Slim.Util.General.Enums;
using LiveChartsCore.SkiaSharpView.WPF;

namespace Songify_Slim.Views
{
    public sealed class ApiMetricsRow : INotifyPropertyChanged
    {
        private string _key;
        private int _rpm;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public int RequestsPerMinute
        {
            get => _rpm;
            set { _rpm = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public sealed class ApiMetricsVm : INotifyPropertyChanged, IDisposable
    {
        private const int Capacity = 60;

        // DataGrid
        public ObservableCollection<ApiMetricsRow> Rows { get; } = new ObservableCollection<ApiMetricsRow>();

        // Chart - LiveChartsCore v2 API
        public ObservableCollection<ISeries> SeriesCollection { get; } = new ObservableCollection<ISeries>();
        public Axis[] XAxes { get; }
        public Axis[] YAxes { get; }

        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        private readonly Dictionary<string, ObservableCollection<int>> _valuesByKey = new Dictionary<string, ObservableCollection<int>>();
        private readonly Dictionary<string, LineSeries<int>> _seriesByKey = new Dictionary<string, LineSeries<int>>();

        private int _totalRequestsPerMinute;

        public int TotalRequestsPerMinute
        {
            get => _totalRequestsPerMinute;
            private set { _totalRequestsPerMinute = value; OnPropertyChanged(); }
        }

        private bool _showTotalInStatusbar;

        public bool ShowTotalInStatusbar
        {
            get => _showTotalInStatusbar;
            private set { _showTotalInStatusbar = value; OnPropertyChanged(); }
        }

        // Color palette similar to your OxyPlot palette
        private static readonly SKColor[] Palette =
        {
            new SKColor(156, 220, 254),
            new SKColor(86, 156, 214),
            new SKColor(181, 206, 168),
            new SKColor(197, 134, 192),
            new SKColor(224, 108, 117),
            new SKColor(229, 192, 123)
        };

        private int _colorIndex = 0;

        private SolidColorPaint NextStroke()
        {
            var c = Palette[_colorIndex++ % Palette.Length];
            return new SolidColorPaint(c) { StrokeThickness = 2 };
        }

        public ApiMetricsVm()
        {
            // Configure axes
            XAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 59,
                    LabelsPaint = new SolidColorPaint(SKColors.Gray)
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    LabelsPaint = new SolidColorPaint(SKColors.Gray)
                }
            };

            _timer.Tick += (_, __) => Refresh();
            _timer.Start();

            // initial state
            ShowTotalInStatusbar = Settings.Player == PlayerType.Spotify;
        }

        private void Refresh()
        {
            bool isSpotify = Settings.Player == PlayerType.Spotify;
            ShowTotalInStatusbar = isSpotify;

            if (!isSpotify)
            {
                TotalRequestsPerMinute = 0;

                // optional: clear UI when not Spotify
                // Rows.Clear();
                // SeriesCollection.Clear();
                // _seriesByKey.Clear();
                // _valuesByKey.Clear();

                return;
            }

            IDictionary<string, int> snapshot = ApiCallMeter.GetAllCountsPerMinute();

            // ----- totals -----
            int total = snapshot.Values.Sum();
            TotalRequestsPerMinute = total;

            // ----- DataGrid rows -----
            // Update/add endpoint rows (excluding TOTAL row from snapshot, we add our own TOTAL)
            foreach (var kv in snapshot.Where(k => k.Key != "TOTAL"))
            {
                var row = Rows.FirstOrDefault(r => r.Key == kv.Key);
                if (row == null) Rows.Add(new ApiMetricsRow { Key = kv.Key, RequestsPerMinute = kv.Value });
                else row.RequestsPerMinute = kv.Value;
            }

            // Remove vanished endpoint rows
            for (int i = Rows.Count - 1; i >= 0; i--)
            {
                string key = Rows[i].Key;
                if (key != "TOTAL" && !snapshot.ContainsKey(key))
                    Rows.RemoveAt(i);
            }

            // Ensure TOTAL row exists/updated
            var totalRow = Rows.FirstOrDefault(r => r.Key == "TOTAL");
            if (totalRow == null) Rows.Add(new ApiMetricsRow { Key = "TOTAL", RequestsPerMinute = total });
            else totalRow.RequestsPerMinute = total;

            // ----- Chart series (LiveChartsCore v2) -----
            foreach (var kv in snapshot.Where(k => k.Key != "TOTAL"))
            {
                if (!_valuesByKey.TryGetValue(kv.Key, out var values))
                {
                    values = new ObservableCollection<int>();
                    _valuesByKey[kv.Key] = values;

                    // Prefill so the series starts flat and the chart looks stable immediately
                    for (int i = 0; i < Capacity; i++) values.Add(0);

                    var series = new LineSeries<int>
                    {
                        Name = kv.Key,
                        Values = values,
                        GeometrySize = 0, // No points
                        LineSmoothness = 0,
                        Stroke = NextStroke(),
                        Fill = null
                    };

                    _seriesByKey[kv.Key] = series;
                    SeriesCollection.Add(series);
                }

                values.Add(kv.Value);
                if (values.Count > Capacity)
                    values.RemoveAt(0);
            }

            // Remove vanished series
            var goneKeys = _seriesByKey.Keys.Where(k => !snapshot.ContainsKey(k)).ToList();
            foreach (var key in goneKeys)
            {
                SeriesCollection.Remove(_seriesByKey[key]);
                _seriesByKey.Remove(key);
                _valuesByKey.Remove(key);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

}