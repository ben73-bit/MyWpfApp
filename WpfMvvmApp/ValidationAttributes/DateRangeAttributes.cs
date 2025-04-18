// WpfMvvmApp/ValidationAttributes/DateRangeAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using WpfMvvmApp.Properties; // AGGIUNGI using

namespace WpfMvvmApp.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public DateRangeAttribute(string startDatePropertyName)
            // Usa la chiave di risorsa come messaggio di default
            : base(() => Resources.Validation_EndDateAfterStartDate ?? "{0} must be after {1}.")
        {
            if (string.IsNullOrEmpty(startDatePropertyName)) throw new ArgumentNullException(nameof(startDatePropertyName));
            _startDatePropertyName = startDatePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // ... (logica IsValid esistente che gestisce null) ...
            PropertyInfo? startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null) return new ValidationResult($"Error: Unknown property '{_startDatePropertyName}'.");
            DateTime? startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance) as DateTime?;
            DateTime? endDateValue = value as DateTime?;
            if (!startDateValue.HasValue || !endDateValue.HasValue) return ValidationResult.Success;
            if (endDateValue.Value <= startDateValue.Value)
            {
                // Formatta il messaggio di errore (usa quello base o quello specificato nell'attributo)
                 string specificStartDateName = validationContext.ObjectType.GetProperty(_startDatePropertyName)?.Name ?? _startDatePropertyName;
                 return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), new[] { validationContext.MemberName!, specificStartDateName });
                 // Alternativa più semplice se non serve formattazione complessa:
                 // return new ValidationResult(ErrorMessageString);
            }
            return ValidationResult.Success;
            // --- FINE Logica Esistente ---
        }

         // Override per formattare il messaggio (se necessario, non indispensabile qui)
        // public override string FormatErrorMessage(string name)
        // {
        //     // Qui potresti usare _startDatePropertyName per costruire un messaggio più specifico
        //     // se non usi ErrorMessageResourceType/Name direttamente nell'attributo
        //     return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, _startDatePropertyName);
        // }
    }
}