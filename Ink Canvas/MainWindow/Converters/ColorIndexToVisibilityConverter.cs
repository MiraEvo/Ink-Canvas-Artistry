using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ink_Canvas.Converters
{
    public sealed class ColorIndexToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int inkColor && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetColorIndex))
                {
                    return inkColor == targetColorIndex ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
