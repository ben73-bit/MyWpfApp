using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _newUsername;
        private string _usernameValidationMessage; // Aggiungi questo campo

        public User User { get; set; }

        public string NewUsername
        {
            get { return _newUsername; }
            set
            {
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
                    UsernameValidationMessage = ""; // Pulisci il messaggio di errore
                    _newUsername = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UsernameValidationMessage
        {
            get { return _usernameValidationMessage; }
            set
            {
                _usernameValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand UpdateUsernameCommand { get; }

        public MainViewModel()
        {
            User = new User { Username = "Simone Benzi" };
            _newUsername = User.Username;
            NewUsername = User.Username;
            UpdateUsernameCommand = new RelayCommand(UpdateUsername);
            _usernameValidationMessage = ""; // Inizializza il campo privato
            UsernameValidationMessage = ""; // Inizializza il messaggio di validazione
        }

        private void UpdateUsername(object? parameter)
        {
            User.Username = NewUsername;
            OnPropertyChanged(nameof(User));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}