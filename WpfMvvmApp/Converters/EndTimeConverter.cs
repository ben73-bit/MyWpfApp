// WpfMvvmApp/Converters/EndTimeConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfMvvmApp.Models; // Necessario se il binding passa l'intera Lesson

namespace WpfMvvmApp.Converters
{
    // Converte StartDateTime e Duration in EndDateTime
    public class EndTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Si aspetta due valori: [0] = StartDateTime (DateTime), [1] = Duration (TimeSpan)
            if (values != null && values.Length == 2 && values[0] is DateTime startTime && values[1] is TimeSpan duration)
            {
                try
                {
                    DateTime endTime = startTime.Add(duration);
                    // Restituisce l'ora di fine formattata
                    return endTime; // Il binding userà StringFormat="HH:mm"
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Addizione potrebbe fallire se il risultato è fuori dal range di DateTime
                    return "--:--"; // O stringa vuota o altro indicatore di errore
                }
            }

            // Valori non validi o non del tipo atteso
            // Restituisce null o un valore che indica errore,
            // DependencyProperty.UnsetValue dice al binding di non fare nulla.
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // La conversione inversa non è necessaria per una visualizzazione calcolata
            throw new NotImplementedException();
        }
    }
}