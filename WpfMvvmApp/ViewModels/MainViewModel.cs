// WpfMvvmApp/ViewModels/MainViewModel.cs
using System; // Aggiunto per DateTime
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
// Assicurati che RelayCommand sia accessibile
// using WpfMvvmApp.Commands;

namespace WpfMvvmApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Istanziare i servizi
        private readonly IDialogService _dialogService = new DialogService();
        private readonly ICalService _calService = new ICalServiceImplementation();

        // Collezioni e Selezione
        public ObservableCollection<ContractViewModel> Contracts { get; } = new ObservableCollection<ContractViewModel>();

        private ContractViewModel? _selectedContract;
        public ContractViewModel? SelectedContract
        {
            get => _selectedContract;
            set
            {
                // Usa SetProperty per notificare il cambiamento alla UI
                if (SetProperty(ref _selectedContract, value))
                {
                    // Aggiorna CanExecute del comando Save
                    (SaveContractCommand as RelayCommand)?.RaiseCanExecuteChanged();

                    // *** MODIFICA CHIAVE: Notifica il VM selezionato ***
                    _selectedContract?.UpdateCommandStates();
                }
            }
        }

        // Comandi
        public ICommand AddNewContractCommand { get; }
        public ICommand SaveContractCommand { get; }

        // Costruttore
        public MainViewModel()
        {
            // Assicurati che RelayCommand sia accessibile
            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            SaveContractCommand = new RelayCommand(ExecuteSaveContract, CanExecuteSaveContract);
            LoadContracts();
        }

        private void LoadContracts()
        {
            // Qui caricheresti i dati da persistenza
            var exampleUser = CreateExampleUser(); // Dati demo

            Contracts.Clear();
            foreach (var contractModel in exampleUser.Contracts)
            {
                // Passa i servizi al costruttore di ContractViewModel
                var contractVM = new ContractViewModel(contractModel, _dialogService, _calService);
                Contracts.Add(contractVM);
            }
            // Seleziona il primo contratto dopo il caricamento
            SelectedContract = Contracts.FirstOrDefault();
        }

        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = "New Company",
                 ContractNumber = $"CN-{DateTime.Now.Ticks}", // Usa DateTime da System
                 TotalHours = 10
                 // StartDate/EndDate sono null di default (essendo DateTime?)
             };
             // Passa i servizi al costruttore
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             Contracts.Add(newContractVM);
             SelectedContract = newContractVM; // Seleziona il nuovo
        }

        private void ExecuteSaveContract(object? parameter)
        {
             if (SelectedContract?.Contract != null && SelectedContract.IsValid)
             {
                 MessageBox.Show($"Contract '{SelectedContract.Company}' saved (simulated).",
                                 "Save Contract", MessageBoxButton.OK, MessageBoxImage.Information);
                 // Qui andrebbe la logica di persistenza per SelectedContract.Contract
             }
        }

        private bool CanExecuteSaveContract(object? parameter)
        {
            // Puoi salvare solo se un contratto è selezionato ed è valido
            return SelectedContract != null && SelectedContract.IsValid;
        }

        // Funzione helper per dati di esempio
        private User CreateExampleUser()
        {
            var user = new User { Username = "DefaultUser" };
            var contract1 = new Contract { Company = "Tech Solutions", ContractNumber = "TS-001", HourlyRate = 70, TotalHours = 100, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var contract2 = new Contract { Company = "Edu World", ContractNumber = "EW-002", HourlyRate = 65, TotalHours = 50 };

            contract1.Lessons = new List<Lesson>(); // Inizializza lista
            contract1.Lessons.Add(new Lesson { StartDateTime = DateTime.Today.AddDays(-5).AddHours(10), Duration = TimeSpan.FromHours(2), Contract = contract1, Summary = "Setup" });
            contract1.Lessons.Add(new Lesson { StartDateTime = DateTime.Today.AddDays(-3).AddHours(14), Duration = TimeSpan.FromMinutes(90), Contract = contract1, Summary = "Training 1", IsConfirmed=true });

            user.Contracts = new List<Contract>(); // Inizializza lista
            user.Contracts.Add(contract1);
            user.Contracts.Add(contract2);
            return user;
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}