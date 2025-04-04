// WpfMvvmApp/ValidationAttributes/DateRangeAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace WpfMvvmApp.ValidationAttributes // Assicurati namespace corretto
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public DateRangeAttribute(string startDatePropertyName)
        {
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
                return new ValidationResult($"Error: Unknown property '{_startDatePropertyName}'.");
            }

            // Ottieni i valori come nullable DateTime?
            DateTime? startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance) as DateTime?;
            DateTime? endDateValue = value as DateTime?; // 'value' è EndDate

            // --- MODIFICA CHIAVE: Gestione Null ---
            // Se una delle due date non è impostata (è null), la validazione del range non si applica.
            if (!startDateValue.HasValue || !endDateValue.HasValue)
            {
                return ValidationResult.Success; // Considera valido se una data manca
            }
            // -------------------------------------

            // Se entrambe le date hanno un valore, esegui il confronto
            if (endDateValue.Value <= startDateValue.Value)
            {
                string errorMessage = ErrorMessage ??
                                    $"{validationContext.DisplayName} must be after the {_startDatePropertyName}.";
                return new ValidationResult(errorMessage);
            }

            return ValidationResult.Success;
        }
    }
}