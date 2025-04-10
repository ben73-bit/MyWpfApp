// WpfMvvmApp/ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
using WpfMvvmApp.Commands;// Assicurati che RelayCommand sia accessibile
// using WpfMvvmApp.Commands;

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

        // Costruttore
        public MainViewModel()
        {
            _currentUser = _persistenceService.LoadUserData();

            // Assicurati che RelayCommand sia accessibile
            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            SaveContractCommand = new RelayCommand(ExecuteSaveContract, CanExecuteSaveContract);
            RemoveContractCommand = new RelayCommand(ExecuteRemoveContract, CanExecuteRemoveContract);

            LoadContractsFromUserData();
            Application.Current.Exit += OnApplicationExit;
        }

        // Metodo per popolare i ViewModel dai dati caricati
        private void LoadContractsFromUserData()
        {
            Contracts.Clear();
            if (_currentUser?.Contracts != null)
            {
                foreach (var contractModel in _currentUser.Contracts)
                {
                    var contractVM = new ContractViewModel(contractModel, _dialogService, _calService);
                    Contracts.Add(contractVM);
                }
            }
            SelectedContract = Contracts.FirstOrDefault();
        }

        // Metodo per aggiungere un nuovo contratto
        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = "New Company", // Default Company Name
                 ContractNumber = $"CN-{DateTime.Now.Ticks}", // Default Contract Number
                 TotalHours = 10, // Default Total Hours
                 Lessons = new List<Lesson>() // Inizializza lista lezioni vuota
                 // StartDate e EndDate saranno null di default (essendo DateTime?)
                 // HourlyRate sarà null di default (essendo decimal?)
             };

             // Assicura che la lista contratti nell'utente esista
             _currentUser.Contracts ??= new List<Contract>();
             // Aggiungi il nuovo modello alla lista dati
             _currentUser.Contracts.Add(newContractModel);

             // Crea il ViewModel corrispondente passando i servizi
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             // Aggiungi il ViewModel alla collezione osservabile (aggiorna UI)
             Contracts.Add(newContractVM);
             // Seleziona il nuovo contratto nella UI
             SelectedContract = newContractVM;

             // Opzionale: Salva subito dopo l'aggiunta
             // ExecuteSaveAllData(null);
        }

        // Metodo per salvare (attualmente simulato)
        private void ExecuteSaveContract(object? parameter)
        {
             if (SelectedContract?.Contract != null && SelectedContract.IsValid)
             {
                 MessageBox.Show($"Contract '{SelectedContract.Company}' changes noted (will be saved on exit).", "Save Contract", MessageBoxButton.OK, MessageBoxImage.Information);
                 // La logica di salvataggio effettiva è in OnApplicationExit
             }
        }
        private bool CanExecuteSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsValid;
        }

        // Metodo per rimuovere il contratto selezionato
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
                    SelectedContract = Contracts.FirstOrDefault(); // Seleziona il primo rimasto
                    // ExecuteSaveAllData(null); // Salva subito?
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        private bool CanExecuteRemoveContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // Metodo per salvare tutti i dati (usato all'uscita)
        private void ExecuteSaveAllData(object? parameter = null)
        {
            if (_currentUser == null) { Debug.WriteLine("Cannot save data, currentUser is null."); return; }
            bool success = false;
            try
            {
                success = _persistenceService.SaveUserData(_currentUser);
                if (success) { Debug.WriteLine("User data saved successfully."); }
                else { Debug.WriteLine("Failed to save user data (SaveUserData returned false)."); MessageBox.Show("Could not save user data. An unknown error occurred.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            catch (Exception ex) { Debug.WriteLine($"Error saving user data: {ex}"); MessageBox.Show($"Could not save user data.\n\nError: {ex.GetType().Name}\nMessage: {ex.Message}\n\nCheck permissions or disk space.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error); success = false; }
        }

        // Gestore evento per l'uscita dall'applicazione
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            ExecuteSaveAllData(null);
            try { Application.Current.Exit -= OnApplicationExit; } catch { }
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}