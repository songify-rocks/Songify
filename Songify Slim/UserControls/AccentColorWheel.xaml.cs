using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Songify_Slim.UserControls
{
    public partial class AccentColorWheel
    {
        private const int BitmapSize = 256;
        private static WriteableBitmap _wheelBitmap;

        private double _hue;
        private double _saturation = 1;
        private double _value = 1;
        private bool _dragging;
        private bool _suppressEvents;

        public AccentColorWheel()
        {
            InitializeComponent();
            ImgWheel.Source = WheelBitmap;
            SldValue.Value = 1;
            UpdateValueTrack();
            UpdateThumb();
        }

        public event EventHandler<Color> ColorChanged;

        public event EventHandler PickingCompleted;

        public Color SelectedColor => FromHsv(_hue, _saturation, _value);

        public void SetColor(Color color)
        {
            ToHsv(color, out double h, out double s, out double v);
            _suppressEvents = true;
            try
            {
                _hue = h;
                _saturation = s;
                _value = v;
                if (SldValue != null)
                    SldValue.Value = v;
                UpdateValueTrack();
                UpdateThumb();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private static WriteableBitmap WheelBitmap => _wheelBitmap ??= CreateWheelBitmap(BitmapSize);

        private void WheelHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WheelHost.ActualWidth <= 0 || WheelHost.ActualHeight <= 0)
                return;

            EllipseGeometry clip = new()
            {
                Center = new Point(WheelHost.ActualWidth / 2, WheelHost.ActualHeight / 2),
                RadiusX = WheelHost.ActualWidth / 2,
                RadiusY = WheelHost.ActualHeight / 2
            };
            WheelHost.Clip = clip;
            UpdateThumb();
        }

        private void WheelHost_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TryPick(e.GetPosition(WheelHost), commitOutside: false))
                return;
            _dragging = true;
            WheelHost.CaptureMouse();
            e.Handled = true;
        }

        private void WheelHost_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging)
                return;
            TryPick(e.GetPosition(WheelHost), commitOutside: true);
        }

        private void WheelHost_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging)
                return;
            _dragging = false;
            WheelHost.ReleaseMouseCapture();
            PickingCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void WheelHost_OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!_dragging)
                return;
            _dragging = false;
            PickingCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void SldValue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressEvents)
                return;
            _value = Math.Clamp(e.NewValue, 0, 1);
            ValueOverlay.Opacity = 1 - _value;
            UpdateValueTrack();
            UpdateThumb();
            RaiseColorChanged();
        }

        private void SldValue_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => PickingCompleted?.Invoke(this, EventArgs.Empty);

        private void SldValue_OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_dragging)
                return;
            PickingCompleted?.Invoke(this, EventArgs.Empty);
        }

        private bool TryPick(Point point, bool commitOutside)
        {
            double width = WheelHost.ActualWidth;
            double height = WheelHost.ActualHeight;
            if (width <= 0 || height <= 0)
                return false;

            double cx = width / 2;
            double cy = height / 2;
            double dx = point.X - cx;
            double dy = point.Y - cy;
            double radius = Math.Min(cx, cy);
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > radius)
            {
                if (!commitOutside)
                    return false;
                dx = dx / dist * radius;
                dy = dy / dist * radius;
                dist = radius;
            }

            _hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            _saturation = radius <= 0 ? 0 : Math.Clamp(dist / radius, 0, 1);
            UpdateValueTrack();
            UpdateThumb();
            RaiseColorChanged();
            return true;
        }

        private void UpdateThumb()
        {
            if (Thumb == null || WheelHost == null)
                return;

            double width = WheelHost.ActualWidth;
            double height = WheelHost.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            double cx = width / 2;
            double cy = height / 2;
            double radius = Math.Min(cx, cy);
            double angle = _hue * Math.PI / 180;
            double x = cx + Math.Cos(angle) * _saturation * radius;
            double y = cy + Math.Sin(angle) * _saturation * radius;
            Canvas.SetLeft(Thumb, x - Thumb.Width / 2);
            Canvas.SetTop(Thumb, y - Thumb.Height / 2);
            Thumb.Fill = new SolidColorBrush(SelectedColor);
        }

        private void UpdateValueTrack()
        {
            if (ValueTrack == null)
                return;
            Color top = FromHsv(_hue, _saturation, 1);
            ValueTrack.Background = new LinearGradientBrush(top, Colors.Black, 90);
            if (ValueOverlay != null)
                ValueOverlay.Opacity = 1 - _value;
        }

        private void RaiseColorChanged()
        {
            if (_suppressEvents)
                return;
            ColorChanged?.Invoke(this, SelectedColor);
        }

        private static WriteableBitmap CreateWheelBitmap(int size)
        {
            WriteableBitmap bmp = new(size, size, 96, 96, PixelFormats.Bgra32, null);
            int stride = size * 4;
            byte[] pixels = new byte[stride * size];
            double cx = (size - 1) / 2.0;
            double cy = cx;
            double radius = cx;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - cx;
                    double dy = y - cy;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                        continue;

                    double sat = radius <= 0 ? 0 : dist / radius;
                    double hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
                    Color color = FromHsv(hue, sat, 1);
                    int i = y * stride + x * 4;
                    double edge = Math.Clamp(radius - dist, 0, 1.25);
                    byte alpha = (byte)(255 * Math.Clamp(edge / 1.25, 0, 1));
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                    pixels[i + 3] = alpha;
                }
            }

            bmp.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);
            bmp.Freeze();
            return bmp;
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            saturation = Math.Clamp(saturation, 0, 1);
            value = Math.Clamp(value, 0, 1);
            double c = value * saturation;
            double h = (hue % 360) / 60;
            if (h < 0)
                h += 6;
            double x = c * (1 - Math.Abs(h % 2 - 1));
            double r = 0, g = 0, b = 0;
            if (h < 1) { r = c; g = x; }
            else if (h < 2) { r = x; g = c; }
            else if (h < 3) { g = c; b = x; }
            else if (h < 4) { g = x; b = c; }
            else if (h < 5) { r = x; b = c; }
            else { r = c; b = x; }

            double m = value - c;
            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        private static void ToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            value = max;
            saturation = max <= 0 ? 0 : delta / max;

            if (delta <= 0)
            {
                hue = 0;
                return;
            }

            if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * ((b - r) / delta + 2);
            else
                hue = 60 * ((r - g) / delta + 4);

            if (hue < 0)
                hue += 360;
        }
    }
}
