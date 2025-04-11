// WpfMvvmApp/Converters/TimeSpanHoursMinutesConverter.cs
using System;
using System.Globalization;
using System.Windows; // Per DependencyProperty
using System.Windows.Data; // Per IValueConverter

namespace WpfMvvmApp.Converters
{
    public class TimeSpanHoursMinutesConverter : IValueConverter
    {
        /// <summary>
        /// Converte un TimeSpan in una stringa formato "hh:mm".
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
            {
                // Formatta come ore:minuti (hh viene troncato a 24h, TotalHours no)
                // Usiamo Math.Floor per gestire correttamente durate > 24h se necessario
                // ma mantenendo il formato hh:mm.
                // Per semplicità, assumiamo durate < 100 ore.
                //string formatted = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
                // Alternativa più robusta per il formato standard:
                string formatted = ts.ToString(@"hh\:mm"); // Formato standard ore e minuti
                return formatted;
            }
            return string.Empty; // O DependencyProperty.UnsetValue o null
        }

        /// <summary>
        /// Converte una stringa (idealmente "hh:mm") in un TimeSpan.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string inputString)
            {
                // Prova a fare il parsing esatto con il formato "hh:mm"
                if (TimeSpan.TryParseExact(inputString, @"hh\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan result))
                {
                    // Parsing riuscito, restituisce il TimeSpan
                    return result;
                }
                // Prova anche un parsing più generico (potrebbe accettare hh:mm:ss) - opzionale
                // else if (TimeSpan.TryParse(inputString, CultureInfo.InvariantCulture, out result))
                // {
                //    // Potremmo voler troncare i secondi qui se il parsing generico li include
                //    return new TimeSpan(result.Hours, result.Minutes, 0);
                // }
            }

            // Se il parsing fallisce o l'input non è una stringa,
            // restituisce UnsetValue per indicare al binding che la conversione è fallita.
            // Questo attiverà il meccanismo di errore di validazione del binding.
            return DependencyProperty.UnsetValue;
        }
    }
}