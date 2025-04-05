// WpfMvvmApp/Services/IDialogService.cs
namespace WpfMvvmApp.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Mostra la finestra di dialogo "Apri File".
        /// </summary>
        /// <param name="filter">Il filtro per i tipi di file (es. "iCalendar files (*.ics)|*.ics|All files (*.*)|*.*").</param>
        /// <param name="title">Il titolo della finestra di dialogo.</param>
        /// <returns>Il percorso completo del file selezionato, o null se l'utente annulla.</returns>
        string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open File");

        /// <summary>
        /// Mostra la finestra di dialogo "Salva File".
        /// </summary>
        /// <param name="filter">Il filtro per i tipi di file.</param>
        /// <param name="defaultFileName">Il nome file suggerito.</param>
        /// <param name="title">Il titolo della finestra di dialogo.</param>
        /// <returns>Il percorso completo dove salvare il file, o null se l'utente annulla.</returns>
        string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string defaultFileName = "", string title = "Save File As");

        // Potremmo aggiungere altri metodi qui in futuro (es. ShowFolderBrowserDialog, ShowMessage)
    }
}