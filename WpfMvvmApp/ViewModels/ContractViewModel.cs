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
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.Services;

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        // --- Campi Privati ---
        private Contract? _contract;
        private DateTime _newLessonStartDateTime = DateTime.Now.Date.AddHours(9);
        private TimeSpan _newLessonDuration = TimeSpan.FromHours(1);
        private string? _newLessonSummary;
        private Lesson? _lessonToEdit;
        private bool _isEditingLesson;
        private readonly IDialogService _dialogService;
        private readonly ICalService _calService;

        // --- Proprietà Pubbliche ---
        public Contract? Contract
        {
            get => _contract;
            // Ripristinato setter completo come era nello stato funzionante
            set
            {
                if (_contract != value)
                {
                    _contract = value;
                    OnPropertyChanged(); // Notifica che il Contract è cambiato
                    OnPropsChanged();   // Notifica le proprietà wrapper (Company, etc.)
                    LoadLessons();      // Carica le lezioni associate
                    ResetAllInputs();   // Resetta input e stato modifica

                    // *** AGGIUNTA CHIAVE: Forza aggiornamento CanExecute dopo aver impostato Contract ***
                    NotifyAllCanExecuteChanged();
                }
            }
        }

        // Helper per notificare tutte le proprietà dipendenti dal Contract
        private void OnPropsChanged()
        {
            OnPropertyChanged(nameof(Company));
            OnPropertyChanged(nameof(ContractNumber));
            OnPropertyChanged(nameof(HourlyRate));
            OnPropertyChanged(nameof(TotalHours));
            OnPropertyChanged(nameof(BilledHours));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(IsValid)); // IsValid dipende solo dal contratto
        }

        // Helper per resettare tutto quando cambia il contratto
        private void ResetAllInputs()
        {
            ResetLessonInputFields();
            IsEditingLesson = false; // Esce dalla modalità modifica
            _lessonToEdit = null;
        }


        // Wrappers (nullable dove necessario) - Implementazione Completa
        public string Company { get => Contract?.Company ?? ""; set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public string ContractNumber { get => Contract?.ContractNumber ?? ""; set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public decimal? HourlyRate { get => Contract?.HourlyRate; set { if (Contract != null && Contract.HourlyRate != value) { Contract.HourlyRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int? TotalHours
        {
            get => Contract?.TotalHours;
            set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); NotifyCalculatedHoursChanged(); } }
        }
        public int BilledHours { get => Contract?.BilledHours ?? 0; set { if (Contract != null && Contract.BilledHours != value) { Contract.BilledHours = value; OnPropertyChanged(); } } }
        public DateTime? StartDate { get => Contract?.StartDate; set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public DateTime? EndDate { get => Contract?.EndDate; set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }


        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        // Proprietà Input Lezione - Implementazione Completa
        public DateTime NewLessonStartDateTime { get => _newLessonStartDateTime; set => SetProperty(ref _newLessonStartDateTime, value); }
        public TimeSpan NewLessonDuration { get => _newLessonDuration; set { if (SetProperty(ref _newLessonDuration, value)) { (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
        public string? NewLessonSummary { get => _newLessonSummary; set => SetProperty(ref _newLessonSummary, value); }


        public bool IsEditingLesson
        {
            get => _isEditingLesson;
            private set
            {
                // Chiama l'helper che notifica TUTTI i comandi
                if (SetProperty(ref _isEditingLesson, value)) { NotifyAllCanExecuteChanged(); }
            }
        }
        // Helper per notificare tutti i CanExecute
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
        }

        // Proprietà Calcolate Ore - Implementazione Completa
        public double TotalInsertedHours => Lessons.Sum(l => l.Duration.TotalHours);
        public double TotalConfirmedHours => Lessons.Where(l => l.IsConfirmed).Sum(l => l.Duration.TotalHours);
        public double RemainingHours => (Contract?.TotalHours ?? 0) - TotalConfirmedHours;


        // --- Comandi ---
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand RemoveSelectedLessonsCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        public ICommand ToggleLessonConfirmationCommand { get; }
        public ICommand ImportLessonsCommand { get; }
        public ICommand ExportLessonsCommand { get; }

        // Costruttore - Implementazione Completa
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
            LoadLessons();

            // Notifica iniziale dello stato CanExecute
            NotifyAllCanExecuteChanged();
        }

        // LoadLessons - Implementazione Completa
        private void LoadLessons()
        {
             Lessons.Clear();
             // Usiamo l'operatore null-conditional (?) per sicurezza, anche se il costruttore lancia eccezione
             if (_contract?.Lessons != null)
             {
                 foreach (var lesson in _contract.Lessons.OrderBy(l => l.StartDateTime))
                 {
                     Lessons.Add(lesson);
                 }
             }
             NotifyCalculatedHoursChanged();
        }

        // INotifyPropertyChanged - Implementazione Completa
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }

        // IDataErrorInfo - Implementazione Completa
        string IDataErrorInfo.Error => GetValidationError();
        string IDataErrorInfo.this[string columnName] => GetValidationError(columnName);
        private string GetValidationError(string? propertyName = null)
        {
             if (Contract == null) return string.Empty;
             var context = new ValidationContext(Contract) { MemberName = propertyName };
             var results = new List<ValidationResult>();
             bool isValid = false;
             if (string.IsNullOrEmpty(propertyName))
             {
                 isValid = Validator.TryValidateObject(Contract, context, results, true);
             }
             else
             {
                 var propertyInfo = Contract.GetType().GetProperty(propertyName);
                 if (propertyInfo != null)
                 {
                     var value = propertyInfo.GetValue(Contract);
                     isValid = Validator.TryValidateProperty(value, context, results);
                 }
                 else
                 {
                     // Se la proprietà non fa parte del modello Contract, non validarla qui
                     return string.Empty;
                 }
             }
             if (isValid || results.Count == 0) return string.Empty;
             return results.First().ErrorMessage ?? "Validation Error";
        }
        // IsValid dipende solo dal modello Contract
        public bool IsValid => Contract != null && string.IsNullOrEmpty(GetValidationError(null)); // Passa null per validare l'oggetto intero


        // --- Metodi Esecuzione Comandi Lezione - Implementazione Completa ---
        private void ExecuteAddOrUpdateLesson(object? parameter)
        {
            if (Contract == null || !CanExecuteAddOrUpdateLesson(parameter)) return;
            bool changed = false;
            if (IsEditingLesson && _lessonToEdit != null)
            {
                var oldDuration = _lessonToEdit.Duration;
                _lessonToEdit.StartDateTime = NewLessonStartDateTime;
                _lessonToEdit.Duration = NewLessonDuration;
                _lessonToEdit.Summary = NewLessonSummary;
                if (oldDuration != _lessonToEdit.Duration) { changed = true; }
            }
            else
            {
                var newLesson = new Lesson { StartDateTime = NewLessonStartDateTime, Duration = NewLessonDuration, Contract = Contract, IsConfirmed = false, Summary = NewLessonSummary };
                Lessons.Add(newLesson);
                // Assicurati che Contract.Lessons non sia null prima di aggiungere
                Contract.Lessons ??= new List<Lesson>(); // Inizializza se null (buona pratica)
                Contract.Lessons.Add(newLesson);
                changed = true;
            }
            if (changed) { NotifyCalculatedHoursChanged(); }
            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }
        // CanExecute controlla solo durata e contratto
        private bool CanExecuteAddOrUpdateLesson(object? parameter)
        { return Contract != null && NewLessonDuration > TimeSpan.Zero; }

        private void ExecuteEditLesson(object? parameter)
        {
             if (parameter is Lesson lessonToEdit)
             {
                 IsEditingLesson = true; _lessonToEdit = lessonToEdit;
                 NewLessonStartDateTime = lessonToEdit.StartDateTime;
                 NewLessonDuration = lessonToEdit.Duration;
                 NewLessonSummary = lessonToEdit.Summary;
             }
        }

        private void ExecuteRemoveLesson(object? parameter)
        {
            if (parameter is Lesson lessonToRemove && Contract?.Lessons != null && CanExecuteEditOrRemoveLesson(parameter))
            {
                var result = MessageBox.Show($"Remove lesson starting {lessonToRemove.StartDateTime:g}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    bool removedFromVM = Lessons.Remove(lessonToRemove);
                    Contract.Lessons?.Remove(lessonToRemove); // Usa null-conditional per sicurezza
                    if (removedFromVM) { NotifyCalculatedHoursChanged(); }
                }
            }
        }

        private void ExecuteRemoveSelectedLessons(object? parameter)
        {
            if (parameter is not IList selectedItems || selectedItems.Count == 0 || Contract?.Lessons == null || !CanExecuteRemoveSelectedLessons(parameter)) return;

            string message = selectedItems.Count == 1 ? "Remove selected lesson?" : $"Remove {selectedItems.Count} selected lessons?";
            var result = MessageBox.Show(message, "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool lessonsRemoved = false;
                var lessonsToRemove = selectedItems.OfType<Lesson>().ToList();
                foreach (var lesson in lessonsToRemove)
                {
                    bool removedFromVM = Lessons.Remove(lesson);
                    bool removedFromModel = Contract.Lessons?.Remove(lesson) ?? false; // Usa null-conditional e ??
                    if (removedFromVM || removedFromModel) { lessonsRemoved = true; }
                }
                if (lessonsRemoved) { NotifyCalculatedHoursChanged(); }
            }
        }
        private bool CanExecuteRemoveSelectedLessons(object? parameter)
        { if (IsEditingLesson) return false; return parameter is IList selectedItems && selectedItems.Count > 0; }


        private void ExecuteCancelEditLesson(object? parameter)
        { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteCancelEditLesson(object? parameter)
        { return IsEditingLesson; }


        private void ExecuteToggleLessonConfirmation(object? parameter)
        {
            if (parameter is Lesson lessonToToggle && CanExecuteToggleLessonConfirmation(parameter))
            {
                var oldState = lessonToToggle.IsConfirmed;
                lessonToToggle.IsConfirmed = !lessonToToggle.IsConfirmed;
                if (oldState != lessonToToggle.IsConfirmed) { NotifyCalculatedHoursChanged(); }
                if (lessonToToggle.IsConfirmed) { /* Logica post-conferma */ } else { /* Logica post-deconferma */ }
            }
        }
        private bool CanExecuteToggleLessonConfirmation(object? parameter)
        { return parameter is Lesson && !IsEditingLesson; }
        private bool CanExecuteEditOrRemoveLesson(object? parameter)
        { return parameter is Lesson && !IsEditingLesson; }


        // Import/Export - Implementazione Completa
        private void ExecuteImportLessons(object? parameter)
        {
            if (Contract == null) { MessageBox.Show("Please select a contract before importing lessons.", "No Contract Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string? filePath = _dialogService.ShowOpenFileDialog("iCalendar files (*.ics)|*.ics|All files (*.*)|*.*", "Import Lessons");
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                List<Lesson> imported = _calService.ImportLessons(filePath);
                if (imported.Count == 0) { MessageBox.Show("No valid lesson events found in the selected file.", "Import Result", MessageBoxButton.OK, MessageBoxImage.Information); return; }
                int addedCount = 0;
                Contract.Lessons ??= new List<Lesson>(); // Assicura che la lista nel modello esista
                foreach (var lesson in imported)
                {
                    lesson.Contract = Contract; // Assegna contratto
                    Lessons.Add(lesson); Contract.Lessons.Add(lesson); addedCount++;
                }
                if (addedCount > 0)
                {
                    NotifyCalculatedHoursChanged();
                    // Riordina UI
                    var sortedLessons = Lessons.OrderBy(l => l.StartDateTime).ToList(); Lessons.Clear(); foreach(var l in sortedLessons) Lessons.Add(l);
                    MessageBox.Show($"{addedCount} lessons imported successfully.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Error during lesson import: {ex}"); MessageBox.Show($"An error occurred while importing the file:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void ExecuteExportLessons(object? parameter)
        {
             if (Contract == null) { MessageBox.Show("Please select a contract to export its lessons.", "No Contract Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
             if (!Lessons.Any()) { MessageBox.Show("There are no lessons to export for this contract.", "Export", MessageBoxButton.OK, MessageBoxImage.Information); return; }
             string defaultFileName = $"Lessons_{Contract.Company}_{Contract.ContractNumber}.ics".Replace(" ", "_");
             foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { defaultFileName = defaultFileName.Replace(c, '_'); }
             string? filePath = _dialogService.ShowSaveFileDialog("iCalendar files (*.ics)|*.ics", defaultFileName, "Export Lessons");
             if (string.IsNullOrEmpty(filePath)) return;
             try
             {
                 string contractInfo = $"{Contract.Company} - {Contract.ContractNumber}";
                 bool success = _calService.ExportLessons(this.Lessons, filePath, contractInfo); // Passa this.Lessons (ObservableCollection)
                 if (success) { MessageBox.Show($"Lessons exported successfully to:\n{filePath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); }
                 else { MessageBox.Show("An error occurred during export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
             }
             catch (Exception ex) { Debug.WriteLine($"Error during lesson export: {ex}"); MessageBox.Show($"An error occurred while exporting the file:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private bool CanExecuteImportExportLessons(object? parameter)
        { return Contract != null && !IsEditingLesson; }


        // --- Metodi Helper - Implementazione Completa ---
        private void ResetLessonInputFields()
        {
             NewLessonStartDateTime = DateTime.Now.Date.AddHours(9);
             NewLessonDuration = TimeSpan.FromHours(1);
             NewLessonSummary = null;
        }
        private void NotifyCalculatedHoursChanged()
        {
            OnPropertyChanged(nameof(TotalInsertedHours));
            OnPropertyChanged(nameof(TotalConfirmedHours));
            OnPropertyChanged(nameof(RemainingHours));
        }
    }
}