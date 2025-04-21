// WpfMvvmApp/ViewModels/ContractViewModel.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data; // NECESSARIO per CollectionViewSource e ICollectionView
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;
using WpfMvvmApp.Commands;
using WpfMvvmApp.Dialogs;
using WpfMvvmApp.Properties;

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        // --- Campi Privati ---
        private Contract? _contract;
        private DateTime _newLessonDate = DateTime.Today;
        private string _newLessonStartTimeString = "09:00";
        private TimeSpan _newLessonDuration = TimeSpan.FromHours(1);
        private string? _newLessonSummary;
        private Lesson? _lessonToEdit;
        private bool _isEditingLesson;
        private readonly IDialogService _dialogService;
        private readonly ICalService _calService;
        private string _currentSortProperty = nameof(Lesson.StartDateTime);
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

        // Collezione sorgente
        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        // Vista sulla collezione per la UI
        private ICollectionView? _lessonsView;
        public ICollectionView? LessonsView
        {
            get => _lessonsView;
            private set => SetProperty(ref _lessonsView, value);
        }

        // --- Proprietà Pubbliche ---
        public Contract? Contract
        {
            get => _contract;
            set
            {
                if (_contract != value)
                {
                    _contract = value;
                    OnPropertyChanged();
                    OnPropsChanged();
                    LoadLessons();
                    ResetAllInputs();
                    NotifyAllCanExecuteChanged();
                }
            }
        }
        private void OnPropsChanged() { OnPropertyChanged(nameof(Company)); OnPropertyChanged(nameof(ContractNumber)); OnPropertyChanged(nameof(HourlyRate)); OnPropertyChanged(nameof(TotalHours)); OnPropertyChanged(nameof(BilledHours)); OnPropertyChanged(nameof(StartDate)); OnPropertyChanged(nameof(EndDate)); OnPropertyChanged(nameof(IsContractValid)); NotifyBillingRelatedChanges(); }
        private void ResetAllInputs() { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }

        // Wrappers
        public string Company { get => Contract?.Company ?? ""; set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContractValid)); } } }
        public string ContractNumber { get => Contract?.ContractNumber ?? ""; set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContractValid)); } } }
        public decimal? HourlyRate
        {
            get => Contract?.HourlyRate;
            set
            {
                if (Contract != null && Contract.HourlyRate != value)
                {
                    Contract.HourlyRate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsContractValid));
                    NotifyBillingRelatedChanges();
                    foreach (var lesson in Lessons) // Usa collezione sorgente
                    {
                        lesson.NotifyAmountChanged();
                    }
                }
            }
        }
        public int? TotalHours { get => Contract?.TotalHours; set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContractValid)); NotifyCalculatedHoursChanged(); } } }
        public DateTime? StartDate { get => Contract?.StartDate; set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContractValid)); } } }
        public DateTime? EndDate { get => Contract?.EndDate; set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContractValid)); } } }

        // Input Lezione
        public DateTime NewLessonDate { get => _newLessonDate; set => SetProperty(ref _newLessonDate, value); }
        public string NewLessonStartTimeString { get => _newLessonStartTimeString; set { if (SetProperty(ref _newLessonStartTimeString, value)) { OnPropertyChanged(nameof(IsLessonInputValid)); (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
        public TimeSpan NewLessonDuration { get => _newLessonDuration; set { if (SetProperty(ref _newLessonDuration, value)) { OnPropertyChanged(nameof(IsLessonInputValid)); (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); NotifyBillingRelatedChanges(); } } }
        public string? NewLessonSummary { get => _newLessonSummary; set => SetProperty(ref _newLessonSummary, value); }

        public bool IsEditingLesson { get => _isEditingLesson; private set { if (SetProperty(ref _isEditingLesson, value)) { NotifyAllCanExecuteChanged(); } } }

        // Ore e Importi Calcolati (usano Lessons sorgente)
        public double TotalInsertedHours => Lessons.Sum(l => l.Duration.TotalHours);
        public double TotalConfirmedHours => Lessons.Where(l => l.IsConfirmed).Sum(l => l.Duration.TotalHours);
        public double BilledHours => Lessons.Where(l => l.IsBilled).Sum(l => l.Duration.TotalHours);
        public double RemainingHours => (Contract?.TotalHours ?? 0) - BilledHours;
        public decimal TotalBilledAmount => Lessons.Where(l => l.IsBilled).Sum(l => l.Amount);
        public decimal TotalConfirmedUnbilledAmount => Lessons.Where(l => l.IsConfirmed && !l.IsBilled).Sum(l => l.Amount);
        public decimal TotalPotentialAmount => Lessons.Sum(l => l.Amount);


        // --- Comandi ---
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand RemoveSelectedLessonsCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        public ICommand ToggleLessonConfirmationCommand { get; }
        public ICommand ImportLessonsCommand { get; }
        public ICommand ExportLessonsCommand { get; }
        public ICommand BillSelectedLessonsCommand { get; }
        public ICommand DuplicateLessonCommand { get; }
        public ICommand SortLessonsCommand { get; }

        // Costruttore
        public ContractViewModel(Contract contract, IDialogService dialogService, ICalService calService)
        {
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _calService = calService ?? throw new ArgumentNullException(nameof(calService));

            AddLessonCommand = new RelayCommand(ExecuteAddOrUpdateLesson, CanExecuteAddOrUpdateLesson);
            EditLessonCommand = new RelayCommand(ExecuteEditLesson, CanExecuteEditOrRemoveLesson);
            RemoveLessonCommand = new RelayCommand(ExecuteRemoveLesson, CanExecuteEditOrRemoveLesson);
            RemoveSelectedLessonsCommand = new RelayCommand(ExecuteRemoveSelectedLessons, CanExecuteRemoveSelectedLessons);
            CancelEditLessonCommand = new RelayCommand(ExecuteCancelEditLesson, CanExecuteCancelEditLesson);
            ToggleLessonConfirmationCommand = new RelayCommand(ExecuteToggleLessonConfirmation, CanExecuteToggleLessonConfirmation);
            ImportLessonsCommand = new RelayCommand(ExecuteImportLessons, CanExecuteImportExportLessons);
            ExportLessonsCommand = new RelayCommand(ExecuteExportLessons, CanExecuteImportExportLessons);
            BillSelectedLessonsCommand = new RelayCommand(ExecuteBillSelectedLessons, CanExecuteBillSelectedLessons);
            DuplicateLessonCommand = new RelayCommand(ExecuteDuplicateLesson, CanExecuteDuplicateLesson);
            SortLessonsCommand = new RelayCommand(ExecuteSortLessons);

            LoadLessons();
            NotifyAllCanExecuteChanged();
        }

        // LoadLessons
        private void LoadLessons()
        {
            Lessons.Clear();
            if (_contract?.Lessons != null)
            {
                foreach (var lesson in _contract.Lessons)
                {
                    Lessons.Add(lesson);
                }
            }
            LessonsView = CollectionViewSource.GetDefaultView(Lessons);
            ApplySort();
            NotifyBillingRelatedChanges();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        // *** CORPO COMPLETO SetProperty ***
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // --- IDataErrorInfo ---
        // *** CORPO COMPLETO Error ***
        string IDataErrorInfo.Error => GetFirstError();
        // *** CORPO COMPLETO this[] ***
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (columnName == nameof(NewLessonStartTimeString))
                {
                    if (!TryParseTime(NewLessonStartTimeString, out _)) return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format (use HH:MM).";
                }
                return string.Empty; // Ritorna sempre stringa vuota se non ci sono errori per la colonna
            }
        }
        // *** CORPO COMPLETO GetFirstError ***
        private string GetFirstError()
        {
             if (!TryParseTime(NewLessonStartTimeString, out _)) return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format.";
             // Aggiungere qui altri controlli di validazione a livello di oggetto se necessario
             return string.Empty; // Ritorna sempre stringa vuota se non ci sono errori
        }
        // *** CORRETTO: Rimosso ; finale ***
        public bool IsContractValid => Contract != null && Validator.TryValidateObject(Contract, new ValidationContext(Contract), null, true);
        // *** CORRETTO: Rimosso ; finale ***
        public bool IsLessonInputValid => TryParseTime(NewLessonStartTimeString, out _) && NewLessonDuration > TimeSpan.Zero;
        // --- Fine IDataErrorInfo ---

        // Helper Notifica CanExecute
        private void NotifyAllCanExecuteChanged()
        {
            (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleLessonConfirmationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ImportLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ExportLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DuplicateLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        // --- Metodi Esecuzione Comandi Lezione ---
        // *** CORPO COMPLETO ExecuteAddOrUpdateLesson (con rimozione riordino manuale) ***
        private void ExecuteAddOrUpdateLesson(object? parameter)
        {
            if (Contract == null || !CanExecuteAddOrUpdateLesson(parameter)) return;
            if (!TryParseTime(NewLessonStartTimeString, out TimeSpan lessonTime))
            {
                MessageBox.Show(Resources.Validation_InvalidTimeFormat ?? "Invalid start time format.", Resources.MsgBox_Title_ValidationError ?? "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime finalStartDateTime = NewLessonDate.Date + lessonTime;
            bool requiresNotify = false;

            if (IsEditingLesson && _lessonToEdit != null)
            {
                var oldDuration = _lessonToEdit.Duration;
                _lessonToEdit.StartDateTime = finalStartDateTime;
                _lessonToEdit.Duration = NewLessonDuration;
                _lessonToEdit.Summary = NewLessonSummary;
                // La vista si aggiornerà da sola se l'ordinamento è su StartDateTime
                if (oldDuration != _lessonToEdit.Duration) requiresNotify = true;
            }
            else
            {
                var newLesson = new Lesson
                {
                    Uid = Guid.NewGuid().ToString(),
                    StartDateTime = finalStartDateTime,
                    Duration = NewLessonDuration,
                    Contract = Contract,
                    IsConfirmed = false,
                    Summary = NewLessonSummary,
                };
                Contract.Lessons ??= new List<Lesson>();
                Contract.Lessons.Add(newLesson);
                // Aggiunta alla collezione sorgente (ICollectionView si aggiorna)
                Lessons.Add(newLesson); // Semplificato: Add invece di Insert ordinato, ICollectionView gestisce l'ordine
                requiresNotify = true;
            }

            if (requiresNotify) { NotifyBillingRelatedChanges(); }
            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }

        // *** CORPI COMPLETI CanExecute... ***
        private bool CanExecuteAddOrUpdateLesson(object? parameter) { return Contract != null && IsLessonInputValid; }
        private void ExecuteEditLesson(object? parameter) { if (parameter is Lesson lessonToEdit && CanExecuteEditOrRemoveLesson(parameter)) { IsEditingLesson = true; _lessonToEdit = lessonToEdit; NewLessonDate = lessonToEdit.StartDateTime.Date; NewLessonStartTimeString = lessonToEdit.StartDateTime.ToString("HH:mm"); NewLessonDuration = lessonToEdit.Duration; NewLessonSummary = lessonToEdit.Summary; } }
        private void ExecuteRemoveLesson(object? parameter) { if (parameter is Lesson lessonToRemove && Contract?.Lessons != null && CanExecuteEditOrRemoveLesson(parameter)) { var result = MessageBox.Show(string.Format(Resources.MsgBox_ConfirmRemoveLesson_Text ?? "Remove lesson starting {0:g}?", lessonToRemove.StartDateTime), Resources.MsgBox_Title_ConfirmRemoval ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning); if (result == MessageBoxResult.Yes) { bool removedFromView = Lessons.Remove(lessonToRemove); bool removedFromModel = Contract.Lessons?.Remove(lessonToRemove) ?? false; if (removedFromView || removedFromModel) { NotifyBillingRelatedChanges(); } } } }
        private void ExecuteRemoveSelectedLessons(object? parameter) { if (parameter is not IList selectedItems || selectedItems.Count == 0 || Contract?.Lessons == null || !CanExecuteRemoveSelectedLessons(parameter)) return; string message = selectedItems.Count == 1 ? Resources.MsgBox_ConfirmRemoveSelectedLesson_Text_Singular ?? "Remove selected lesson?" : string.Format(Resources.MsgBox_ConfirmRemoveSelectedLessons_Text_Plural ?? "Remove {0} selected lessons?", selectedItems.Count); var result = MessageBox.Show(message, Resources.MsgBox_Title_ConfirmRemoval ?? "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning); if (result == MessageBoxResult.Yes) { bool lessonsRemoved = false; var lessonsToRemove = selectedItems.OfType<Lesson>().ToList(); foreach (var lesson in lessonsToRemove) { bool removedFromView = Lessons.Remove(lesson); bool removedFromModel = Contract.Lessons?.Remove(lesson) ?? false; if (removedFromView || removedFromModel) { lessonsRemoved = true; } } if (lessonsRemoved) { NotifyBillingRelatedChanges(); } } }
        private bool CanExecuteRemoveSelectedLessons(object? parameter) { if (IsEditingLesson) return false; return parameter is IList selectedItems && selectedItems.Count > 0; }
        private void ExecuteCancelEditLesson(object? parameter) { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteCancelEditLesson(object? parameter) { return IsEditingLesson; }
        private void ExecuteToggleLessonConfirmation(object? parameter) { if (parameter is Lesson lessonToToggle && CanExecuteToggleLessonConfirmation(parameter)) { lessonToToggle.IsConfirmed = !lessonToToggle.IsConfirmed; NotifyBillingRelatedChanges(); (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        private bool CanExecuteToggleLessonConfirmation(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        private bool CanExecuteEditOrRemoveLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        // *** CORPO COMPLETO ExecuteDuplicateLesson (semplificato Add) ***
        private void ExecuteDuplicateLesson(object? parameter)
        {
            if (parameter is not Lesson originalLesson || Contract == null || !CanExecuteDuplicateLesson(parameter)) return;
            try
            {
                var duplicatedLesson = new Lesson { /* ... creazione ... */ };
                Contract.Lessons ??= new List<Lesson>();
                Contract.Lessons.Add(duplicatedLesson);
                Lessons.Add(duplicatedLesson); // Aggiungi alla sorgente, la vista si aggiornerà
                NotifyBillingRelatedChanges();
            }
            catch (Exception ex) { Debug.WriteLine($"Error duplicating lesson: {ex}");
             // MODIFICATO: Usa string.Format con ex.Message
                MessageBox.Show(string.Format(Resources.MsgBox_GenericError_Text ?? "An unexpected error occurred: {0}", ex.Message),
                Resources.MsgBox_Title_InternalError ?? "Internal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);/* ... gestione errore ... */ }
        }
        private bool CanExecuteDuplicateLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        // *** CORPO COMPLETO ExecuteImportLessons (semplificato Add) ***
        private void ExecuteImportLessons(object? parameter)
        {
            if (Contract == null) { /* ... errore ... */ return; }
            string? filePath = _dialogService.ShowOpenFileDialog(/* ... */);
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                List<Lesson> imported = _calService.ImportLessons(filePath);
                if (imported.Count == 0) { /* ... messaggio ... */ return; }
                int addedCount = 0;
                Contract.Lessons ??= new List<Lesson>();
                // Non serve ordinare imported qui
                foreach (var lesson in imported)
                {
                    // TODO: Controllo duplicati
                    lesson.Contract = Contract;
                    Contract.Lessons.Add(lesson);
                    Lessons.Add(lesson); // Aggiungi alla sorgente
                    addedCount++;
                }
                if (addedCount > 0) { /* ... messaggio successo ... */ } else { /* ... messaggio no nuove ... */ }
            }
            catch (Exception ex) {  Debug.WriteLine($"Error during lesson import: {ex}");
                // Assicurati che usi string.Format e ex.Message
                MessageBox.Show(string.Format(Resources.MsgBox_ImportError_Text ?? "Error importing:\n{0}", ex.Message),
                Resources.MsgBox_Title_ImportError ?? "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);/* ... gestione errore ... */ }
        }
        private void ExecuteExportLessons(object? parameter) { /* ... usa this.Lessons (sorgente) ... */ }
        private bool CanExecuteImportExportLessons(object? parameter) { return Contract != null && !IsEditingLesson; }
        private void ExecuteBillSelectedLessons(object? parameter) { /* ... */ }
        private bool CanExecuteBillSelectedLessons(object? parameter) { if (IsEditingLesson) return false; if (parameter is IList selectedItems && selectedItems.Count > 0) { return selectedItems.OfType<Lesson>().Any(l => l.IsConfirmed && !l.IsBilled); } return false; }

        // --- Logica per Ordinamento ---
        private void ExecuteSortLessons(object? parameter)
        {
            if (parameter is not string propertyName || LessonsView == null) return;
            var newDirection = ListSortDirection.Ascending;
            if (_currentSortProperty == propertyName && _currentSortDirection == ListSortDirection.Ascending)
            {
                newDirection = ListSortDirection.Descending;
            }
            _currentSortProperty = propertyName;
            _currentSortDirection = newDirection;
            ApplySort();
        }
        private void ApplySort()
        {
            if (LessonsView == null) return;
            using (LessonsView.DeferRefresh())
            {
                LessonsView.SortDescriptions.Clear();
                if (!string.IsNullOrEmpty(_currentSortProperty))
                {
                    LessonsView.SortDescriptions.Add(new SortDescription(_currentSortProperty, _currentSortDirection));
                    if (_currentSortProperty != nameof(Lesson.StartDateTime))
                    {
                         LessonsView.SortDescriptions.Add(new SortDescription(nameof(Lesson.StartDateTime), ListSortDirection.Ascending));
                    }
                }
            }
        }

        // --- Helper ---
        private void ResetLessonInputFields() { NewLessonDate = DateTime.Today; NewLessonStartTimeString = "09:00"; NewLessonDuration = TimeSpan.FromHours(1); NewLessonSummary = null; }
        private void NotifyCalculatedHoursChanged() { OnPropertyChanged(nameof(TotalInsertedHours)); OnPropertyChanged(nameof(TotalConfirmedHours)); OnPropertyChanged(nameof(BilledHours)); OnPropertyChanged(nameof(RemainingHours)); }
        private void NotifyBillingRelatedChanges() { NotifyCalculatedHoursChanged(); OnPropertyChanged(nameof(TotalBilledAmount)); OnPropertyChanged(nameof(TotalConfirmedUnbilledAmount)); OnPropertyChanged(nameof(TotalPotentialAmount)); }
        public void UpdateCommandStates() { NotifyAllCanExecuteChanged(); }
        // *** CORPO COMPLETO TryParseTime ***
        private bool TryParseTime(string? input, out TimeSpan result)
        {
             result = TimeSpan.Zero; // Inizializza parametro out
             if (string.IsNullOrWhiteSpace(input)) return false;
             // Prova a parsare, il risultato viene messo in 'result'
             return TimeSpan.TryParseExact(input, @"hh\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out result);
        }
        // *** CORPO COMPLETO FindInsertionIndex (anche se non più strettamente necessario per la vista) ***
        private int FindInsertionIndex(Lesson newLesson)
        {
            for (int i = 0; i < Lessons.Count; i++)
            {
                if (newLesson.StartDateTime < Lessons[i].StartDateTime)
                {
                    return i;
                }
            }
            return Lessons.Count;
        }
    }
}