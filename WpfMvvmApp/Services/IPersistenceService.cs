// WpfMvvmApp/Services/IPersistenceService.cs
using WpfMvvmApp.Models; // Necessario per User

namespace WpfMvvmApp.Services
{
    public interface IPersistenceService
    {
        /// <summary>
        /// Carica i dati dell'utente dal meccanismo di persistenza.
        /// </summary>
        /// <returns>L'oggetto User caricato, o un nuovo User se non ci sono dati salvati o si verifica un errore.</returns>
        User LoadUserData();

        /// <summary>
        /// Salva i dati dell'utente nel meccanismo di persistenza.
        /// </summary>
        /// <param name="user">L'oggetto User da salvare.</param>
        /// <returns>True se il salvataggio è riuscito, false altrimenti.</returns>
        bool SaveUserData(User user);
        string GetUserDataPath(); // NUOVO METODO
    }
}