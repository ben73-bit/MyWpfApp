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
using WpfMvvmApp.Commands;
using WpfMvvmApp.Dialogs;
using WpfMvvmApp.Properties; // NECESSARIO per Resources

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

        // Collezione pubblica diretta
        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        // --- Proprietà Pubbliche ---
        public Contract? Contract
        {
            get => _contract;
            set { if (_contract != value) { _contract = value; OnPropertyChanged(); OnPropsChanged(); LoadLessons(); ResetAllInputs(); NotifyAllCanExecuteChanged(); } }
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
                    foreach (var lesson in Lessons)
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

        // Ore e Importi Calcolati
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
        public ICommand DuplicateLessonCommand { get; } // NUOVO COMANDO

        // Costruttore
        public ContractViewModel(Contract contract, IDialogService dialogService, ICalService calService)
        {
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _calService = calService ?? throw new ArgumentNullException(nameof(calService));

            // Comandi esistenti
            AddLessonCommand = new RelayCommand(ExecuteAddOrUpdateLesson, CanExecuteAddOrUpdateLesson);
            EditLessonCommand = new RelayCommand(ExecuteEditLesson, CanExecuteEditOrRemoveLesson);
            RemoveLessonCommand = new RelayCommand(ExecuteRemoveLesson, CanExecuteEditOrRemoveLesson);
            RemoveSelectedLessonsCommand = new RelayCommand(ExecuteRemoveSelectedLessons, CanExecuteRemoveSelectedLessons);
            CancelEditLessonCommand = new RelayCommand(ExecuteCancelEditLesson, CanExecuteCancelEditLesson);
            ToggleLessonConfirmationCommand = new RelayCommand(ExecuteToggleLessonConfirmation, CanExecuteToggleLessonConfirmation);
            ImportLessonsCommand = new RelayCommand(ExecuteImportLessons, CanExecuteImportExportLessons);
            ExportLessonsCommand = new RelayCommand(ExecuteExportLessons, CanExecuteImportExportLessons);
            BillSelectedLessonsCommand = new RelayCommand(ExecuteBillSelectedLessons, CanExecuteBillSelectedLessons);

            // NUOVO: Istanziazione comando Duplica
            DuplicateLessonCommand = new RelayCommand(ExecuteDuplicateLesson, CanExecuteDuplicateLesson);

            LoadLessons();
            NotifyAllCanExecuteChanged();
        }

        // LoadLessons - Mantiene l'ordinamento al caricamento
        private void LoadLessons()
        {
            Lessons.Clear();
            if (_contract?.Lessons != null)
            {
                // Ordina sempre quando carica le lezioni dal modello
                foreach (var lesson in _contract.Lessons.OrderBy(l => l.StartDateTime))
                {
                    Lessons.Add(lesson);
                }
            }
            NotifyBillingRelatedChanges();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }

        // --- IDataErrorInfo ---
        string IDataErrorInfo.Error => GetFirstError();
        string IDataErrorInfo.this[string columnName] { get { if (columnName == nameof(NewLessonStartTimeString)) { if (!TryParseTime(NewLessonStartTimeString, out _)) return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format (use HH:MM)."; } return string.Empty; } }
        private string GetFirstError() { if (!TryParseTime(NewLessonStartTimeString, out _)) return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format."; return string.Empty; }
        public bool IsContractValid => Contract != null && Validator.TryValidateObject(Contract, new ValidationContext(Contract), null, true);
        public bool IsLessonInputValid => TryParseTime(NewLessonStartTimeString, out _) && NewLessonDuration > TimeSpan.Zero;
        // --- Fine IDataErrorInfo ---

        // Helper Notifica CanExecute - AGGIORNATO
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
            (DuplicateLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiunto
        }


        // --- Metodi Esecuzione Comandi Lezione ---
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
                var oldStartTime = _lessonToEdit.StartDateTime; // Salva l'ora di inizio originale
                var oldDuration = _lessonToEdit.Duration;

                _lessonToEdit.StartDateTime = finalStartDateTime; // Aggiorna data/ora
                _lessonToEdit.Duration = NewLessonDuration;
                _lessonToEdit.Summary = NewLessonSummary;

                // Se l'ora o la data sono cambiate, dobbiamo riordinare la lista UI
                if (_lessonToEdit.StartDateTime != oldStartTime)
                {
                    // Riordina la ObservableCollection dopo la modifica
                    var sortedLessons = Lessons.OrderBy(l => l.StartDateTime).ToList();
                    Lessons.Clear(); // Cancella la collezione attuale
                    foreach(var lesson in sortedLessons) Lessons.Add(lesson); // Ricarica ordinata
                }

                if (oldDuration != _lessonToEdit.Duration) requiresNotify = true;
            }
            else
            {
                // Aggiunta di una nuova lezione
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

                int index = FindInsertionIndex(newLesson);
                Lessons.Insert(index, newLesson);

                requiresNotify = true;
            }

            if (requiresNotify)
            {
                NotifyBillingRelatedChanges();
            }
            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }

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

        // NUOVO: Metodo Esecuzione Duplica Lezione
        private void ExecuteDuplicateLesson(object? parameter)
        {
            if (parameter is not Lesson originalLesson || Contract == null || !CanExecuteDuplicateLesson(parameter))
                return;

            try
            {
                // Crea la nuova lezione copiando i dati rilevanti
                var duplicatedLesson = new Lesson
                {
                    Uid = Guid.NewGuid().ToString(), // Nuovo UID univoco
                    StartDateTime = originalLesson.StartDateTime, // Mantiene data e ora originali
                    Duration = originalLesson.Duration,
                    Summary = originalLesson.Summary,
                    Description = originalLesson.Description,
                    Location = originalLesson.Location,
                    Contract = this.Contract,

                    // Resetta gli stati
                    IsConfirmed = false,
                    IsBilled = false,
                    InvoiceNumber = null,
                    InvoiceDate = null
                };

                Contract.Lessons ??= new List<Lesson>();
                Contract.Lessons.Add(duplicatedLesson);

                int index = FindInsertionIndex(duplicatedLesson);
                Lessons.Insert(index, duplicatedLesson);

                NotifyBillingRelatedChanges();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error duplicating lesson: {ex}");
                MessageBox.Show(Resources.MsgBox_GenericError_Text ?? "An unexpected error occurred.",
                                Resources.MsgBox_Title_InternalError ?? "Internal Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NUOVO: Metodo CanExecute Duplica Lezione
        private bool CanExecuteDuplicateLesson(object? parameter)
        {
            return parameter is Lesson && !IsEditingLesson;
        }

        private void ExecuteImportLessons(object? parameter)
        {
            if (Contract == null) { MessageBox.Show(Resources.MsgBox_SelectContractBeforeImport_Text ?? "Select contract first.", Resources.MsgBox_Title_NoContractSelected ?? "No Contract", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string? filePath = _dialogService.ShowOpenFileDialog("iCalendar files (*.ics)|*.ics|All files (*.*)|*.*", Resources.ImportIcsButton_Content ?? "Import Lessons");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                List<Lesson> imported = _calService.ImportLessons(filePath);
                if (imported.Count == 0)
                {
                    MessageBox.Show(Resources.MsgBox_NoLessonsToImport_Text ?? "No lessons found.", Resources.MsgBox_Title_ImportResult ?? "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int addedCount = 0;
                Contract.Lessons ??= new List<Lesson>();
                imported = imported.OrderBy(l => l.StartDateTime).ToList(); // Ordina prima di inserire

                foreach (var lesson in imported)
                {
                    // TODO: Controllo duplicati (es. basato su Uid o Start/Duration)
                    // if (Lessons.Any(l => l.Uid == lesson.Uid && !string.IsNullOrEmpty(lesson.Uid))) continue;

                    lesson.Contract = Contract;
                    Contract.Lessons.Add(lesson);
                    int index = FindInsertionIndex(lesson);
                    Lessons.Insert(index, lesson);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    NotifyBillingRelatedChanges();
                    MessageBox.Show(string.Format(Resources.MsgBox_ImportSuccessful_Text ?? "{0} lessons imported.", addedCount), Resources.MsgBox_Title_ImportComplete ?? "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                     MessageBox.Show(Resources.MsgBox_NoNewLessonsImported_Text ?? "No new lessons found to import (duplicates?).", Resources.MsgBox_Title_ImportResult ?? "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during lesson import: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_ImportError_Text ?? "Error importing:\n{0}", ex.Message), Resources.MsgBox_Title_ImportError ?? "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportLessons(object? parameter) { if (Contract == null) { MessageBox.Show(Resources.MsgBox_SelectContractBeforeExport_Text ?? "Select contract first.", Resources.MsgBox_Title_NoContractSelected ?? "No Contract", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (!Lessons.Any()) { MessageBox.Show(Resources.MsgBox_NoLessonsToExport_Text ?? "No lessons to export.", Resources.MsgBox_Title_Export ?? "Export", MessageBoxButton.OK, MessageBoxImage.Information); return; } string defaultFileName = $"Lessons_{Contract.Company}_{Contract.ContractNumber}.ics".Replace(" ", "_"); foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { defaultFileName = defaultFileName.Replace(c, '_'); } string? filePath = _dialogService.ShowSaveFileDialog("iCalendar files (*.ics)|*.ics", defaultFileName, Resources.ExportIcsButton_Content ?? "Export Lessons"); if (string.IsNullOrEmpty(filePath)) return; try { string contractInfo = $"{Contract.Company} - {Contract.ContractNumber}"; bool success = _calService.ExportLessons(this.Lessons, filePath, contractInfo); if (success) { MessageBox.Show(string.Format(Resources.MsgBox_ExportSuccessful_Text ?? "Exported to:\n{0}", filePath), Resources.MsgBox_Title_ExportComplete ?? "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); } else { MessageBox.Show(Resources.MsgBox_ExportError_Text ?? "Export failed.", Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning); } } catch (Exception ex) { Debug.WriteLine($"Error during lesson export: {ex}"); MessageBox.Show(string.Format(Resources.MsgBox_ExportFileError_Text ?? "Error exporting file:\n{0}", ex.Message), Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Error); } }

        private bool CanExecuteImportExportLessons(object? parameter) { return Contract != null && !IsEditingLesson; }

        private void ExecuteBillSelectedLessons(object? parameter) { if (parameter is not IList selectedItems || selectedItems.Count == 0 || !CanExecuteBillSelectedLessons(parameter)) return; var lessonsToBill = selectedItems.OfType<Lesson>().Where(l => l.IsConfirmed && !l.IsBilled).ToList(); if (!lessonsToBill.Any()) { MessageBox.Show(Resources.MsgBox_NoLessonsToBill_Text ?? "No lessons to bill.", Resources.MsgBox_Title_Billing ?? "Billing", MessageBoxButton.OK, MessageBoxImage.Information); return; } var inputDialog = new InvoiceInputDialog { Owner = Application.Current.MainWindow, Title = Resources.MsgBox_Title_Billing ?? "Enter Invoice Details" }; if (inputDialog.ShowDialog() == true) { string invoiceNumber = inputDialog.InvoiceNumber; DateTime? invoiceDate = inputDialog.InvoiceDate; if (!invoiceDate.HasValue) { MessageBox.Show(Resources.MsgBox_InvoiceDateError_Text ?? "Date error.", Resources.MsgBox_Title_InternalError ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; } foreach (var lesson in lessonsToBill) { lesson.IsBilled = true; lesson.InvoiceNumber = invoiceNumber; lesson.InvoiceDate = invoiceDate.Value; } NotifyBillingRelatedChanges(); (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); MessageBox.Show(string.Format(Resources.MsgBox_BillingSuccessful_Text ?? "{0} lessons billed with #{1} on {2:d}.", lessonsToBill.Count, invoiceNumber, invoiceDate.Value), Resources.MsgBox_Title_BillingComplete ?? "Billing Complete", MessageBoxButton.OK, MessageBoxImage.Information); } }

        private bool CanExecuteBillSelectedLessons(object? parameter) { if (IsEditingLesson) return false; if (parameter is IList selectedItems && selectedItems.Count > 0) { return selectedItems.OfType<Lesson>().Any(l => l.IsConfirmed && !l.IsBilled); } return false; }


        // --- Helper ---
        private void ResetLessonInputFields() { NewLessonDate = DateTime.Today; NewLessonStartTimeString = "09:00"; NewLessonDuration = TimeSpan.FromHours(1); NewLessonSummary = null; }
        private void NotifyCalculatedHoursChanged() { OnPropertyChanged(nameof(TotalInsertedHours)); OnPropertyChanged(nameof(TotalConfirmedHours)); OnPropertyChanged(nameof(BilledHours)); OnPropertyChanged(nameof(RemainingHours)); }
        private void NotifyBillingRelatedChanges() { NotifyCalculatedHoursChanged(); OnPropertyChanged(nameof(TotalBilledAmount)); OnPropertyChanged(nameof(TotalConfirmedUnbilledAmount)); OnPropertyChanged(nameof(TotalPotentialAmount)); }
        public void UpdateCommandStates() { NotifyAllCanExecuteChanged(); }
        private bool TryParseTime(string? input, out TimeSpan result) { result = TimeSpan.Zero; if (string.IsNullOrWhiteSpace(input)) return false; return TimeSpan.TryParseExact(input, @"hh\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out result); }

        // Metodo Helper per trovare indice inserimento
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