// WpfMvvmApp/ViewModels/SortableLessonColumns.cs
using WpfMvvmApp.Models; // Necessario per usare nameof(Lesson.Property)

// Assicurati che questo namespace corrisponda alla posizione del file
// e a quello usato nello XAML (es. xmlns:vm="clr-namespace:WpfMvvmApp.ViewModels")
namespace WpfMvvmApp.ViewModels
{
    /// <summary>
    /// Definisce i nomi delle proprietà della classe Lesson per cui è possibile ordinare.
    /// Usato per evitare stringhe magiche nello XAML come CommandParameter per l'ordinamento.
    /// </summary>
    public static class SortableLessonColumns
    {
        // Usa nameof() per ottenere il nome esatto della proprietà dal modello Lesson
        public static string StartDateTime => nameof(Lesson.StartDateTime);
        public static string Duration => nameof(Lesson.Duration);
        public static string Summary => nameof(Lesson.Summary);
        public static string IsConfirmed => nameof(Lesson.IsConfirmed);
        public static string IsBilled => nameof(Lesson.IsBilled);
        public static string InvoiceNumber => nameof(Lesson.InvoiceNumber);
        public static string InvoiceDate => nameof(Lesson.InvoiceDate);

        // Aggiungi qui altre proprietà di Lesson se vuoi renderle ordinabili
        // Esempio: public static string Location => nameof(Lesson.Location);
    }
}