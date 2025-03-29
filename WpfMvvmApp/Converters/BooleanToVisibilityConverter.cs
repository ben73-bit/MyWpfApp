using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfMvvmApp.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Converte un booleano in Visibility.Visible o Visibility.Collapsed
            if (value is bool isVisible && isVisible)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Converte Visibility in booleano (utile in alcuni scenari, ma non strettamente necessario qui)
            if (value is Visibility visibility && visibility == Visibility.Visible)
            {
                return true;
            }
            return false;
        }
    }
}