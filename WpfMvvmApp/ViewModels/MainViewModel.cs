using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _newUsername = "";
        private string _usernameValidationMessage = "";
        private User _user = null!; // Inizializzato nel costruttore
        private ICommand? _updateUsernameCommand;
        private ContractViewModel? _selectedContract;

        public User User
        {
            get { return _user; }
            set
            {
                _user = value;
                OnPropertyChanged();
            }
        }

        public string NewUsername
        {
            get { return _newUsername; }
            set
            {
                // Validazione
                if (string.IsNullOrWhiteSpace(value))
                {
                    UsernameValidationMessage = "Username cannot be empty.";
                    // Non aggiorna _newUsername quando non valido
                }
                else if (value.Length > 20)
                {
                    UsernameValidationMessage = "Username cannot be longer than 20 characters.";
                    // Non aggiorna _newUsername quando non valido
                }
                else
                {
                    UsernameValidationMessage = "";
                    if (_newUsername != value)
                    {
                        _newUsername = value;
                        OnPropertyChanged();
                    }
                }

                // Informa il comando che lo stato di CanExecute potrebbe essere cambiato
                if (UpdateUsernameCommand is RelayCommand command)
                {
                    command.RaiseCanExecuteChanged();
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

        public ICommand UpdateUsernameCommand
        {
            get
            {
                _updateUsernameCommand ??= new RelayCommand(UpdateUsername, CanUpdateUsername);
                return _updateUsernameCommand;
            }
        }

        // Proprietà per la gestione dei contratti
        public ObservableCollection<ContractViewModel> Contracts { get; } = new ObservableCollection<ContractViewModel>();

        public ContractViewModel? SelectedContract
        {
            get { return _selectedContract; }
            set
            {
                if (_selectedContract != value)
                {
                    // Rimuovi l'handler degli eventi dal vecchio contratto
                    if (_selectedContract != null)
                        _selectedContract.PropertyChanged -= Contract_PropertyChanged;
                    
                    _selectedContract = value;
                    
                    // Aggiungi l'handler degli eventi al nuovo contratto
                    if (_selectedContract != null)
                        _selectedContract.PropertyChanged += Contract_PropertyChanged;
                    
                    OnPropertyChanged();
                    
                    // Notifica che lo stato del comando potrebbe essere cambiato
                    InvalidateSaveCommand();
                    
                    // Mantieni anche il comportamento originale
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand AddNewContractCommand { get; }

        //Inizializza il command. Era questo il problema, si dichiarava la variabile ma non la si inizializzava.
        public ICommand SaveContractCommand { get; private set; } = null!;

        public MainViewModel()
        {
            try
            {
                // Inizializza l'utente
                User = new User { Username = "Simone Benzi" };

                // Imposta il valore iniziale di NewUsername dopo aver inizializzato User
                // Questa chiamata attiverà la validazione
                NewUsername = User.Username;

                // Inizializza i command
                AddNewContractCommand = new RelayCommand(AddNewContract);
                SaveContractCommand = new RelayCommand(SaveContract, CanSaveContract); //Inizializza il command Save

                // Inizializza i contratti di esempio
                Contracts.Add(new ContractViewModel(new Contract { Company = "Azienda 1", ContractNumber = "Contratto 1", HourlyRate = 50, TotalHours = 100 }));
                Contracts.Add(new ContractViewModel(new Contract { Company = "Azienda 2", ContractNumber = "Contratto 2", HourlyRate = 60, TotalHours = 120 }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MainViewModel constructor: {ex}");
                // Considerare di propagare l'eccezione o gestirla meglio
                throw;
            }
        }

        // Metodo per ascoltare i cambiamenti nel contratto selezionato
        private void Contract_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Quando qualsiasi proprietà del contratto cambia, invalidare il comando Save
            InvalidateSaveCommand();
        }

        // Metodo di utilità per invalidare il comando Save
        private void InvalidateSaveCommand()
        {
            if (SaveContractCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }

        private bool CanUpdateUsername(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(NewUsername) &&
                   NewUsername.Length <= 20 &&
                   string.IsNullOrEmpty(UsernameValidationMessage);
        }

        private bool CanSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsValid;
        }

        private void UpdateUsername(object? parameter)
        {
            if (CanUpdateUsername(parameter))
            {
                User.Username = NewUsername;
                OnPropertyChanged(nameof(User));
            }
        }

        // Metodo per aggiungere un nuovo contratto
        private void AddNewContract(object? parameter)
        {
            // Creazione di un nuovo contratto
            Contract newContract = new Contract
            {
                Company = "Nuova Azienda",
                ContractNumber = "Nuovo Contratto",
                HourlyRate = 0,
                TotalHours = 0
            };

            // Creazione di un nuovo contratto view model
            ContractViewModel newContractViewModel = new ContractViewModel(newContract);

            // Aggiunta del contratto alla lista
            Contracts.Add(newContractViewModel);
        }

        //Salva contratto
        private void SaveContract(object? parameter)
        {
            if (SelectedContract != null && SelectedContract.IsValid)
            {
                // In questo caso non facciamo altro che aggiornare le proprietà del contract view model.
                // In un caso reale potremmo persistere i dati nel database.
                
                // Aggiunge un feedback di salvataggio avvenuto (opzionale)
                Console.WriteLine($"Contratto {SelectedContract.ContractNumber} salvato con successo!");
                
                // Aggiorna il comando per la view
                OnPropertyChanged(nameof(Contracts));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}