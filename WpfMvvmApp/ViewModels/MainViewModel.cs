// WpfMvvmApp/ViewModels/MainViewModel.cs

// ***** USING AGGIUNTI *****
using System.Collections.Generic; // Necessario se si usano List<> o altri generics
using System.Collections.ObjectModel;
using System.ComponentModel; // Per INotifyPropertyChanged, EventArgs, ecc.
using System.Linq;
using System.Runtime.CompilerServices; // Per CallerMemberName
using System.Windows; // Per MessageBox
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
// Assicurati che anche RelayCommand sia accessibile (potrebbe richiedere un using specifico)
// using WpfMvvmApp.Commands;
// **************************

namespace WpfMvvmApp.ViewModels
{
    // Aggiunto : INotifyPropertyChanged che mancava nella dichiarazione della classe nell'esempio precedente
    public class MainViewModel : INotifyPropertyChanged
    {
        // Istanziare i servizi (o ottenerli tramite DI)
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
                // Usa SetProperty per notificare il cambiamento
                if (SetProperty(ref _selectedContract, value))
                {
                     (SaveContractCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
            var exampleUser = CreateExampleUser();

            Contracts.Clear();
            foreach (var contractModel in exampleUser.Contracts)
            {
                // Passa i servizi al costruttore di ContractViewModel
                var contractVM = new ContractViewModel(contractModel, _dialogService, _calService);
                Contracts.Add(contractVM);
            }
            SelectedContract = Contracts.FirstOrDefault();
        }

        private void ExecuteAddNewContract(object? parameter)
        {
             var newContractModel = new Contract
             {
                 Company = "New Company",
                 ContractNumber = $"CN-{DateTime.Now.Ticks}",
                 TotalHours = 10
             };
             var newContractVM = new ContractViewModel(newContractModel, _dialogService, _calService);
             Contracts.Add(newContractVM);
             SelectedContract = newContractVM;
        }

        private void ExecuteSaveContract(object? parameter)
        {
             if (SelectedContract?.Contract != null && SelectedContract.IsValid)
             {
                 // Ora MessageBox è riconosciuto grazie a 'using System.Windows;'
                 MessageBox.Show($"Contract '{SelectedContract.Company}' saved (simulated).",
                                 "Save Contract",
                                 MessageBoxButton.OK, // Riconosciuto
                                 MessageBoxImage.Information); // Riconosciuto
             }
        }

        private bool CanExecuteSaveContract(object? parameter)
        {
            return SelectedContract != null && SelectedContract.IsValid;
        }

        // Funzione helper per dati di esempio
        private User CreateExampleUser()
        {
            var user = new User { Username = "DefaultUser" };
            // Assicurati che Contract sia istanziabile correttamente
            var contract1 = new Contract { Company = "Tech Solutions", ContractNumber = "TS-001", HourlyRate = 70, TotalHours = 100, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var contract2 = new Contract { Company = "Edu World", ContractNumber = "EW-002", HourlyRate = 65, TotalHours = 50 };

            // Assicurati che Lesson sia istanziabile correttamente
            contract1.Lessons.Add(new Lesson { StartDateTime = DateTime.Today.AddDays(-5).AddHours(10), Duration = TimeSpan.FromHours(2), Contract = contract1, Summary = "Setup" });
            contract1.Lessons.Add(new Lesson { StartDateTime = DateTime.Today.AddDays(-3).AddHours(14), Duration = TimeSpan.FromMinutes(90), Contract = contract1, Summary = "Training 1", IsConfirmed=true });

            user.Contracts.Add(contract1);
            user.Contracts.Add(contract2);
            return user;
        }

        // --- Implementazione INotifyPropertyChanged ---
        // Ora PropertyChangedEventHandler e PropertyChangedEventArgs sono riconosciuti
        public event PropertyChangedEventHandler? PropertyChanged;

        // Ora CallerMemberName è riconosciuto
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Ora CallerMemberName e EqualityComparer sono riconosciuti
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}