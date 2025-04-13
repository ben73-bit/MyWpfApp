// WpfMvvmApp/ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; // Necessario per Debug.WriteLine
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
using WpfMvvmApp.Commands; // Assicurati using corretto

namespace WpfMvvmApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Servizi
        private readonly IDialogService _dialogService = new DialogService();
        private readonly ICalService _calService = new ICalServiceImplementation();
        private readonly IPersistenceService _persistenceService = new JsonPersistenceService();
        private User _currentUser;

        // Collezioni e Selezione
        public ObservableCollection<ContractViewModel> Contracts { get; } = new ObservableCollection<ContractViewModel>();
        private ContractViewModel? _selectedContract;
        public ContractViewModel? SelectedContract
        {
            get => _selectedContract;
            set
            {
                if (SetProperty(ref _selectedContract, value))
                {
                    (SaveContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    _selectedContract?.UpdateCommandStates();
                }
            }
        }

        // Comandi
        public ICommand AddNewContractCommand { get; }
        public ICommand SaveContractCommand { get; }
        public ICommand RemoveContractCommand { get; }
        // NUOVO: Comando per salvataggio esplicito (debug)
        public ICommand SaveAllDataCommand { get; }

        // Costruttore
        public MainViewModel()
        {
            _currentUser = _persistenceService.LoadUserData();

            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            SaveContractCommand = new RelayCommand(ExecuteSaveContract, CanExecuteSaveContract);
            RemoveContractCommand = new RelayCommand(ExecuteRemoveContract, CanExecuteRemoveContract);
            // NUOVO: Inizializza comando SaveAll
            SaveAllDataCommand = new RelayCommand(ExecuteSaveAllData); // Sempre abilitato

            LoadContractsFromUserData();
            Application.Current.Exit += OnApplicationExit;
        }

        // Metodo per popolare i ViewModel dai dati caricati
        private void LoadContractsFromUserData()
        {
            Contracts.Clear();
            // Aggiunto controllo null per _currentUser anche se LoadUserData dovrebbe sempre restituire un oggetto
            if (_currentUser?.Contracts != null)
            {
                foreach (var contractModel in _currentUser.Contracts)
                {
                    // Assicurati che ContractViewModel accetti Contract (non Contract?)
                    var contractVM = new ContractViewModel(contractModel, _dialogService, _calService);
                    Contracts.Add(contractVM);
                }
            }
            SelectedContract = Contracts.FirstOrDefault();
        }


        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = "New Company",
                 ContractNumber = $"CN-{DateTime.Now.Ticks}",
                 TotalHours = 10,
                 Lessons = new List<Lesson>()
             };
             _currentUser.Contracts ??= new List<Contract>();
             _currentUser.Contracts.Add(newContractModel);
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             Contracts.Add(newContractVM);
             SelectedContract = newContractVM;
             // ExecuteSaveAllData(null); // Non salvare automaticamente qui per ora
        }

        private void ExecuteSaveContract(object? parameter)
        {
             if (SelectedContract?.Contract != null && SelectedContract.IsContractValid)
             {
                 MessageBox.Show($"Contract '{SelectedContract.Company}' changes noted (will be saved on exit or via Save All).", "Save Contract", MessageBoxButton.OK, MessageBoxImage.Information);
             }
        }
        private bool CanExecuteSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsContractValid;
        }

        private void ExecuteRemoveContract(object? parameter)
        {
            var contractToRemoveVM = parameter as ContractViewModel ?? SelectedContract;
            if (contractToRemoveVM?.Contract == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete the contract '{contractToRemoveVM.Company} - {contractToRemoveVM.ContractNumber}'?\nThis will also delete all associated lessons.",
                                         "Confirm Contract Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool removedFromModel = _currentUser.Contracts?.Remove(contractToRemoveVM.Contract) ?? false;
                bool removedFromVMCollection = Contracts.Remove(contractToRemoveVM);

                if (removedFromModel || removedFromVMCollection)
                {
                    Debug.WriteLine($"Contract '{contractToRemoveVM.ContractNumber}' removed.");
                    SelectedContract = Contracts.FirstOrDefault();
                    // ExecuteSaveAllData(null); // Non salvare automaticamente qui per ora
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        private bool CanExecuteRemoveContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // *** Metodo per salvare tutti i dati (MODIFICATO per Logging Dettagliato) ***
        private void ExecuteSaveAllData(object? parameter = null)
        {
            if (_currentUser == null)
            {
                Debug.WriteLine("ExecuteSaveAllData: Cannot save data, _currentUser is null.");
                MessageBox.Show("Cannot save data: User data is missing.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Debug.WriteLine("ExecuteSaveAllData: Attempting to save...");
            bool success = false;
            try
            {
                 // Chiama il servizio che ora ha log dettagliato e catch specifici
                 success = _persistenceService.SaveUserData(_currentUser);

                 if(success)
                 {
                     Debug.WriteLine("ExecuteSaveAllData: Save reported successful by service.");
                     // Mostra messaggio di successo solo se chiamato esplicitamente (non all'uscita)
                     if (parameter != null || !(parameter is ExitEventArgs)) // Controlla se non è l'evento Exit
                     {
                          // Potremmo mostrare un messaggio, ma forse è meglio di no per non essere troppo invasivi
                          // MessageBox.Show("All data saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                     }
                 }
                 else
                 {
                     // Il servizio ha restituito false senza eccezioni (improbabile ma gestito)
                     Debug.WriteLine("ExecuteSaveAllData: Save reported FAILED by service (returned false).");
                     MessageBox.Show("Could not save user data. The save operation failed without a specific error.",
                                     "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 }
            }
            catch (Exception ex) // Cattura eccezioni impreviste che potrebbero sfuggire al servizio
            {
                 Debug.WriteLine($"ExecuteSaveAllData: UNEXPECTED Exception during save call: {ex}");
                 MessageBox.Show($"An unexpected error occurred while trying to save data:\n{ex.Message}",
                                 "Critical Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 success = false; // Assicura che success sia false
            }
        }

        // Gestore evento per l'uscita dall'applicazione
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            Debug.WriteLine("OnApplicationExit: Saving data...");
            ExecuteSaveAllData(e); // Passa ExitEventArgs come parametro per distinguerlo
            try { Application.Current.Exit -= OnApplicationExit; } catch { }
        }


        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}