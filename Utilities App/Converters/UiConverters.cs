using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UtilitiesApp.Converters
{
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Visibility.Collapsed;
        }
    }

    public sealed class StatusBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Accent = Freeze(Color.FromRgb(0x7B, 0x93, 0xFF));
        private static readonly SolidColorBrush AccentSoft = Freeze(Color.FromRgb(0x24, 0x31, 0x56));
        private static readonly SolidColorBrush Success = Freeze(Color.FromRgb(0x3D, 0xDC, 0x97));
        private static readonly SolidColorBrush SuccessSoft = Freeze(Color.FromRgb(0x16, 0x35, 0x2A));
        private static readonly SolidColorBrush Danger = Freeze(Color.FromRgb(0xFF, 0x7B, 0x88));
        private static readonly SolidColorBrush DangerSoft = Freeze(Color.FromRgb(0x3A, 0x1E, 0x28));
        private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0xA7, 0xB0, 0xC2));
        private static readonly SolidColorBrush MutedSoft = Freeze(Color.FromRgb(0x23, 0x28, 0x38));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string status = value as string ?? "";
            bool foreground = parameter as string == "fg";
            bool done = status == "Done";
            bool failed = status.StartsWith("Failed", StringComparison.Ordinal);
            bool active = !done && !failed && status != "Ready" && !string.IsNullOrEmpty(status);

            if (done)
            {
                return foreground ? Success : SuccessSoft;
            }

            if (failed)
            {
                return foreground ? Danger : DangerSoft;
            }

            if (active)
            {
                return foreground ? Accent : AccentSoft;
            }

            return foreground ? Muted : MutedSoft;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
