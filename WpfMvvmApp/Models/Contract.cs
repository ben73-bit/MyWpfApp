using System;
using System.ComponentModel.DataAnnotations;

namespace WpfMvvmApp.Models
{
    public class Contract
    {
        [Required(ErrorMessage = "Company is required.")]
        public required string Company { get; set; }       // Azienda committente

        [Required(ErrorMessage = "Contract Number is required.")]
        public required string ContractNumber { get; set; }  // Numero contratto

        [Range(0.01, double.MaxValue, ErrorMessage = "Hourly Rate must be greater than 0.")]
        public decimal HourlyRate { get; set; }             // Compenso orario

        [Range(1, int.MaxValue, ErrorMessage = "Total Hours must be greater than 0.")]
        public int TotalHours { get; set; }                // Monte ore complessivo

        [Range(0, int.MaxValue, ErrorMessage = "Billed Hours cannot be negative.")]
        public int BilledHours { get; set; }               // Ore fatturate

        [DateRange(ErrorMessage = "Start Date must be before End Date.")]
        public DateTime StartDate { get; set; }            // Data di inizio

        public DateTime EndDate { get; set; }              // Data di fine
    }

    // Custom attribute per la validazione delle date
    public class DateRangeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance as Contract;
            if (instance != null && instance.StartDate > instance.EndDate)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}