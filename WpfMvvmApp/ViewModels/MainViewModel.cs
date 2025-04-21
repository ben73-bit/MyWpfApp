// WpfMvvmApp/ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
            ApplyContractsSort();
            SelectedContract = ContractsView?.Cast<ContractViewModel>().FirstOrDefault();
        }

        // ExecuteAddNewContract
        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = Resources.NewContractDefault_CompanyName ?? "New Company",
                 ContractNumber = $"{Resources.NewContractDefault_NumberPrefix ?? "CN"}-{DateTime.Now.Ticks}",
                 TotalHours = 10,
                 HourlyRate = 0,
                 StartDate = DateTime.Today,
                 Lessons = new List<Lesson>()
             };
             _currentUser.Contracts ??= new List<Contract>();
             _currentUser.Contracts.Add(newContractModel);
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             Contracts.Add(newContractVM);
             ContractsView?.MoveCurrentTo(newContractVM);
             SelectedContract = newContractVM;
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
        // *** CORPO COMPLETO CanExecuteSaveContract ***
        private bool CanExecuteSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsContractValid;
        }

        // ExecuteRemoveContract
        private void ExecuteRemoveContract(object? parameter)
        {
            var contractToRemoveVM = parameter as ContractViewModel ?? SelectedContract;
            if (contractToRemoveVM?.Contract == null) return;

            // *** CORRETTI ARGOMENTI MessageBox.Show ***
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
                    // La selezione si aggiornerà automaticamente perché la vista cambia
                }
                else { Debug.WriteLine($"Failed to remove contract '{contractToRemoveVM.ContractNumber}'."); }
            }
        }
        // *** CORPO COMPLETO CanExecuteRemoveContract ***
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
            Contracts.Add(newContractVM);
            ContractsView?.MoveCurrentTo(newContractVM);
            SelectedContract = newContractVM;
        }
        // *** CORPO COMPLETO CanExecuteDuplicateContract ***
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

        // OnApplicationExit
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            Debug.WriteLine("OnApplicationExit: Saving data...");
            ExecuteSaveAllData(e);
            try { Application.Current.Exit -= OnApplicationExit; } catch { }
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
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}