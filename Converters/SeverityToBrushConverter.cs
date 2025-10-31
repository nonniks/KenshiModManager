using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KenshiModManager.Converters
{
    /// <summary>
    /// Converts severity level to appropriate brush color
    /// </summary>
    public class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string severity)
            {
                return severity.ToLowerInvariant() switch
                {
                    "info" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                    "warning" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500")),
                    "error" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
                };
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
