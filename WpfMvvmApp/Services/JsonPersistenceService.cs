// WpfMvvmApp/Services/JsonPersistenceService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WpfMvvmApp.Models;
using System.Diagnostics;
using System.Reflection;
using System.Windows; // Aggiunto per MessageBox

namespace WpfMvvmApp.Services
{
    public class JsonPersistenceService : IPersistenceService
    {
        private readonly string _saveFolderPath;
        private readonly string _saveFileName = "UserData.json";
        private readonly string _fullPath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            // ReferenceHandler = ReferenceHandler.Preserve // Scommenta se hai cicli di riferimento complessi
        };

        public JsonPersistenceService()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Usa un nome cartella specifico per la tua applicazione
            string appFolderName = "ControllOreApp"; // Assicurati sia lo stesso nome usato altrove se necessario
            _saveFolderPath = Path.Combine(documentsPath, appFolderName);
            _fullPath = Path.Combine(_saveFolderPath, _saveFileName);
            Debug.WriteLine($"Persistence Service Initialized. Data Path: {_fullPath}");

            // Assicurati che la directory esista all'avvio
             EnsureDataDirectoryExists();
        }

        // Metodo helper per creare la directory se non esiste
        private void EnsureDataDirectoryExists()
        {
             try
             {
                 if (!Directory.Exists(_saveFolderPath))
                 {
                     Directory.CreateDirectory(_saveFolderPath);
                     Debug.WriteLine($"Created data directory: {_saveFolderPath}");
                 }
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"CRITICAL ERROR: Could not create data directory '{_saveFolderPath}'. Data cannot be saved or loaded reliably. Error: {ex.Message}");
                 // Considera di mostrare un errore critico all'utente qui,
                 // perché senza directory l'app non può funzionare correttamente.
                 MessageBox.Show($"Fatal Error: Could not create application data folder at\n{_saveFolderPath}\n\nPlease check permissions.\n\nError: {ex.Message}",
                                 "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }

        // NUOVO: Implementazione di GetUserDataPath
        public string GetUserDataPath()
        {
            return _fullPath;
        }

        public User LoadUserData()
        {
            Debug.WriteLine($"Attempting to load user data from: {_fullPath}");
            if (!File.Exists(_fullPath))
            {
                Debug.WriteLine("Save file not found. Returning new User.");
                return new User { Username = "DefaultUser" }; // Fornisci un utente di default
            }
            try
            {
                string jsonString = File.ReadAllText(_fullPath);
                 if (string.IsNullOrWhiteSpace(jsonString))
                 {
                     Debug.WriteLine("Loaded JSON string is empty or whitespace. Returning new User.");
                     return new User { Username = "DefaultUser" };
                 }
                User? loadedUser = JsonSerializer.Deserialize<User>(jsonString, _jsonOptions);
                if (loadedUser != null)
                {
                    Debug.WriteLine($"User data loaded successfully for user: {loadedUser.Username}");
                    // Inizializza collezioni se null e collega riferimenti Contract nelle Lesson
                    loadedUser.Contracts ??= new List<Contract>();
                    foreach(var contract in loadedUser.Contracts)
                    {
                        contract.Lessons ??= new List<Lesson>();
                        foreach(var lesson in contract.Lessons)
                        {
                            lesson.Contract = contract; // Ripristina riferimento ignorato da JsonIgnore
                        }
                    }
                    return loadedUser;
                }
                else
                {
                     Debug.WriteLine("Deserialization resulted in null. Returning new User.");
                     return new User { Username = "DefaultUser" };
                }
            }
            catch (JsonException jsonEx)
            {
                 Debug.WriteLine($"JSON DESERIALIZATION ERROR loading from {_fullPath}: {jsonEx.Message}");
                 // Potresti voler informare l'utente che il file dati è corrotto
                 MessageBox.Show($"Error reading data file. It might be corrupted.\nLoading default data.\n\nDetails: {jsonEx.Message}",
                                 "Data Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return new User { Username = "DefaultUser" };
            }
            catch (Exception ex) // Altri errori (es. IO)
            {
                Debug.WriteLine($"ERROR loading user data from {_fullPath}: {ex.GetType().Name} - {ex.Message}");
                 MessageBox.Show($"Could not load user data from file:\n{_fullPath}\n\nLoading default data.\n\nError: {ex.Message}",
                                 "Data Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new User { Username = "DefaultUser" };
            }
        }

        public bool SaveUserData(User user)
        {
            if (user == null) { Debug.WriteLine("SaveUserData called with null user. Aborting."); return false; }

            // Assicurati che la directory esista prima di salvare
            EnsureDataDirectoryExists();
            if (!Directory.Exists(_saveFolderPath))
            {
                 // Se EnsureDataDirectoryExists fallisce e mostra un messaggio, potremmo voler uscire qui
                 Debug.WriteLine($"Save aborted because data directory does not exist: {_saveFolderPath}");
                 return false; // O lanciare un'eccezione
            }


            Debug.WriteLine($"Attempting to save user data for '{user.Username}' to: {_fullPath}");
            string? jsonString = null;

            try
            {
                Debug.WriteLine("Serializing user object...");
                jsonString = JsonSerializer.Serialize(user, _jsonOptions);
                Debug.WriteLine($"Serialization successful. JSON Length: {jsonString?.Length ?? -1}");

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Debug.WriteLine("Serialization resulted in empty or whitespace string. Aborting save.");
                     MessageBox.Show("Error saving data: Could not serialize user data.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                Debug.WriteLine($"Writing JSON to file: {_fullPath}");
                File.WriteAllText(_fullPath, jsonString);
                Debug.WriteLine("File write operation completed.");

                Debug.WriteLine("User data save process completed successfully.");
                return true;
            }
            catch (JsonException jsonEx)
            {
                 Debug.WriteLine($"!!! JSON SERIALIZATION ERROR: {jsonEx.Message}");
                 Debug.WriteLine($"   Path: {jsonEx.Path}, Line: {jsonEx.LineNumber}, Pos: {jsonEx.BytePositionInLine}");
                  MessageBox.Show($"Error saving data (Serialization): {jsonEx.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 return false;
            }
            catch (UnauthorizedAccessException authEx)
            {
                 Debug.WriteLine($"!!! UNAUTHORIZED ACCESS ERROR saving to {_fullPath}: {authEx.Message}");
                  MessageBox.Show($"Error saving data: Access denied to path\n{_fullPath}\n\nPlease check permissions.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 return false;
            }
            catch (IOException ioEx)
            {
                 Debug.WriteLine($"!!! IO ERROR saving to {_fullPath}: {ioEx.Message}");
                  MessageBox.Show($"Error saving data (File IO): {ioEx.Message}\nCheck disk space or if file is in use.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! UNEXPECTED ERROR during save to {_fullPath}: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"   Stack Trace: {ex.StackTrace}");
                 MessageBox.Show($"An unexpected error occurred while saving data:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}