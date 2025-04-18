// WpfMvvmApp/Converters/BoolToButtonTextConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using WpfMvvmApp.Properties; // AGGIUNGI using per accedere a Resources

namespace WpfMvvmApp.Converters
{
    public class BoolToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditing)
            {
                // Legge la stringa appropriata dalle risorse
                return isEditing ? Resources.Button_UpdateLesson ?? "Update Lesson"
                                 : Resources.Button_AddLesson ?? "Add Lesson";
            }
            // Fallback se il valore non è booleano
            return Resources.Button_AddLesson ?? "Add Lesson";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Non necessario
        }
    }
}