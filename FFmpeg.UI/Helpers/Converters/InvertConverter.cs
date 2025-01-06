using System.Globalization;

namespace FFmpeg.UI.Helpers.Converters
{
    public class InvertConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool result = false;
            if (value != null && value is bool state)
            {
                result = !state;
            }

            return result;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool result = false;
            if (value != null && value is bool state)
            {
                result = !state;
            }

            return result;
        }
    }
}
