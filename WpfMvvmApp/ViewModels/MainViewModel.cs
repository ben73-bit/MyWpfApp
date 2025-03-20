using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _newUsername = "";
        private string _usernameValidationMessage = "";

        public required User User { get; set; }
        public string NewUsername
        {
            get { return _newUsername; }
            set
            {
                if (_newUsername != value)
                {
                    _newUsername = value;
                    OnPropertyChanged();
                    
                    // Validazione dopo aver aggiornato il valore
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        UsernameValidationMessage = "Username cannot be empty.";
                    }
                    else if (value.Length > 20)
                    {
                        UsernameValidationMessage = "Username cannot be longer than 20 characters.";
                    }
                    else
                    {
                        UsernameValidationMessage = "";
                    }
                    
                    // Avvisa che il comando può essere eseguito o meno
                    ((RelayCommand)UpdateUsernameCommand).RaiseCanExecuteChanged();
                }
            }
        }
        
        public string UsernameValidationMessage
        {
            get { return _usernameValidationMessage; }
            set
            {
                if (_usernameValidationMessage != value)
                {
                    _usernameValidationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Inizializzazione diretta con null! per indicare al compilatore che sarà inizializzato
        public ICommand UpdateUsernameCommand { get; private init; } = null!;

        public MainViewModel()
        {
            try
            {
                // Inizializzazione esplicita dell'oggetto User
                User = new User { Username = "Simone Benzi" };
                UpdateUsernameCommand = new RelayCommand(UpdateUsername, CanUpdateUsername);
                
                // Imposta NewUsername dopo aver inizializzato i comandi e le validazioni
                NewUsername = User.Username;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MainViewModel constructor: {ex}");
                // In alternativa, puoi usare un debugger per esaminare l'eccezione
                // System.Diagnostics.Debugger.Break();
            }
        }

        private bool CanUpdateUsername(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(NewUsername) && 
                   NewUsername.Length <= 20 &&
                   string.IsNullOrEmpty(UsernameValidationMessage);
        }

        private void UpdateUsername(object? parameter)
        {
            if (CanUpdateUsername(parameter))
            {
                User.Username = NewUsername;
                OnPropertyChanged(nameof(User));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}