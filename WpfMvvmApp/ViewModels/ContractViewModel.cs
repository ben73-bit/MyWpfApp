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
// *** MODIFICA QUESTA USING CON IL NAMESPACE CORRETTO ***
using WpfMvvmApp.Dialogs; // O il namespace dove si trova InvoiceInputDialog
// ******************************************************

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
            set { if (_contract != value) { _contract = value; OnPropertyChanged(); OnPropsChanged(); LoadLessons(); ResetAllInputs(); NotifyAllCanExecuteChanged(); } }
        }
        private void OnPropsChanged() { OnPropertyChanged(nameof(Company)); OnPropertyChanged(nameof(ContractNumber)); OnPropertyChanged(nameof(HourlyRate)); OnPropertyChanged(nameof(TotalHours)); OnPropertyChanged(nameof(BilledHours)); OnPropertyChanged(nameof(StartDate)); OnPropertyChanged(nameof(EndDate)); OnPropertyChanged(nameof(IsValid)); }
        private void ResetAllInputs() { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }

        // Wrappers
        public string Company { get => Contract?.Company ?? ""; set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public string ContractNumber { get => Contract?.ContractNumber ?? ""; set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public decimal? HourlyRate { get => Contract?.HourlyRate; set { if (Contract != null && Contract.HourlyRate != value) { Contract.HourlyRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int? TotalHours { get => Contract?.TotalHours; set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); NotifyCalculatedHoursChanged(); } } }
        public double BilledHours => Lessons.Where(l => l.IsBilled).Sum(l => l.Duration.TotalHours);
        public DateTime? StartDate { get => Contract?.StartDate; set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public DateTime? EndDate { get => Contract?.EndDate; set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }

        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();
        public DateTime NewLessonStartDateTime { get => _newLessonStartDateTime; set => SetProperty(ref _newLessonStartDateTime, value); }
        public TimeSpan NewLessonDuration { get => _newLessonDuration; set { if (SetProperty(ref _newLessonDuration, value)) { (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
        public string? NewLessonSummary { get => _newLessonSummary; set => SetProperty(ref _newLessonSummary, value); }

        public bool IsEditingLesson { get => _isEditingLesson; private set { if (SetProperty(ref _isEditingLesson, value)) { NotifyAllCanExecuteChanged(); } } }
        private void NotifyAllCanExecuteChanged() { (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); (RemoveSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); (ToggleLessonConfirmationCommand as RelayCommand)?.RaiseCanExecuteChanged(); (ImportLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); (ExportLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); }

        // Ore Calcolate
        public double TotalInsertedHours => Lessons.Sum(l => l.Duration.TotalHours);
        public double TotalConfirmedHours => Lessons.Where(l => l.IsConfirmed).Sum(l => l.Duration.TotalHours);
        public double RemainingHours => (Contract?.TotalHours ?? 0) - BilledHours;

        // Comandi
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand RemoveSelectedLessonsCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        public ICommand ToggleLessonConfirmationCommand { get; }
        public ICommand ImportLessonsCommand { get; }
        public ICommand ExportLessonsCommand { get; }
        public ICommand BillSelectedLessonsCommand { get; }

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
            LoadLessons();
            NotifyAllCanExecuteChanged();
        }

        // LoadLessons
        private void LoadLessons() { Lessons.Clear(); if (_contract?.Lessons != null) { foreach (var lesson in _contract.Lessons.OrderBy(l => l.StartDateTime)) { Lessons.Add(lesson); } } NotifyCalculatedHoursChanged(); }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }

        // IDataErrorInfo
        string IDataErrorInfo.Error => GetValidationError(null);
        string IDataErrorInfo.this[string columnName] => GetValidationError(columnName);
        private string GetValidationError(string? propertyName = null) { if (Contract == null) return string.Empty; var context = new ValidationContext(Contract) { MemberName = propertyName }; var results = new List<ValidationResult>(); bool isValid = false; if (string.IsNullOrEmpty(propertyName)) { isValid = Validator.TryValidateObject(Contract, context, results, true); } else { var p = Contract.GetType().GetProperty(propertyName); if (p != null) { isValid = Validator.TryValidateProperty(p.GetValue(Contract), context, results); } else { return string.Empty; } } if (isValid || results.Count == 0) return string.Empty; return results.First().ErrorMessage ?? "Validation Error"; }
        public bool IsValid => Contract != null && string.IsNullOrEmpty(GetValidationError(null));

        // Comandi Lezione
        private void ExecuteAddOrUpdateLesson(object? parameter) { if (Contract == null || !CanExecuteAddOrUpdateLesson(parameter)) return; bool changed = false; if (IsEditingLesson && _lessonToEdit != null) { var oldDuration = _lessonToEdit.Duration; _lessonToEdit.StartDateTime = NewLessonStartDateTime; _lessonToEdit.Duration = NewLessonDuration; _lessonToEdit.Summary = NewLessonSummary; if (oldDuration != _lessonToEdit.Duration) { changed = true; } } else { var newLesson = new Lesson { StartDateTime = NewLessonStartDateTime, Duration = NewLessonDuration, Contract = Contract, IsConfirmed = false, Summary = NewLessonSummary }; Lessons.Add(newLesson); Contract.Lessons ??= new List<Lesson>(); Contract.Lessons.Add(newLesson); changed = true; } if (changed) { NotifyCalculatedHoursChanged(); } ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteAddOrUpdateLesson(object? parameter) { return Contract != null && NewLessonDuration > TimeSpan.Zero; }
        private void ExecuteEditLesson(object? parameter) { if (parameter is Lesson lessonToEdit) { IsEditingLesson = true; _lessonToEdit = lessonToEdit; NewLessonStartDateTime = lessonToEdit.StartDateTime; NewLessonDuration = lessonToEdit.Duration; NewLessonSummary = lessonToEdit.Summary; } }
        private void ExecuteRemoveLesson(object? parameter) { if (parameter is Lesson lessonToRemove && Contract?.Lessons != null && CanExecuteEditOrRemoveLesson(parameter)) { var result = MessageBox.Show($"Remove lesson starting {lessonToRemove.StartDateTime:g}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning); if (result == MessageBoxResult.Yes) { bool removedFromVM = Lessons.Remove(lessonToRemove); Contract.Lessons?.Remove(lessonToRemove); if (removedFromVM) { NotifyCalculatedHoursChanged(); } } } }
        private void ExecuteRemoveSelectedLessons(object? parameter) { if (parameter is not IList selectedItems || selectedItems.Count == 0 || Contract?.Lessons == null || !CanExecuteRemoveSelectedLessons(parameter)) return; string message = selectedItems.Count == 1 ? "Remove selected lesson?" : $"Remove {selectedItems.Count} selected lessons?"; var result = MessageBox.Show(message, "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning); if (result == MessageBoxResult.Yes) { bool lessonsRemoved = false; var lessonsToRemove = selectedItems.OfType<Lesson>().ToList(); foreach (var lesson in lessonsToRemove) { bool removedFromVM = Lessons.Remove(lesson); bool removedFromModel = Contract.Lessons?.Remove(lesson) ?? false; if (removedFromVM || removedFromModel) { lessonsRemoved = true; } } if (lessonsRemoved) { NotifyCalculatedHoursChanged(); } } }
        private bool CanExecuteRemoveSelectedLessons(object? parameter) { if (IsEditingLesson) return false; return parameter is IList selectedItems && selectedItems.Count > 0; }
        private void ExecuteCancelEditLesson(object? parameter) { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteCancelEditLesson(object? parameter) { return IsEditingLesson; }
        private void ExecuteToggleLessonConfirmation(object? parameter) { if (parameter is Lesson lessonToToggle && CanExecuteToggleLessonConfirmation(parameter)) { var oldState = lessonToToggle.IsConfirmed; lessonToToggle.IsConfirmed = !lessonToToggle.IsConfirmed; if (oldState != lessonToToggle.IsConfirmed) { NotifyCalculatedHoursChanged(); (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
        private bool CanExecuteToggleLessonConfirmation(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        private bool CanExecuteEditOrRemoveLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }

        // Import/Export
        private void ExecuteImportLessons(object? parameter) { if (Contract == null) { MessageBox.Show("Please select a contract before importing lessons.", "No Contract Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; } string? filePath = _dialogService.ShowOpenFileDialog("iCalendar files (*.ics)|*.ics|All files (*.*)|*.*", "Import Lessons"); if (string.IsNullOrEmpty(filePath)) return; try { List<Lesson> imported = _calService.ImportLessons(filePath); if (imported.Count == 0) { MessageBox.Show("No valid lesson events found in the selected file.", "Import Result", MessageBoxButton.OK, MessageBoxImage.Information); return; } int addedCount = 0; Contract.Lessons ??= new List<Lesson>(); foreach (var lesson in imported) { lesson.Contract = Contract; Lessons.Add(lesson); Contract.Lessons.Add(lesson); addedCount++; } if (addedCount > 0) { NotifyCalculatedHoursChanged(); var sortedLessons = Lessons.OrderBy(l => l.StartDateTime).ToList(); Lessons.Clear(); foreach(var l in sortedLessons) Lessons.Add(l); MessageBox.Show($"{addedCount} lessons imported successfully.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information); } } catch (Exception ex) { Debug.WriteLine($"Error during lesson import: {ex}"); MessageBox.Show($"An error occurred while importing the file:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void ExecuteExportLessons(object? parameter) { if (Contract == null) { MessageBox.Show("Please select a contract to export its lessons.", "No Contract Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (!Lessons.Any()) { MessageBox.Show("There are no lessons to export for this contract.", "Export", MessageBoxButton.OK, MessageBoxImage.Information); return; } string defaultFileName = $"Lessons_{Contract.Company}_{Contract.ContractNumber}.ics".Replace(" ", "_"); foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { defaultFileName = defaultFileName.Replace(c, '_'); } string? filePath = _dialogService.ShowSaveFileDialog("iCalendar files (*.ics)|*.ics", defaultFileName, "Export Lessons"); if (string.IsNullOrEmpty(filePath)) return; try { string contractInfo = $"{Contract.Company} - {Contract.ContractNumber}"; bool success = _calService.ExportLessons(this.Lessons, filePath, contractInfo); if (success) { MessageBox.Show($"Lessons exported successfully to:\n{filePath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); } else { MessageBox.Show("An error occurred during export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning); } } catch (Exception ex) { Debug.WriteLine($"Error during lesson export: {ex}"); MessageBox.Show($"An error occurred while exporting the file:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private bool CanExecuteImportExportLessons(object? parameter) { return Contract != null && !IsEditingLesson; }

        // Fatturazione
        private void ExecuteBillSelectedLessons(object? parameter)
        {
            if (parameter is not IList selectedItems || selectedItems.Count == 0 || !CanExecuteBillSelectedLessons(parameter)) return;
            var lessonsToBill = selectedItems.OfType<Lesson>().Where(l => l.IsConfirmed && !l.IsBilled).ToList();
            if (!lessonsToBill.Any()) { MessageBox.Show("No confirmed, unbilled lessons selected.", "Billing", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            // Ora InvoiceInputDialog dovrebbe essere riconosciuto grazie alla using corretta
            var inputDialog = new InvoiceInputDialog { Owner = Application.Current.MainWindow };
            if (inputDialog.ShowDialog() == true)
            {
                string invoiceNumber = inputDialog.InvoiceNumber;
                DateTime invoiceDate = DateTime.Now;
                foreach (var lesson in lessonsToBill) { lesson.IsBilled = true; lesson.InvoiceNumber = invoiceNumber; lesson.InvoiceDate = invoiceDate; }
                NotifyBillingRelatedChanges();
                (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                MessageBox.Show($"{lessonsToBill.Count} lessons marked as billed with Invoice # {invoiceNumber}.", "Billing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private bool CanExecuteBillSelectedLessons(object? parameter) { if (IsEditingLesson || parameter is not IList selectedItems || selectedItems.Count == 0) return false; return selectedItems.OfType<Lesson>().Any(l => l.IsConfirmed && !l.IsBilled); }

        // Helper
        private void ResetLessonInputFields() { NewLessonStartDateTime = DateTime.Now.Date.AddHours(9); NewLessonDuration = TimeSpan.FromHours(1); NewLessonSummary = null; }
        private void NotifyCalculatedHoursChanged() { OnPropertyChanged(nameof(TotalInsertedHours)); OnPropertyChanged(nameof(TotalConfirmedHours)); OnPropertyChanged(nameof(BilledHours)); OnPropertyChanged(nameof(RemainingHours)); }
        private void NotifyBillingRelatedChanges() { NotifyCalculatedHoursChanged(); }

        // Metodo Pubblico per aggiornare CanExecute
        public void UpdateCommandStates() { NotifyAllCanExecuteChanged(); }
    }
}