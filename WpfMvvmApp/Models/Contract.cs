// WpfMvvmApp/Models/Contract.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WpfMvvmApp.ValidationAttributes; // Assicurati namespace corretto

namespace WpfMvvmApp.Models
{
    public class Contract
    {
        [Required(ErrorMessage = "Company name is required.")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contract number is required.")]
        public string ContractNumber { get; set; } = string.Empty;

        // Rendi HourlyRate nullable se anche zero non è valido o se vuoi distinguerlo da "non impostato"
        // [Range(0.01, 10000, ErrorMessage = "Hourly rate must be positive.")]
        public decimal? HourlyRate { get; set; } // Esempio: reso nullable

        // Rendi TotalHours nullable se vuoi distinguerlo da "non impostato"
        // [Range(1, int.MaxValue, ErrorMessage = "Total hours must be at least 1.")]
        public int? TotalHours { get; set; } // Esempio: reso nullable

        // BilledHours probabilmente può rimanere int (0 di default)
        [Range(0, int.MaxValue, ErrorMessage = "Billed hours cannot be negative.")]
        public int BilledHours { get; /* private set; */ }
        // --- MODIFICHE PER DATE FACOLTATIVE ---
        // 1. Rimosso [Required]
        // 2. Cambiato tipo in DateTime?
        public DateTime? StartDate { get; set; } // Non più obbligatorio, può essere null

        // 1. Rimosso [Required]
        // 2. Cambiato tipo in DateTime?
        // 3. L'attributo DateRange deve gestire i null (vedi sotto)
        [DateRange(nameof(StartDate), ErrorMessage = "End date must be after start date.")]
        public DateTime? EndDate { get; set; } // Non più obbligatorio, può essere null
        // --------------------------------------

        public List<Lesson> Lessons { get; set; } = new List<Lesson>();

        // Costruttore opzionale per inizializzare valori di default se necessario
        // public Contract()
        // {
        //     // Non impostare StartDate/EndDate qui se devono essere null di default
        // }
    }
}