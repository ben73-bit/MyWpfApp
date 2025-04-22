// WpfMvvmApp/ViewModels/ContractViewModel.cs
using System;
using System.Collections; // Necessario per IList non generico
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
            BillSelectedLessonsCommand = new RelayCommand(ExecuteBillSelectedLessons, CanExecuteBillSelectedLessons); // Usa i metodi aggiornati
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
        string IDataErrorInfo.Error => string.Empty;
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                string error = string.Empty;
                if (columnName == nameof(NewLessonStartTimeString)) { if (!TryParseTime(NewLessonStartTimeString, out _)) { error = Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format (use HH:MM)."; } }
                else if (Contract != null)
                {
                    object? propertyValue = GetPropertyValue(this, columnName);
                    var validationContext = new ValidationContext(Contract, null, null) { MemberName = columnName };
                    var validationResults = new List<ValidationResult>();
                    bool isValid = Validator.TryValidateProperty(propertyValue, validationContext, validationResults);
                    if (!isValid) { error = validationResults.First().ErrorMessage ?? "Validation Error"; }
                }
                return error;
            }
        }
        private static object? GetPropertyValue(object obj, string propertyName) { try { return obj.GetType().GetProperty(propertyName)?.GetValue(obj); } catch { return null; } }
        private string GetFirstError() { if (!TryParseTime(NewLessonStartTimeString, out _)) { return Resources.Validation_InvalidTimeFormat ?? "Invalid Start Time format."; } return string.Empty; }
        public bool IsContractValid => Contract != null && Validator.TryValidateObject(Contract, new ValidationContext(Contract), null, true);
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
            if (!TryParseTime(NewLessonStartTimeString, out TimeSpan lessonTime)) { MessageBox.Show(Resources.Validation_InvalidTimeFormat ?? "Invalid start time format.", Resources.MsgBox_Title_ValidationError ?? "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
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
                var newLesson = new Lesson { Uid = Guid.NewGuid().ToString(), StartDateTime = finalStartDateTime, Duration = NewLessonDuration, Contract = Contract, IsConfirmed = false, Summary = NewLessonSummary, };
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
        private void ExecuteRemoveSelectedLessons(object? parameter)
        {
            if (parameter is not IList selectedItems || selectedItems.Count == 0 || Contract?.Lessons == null) return;
            var lessonsToRemove = selectedItems.OfType<Lesson>().ToList();
            if (!lessonsToRemove.Any()) return;

            string message = lessonsToRemove.Count == 1 ? Resources.MsgBox_ConfirmRemoveSelectedLesson_Text_Singular ?? "Remove selected lesson?" : string.Format(Resources.MsgBox_ConfirmRemoveSelectedLessons_Text_Plural ?? "Remove {0} selected lessons?", lessonsToRemove.Count);
            var result = MessageBox.Show(message, Resources.MsgBox_Title_ConfirmRemoval ?? "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                bool lessonsRemoved = false;
                foreach (var lesson in lessonsToRemove)
                {
                    bool removedFromView = Lessons.Remove(lesson);
                    bool removedFromModel = Contract.Lessons?.Remove(lesson) ?? false;
                    if (removedFromView || removedFromModel) { lessonsRemoved = true; }
                }
                if (lessonsRemoved) { NotifyBillingRelatedChanges(); }
            }
        }
        private bool CanExecuteRemoveSelectedLessons(object? parameter)
        {
             if (IsEditingLesson) return false;
             if (parameter is not IList selectedItems) return false;
             return selectedItems.OfType<Lesson>().Any();
        }
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
                var duplicatedLesson = new Lesson { Uid = Guid.NewGuid().ToString(), StartDateTime = originalLesson.StartDateTime, Duration = originalLesson.Duration, Summary = originalLesson.Summary, Description = originalLesson.Description, Location = originalLesson.Location, Contract = this.Contract, IsConfirmed = false, IsBilled = false, InvoiceNumber = null, InvoiceDate = null };
                Contract.Lessons ??= new List<Lesson>();
                Contract.Lessons.Add(duplicatedLesson);
                Lessons.Add(duplicatedLesson);
                NotifyBillingRelatedChanges();
            }
            catch (Exception ex) { Debug.WriteLine($"Error duplicating lesson: {ex}"); MessageBox.Show(string.Format(Resources.MsgBox_GenericError_Text ?? "An unexpected error occurred: {0}", ex.Message), Resources.MsgBox_Title_InternalError ?? "Internal Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private bool CanExecuteDuplicateLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        private void ExecuteImportLessons(object? parameter)
        {
            if (Contract == null) { MessageBox.Show(Resources.MsgBox_SelectContractBeforeImport_Text ?? "Select contract first.", Resources.MsgBox_Title_NoContractSelected ?? "No Contract", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string? filePath = _dialogService.ShowOpenFileDialog("iCalendar files (*.ics)|*.ics|All files (*.*)|*.*", Resources.ImportIcsButton_Content ?? "Import Lessons");
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                List<Lesson> importedLessons = _calService.ImportLessons(filePath);
                if (importedLessons.Count == 0) { MessageBox.Show(Resources.MsgBox_NoLessonsToImport_Text ?? "No lessons found.", Resources.MsgBox_Title_ImportResult ?? "Import", MessageBoxButton.OK, MessageBoxImage.Information); return; }
                int addedCount = 0; int skippedCount = 0; Contract.Lessons ??= new List<Lesson>();
                var existingLessonKeys = new HashSet<string>(Lessons.Select(l => !string.IsNullOrEmpty(l.Uid) ? $"UID:{l.Uid}" : $"DT:{l.StartDateTime:O}|DUR:{l.Duration.Ticks}"));
                foreach (var importedLesson in importedLessons)
                {
                    string lessonKey; if (!string.IsNullOrEmpty(importedLesson.Uid)) { lessonKey = $"UID:{importedLesson.Uid}"; } else { lessonKey = $"DT:{importedLesson.StartDateTime:O}|DUR:{importedLesson.Duration.Ticks}"; }
                    if (existingLessonKeys.Contains(lessonKey)) { skippedCount++; Debug.WriteLine($"Skipping duplicate lesson: {lessonKey}"); continue; }
                    importedLesson.Contract = Contract; Contract.Lessons.Add(importedLesson); Lessons.Add(importedLesson); existingLessonKeys.Add(lessonKey); addedCount++;
                }
                string finalMessage; if (addedCount > 0 && skippedCount > 0) { finalMessage = string.Format(Resources.MsgBox_ImportResult_AddedAndSkipped_Text ?? "{0} lessons imported, {1} duplicates skipped.", addedCount, skippedCount); } else if (addedCount > 0) { finalMessage = string.Format(Resources.MsgBox_ImportSuccessful_Text ?? "{0} lessons imported successfully.", addedCount); } else if (skippedCount > 0) { finalMessage = string.Format(Resources.MsgBox_ImportResult_OnlySkipped_Text ?? "No new lessons imported, {0} duplicates found and skipped.", skippedCount); } else { finalMessage = Resources.MsgBox_NoLessonsToImport_Text ?? "No valid lesson events found in the selected file."; }
                MessageBox.Show(finalMessage, Resources.MsgBox_Title_ImportComplete ?? "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                if (addedCount > 0) { NotifyBillingRelatedChanges(); }
            }
            catch (Exception ex) { Debug.WriteLine($"Error during lesson import: {ex}"); MessageBox.Show(string.Format(Resources.MsgBox_ImportError_Text ?? "Error importing:\n{0}", ex.Message), Resources.MsgBox_Title_ImportError ?? "Import Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
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
                bool success = _calService.ExportLessons(this.Lessons, filePath, contractInfo);
                if (success) { MessageBox.Show(string.Format(Resources.MsgBox_ExportSuccessful_Text ?? "Exported to:\n{0}", filePath), Resources.MsgBox_Title_ExportComplete ?? "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); }
                else { MessageBox.Show(Resources.MsgBox_ExportError_Text ?? "Export failed.", Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            catch (Exception ex) { Debug.WriteLine($"Error during lesson export: {ex}"); MessageBox.Show(string.Format(Resources.MsgBox_ExportFileError_Text ?? "Error exporting file:\n{0}", ex.Message), Resources.MsgBox_Title_ExportError ?? "Export Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private bool CanExecuteImportExportLessons(object? parameter) { return Contract != null && !IsEditingLesson; }

        // *** ExecuteBillSelectedLessons - CORRETTO ***
        private void ExecuteBillSelectedLessons(object? parameter)
        {
            Debug.WriteLine("--- ExecuteBillSelectedLessons START ---");

            // 1. Ottieni le lezioni selezionate idonee usando l'helper
            List<Lesson> lessonsToBill = GetBillableLessonsFromParameter(parameter);

            // 2. Controlla se ci sono lezioni da fatturare
            if (lessonsToBill == null || !lessonsToBill.Any())
            {
                Debug.WriteLine("ExecuteBillSelectedLessons: No eligible lessons found by helper. Showing message box.");
                // Mostra messaggio solo se il parametro originale non era vuoto,
                // altrimenti CanExecute dovrebbe aver già gestito la disabilitazione.
                if (parameter is IList selectedItems && selectedItems.Count > 0)
                {
                     MessageBox.Show(Resources.MsgBox_NoLessonsToBill_Text ?? "No confirmed, unbilled lessons selected to bill.",
                                    Resources.MsgBox_Title_Billing ?? "Billing", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Debug.WriteLine("--- ExecuteBillSelectedLessons END (No eligible lessons) ---");
                return;
            }
            Debug.WriteLine($"ExecuteBillSelectedLessons: Found {lessonsToBill.Count} eligible lessons.");

            // 3. Crea e mostra il dialogo
            try
            {
                Debug.WriteLine("ExecuteBillSelectedLessons: Creating InvoiceInputDialog...");
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null)
                {
                     Debug.WriteLine("!!! ExecuteBillSelectedLessons ERROR: Application.Current.MainWindow is null!");
                     MessageBox.Show("Cannot show billing dialog because the main window is not available.", Resources.MsgBox_Title_InternalError ?? "Internal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                     Debug.WriteLine("--- ExecuteBillSelectedLessons END (MainWindow Null) ---");
                     return;
                }

                var inputDialog = new InvoiceInputDialog
                {
                    Owner = mainWindow,
                    Title = Resources.MsgBox_Title_Billing ?? "Enter Invoice Details"
                };
                 Debug.WriteLine($"ExecuteBillSelectedLessons: Showing InvoiceInputDialog (Owner: {inputDialog.Owner.Title})...");

                // 4. Processa il risultato
                if (inputDialog.ShowDialog() == true)
                {
                    Debug.WriteLine("ExecuteBillSelectedLessons: Dialog confirmed by user.");
                    string invoiceNumber = inputDialog.InvoiceNumber;
                    DateTime? invoiceDate = inputDialog.InvoiceDate;

                    // Validazione input (anche se il dialogo dovrebbe già farla)
                    if (string.IsNullOrWhiteSpace(invoiceNumber)) { MessageBox.Show(Resources.MsgBox_InvoiceNumberEmpty_Text ?? "Invoice number cannot be empty.", Resources.MsgBox_Title_ValidationError ?? "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    if (!invoiceDate.HasValue) { MessageBox.Show(Resources.MsgBox_InvoiceDateNotSelected_Text ?? "Please select an invoice date.", Resources.MsgBox_Title_ValidationError ?? "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                     Debug.WriteLine($"ExecuteBillSelectedLessons: Applying Invoice #{invoiceNumber} Date {invoiceDate.Value:d} to {lessonsToBill.Count} lessons.");
                    // 5. Aggiorna le lezioni
                    foreach (var lesson in lessonsToBill)
                    {
                        lesson.IsBilled = true;
                        lesson.InvoiceNumber = invoiceNumber;
                        lesson.InvoiceDate = invoiceDate.Value;
                    }

                    // 6. Aggiorna stato generale e notifica utente
                    NotifyBillingRelatedChanges();
                    (BillSelectedLessonsCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiorna stato pulsante
                    MessageBox.Show(string.Format(Resources.MsgBox_BillingSuccessful_Text ?? "{0} lessons billed with #{1} on {2:d}.", lessonsToBill.Count, invoiceNumber, invoiceDate.Value),
                                    Resources.MsgBox_Title_BillingComplete ?? "Billing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                } else { Debug.WriteLine("ExecuteBillSelectedLessons: Dialog cancelled by user."); }
            } catch (Exception ex) { Debug.WriteLine($"!!! ERROR during InvoiceInputDialog creation or showing: {ex}"); MessageBox.Show($"An error occurred while trying to open the billing dialog:\n{ex.Message}", Resources.MsgBox_Title_InternalError ?? "Internal Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            Debug.WriteLine("--- ExecuteBillSelectedLessons END ---");
        }

        // *** CanExecuteBillSelectedLessons - CORRETTO ***
        private bool CanExecuteBillSelectedLessons(object? parameter)
        {
            // Non abilitare se si sta modificando una lezione
            if (IsEditingLesson) return false;

            // Ottieni la lista filtrata e verifica se ha elementi
            List<Lesson> billableLessons = GetBillableLessonsFromParameter(parameter); // Usa helper
            bool canExecute = billableLessons != null && billableLessons.Any();
            // Debug.WriteLine($"CanExecuteBillSelectedLessons: Result = {canExecute}"); // Debug opzionale
            return canExecute;
        }

        // *** Metodo helper GetBillableLessonsFromParameter - CORRETTO ***
        private List<Lesson> GetBillableLessonsFromParameter(object? parameter)
        {
             if (parameter is IList selectedItems)
             {
                 // Filtra direttamente qui
                 return selectedItems.OfType<Lesson>()
                                     .Where(l => l.IsConfirmed && !l.IsBilled)
                                     .ToList();
             }
             // Se il parametro non è una lista valida, restituisce lista vuota
             return new List<Lesson>();
        }


        // --- Logica per Ordinamento ---
        private void ExecuteSortLessons(object? parameter)
        {
            if (parameter is not string propertyName || LessonsView == null) return;
            var newDirection = ListSortDirection.Ascending;
            if (_currentSortProperty == propertyName && _currentSortDirection == ListSortDirection.Ascending) { newDirection = ListSortDirection.Descending; }
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
                    if (_currentSortProperty != nameof(Lesson.StartDateTime)) { LessonsView.SortDescriptions.Add(new SortDescription(nameof(Lesson.StartDateTime), ListSortDirection.Ascending)); }
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
        private int FindInsertionIndex(Lesson newLesson)
        {
            for (int i = 0; i < Lessons.Count; i++) { if (newLesson.StartDateTime < Lessons[i].StartDateTime) { return i; } }
            return Lessons.Count;
        }
    }
}