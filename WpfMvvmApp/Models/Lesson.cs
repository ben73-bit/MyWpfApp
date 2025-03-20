using System;
using System.ComponentModel.DataAnnotations;

namespace WpfMvvmApp.Models
{
    public class Lesson
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }      // Data della lezione

        [Required(ErrorMessage = "Duration is required.")]
        public TimeSpan Duration { get; set; }    // Durata della lezione

        [Required(ErrorMessage = "Contract is required.")]
        public required Contract Contract { get; set; }  // Contratto a cui si riferisce la lezione
    }
}