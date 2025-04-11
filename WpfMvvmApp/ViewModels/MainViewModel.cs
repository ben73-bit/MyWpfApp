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
                    // Aggiorna CanExecute dei comandi che dipendono dalla selezione
                    (SaveContractCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Usa CanExecuteSaveContract
                    (RemoveContractCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Usa CanExecuteRemoveContract
                    _selectedContract?.UpdateCommandStates(); // Notifica il VM figlio
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
            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            // Passa il metodo CanExecute corretto
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
                    // Passa i servizi (assicurati che ContractViewModel accetti Contract e non Contract?)
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
        }

        // Metodo per salvare (simulato)
        private void ExecuteSaveContract(object? parameter)
        {
             // Usa IsContractValid per il check
             if (SelectedContract?.Contract != null && SelectedContract.IsContractValid)
             {
                 MessageBox.Show($"Contract '{SelectedContract.Company}' changes noted (will be saved on exit).", "Save Contract", MessageBoxButton.OK, MessageBoxImage.Information);
             }
        }
        // MODIFICATO: CanExecuteSaveContract usa IsContractValid
        private bool CanExecuteSaveContract(object? parameter)
        {
            // Puoi salvare solo se un contratto è selezionato ed è valido (il contratto, non l'input lezione)
            return SelectedContract != null && SelectedContract.IsContractValid;
        }

        // Metodo per rimuovere il contratto selezionato
        private void ExecuteRemoveContract(object? parameter)
        {
            var contractToRemoveVM = parameter as ContractViewModel ?? SelectedContract;
            // Aggiunto controllo null su contractToRemoveVM per sicurezza
            if (contractToRemoveVM?.Contract == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete the contract '{contractToRemoveVM.Company} - {contractToRemoveVM.ContractNumber}'?\nThis will also delete all associated lessons.",
                                         "Confirm Contract Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // CORRETTO: Gestione null più sicura per _currentUser.Contracts
                bool removedFromModel = _currentUser.Contracts?.Remove(contractToRemoveVM.Contract) ?? false;
                bool removedFromVMCollection = Contracts.Remove(contractToRemoveVM);

                if (removedFromModel || removedFromVMCollection)
                {
                    Debug.WriteLine($"Contract '{contractToRemoveVM.ContractNumber}' removed.");
                    SelectedContract = Contracts.FirstOrDefault();
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        // CanExecuteRemoveContract rimane corretto
        private bool CanExecuteRemoveContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // Metodo per salvare tutti i dati (usato all'uscita)
        private void ExecuteSaveAllData(object? parameter = null) { /* ... implementazione esistente ... */ }
        private void OnApplicationExit(object sender, ExitEventArgs e) { /* ... implementazione esistente ... */ }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}