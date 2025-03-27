using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.ViewModels; // Assicurati che questo using sia presente

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
                    _selectedContract = value;
                    OnPropertyChanged();
                    //Informiamo anche gli eventi in caso di salvataggio o cambio selezione
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand AddNewContractCommand { get; }
        public ICommand SaveContractCommand { get; } = null!;
        
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
                SaveContractCommand = new RelayCommand(SaveContract, CanSaveContract);

                // Inizializza i contratti di esempio
                LoadContracts(); // Chiama il metodo per caricare i contratti
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in MainViewModel constructor: {ex}");
                // Considerare di propagare l'eccezione o gestirla meglio
                throw;
            }
        }

        private void LoadContracts()
        {
            // Crea il primo contratto di esempio
            Contract contract1 = new Contract { Company = "Azienda 1", ContractNumber = "Contratto 1", HourlyRate = 50, TotalHours = 100, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31) };
            ContractViewModel contractVM1 = new ContractViewModel(contract1);
            // Aggiungi lezioni di esempio al primo contratto
            contractVM1.Lessons.Add(new Lesson { Date = new DateTime(2024, 5, 10), Duration = TimeSpan.FromHours(2), Contract = contract1 });
            contractVM1.Lessons.Add(new Lesson { Date = new DateTime(2024, 5, 12), Duration = TimeSpan.FromHours(1.5), Contract = contract1 });
            Contracts.Add(contractVM1);

            // Crea il secondo contratto di esempio
            Contract contract2 = new Contract { Company = "Azienda 2", ContractNumber = "Contratto 2", HourlyRate = 60, TotalHours = 120, StartDate = new DateTime(2024, 3, 15), EndDate = new DateTime(2025, 3, 14) };
            ContractViewModel contractVM2 = new ContractViewModel(contract2);
            // Aggiungi lezioni di esempio al secondo contratto
            contractVM2.Lessons.Add(new Lesson { Date = new DateTime(2024, 5, 11), Duration = TimeSpan.FromHours(3), Contract = contract2 });
            Contracts.Add(contractVM2);
        }


        private bool CanUpdateUsername(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(NewUsername) &&
                   NewUsername.Length <= 20 &&
                   string.IsNullOrEmpty(UsernameValidationMessage);
        }

        private bool CanSaveContract(object? parameter)
        {

            return SelectedContract != null;
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
            //In questo caso non facciamo altro che aggiornare le proprietà del contract view model.
            //In un caso reale potremmo persistere i dati nel database.
            if (SelectedContract != null)
            {
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