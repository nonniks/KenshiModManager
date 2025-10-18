using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KenshiModManager.Converters
{
    /// <summary>
    /// Converts bool to Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check if inverted parameter is provided
                bool invert = parameter is string paramString && paramString.Equals("Inverted", StringComparison.OrdinalIgnoreCase);

                if (invert)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
