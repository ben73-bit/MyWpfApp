// WpfMvvmApp/ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO; // NECESSARIO per File.Copy
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data; // NECESSARIO per ICollectionView e CollectionViewSource
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
using WpfMvvmApp.Commands;
using WpfMvvmApp.Properties;

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
        private ICollectionView? _contractsView;
        public ICollectionView? ContractsView
        {
            get => _contractsView;
            private set => SetProperty(ref _contractsView, value);
        }
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
                    (DuplicateContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    _selectedContract?.UpdateCommandStates();
                }
            }
        }
        private string _currentContractSortProperty = nameof(ContractViewModel.Company);
        private ListSortDirection _currentContractSortDirection = ListSortDirection.Ascending;

        // Comandi
        public ICommand AddNewContractCommand { get; }
        public ICommand SaveContractCommand { get; }
        public ICommand RemoveContractCommand { get; }
        public ICommand SaveAllDataCommand { get; }
        public ICommand DuplicateContractCommand { get; }
        public ICommand SortContractsCommand { get; }
        public ICommand BackupDataCommand { get; } // NUOVO: Backup
        public ICommand RestoreDataCommand { get; } // NUOVO: Ripristino

        // Costruttore
        public MainViewModel()
        {
            _currentUser = _persistenceService.LoadUserData();

            AddNewContractCommand = new RelayCommand(ExecuteAddNewContract);
            SaveContractCommand = new RelayCommand(ExecuteSaveContract, CanExecuteSaveContract);
            RemoveContractCommand = new RelayCommand(ExecuteRemoveContract, CanExecuteRemoveContract);
            SaveAllDataCommand = new RelayCommand(ExecuteSaveAllData);
            DuplicateContractCommand = new RelayCommand(ExecuteDuplicateContract, CanExecuteDuplicateContract);
            SortContractsCommand = new RelayCommand(ExecuteSortContracts);
            // NUOVO: Inizializza comandi Backup/Ripristino
            BackupDataCommand = new RelayCommand(ExecuteBackupData);
            RestoreDataCommand = new RelayCommand(ExecuteRestoreData);

            LoadContractsFromUserData();
            Application.Current.Exit += OnApplicationExit;
        }

        // LoadContractsFromUserData
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
            ContractsView = CollectionViewSource.GetDefaultView(Contracts);
            ApplyContractsSort(); // Applica ordinamento alla vista
            SelectedContract = ContractsView?.Cast<ContractViewModel>().FirstOrDefault(); // Seleziona dalla vista
        }

        // ExecuteAddNewContract
        private void ExecuteAddNewContract(object? parameter)
        {
            var newContractModel = new Contract
            {
                Company = string.Empty, // Campo vuoto
                ContractNumber = string.Empty, // Campo vuoto
                TotalHours = null, // Campo vuoto
                HourlyRate = null, // Campo vuoto
                StartDate = null, // Campo vuoto
                EndDate = null, // Campo vuoto
                Lessons = new List<Lesson>()
            };

            _currentUser.Contracts ??= new List<Contract>();
            _currentUser.Contracts.Add(newContractModel);

            var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
            Contracts.Add(newContractVM); // Aggiunge alla sorgente
            ContractsView?.MoveCurrentTo(newContractVM); // Sposta selezione nella vista
            SelectedContract = newContractVM; // Aggiorna proprietà
        }

        // ExecuteSaveContract
        private void ExecuteSaveContract(object? parameter)
        {
            if (SelectedContract?.Contract != null && SelectedContract.IsContractValid)
            {
                MessageBox.Show(string.Format(Resources.MsgBox_SaveContractNoted_Text ?? "Contract '{0}' changes noted (will be saved on exit or via Save All).", SelectedContract.Company),
                                Resources.MsgBox_Title_SaveContract ?? "Save Contract",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private bool CanExecuteSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsContractValid;
        }

        // ExecuteRemoveContract
        private void ExecuteRemoveContract(object? parameter)
        {
            var contractToRemoveVM = parameter as ContractViewModel ?? SelectedContract;
            if (contractToRemoveVM?.Contract == null) return;

            var result = MessageBox.Show(string.Format(Resources.MsgBox_ConfirmContractDeletion_Text ?? "Are you sure you want to delete the contract '{0} - {1}'?\nThis will also delete all associated lessons.", contractToRemoveVM.Company, contractToRemoveVM.ContractNumber),
                                         Resources.MsgBox_Title_ConfirmContractDeletion ?? "Confirm Contract Deletion",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool removedFromModel = _currentUser.Contracts?.Remove(contractToRemoveVM.Contract) ?? false;
                bool removedFromVMCollection = Contracts.Remove(contractToRemoveVM); // Rimuove da sorgente

                if (removedFromModel || removedFromVMCollection)
                {
                    Debug.WriteLine($"Contract '{contractToRemoveVM.ContractNumber}' removed.");
                    // La vista si aggiorna automaticamente, la selezione potrebbe spostarsi
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        private bool CanExecuteRemoveContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // ExecuteDuplicateContract
        private void ExecuteDuplicateContract(object? parameter)
        {
            if (SelectedContract?.Contract == null) return;
            var originalContract = SelectedContract.Contract;
            var duplicatedContractModel = new Contract
            {
                Company = $"{originalContract.Company} ({Resources.DuplicateContractSuffix_Company ?? "Copy"})",
                ContractNumber = $"{originalContract.ContractNumber} ({Resources.DuplicateContractSuffix_Number ?? "Copy"})",
                HourlyRate = originalContract.HourlyRate,
                TotalHours = originalContract.TotalHours,
                StartDate = originalContract.StartDate,
                EndDate = originalContract.EndDate,
                Lessons = new List<Lesson>()
            };
            _currentUser.Contracts ??= new List<Contract>();
            _currentUser.Contracts.Add(duplicatedContractModel);
            var newContractVM = new ContractViewModel(duplicatedContractModel, _dialogService, _calService);
            Contracts.Add(newContractVM); // Aggiunge alla sorgente
            ContractsView?.MoveCurrentTo(newContractVM); // Sposta selezione nella vista
            SelectedContract = newContractVM; // Aggiorna proprietà
        }
        private bool CanExecuteDuplicateContract(object? parameter)
        {
            return SelectedContract != null;
        }

        // ExecuteSaveAllData
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
                if(success) { Debug.WriteLine("ExecuteSaveAllData: Save reported successful by service."); }
                else
                {
                    Debug.WriteLine("ExecuteSaveAllData: Save reported FAILED by service (returned false).");
                    // Il servizio ora mostra MessageBox più specifici, quindi potremmo rimuovere questo
                    // MessageBox.Show(Resources.MsgBox_SaveError_Text ?? "Could not save user data. The save operation failed.",
                    //               Resources.MsgBox_Title_SaveError ?? "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) // Cattura eccezioni impreviste non gestite dal servizio
            {
                Debug.WriteLine($"ExecuteSaveAllData: UNEXPECTED Exception during save call: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_SaveErrorCritical_Text ?? "An unexpected error occurred while trying to save data:\n{0}", ex.Message),
                                Resources.MsgBox_Title_SaveError ?? "Critical Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                success = false;
            }
        }

        // OnApplicationExit
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            Debug.WriteLine("OnApplicationExit: Saving data...");
            ExecuteSaveAllData(e);
            try { Application.Current.Exit -= OnApplicationExit; } catch { }
        }

        // --- NUOVI Metodi per Backup e Ripristino ---

        private void ExecuteBackupData(object? parameter)
        {
            try
            {
                string currentDataPath = _persistenceService.GetUserDataPath();
                if (!File.Exists(currentDataPath))
                {
                    MessageBox.Show("Current data file not found. Cannot create backup.",
                                    Resources.MsgBox_Title_Backup ?? "Backup Data",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string suggestedFileName = $"ControllOre_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string? backupFilePath = _dialogService.ShowSaveFileDialog("JSON files (*.json)|*.json|All files (*.*)|*.*",
                                                                          suggestedFileName,
                                                                          Resources.BackupDataButton_Content ?? "Backup Data");

                if (!string.IsNullOrEmpty(backupFilePath))
                {
                    if (string.Equals(Path.GetFullPath(backupFilePath), Path.GetFullPath(currentDataPath), StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Backup path cannot be the same as the current data file path.",
                                        Resources.MsgBox_Title_Backup ?? "Backup Data",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    File.Copy(currentDataPath, backupFilePath, true);
                    MessageBox.Show(string.Format(Resources.MsgBox_BackupSuccessful_Text ?? "Data successfully backed up to:\n{0}", backupFilePath),
                                    Resources.MsgBox_Title_Backup ?? "Backup Data",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during backup: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_BackupError_Text ?? "Could not create backup file:\n{0}", ex.Message),
                                Resources.MsgBox_Title_Backup ?? "Backup Data",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteRestoreData(object? parameter)
        {
            var confirmationResult = MessageBox.Show(Resources.MsgBox_ConfirmRestore_Text ?? "WARNING!\nThis will overwrite your current data with the data from the selected backup file.\nThis operation cannot be undone.\n\nAre you sure you want to continue?",
                                                     Resources.MsgBox_Title_Restore ?? "Restore Data",
                                                     MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes) return;

            string? backupFilePath = _dialogService.ShowOpenFileDialog("JSON files (*.json)|*.json|All files (*.*)|*.*",
                                                                      Resources.RestoreDataButton_Content ?? "Restore Data");

            if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath)) return;

            try
            {
                string currentDataPath = _persistenceService.GetUserDataPath();

                if (string.Equals(Path.GetFullPath(backupFilePath), Path.GetFullPath(currentDataPath), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Restore path cannot be the same as the current data file path.",
                                    Resources.MsgBox_Title_Restore ?? "Restore Data",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                File.Copy(backupFilePath, currentDataPath, true);
                Debug.WriteLine($"Data file restored from: {backupFilePath}");

                // Ricarica dati
                _currentUser = _persistenceService.LoadUserData();
                LoadContractsFromUserData(); // Aggiorna collezioni e vista

                MessageBox.Show(Resources.MsgBox_RestoreSuccessful_Text ?? "Data successfully restored. Application reloaded.",
                                Resources.MsgBox_Title_Restore ?? "Restore Data",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during restore: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_RestoreError_Text ?? "Could not restore data from backup file:\n{0}", ex.Message),
                                Resources.MsgBox_Title_Restore ?? "Restore Data",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                try
                {
                    // Tenta di ricaricare lo stato precedente in caso di fallimento del ripristino
                    _currentUser = _persistenceService.LoadUserData();
                    LoadContractsFromUserData();
                }
                catch (Exception loadEx)
                {
                    Debug.WriteLine($"Error reloading data after failed restore: {loadEx}");
                    MessageBox.Show(Resources.MsgBox_RestoreLoadError_Text ?? "Data restored, but failed to reload. Please restart.",
                                    Resources.MsgBox_Title_Restore ?? "Restore Data",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // --- Logica per Ordinamento Contratti ---
        private void ExecuteSortContracts(object? parameter)
        {
            if (parameter is not string propertyName || ContractsView == null) return;
            var newDirection = ListSortDirection.Ascending;
            if (_currentContractSortProperty == propertyName && _currentContractSortDirection == ListSortDirection.Ascending)
            {
                newDirection = ListSortDirection.Descending;
            }
            _currentContractSortProperty = propertyName;
            _currentContractSortDirection = newDirection;
            ApplyContractsSort();
        }
        private void ApplyContractsSort()
        {
            if (ContractsView == null) return;
            using (ContractsView.DeferRefresh())
            {
                ContractsView.SortDescriptions.Clear();
                if (!string.IsNullOrEmpty(_currentContractSortProperty))
                {
                    ContractsView.SortDescriptions.Add(new SortDescription(_currentContractSortProperty, _currentContractSortDirection));
                    string secondarySort = _currentContractSortProperty == nameof(ContractViewModel.Company)
                                            ? nameof(ContractViewModel.ContractNumber)
                                            : nameof(ContractViewModel.Company);
                    ContractsView.SortDescriptions.Add(new SortDescription(secondarySort, ListSortDirection.Ascending));
                }
            }
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
    }
}