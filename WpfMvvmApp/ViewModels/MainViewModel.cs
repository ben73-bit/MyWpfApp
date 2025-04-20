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
using WpfMvvmApp.Commands;
using WpfMvvmApp.Properties; // Aggiungi using per Resources

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
                    (DuplicateContractCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiorna CanExecute del nuovo comando
                    _selectedContract?.UpdateCommandStates(); // Aggiorna stati comandi interni al ContractViewModel
                }
            }
        }

        // Comandi
        public ICommand AddNewContractCommand { get; }
        public ICommand SaveContractCommand { get; }
        public ICommand RemoveContractCommand { get; }
        public ICommand SaveAllDataCommand { get; }
        public ICommand DuplicateContractCommand { get; } // NUOVO COMANDO

        // Costruttore
        public MainViewModel()
        {
            _currentUser = _persistenceService.LoadUserData();

            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            SaveContractCommand = new RelayCommand(ExecuteSaveContract, CanExecuteSaveContract);
            RemoveContractCommand = new RelayCommand(ExecuteRemoveContract, CanExecuteRemoveContract);
            SaveAllDataCommand = new RelayCommand(ExecuteSaveAllData);
            DuplicateContractCommand = new RelayCommand(ExecuteDuplicateContract, CanExecuteDuplicateContract); // NUOVO: Inizializza comando

            LoadContractsFromUserData();
            Application.Current.Exit += OnApplicationExit;
        }

        // Metodo per popolare i ViewModel dai dati caricati
        private void LoadContractsFromUserData()
        {
            Contracts.Clear();
            if (_currentUser?.Contracts != null)
            {
                foreach (var contractModel in _currentUser.Contracts.OrderBy(c => c.Company).ThenBy(c => c.ContractNumber)) // Aggiunto piccolo ordinamento
                {
                    var contractVM = new ContractViewModel(contractModel, _dialogService, _calService);
                    Contracts.Add(contractVM);
                }
            }
            // Seleziona il primo o null se la lista è vuota
            SelectedContract = Contracts.FirstOrDefault();
        }


        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = Resources.NewContractDefault_CompanyName ?? "New Company", // Usa risorsa
                 ContractNumber = $"{Resources.NewContractDefault_NumberPrefix ?? "CN"}-{DateTime.Now.Ticks}", // Usa risorsa
                 TotalHours = 10, // Valore di default
                 HourlyRate = 0, // Valore di default
                 StartDate = DateTime.Today, // Default ragionevole
                 // EndDate = null, // Lascia null di default
                 Lessons = new List<Lesson>() // Lista vuota
             };
             _currentUser.Contracts ??= new List<Contract>();
             _currentUser.Contracts.Add(newContractModel);
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             Contracts.Add(newContractVM);
             SelectedContract = newContractVM; // Seleziona il nuovo contratto aggiunto
        }

        private void ExecuteSaveContract(object? parameter)
        {
             if (SelectedContract?.Contract != null && SelectedContract.IsContractValid)
             {
                 // Messaggio aggiornato per usare le risorse
                 MessageBox.Show(string.Format(Resources.MsgBox_SaveContractNoted_Text ?? "Contract '{0}' changes noted (will be saved on exit or via Save All).", SelectedContract.Company),
                                 Resources.MsgBox_Title_SaveContract ?? "Save Contract",
                                 MessageBoxButton.OK, MessageBoxImage.Information);
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

            // Messaggio aggiornato per usare le risorse
            var result = MessageBox.Show(string.Format(Resources.MsgBox_ConfirmContractDeletion_Text ?? "Are you sure you want to delete the contract '{0} - {1}'?\nThis will also delete all associated lessons.", contractToRemoveVM.Company, contractToRemoveVM.ContractNumber),
                                         Resources.MsgBox_Title_ConfirmContractDeletion ?? "Confirm Contract Deletion",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool removedFromModel = _currentUser.Contracts?.Remove(contractToRemoveVM.Contract) ?? false;
                bool removedFromVMCollection = Contracts.Remove(contractToRemoveVM);

                if (removedFromModel || removedFromVMCollection)
                {
                    Debug.WriteLine($"Contract '{contractToRemoveVM.ContractNumber}' removed.");
                    SelectedContract = Contracts.FirstOrDefault(); // Seleziona il primo rimanente
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        private bool CanExecuteRemoveContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // *** NUOVO: Metodo Esecuzione Duplica Contratto ***
        private void ExecuteDuplicateContract(object? parameter)
        {
            if (SelectedContract?.Contract == null) return;

            var originalContract = SelectedContract.Contract;

            // Crea il nuovo modello copiando i dati
            var duplicatedContractModel = new Contract
            {
                // Aggiunge " - Copy" al nome e numero per distinguerlo
                Company = $"{originalContract.Company} ({Resources.DuplicateContractSuffix_Company ?? "Copy"})",
                ContractNumber = $"{originalContract.ContractNumber} ({Resources.DuplicateContractSuffix_Number ?? "Copy"})",
                HourlyRate = originalContract.HourlyRate,
                TotalHours = originalContract.TotalHours,
                StartDate = originalContract.StartDate,
                EndDate = originalContract.EndDate,
                // IMPORTANTE: NON copiare le lezioni
                Lessons = new List<Lesson>()
            };

            // Aggiungi il nuovo modello alla lista del User
             _currentUser.Contracts ??= new List<Contract>();
             _currentUser.Contracts.Add(duplicatedContractModel);

             // Crea il nuovo ViewModel
             var newContractVM = new ContractViewModel(duplicatedContractModel, _dialogService, _calService);

             // Aggiungi il nuovo ViewModel alla ObservableCollection
             Contracts.Add(newContractVM);

             // Seleziona il contratto appena duplicato
             SelectedContract = newContractVM;
        }

        // *** NUOVO: Metodo CanExecute Duplica Contratto ***
        private bool CanExecuteDuplicateContract(object? parameter)
        {
            // Può duplicare solo se c'è un contratto selezionato
            return SelectedContract != null;
        }


        // Metodo per salvare tutti i dati
        private void ExecuteSaveAllData(object? parameter = null)
        {
            if (_currentUser == null)
            {
                Debug.WriteLine("ExecuteSaveAllData: Cannot save data, _currentUser is null.");
                MessageBox.Show(Resources.MsgBox_SaveErrorUnknown_Text ?? "Cannot save data: User data is missing.",
                                Resources.MsgBox_Title_SaveError ?? "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Debug.WriteLine("ExecuteSaveAllData: Attempting to save...");
            bool success = false;
            try
            {
                 success = _persistenceService.SaveUserData(_currentUser);
                 if(success)
                 {
                     Debug.WriteLine("ExecuteSaveAllData: Save reported successful by service.");
                 }
                 else
                 {
                     Debug.WriteLine("ExecuteSaveAllData: Save reported FAILED by service (returned false).");
                      MessageBox.Show(Resources.MsgBox_SaveError_Text ?? "Could not save user data. The save operation failed.",
                                     Resources.MsgBox_Title_SaveError ?? "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"ExecuteSaveAllData: UNEXPECTED Exception during save call: {ex}");
                 MessageBox.Show(string.Format(Resources.MsgBox_SaveErrorCritical_Text ?? "An unexpected error occurred while trying to save data:\n{0}", ex.Message),
                                 Resources.MsgBox_Title_SaveError ?? "Critical Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 success = false;
            }
        }

        // Gestore evento per l'uscita dall'applicazione
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            Debug.WriteLine("OnApplicationExit: Saving data...");
            ExecuteSaveAllData(e);
            try { Application.Current.Exit -= OnApplicationExit; } catch { }
        }


        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}