// WpfMvvmApp/Services/ICalService.cs
using System.Collections.Generic;
using WpfMvvmApp.Models; // Necessario per Lesson

namespace WpfMvvmApp.Services
{
    public interface ICalService
    {
        /// <summary>
        /// Esporta una lista di lezioni in formato iCalendar su un file.
        /// </summary>
        /// <param name="lessons">La lista di lezioni da esportare.</param>
        /// <param name="filePath">Il percorso completo del file .ics dove salvare.</param>
        /// <param name="contractInfo">Informazioni opzionali sul contratto da includere nel calendario.</param>
        /// <returns>True se l'esportazione è riuscita, false altrimenti.</returns>
        bool ExportLessons(IEnumerable<Lesson> lessons, string filePath, string? contractInfo = null);

        /// <summary>
        /// Importa lezioni da un file iCalendar.
        /// </summary>
        /// <param name="filePath">Il percorso completo del file .ics da importare.</param>
        /// <returns>Una lista delle lezioni importate. Restituisce una lista vuota in caso di errore o se il file non contiene eventi validi.</returns>
        List<Lesson> ImportLessons(string filePath);
    }
}