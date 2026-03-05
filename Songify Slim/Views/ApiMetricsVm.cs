using LiveCharts;
using LiveCharts.Wpf;
using Songify_Slim.Util.Spotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;

namespace Songify_Slim.Views
{
    internal sealed class ApiMetricsVm : IDisposable
    {
        private const int Capacity = 60;

        public SeriesCollection SeriesCollection { get; }

        private readonly DispatcherTimer _timer;
        private readonly Dictionary<string, ChartValues<int>> _seriesValues = new Dictionary<string, ChartValues<int>>();
        private readonly Dictionary<string, LineSeries> _seriesByKey = new Dictionary<string, LineSeries>();

        // similar vibe to your Oxy palette
        private static readonly Color[] Palette =
        [
            Color.FromRgb(156, 220, 254),
            Color.FromRgb(86, 156, 214),
            Color.FromRgb(181, 206, 168),
            Color.FromRgb(197, 134, 192),
            Color.FromRgb(224, 108, 117),
            Color.FromRgb(229, 192, 123)
        ];

        private int _colorIndex = 0;

        public ApiMetricsVm()
        {
            SeriesCollection = [];

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
        }

        private Brush NextStroke()
        {
            Color c = Palette[_colorIndex++ % Palette.Length];
            return new SolidColorBrush(c);
        }

        private void Refresh()
        {
            IDictionary<string, int> snapshot = ApiCallMeter.GetAllCountsPerMinute();

            // add/update series
            foreach (KeyValuePair<string, int> kv in snapshot.Where(k => k.Key != "TOTAL"))
            {
                if (!_seriesValues.TryGetValue(kv.Key, out ChartValues<int> values))
                {
                    values = [];
                    _seriesValues[kv.Key] = values;

                    // optional: prefill so new lines don't "grow in" from nothing
                    for (int i = 0; i < Capacity; i++) values.Add(0);

                    Brush stroke = NextStroke();

                    LineSeries series = new()
                    {
                        Title = kv.Key,
                        Values = values,
                        PointGeometry = null,
                        LineSmoothness = 0,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent
                    };

                    _seriesByKey[kv.Key] = series;
                    SeriesCollection.Add(series);
                }

                // rolling window update
                values.Add(kv.Value);
                if (values.Count > Capacity)
                    values.RemoveAt(0);
            }

            // remove vanished endpoints
            List<string> goneKeys = _seriesByKey.Keys.Where(k => !snapshot.ContainsKey(k)).ToList();
            foreach (string key in goneKeys)
            {
                SeriesCollection.Remove(_seriesByKey[key]);
                _seriesByKey.Remove(key);
                _seriesValues.Remove(key);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}