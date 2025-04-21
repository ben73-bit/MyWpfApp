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
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

                // --- IDataErrorInfo ---

        // La proprietà Error restituisce un errore generale per l'oggetto.
        // Potremmo usarla per errori che non appartengono a una singola proprietà,
        // ma per ora la lasciamo semplice e ci concentriamo sull'indicizzatore.
        string IDataErrorInfo.Error => string.Empty; // O potremmo chiamare GetFirstError() qui se volessimo un errore generale

        // L'indicizzatore restituisce l'errore per una proprietà specifica.
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                string error = string.Empty;

                // Validazione proprietà specifiche del ViewModel (se ce ne sono altre oltre a questa)
                if (columnName == nameof(NewLessonStartTimeString))
                {
                    if (!TryParseTime(NewLessonStartTimeString, out _))
                    {
                        error = Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format (use HH:MM).";
                    }
                }
                // Altrimenti, prova a validare la proprietà sul modello Contract sottostante
                else if (Contract != null)
                {
                    // Ottieni il valore corrente della proprietà dal ViewModel (che wrappa il modello)
                    // Questo è necessario perché TryValidateProperty vuole il valore attuale.
                    object? propertyValue = GetPropertyValue(this, columnName);

                    var validationContext = new ValidationContext(Contract, null, null)
                    {
                        MemberName = columnName
                    };

                    var validationResults = new List<ValidationResult>();
                    bool isValid = Validator.TryValidateProperty(propertyValue, validationContext, validationResults);

                    if (!isValid)
                    {
                        // Restituisce il primo errore trovato per questa proprietà
                        error = validationResults.First().ErrorMessage ?? "Validation Error";
                    }
                }

                return error; // Restituisce l'errore trovato o string.Empty
            }
        }

        // Metodo helper per ottenere il valore di una proprietà tramite reflection
        // (necessario per TryValidateProperty quando si valida da un ViewModel)
        private static object? GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
            }
            catch
            {
                // Ignora errori di reflection (es. proprietà non trovata)
                return null;
            }
        }


        // GetFirstError non è strettamente necessario per la validazione per proprietà,
        // ma lo manteniamo se Error dovesse usarlo.
        // Restituisce solo l'errore di NewLessonStartTimeString per ora.
        private string GetFirstError()
        {
             if (!TryParseTime(NewLessonStartTimeString, out _))
             {
                 return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format.";
             }
             // Qui potremmo aggiungere una chiamata a Validator.ValidateObject(Contract, ...)
             // per ottenere un errore generale se l'intero contratto non è valido, ma
             // di solito è più utile avere errori per proprietà.
             return string.Empty;
        }

        // IsContractValid rimane invariato, usa la validazione dell'intero oggetto
        public bool IsContractValid => Contract != null && Validator.TryValidateObject(Contract, new ValidationContext(Contract), null, true);

        // IsLessonInputValid rimane invariato
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
                Lessons.Add(newLesson);
                requiresNotify = true;
            }

            if (requiresNotify) { NotifyBillingRelatedChanges(); }
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
        private void ExecuteDuplicateLesson(object? parameter)
        {
            if (parameter is not Lesson originalLesson || Contract == null || !CanExecuteDuplicateLesson(parameter)) return;
            try
            {
                var duplicatedLesson = new Lesson
                {
                    Uid = Guid.NewGuid().ToString(),
                    StartDateTime = originalLesson.StartDateTime,
                    Duration = originalLesson.Duration,
                    Summary = originalLesson.Summary,
                    Description = originalLesson.Description,
                    Location = originalLesson.Location,
                    Contract = this.Contract,
                    IsConfirmed = false,
                    IsBilled = false,
                    InvoiceNumber = null,
                    InvoiceDate = null
                };
                Contract.Lessons ??= new List<Lesson>();
                Contract.Lessons.Add(duplicatedLesson);
                Lessons.Add(duplicatedLesson);
                NotifyBillingRelatedChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error duplicating lesson: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_GenericError_Text ?? "An unexpected error occurred: {0}", ex.Message),
                                Resources.MsgBox_Title_InternalError ?? "Internal Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool CanExecuteDuplicateLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }

        // ExecuteImportLessons - MODIFICATO per controllo duplicati
        private void ExecuteImportLessons(object? parameter)
        {
            if (Contract == null)
            {
                MessageBox.Show(Resources.MsgBox_SelectContractBeforeImport_Text ?? "Select contract first.", Resources.MsgBox_Title_NoContractSelected ?? "No Contract", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string? filePath = _dialogService.ShowOpenFileDialog("iCalendar files (*.ics)|*.ics|All files (*.*)|*.*", Resources.ImportIcsButton_Content ?? "Import Lessons");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                List<Lesson> importedLessons = _calService.ImportLessons(filePath);
                if (importedLessons.Count == 0)
                {
                    MessageBox.Show(Resources.MsgBox_NoLessonsToImport_Text ?? "No lessons found.", Resources.MsgBox_Title_ImportResult ?? "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int addedCount = 0;
                int skippedCount = 0;
                Contract.Lessons ??= new List<Lesson>();

                var existingLessonKeys = new HashSet<string>(
                    Lessons.Select(l => !string.IsNullOrEmpty(l.Uid)
                                        ? $"UID:{l.Uid}"
                                        : $"DT:{l.StartDateTime:O}|DUR:{l.Duration.Ticks}")
                );

                foreach (var importedLesson in importedLessons)
                {
                    string lessonKey;
                    if (!string.IsNullOrEmpty(importedLesson.Uid))
                    {
                        lessonKey = $"UID:{importedLesson.Uid}";
                    }
                    else
                    {
                        lessonKey = $"DT:{importedLesson.StartDateTime:O}|DUR:{importedLesson.Duration.Ticks}";
                    }

                    if (existingLessonKeys.Contains(lessonKey))
                    {
                        skippedCount++;
                        Debug.WriteLine($"Skipping duplicate lesson: {lessonKey}");
                        continue;
                    }

                    importedLesson.Contract = Contract;
                    Contract.Lessons.Add(importedLesson);
                    Lessons.Add(importedLesson);
                    existingLessonKeys.Add(lessonKey); // Aggiungi chiave nuova per controllo duplicati interni al file
                    addedCount++;
                }

                string finalMessage;
                if (addedCount > 0 && skippedCount > 0)
                {
                    finalMessage = string.Format(Resources.MsgBox_ImportResult_AddedAndSkipped_Text ?? "{0} lessons imported, {1} duplicates skipped.", addedCount, skippedCount);
                }
                else if (addedCount > 0)
                {
                    finalMessage = string.Format(Resources.MsgBox_ImportSuccessful_Text ?? "{0} lessons imported successfully.", addedCount);
                }
                else if (skippedCount > 0)
                {
                    finalMessage = string.Format(Resources.MsgBox_ImportResult_OnlySkipped_Text ?? "No new lessons imported, {0} duplicates found and skipped.", skippedCount);
                }
                else
                {
                    finalMessage = Resources.MsgBox_NoLessonsToImport_Text ?? "No valid lesson events found in the selected file.";
                }

                MessageBox.Show(finalMessage, Resources.MsgBox_Title_ImportComplete ?? "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                if (addedCount > 0)
                {
                    NotifyBillingRelatedChanges();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during lesson import: {ex}");
                MessageBox.Show(string.Format(Resources.MsgBox_ImportError_Text ?? "Error importing:\n{0}", ex.Message),
                                Resources.MsgBox_Title_ImportError ?? "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ExecuteExportLessons
        private void ExecuteExportLessons(object? parameter)
        {
             if (Contract == null) { MessageBox.Show(Resources.MsgBox_SelectContractBeforeExport_Text ?? "Select contract first.", Resources.MsgBox_Title_NoContractSelected ?? "No Contract", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
             if (!Lessons.Any()) { MessageBox.Show(Resources.MsgBox_NoLessonsToExport_Text ?? "No lessons to export.", Resources.MsgBox_Title_Export ?? "Export", MessageBoxButton.OK, MessageBoxImage.Information); return; }
             string defaultFileName = $"Lessons_{Contract.Company}_{Contract.ContractNumber}.ics".Replace(" ", "_");
             foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { defaultFileName = defaultFileName.Replace(c, '_'); }
             string? filePath = _dialogService.ShowSaveFileDialog("iCalendar files (*.ics)|*.ics", defaultFileName, Resources.ExportIcsButton_Content ?? "Export Lessons");
             if (string.IsNullOrEmpty(filePath)) return;
             try
             {
                 string contractInfo = $"{Contract.Company} - {Contract.ContractNumber}";
                 // Passa la collezione sorgente Lessons al servizio
                 bool success = _calService.ExportLessons(this.Lessons, filePath, contractInfo);
                 if (success) { MessageBox.Show(string.Format(Resources.MsgBox_ExportSuccessful_Text ?? "Exported to:\n{0}", filePath), Resources.MsgBox_Title_ExportComplete ?? "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); }
                 else { MessageBox.Show(Resources.MsgBox_ExportError_Text ?? "Export failed.", Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"Error during lesson export: {ex}");
                 MessageBox.Show(string.Format(Resources.MsgBox_ExportFileError_Text ?? "Error exporting file:\n{0}", ex.Message), Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }
        private bool CanExecuteImportExportLessons(object? parameter) { return Contract != null && !IsEditingLesson; }
        private void ExecuteBillSelectedLessons(object? parameter)
        {
             if (parameter is not IList selectedItems || selectedItems.Count == 0 || !CanExecuteBillSelectedLessons(parameter)) return;
             var lessonsToBill = selectedItems.OfType<Lesson>().Where(l => l.IsConfirmed && !l.IsBilled).ToList();
             if (!lessonsToBill.Any()) { MessageBox.Show(Resources.MsgBox_NoLessonsToBill_Text ?? "No lessons to bill.", Resources.MsgBox_Title_Billing ?? "Billing", MessageBoxButton.OK, MessageBoxImage.Information); return; }
             var inputDialog = new InvoiceInputDialog { Owner = Application.Current.MainWindow, Title = Resources.MsgBox_Title_Billing ?? "Enter Invoice Details" };
             if (inputDialog.ShowDialog() == true)
             {
                 string invoiceNumber = inputDialog.InvoiceNumber;
                 DateTime? invoiceDate = inputDialog.InvoiceDate;
                 if (!invoiceDate.HasValue) { MessageBox.Show(Resources.MsgBox_InvoiceDateError_Text ?? "Date error.", Resources.MsgBox_Title_InternalError ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                 foreach (var lesson in lessonsToBill) { lesson.IsBilled = true; lesson.InvoiceNumber = invoiceNumber; lesson.InvoiceDate = invoiceDate.Value; }
                 NotifyBillingRelatedChanges();
                 (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                 MessageBox.Show(string.Format(Resources.MsgBox_BillingSuccessful_Text ?? "{0} lessons billed with #{1} on {2:d}.", lessonsToBill.Count, invoiceNumber, invoiceDate.Value), Resources.MsgBox_Title_BillingComplete ?? "Billing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
             }
        }
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
        private bool TryParseTime(string? input, out TimeSpan result)
        {
             result = TimeSpan.Zero;
             if (string.IsNullOrWhiteSpace(input)) return false;
             return TimeSpan.TryParseExact(input, @"hh\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out result);
        }
        private int FindInsertionIndex(Lesson newLesson) // Non più usato per aggiungere, ma lo lascio
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