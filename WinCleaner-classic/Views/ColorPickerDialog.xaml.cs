using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WinCleaner.Views
{
    public partial class ColorPickerDialog : Window
    {
        private bool _isUpdating;

        public ColorPickerDialog()
        {
            InitializeComponent();
            Color = Colors.Blue;
            Loaded += OnLoaded;
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(ColorPickerDialog),
                new PropertyMetadata(Colors.Blue, OnColorChanged));

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPickerDialog dialog)
            {
                dialog.UpdateUIFromColor((Color)e.NewValue);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateUIFromColor(Color);
        }

        private void UpdateUIFromColor(Color color)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                var (h, s, v) = RgbToHsv(color.R, color.G, color.B);

                HueSlider.Value = h;
                SaturationSlider.Value = s * 100;
                ValueSlider.Value = v * 100;

                ColorPreview.Color = color;
                HexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                UpdatePickerPosition(h, s, v);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdatePickerPosition(double h, double s, double v)
        {
            var pickerWidth = SaturationValueCanvas.ActualWidth;
            var pickerHeight = SaturationValueCanvas.ActualHeight;

            if (pickerWidth > 0 && pickerHeight > 0)
            {
                var x = s * pickerWidth;
                var y = (1 - v) * pickerHeight;

                Canvas.SetLeft(PickerEllipse, x - PickerEllipse.Width / 2);
                Canvas.SetTop(PickerEllipse, y - PickerEllipse.Height / 2);

                var hueHeight = HueCanvas.ActualHeight;
                if (hueHeight > 0)
                {
                    var hueY = (h / 360.0) * hueHeight;
                    Canvas.SetTop(HueMarker, hueY - HueMarker.Height / 2);
                }
            }
        }

        private static (double h, double s, double v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            double h = 0;
            if (delta != 0)
            {
                if (max == rd)
                    h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd)
                    h = 60 * (((bd - rd) / delta) + 2);
                else
                    h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        private static Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        private void OnHueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating) return;
            UpdateColorFromSliders();
        }

        private void OnSaturationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating) return;
            UpdateColorFromSliders();
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating) return;
            UpdateColorFromSliders();
        }

        private void UpdateColorFromSliders()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                double h = HueSlider.Value;
                double s = SaturationSlider.Value / 100.0;
                double v = ValueSlider.Value / 100.0;

                var color = HsvToRgb(h, s, v);
                Color = color;

                ColorPreview.Color = color;
                HexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                UpdatePickerPosition(h, s, v);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnSaturationValueMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                canvas.CaptureMouse();
                UpdateSaturationValueFromMouse(e.GetPosition(canvas), canvas.ActualWidth, canvas.ActualHeight);
            }
        }

        private void OnSaturationValueMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Canvas canvas && canvas.IsMouseCaptured)
            {
                UpdateSaturationValueFromMouse(e.GetPosition(canvas), canvas.ActualWidth, canvas.ActualHeight);
            }
        }

        private void OnSaturationValueMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                canvas.ReleaseMouseCapture();
            }
        }

        private void UpdateSaturationValueFromMouse(Point position, double width, double height)
        {
            if (_isUpdating || width <= 0 || height <= 0) return;
            _isUpdating = true;

            try
            {
                double s = Math.Clamp(position.X / width, 0, 1);
                double v = Math.Clamp(1 - position.Y / height, 0, 1);
                double h = HueSlider.Value;

                var color = HsvToRgb(h, s, v);
                Color = color;

                SaturationSlider.Value = s * 100;
                ValueSlider.Value = v * 100;

                ColorPreview.Color = color;
                HexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                Canvas.SetLeft(PickerEllipse, position.X - PickerEllipse.Width / 2);
                Canvas.SetTop(PickerEllipse, position.Y - PickerEllipse.Height / 2);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                canvas.CaptureMouse();
                UpdateHueFromMouse(e.GetPosition(canvas), canvas.ActualHeight);
            }
        }

        private void OnHueMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Canvas canvas && canvas.IsMouseCaptured)
            {
                UpdateHueFromMouse(e.GetPosition(canvas), canvas.ActualHeight);
            }
        }

        private void OnHueMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                canvas.ReleaseMouseCapture();
            }
        }

        private void UpdateHueFromMouse(Point position, double height)
        {
            if (_isUpdating || height <= 0) return;
            _isUpdating = true;

            try
            {
                double h = Math.Clamp((position.Y / height) * 360, 0, 360);
                HueSlider.Value = h;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnHexTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                var text = HexTextBox.Text.TrimStart('#');
                if (text.Length == 6 && uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint value))
                {
                    byte r = (byte)(value >> 16);
                    byte g = (byte)(value >> 8);
                    byte b = (byte)value;

                    var color = Color.FromRgb(r, g, b);
                    Color = color;

                    var (h, s, v) = RgbToHsv(r, g, b);
                    HueSlider.Value = h;
                    SaturationSlider.Value = s * 100;
                    ValueSlider.Value = v * 100;

                    ColorPreview.Color = color;
                    UpdatePickerPosition(h, s, v);
                }
            }
            catch
            {
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}