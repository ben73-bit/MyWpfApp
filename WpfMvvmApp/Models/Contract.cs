// WpfMvvmApp/Models/Contract.cs
using System;
using System.Collections.Generic; // Aggiunto per List<>
using System.ComponentModel.DataAnnotations;
using WpfMvvmApp.ValidationAttributes; // Assumendo che DateRangeAttribute sia qui

namespace WpfMvvmApp.Models
{
    public class Contract
    {
        [Required(ErrorMessage = "Company name is required.")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contract number is required.")]
        public string ContractNumber { get; set; } = string.Empty;

        [Range(0.01, 10000, ErrorMessage = "Hourly rate must be positive.")]
        public decimal HourlyRate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Total hours must be at least 1.")]
        public int TotalHours { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Billed hours cannot be negative.")]
        public int BilledHours { get; set; } // Questo potrebbe essere calcolato

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required.")]
        [DateRange(nameof(StartDate), ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

        // NUOVO: Lista delle lezioni associate a questo contratto
        // Inizializzata per evitare NullReferenceException
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}