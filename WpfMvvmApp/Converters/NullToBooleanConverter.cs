// WpfMvvmApp/Converters/NullToBooleanConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfMvvmApp.Converters
{
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Restituisce true se l'oggetto NON è null, false altrimenti
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // La conversione inversa non è solitamente necessaria per IsEnabled
            throw new NotImplementedException();
        }
    }
}