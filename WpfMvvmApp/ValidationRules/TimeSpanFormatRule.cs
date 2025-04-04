// WpfMvvmApp/ValidationRules/TimeSpanFormatRule.cs (o percorso simile)
using System;
using System.Globalization;
using System.Windows.Controls; // Per ValidationResult
using System.Text.RegularExpressions; // Per Regex (opzionale ma utile)

// Assicurati che il namespace corrisponda alla posizione del file
namespace WpfMvvmApp.ValidationRules
{
    public class TimeSpanFormatRule : ValidationRule
    {
        // Sovrascrivi il metodo Validate
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? inputString = value as string;

            if (string.IsNullOrWhiteSpace(inputString))
            {
                // Consideriamo la stringa vuota o solo spazi non valida qui,
                // perché il binding a TimeSpan fallirebbe comunque.
                // Potresti restituire Success se vuoi permettere il vuoto
                // e lasciare che ExceptionValidationRule gestisca il binding nullo a TimeSpan.
                return new ValidationResult(false, "Duration cannot be empty.");
            }

            // Tenta il parsing usando il formato "hh:mm" (escape \:)
            // CultureInfo.InvariantCulture assicura che ':' sia il separatore atteso.
            if (TimeSpan.TryParseExact(inputString, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan parsedTimeSpan))
            {
                // Parsing riuscito. Ora controlla se è positivo.
                if (parsedTimeSpan <= TimeSpan.Zero)
                {
                    return new ValidationResult(false, "Duration must be positive (e.g., 00:01 or more).");
                }
                // Tutto ok
                return ValidationResult.ValidResult;
            }
            else
            {
                // Parsing fallito, formato non corretto
                return new ValidationResult(false, "Invalid format. Please use HH:MM (e.g., 01:30).");
            }

            /* Approccio Alternativo con Regex (più flessibile ma potrebbe essere overkill):
            if (inputString != null)
            {
                // Regex per hh:mm (00-23 per ore, 00-59 per minuti)
                var regex = new Regex(@"^([01]\d|2[0-3]):([0-5]\d)$");
                if (regex.IsMatch(inputString))
                {
                    // Il formato base è corretto, ora prova il parsing per essere sicuro
                    // e controlla se è positivo
                    if (TimeSpan.TryParse(inputString, CultureInfo.InvariantCulture, out TimeSpan parsedSpan) && parsedSpan > TimeSpan.Zero)
                    {
                         return ValidationResult.ValidResult;
                    }
                    else
                    {
                         // Formato valido ma valore <= 0 o errore strano nel parsing
                         return new ValidationResult(false, "Duration must be positive and valid.");
                    }
                }
            }
            // Se arriva qui, il formato regex non corrisponde
            return new ValidationResult(false, "Invalid format. Please use HH:MM.");
            */
        }
    }
}