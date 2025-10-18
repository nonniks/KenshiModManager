using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace KenshiModManager.Converters
{
    /// <summary>
    /// Converts mod image path (_modname.img) to BitmapImage
    /// .img files are actually PNG images with different extension
    /// </summary>
    public class ImagePathConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.DecodePixelWidth = 64; // Resize to 64px width for performance
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ImagePathConverter] Error loading image {imagePath}: {ex.Message}");
                    return null;
                }
            }

            // Return null if no image - WPF will show nothing instead of error
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
