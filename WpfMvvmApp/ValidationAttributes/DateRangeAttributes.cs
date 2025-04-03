// WpfMvvmApp/ValidationAttributes/DateRangeAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection; // Necessario per PropertyInfo

// Assicurati che il namespace corrisponda alla struttura delle cartelle!
namespace WpfMvvmApp.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        // Il costruttore accetta il nome della proprietà della data di inizio
        public DateRangeAttribute(string startDatePropertyName)
        {
            if (string.IsNullOrEmpty(startDatePropertyName))
            {
                throw new ArgumentNullException(nameof(startDatePropertyName));
            }
            _startDatePropertyName = startDatePropertyName;
        }

        // Metodo principale per la validazione
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Ottieni informazioni sulla proprietà della data di inizio usando Reflection
            PropertyInfo? startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);

            if (startDateProperty == null)
            {
                // Errore: la proprietà specificata nel costruttore non esiste
                return new ValidationResult($"Error: Unknown property '{_startDatePropertyName}'.");
            }

            // Ottieni il valore effettivo della data di inizio dall'oggetto che stiamo validando
            object? startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);

            // Il 'value' passato a questo metodo è il valore della proprietà EndDate (su cui è applicato l'attributo)
            // Se EndDate o StartDate non sono date valide (es. null), lasciamo che [Required] se ne occupi.
            // Questa validazione si concentra solo sul confronto se entrambe sono date.
            if (value is not DateTime endDate || startDateValue is not DateTime startDate)
            {
                return ValidationResult.Success; // Non è compito nostro validare se sono null, ma solo il range
            }

            // Esegui il confronto: EndDate deve essere successiva a StartDate
            if (endDate <= startDate)
            {
                // Costruisci il messaggio di errore
                string errorMessage = ErrorMessage ?? // Usa il messaggio fornito nell'attributo...
                                    $"{validationContext.DisplayName} must be after the {_startDatePropertyName}."; // ...o crea un messaggio di default

                // Restituisci l'errore di validazione
                return new ValidationResult(errorMessage);
            }

            // Se siamo qui, le date sono nell'ordine corretto
            return ValidationResult.Success;
        }
    }
}