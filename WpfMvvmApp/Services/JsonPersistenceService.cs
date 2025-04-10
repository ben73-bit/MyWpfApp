// WpfMvvmApp/Services/JsonPersistenceService.cs
using System;
using System.Collections.Generic; // Aggiunto per List<T>
using System.IO;
using System.Text.Json;
using WpfMvvmApp.Models;
using System.Diagnostics;
using System.Reflection;

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
            // ReferenceHandler = ReferenceHandler.Preserve // Utile per cicli
        };

        public JsonPersistenceService()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolderName = "WpfMvvmApp";
            _saveFolderPath = Path.Combine(documentsPath, appFolderName);
            _fullPath = Path.Combine(_saveFolderPath, _saveFileName);
            Debug.WriteLine($"Persistence Service Initialized. Save Path: {_fullPath}");
        }

        public User LoadUserData()
        {
            Debug.WriteLine($"Attempting to load user data from: {_fullPath}");
            if (!File.Exists(_fullPath))
            {
                Debug.WriteLine("Save file not found. Returning new User.");
                return new User { Username = "DefaultUser" };
            }
            try
            {
                string jsonString = File.ReadAllText(_fullPath);
                 if (string.IsNullOrWhiteSpace(jsonString)) {
                     Debug.WriteLine("Loaded JSON string is empty or whitespace. Returning new User.");
                     return new User { Username = "DefaultUser" };
                 }
                User? loadedUser = JsonSerializer.Deserialize<User>(jsonString, _jsonOptions);
                if (loadedUser != null)
                {
                    Debug.WriteLine($"User data loaded successfully for user: {loadedUser.Username}");
                    loadedUser.Contracts ??= new List<Contract>();
                    foreach(var contract in loadedUser.Contracts)
                    {
                        contract.Lessons ??= new List<Lesson>();
                        foreach(var lesson in contract.Lessons) { lesson.Contract = contract; }
                    }
                    return loadedUser;
                }
                else
                {
                     Debug.WriteLine("Deserialization resulted in null. Returning new User.");
                     return new User { Username = "DefaultUser" };
                }
            }
            // *** CORRETTO: Usa 'ex' nel Debug.WriteLine ***
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user data from {_fullPath}: {ex.GetType().Name} - {ex.Message}"); // Usa ex
                return new User { Username = "DefaultUser" };
            }
        }

        public bool SaveUserData(User user)
        {
            if (user == null) { Debug.WriteLine("SaveUserData called with null user. Aborting."); return false; }

            Debug.WriteLine($"Attempting to save user data for '{user.Username}' to: {_fullPath}");
            string? jsonString = null;

            try
            {
                Debug.WriteLine($"Ensuring directory exists: {_saveFolderPath}");
                DirectoryInfo dirInfo = Directory.CreateDirectory(_saveFolderPath);
                Debug.WriteLine($"Directory exists or created: {dirInfo.Exists}");

                Debug.WriteLine("Serializing user object...");
                jsonString = JsonSerializer.Serialize(user, _jsonOptions);
                Debug.WriteLine($"Serialization successful. JSON Length: {jsonString?.Length ?? -1}");

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Debug.WriteLine("Serialization resulted in empty or whitespace string. Aborting save.");
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
                 return false;
            }
            catch (UnauthorizedAccessException authEx)
            {
                 Debug.WriteLine($"!!! UNAUTHORIZED ACCESS ERROR saving to {_fullPath}: {authEx.Message}");
                 return false;
            }
            catch (IOException ioEx)
            {
                 Debug.WriteLine($"!!! IO ERROR saving to {_fullPath}: {ioEx.Message}");
                 return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! UNEXPECTED ERROR during save to {_fullPath}: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"   Stack Trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}