// WpfMvvmApp/ValidationAttributes/DateRangeAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization; // Aggiunto per CultureInfo
using System.Reflection;
using WpfMvvmApp.Properties; // Assicurati using corretto

namespace WpfMvvmApp.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public DateRangeAttribute(string startDatePropertyName)
        {
            // Non impostiamo più un messaggio di default qui nel costruttore base,
            // perché vogliamo dare priorità a ErrorMessageResourceType/Name
            if (string.IsNullOrEmpty(startDatePropertyName))
            {
                throw new ArgumentNullException(nameof(startDatePropertyName));
            }
            _startDatePropertyName = startDatePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            PropertyInfo? startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);

            if (startDateProperty == null)
            {
                // Errore di configurazione, non di validazione utente
                return new ValidationResult($"Error: Unknown property '{_startDatePropertyName}'.");
            }

            DateTime? startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance) as DateTime?;
            DateTime? endDateValue = value as DateTime?; // 'value' è la EndDate

            // Se una delle date non è impostata, la validazione passa (consideriamo le date opzionali)
            if (!startDateValue.HasValue || !endDateValue.HasValue)
            {
                return ValidationResult.Success;
            }

            // Se EndDate non è successiva a StartDate, la validazione fallisce
            if (endDateValue.Value <= startDateValue.Value)
            {
                // Ottieni il nome visualizzato della proprietà StartDate (se disponibile)
                string? startDateDisplayName = startDateProperty.GetCustomAttribute<DisplayAttribute>()?.GetName()
                                               ?? _startDatePropertyName;

                // Ottieni il nome visualizzato della proprietà corrente (EndDate)
                string? endDateDisplayName = validationContext.DisplayName;

                // Formatta il messaggio di errore usando le risorse o il messaggio di default
                // Il metodo base FormatErrorMessage gestisce ErrorMessageResourceType/Name
                string errorMessage = FormatErrorMessage(endDateDisplayName ?? validationContext.MemberName!);

                // Aggiungi entrambi i nomi dei membri all'errore per evidenziare entrambi i campi
                return new ValidationResult(errorMessage, new[] { validationContext.MemberName!, _startDatePropertyName });
            }

            // Se EndDate è successiva a StartDate, la validazione passa
            return ValidationResult.Success;
        }

        // Override di FormatErrorMessage per fornire un messaggio di default più specifico
        // se ErrorMessageResourceType/Name non sono stati specificati sull'attributo.
        public override string FormatErrorMessage(string name)
        {
            // Se ErrorMessage o ErrorMessageResourceName sono impostati, il metodo base li userà.
            // Altrimenti, usiamo questo messaggio di default specifico per DateRange.
            // Assicura che le risorse siano accessibili
            string? defaultMessage = ErrorMessageString; // Prende il messaggio dalle risorse se specificato, altrimenti null

            if (string.IsNullOrEmpty(defaultMessage))
            {
                // Messaggio di fallback hardcoded se nessuna risorsa è specificata
                defaultMessage = "{0} must be after {1}."; // {0} = EndDate, {1} = StartDate
            }

            string? startDateDisplayName = _startDatePropertyName; // Default al nome della proprietà
            // Potremmo provare a ottenere il DisplayName anche qui, ma è più complesso senza ValidationContext
            // PropertyInfo? startDateProperty = ... (richiede il tipo dell'oggetto)

            return string.Format(CultureInfo.CurrentCulture, defaultMessage, name, startDateDisplayName);
        }
    }
}