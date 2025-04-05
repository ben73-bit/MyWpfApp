// WpfMvvmApp/Services/DialogService.cs
using Microsoft.Win32; // Necessario per OpenFileDialog e SaveFileDialog

namespace WpfMvvmApp.Services
{
    public class DialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open File")
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true
            };

            bool? result = openFileDialog.ShowDialog(); // Mostra la finestra di dialogo

            if (result == true)
            {
                return openFileDialog.FileName; // Restituisce il percorso selezionato
            }
            else
            {
                return null; // L'utente ha annullato
            }
        }

        public string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string defaultFileName = "", string title = "Save File As")
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                Title = title,
                FileName = defaultFileName,
                OverwritePrompt = true // Chiede conferma se il file esiste già
            };

            bool? result = saveFileDialog.ShowDialog(); // Mostra la finestra di dialogo

            if (result == true)
            {
                return saveFileDialog.FileName; // Restituisce il percorso scelto
            }
            else
            {
                return null; // L'utente ha annullato
            }
        }
    }
}