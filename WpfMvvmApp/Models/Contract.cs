// WpfMvvmApp/Models/Contract.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WpfMvvmApp.ValidationAttributes;
using WpfMvvmApp.Properties; // AGGIUNGI using per Resources

namespace WpfMvvmApp.Models
{
    public class Contract // Assicurati NON implementi INotifyPropertyChanged qui
    {
        // MODIFICATO: Attributi Required
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FieldRequired")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FieldRequired")]
        public string ContractNumber { get; set; } = string.Empty;

        // MODIFICATO: Attributi Range (assumendo nullable)
        // Se non è nullable, potresti aggiungere anche Required
        [Range(0.01, (double)decimal.MaxValue, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_PositiveNumber")]
        public decimal? HourlyRate { get; set; }

        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_MinimumValue")]
        public int? TotalHours { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_CannotBeNegative")]
        public int BilledHours { get; set; } // Lasciato int non nullable

        // Rimosso Required perché li abbiamo resi nullable/facoltativi
        public DateTime? StartDate { get; set; }

        // MODIFICATO: DateRange usa risorsa
        [DateRange(nameof(StartDate), ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_EndDateAfterStartDate")]
        public DateTime? EndDate { get; set; }

        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}