using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfMvvmApp.Converters
{
    public class BoolToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditing && isEditing)
            {
                return "Update Lesson"; // Testo quando si sta modificando
            }
            return "Add Lesson"; // Testo predefinito per aggiungere
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // La conversione inversa non è necessaria per questo scenario
            throw new NotImplementedException();
        }
    }
}