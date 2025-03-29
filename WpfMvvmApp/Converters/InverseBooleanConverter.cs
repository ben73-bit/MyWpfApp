using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfMvvmApp.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue; // Inverte il valore booleano
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue; // Inverte anche al ritorno
            }
            return value;
        }
    }
}