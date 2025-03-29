using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows; // Aggiunto per MessageBox
using System.Windows.Input;
using WpfMvvmApp.Models;
using WpfMvvmApp.ViewModels; // Assicurati che questo sia presente se RelayCommand è qui

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Contract? _contract;
        private DateTime _newLessonDate = DateTime.Today;
        private TimeSpan _newLessonDuration;
        private Lesson? _lessonToEdit; // Memorizza la lezione in modifica
        private bool _isEditingLesson; // Flag per indicare la modalità di modifica

        public Contract? Contract
        {
            get { return _contract; }
            set
            {
                if (_contract != value)
                {
                    _contract = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Company));
                    OnPropertyChanged(nameof(ContractNumber));
                    OnPropertyChanged(nameof(HourlyRate));
                    OnPropertyChanged(nameof(TotalHours));
                    OnPropertyChanged(nameof(BilledHours));
                    OnPropertyChanged(nameof(StartDate));
                    OnPropertyChanged(nameof(EndDate));
                    OnPropertyChanged(nameof(IsValid));
                    OnPropertyChanged(nameof(Lessons));
                }
            }
        }

        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        public DateTime NewLessonDate
        {
            get => _newLessonDate;
            set
            {
                if (_newLessonDate != value)
                {
                    _newLessonDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan NewLessonDuration
        {
            get => _newLessonDuration;
            set
            {
                if (_newLessonDuration != value)
                {
                    _newLessonDuration = value;
                    OnPropertyChanged();
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiorna CanExecute di Add
                }
            }
        }

        public bool IsEditingLesson
        {
            get => _isEditingLesson;
            set
            {
                if (_isEditingLesson != value)
                {
                    _isEditingLesson = value;
                    OnPropertyChanged();
                    // Aggiorna CanExecute dei comandi quando lo stato di modifica cambia
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiorna anche Remove
                    (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Aggiorna Cancel
                }
            }
        }

        // Comandi per le lezioni
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand CancelEditLessonCommand { get; } // Nuovo comando Cancel

        public ContractViewModel(Contract? contract)
        {
            _contract = contract;
            AddLessonCommand = new RelayCommand(AddOrUpdateLesson, CanAddOrUpdateLesson); // Unico comando per Add/Update
            EditLessonCommand = new RelayCommand(EditLesson, CanEditOrRemoveLesson);
            RemoveLessonCommand = new RelayCommand(RemoveLesson, CanEditOrRemoveLesson);
            CancelEditLessonCommand = new RelayCommand(CancelEditLesson, CanCancelEditLesson); // Inizializza Cancel
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Aggiorna CanExecute per i comandi delle lezioni quando rilevante
            if (propertyName == nameof(NewLessonDuration) || propertyName == nameof(Contract) || propertyName == nameof(IsEditingLesson))
            {
                 (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Ora è AddOrUpdateLesson
            }
            // CanEditOrRemoveLesson dipende dal parametro e da IsEditingLesson
            if (propertyName == nameof(IsEditingLesson))
            {
                 (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                 (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                 (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }

            if (propertyName != nameof(IsValid))
            {
                 OnPropertyChanged(nameof(IsValid));
            }
        }

        // --- Implementazione IDataErrorInfo ---
        string IDataErrorInfo.Error => null!;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName) || Contract == null)
                    return string.Empty;

                string propertyToValidate = columnName;
                object? valueToValidate = null;

                var propertyInfo = Contract.GetType().GetProperty(columnName);
                if (propertyInfo != null)
                {
                    valueToValidate = propertyInfo.GetValue(Contract);
                    propertyToValidate = propertyInfo.Name;
                }
                else
                {
                     return string.Empty;
                }

                ValidationContext context = new ValidationContext(Contract) { MemberName = propertyToValidate };
                List<ValidationResult> results = new List<ValidationResult>();

                bool isValid = Validator.TryValidateProperty(
                    valueToValidate,
                    context,
                    results
                );

                if (!isValid && results.Count > 0)
                {
                    return string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                }

                return string.Empty;
            }
        }

        // --- Proprietà IsValid ---
        public bool IsValid
        {
            get
            {
                if (Contract == null)
                    return false;
                ValidationContext context = new ValidationContext(Contract);
                List<ValidationResult> results = new List<ValidationResult>();
                return Validator.TryValidateObject(Contract, context, results, true);
            }
        }

        // --- Metodi dei Comandi Lezione ---
        private bool CanAddOrUpdateLesson(object? parameter)
        {
            // Può aggiungere/aggiornare solo se c'è un contratto e la durata è valida
            return Contract != null && NewLessonDuration > TimeSpan.Zero;
        }

        private void AddOrUpdateLesson(object? parameter)
        {
            if (CanAddOrUpdateLesson(parameter) && Contract != null)
            {
                 if (IsEditingLesson && _lessonToEdit != null)
                 {
                     // Aggiorna lezione esistente
                     _lessonToEdit.Date = NewLessonDate;
                     _lessonToEdit.Duration = NewLessonDuration;

                     // Workaround per aggiornare la lista (Lesson non è INotifyPropertyChanged)
                     var index = Lessons.IndexOf(_lessonToEdit);
                     if (index != -1)
                     {
                         Lessons.RemoveAt(index);
                         Lessons.Insert(index, _lessonToEdit);
                     }

                     IsEditingLesson = false;
                     _lessonToEdit = null;
                 }
                 else
                 {
                    // Aggiungi nuova lezione
                    Lesson newLesson = new Lesson
                    {
                        Date = NewLessonDate,
                        Duration = NewLessonDuration,
                        Contract = Contract
                    };
                    Lessons.Add(newLesson);
                 }

                // Resetta i campi di input
                NewLessonDate = DateTime.Today;
                NewLessonDuration = TimeSpan.Zero;
            }
        }

        private bool CanEditOrRemoveLesson(object? parameter)
        {
            // Disabilita Edit/Remove se siamo già in modalità modifica
            return parameter is Lesson && !IsEditingLesson;
        }

        private void EditLesson(object? parameter)
        {
            if (parameter is Lesson lessonToEdit)
            {
                 IsEditingLesson = true; // Entra in modalità modifica
                 _lessonToEdit = lessonToEdit; // Memorizza la lezione da modificare

                 // Popola i campi di input
                 NewLessonDate = lessonToEdit.Date;
                 NewLessonDuration = lessonToEdit.Duration;
            }
        }

         private bool CanCancelEditLesson(object? parameter)
         {
            return IsEditingLesson; // Può annullare solo se si sta modificando
         }

        private void CancelEditLesson(object? parameter)
        {
            IsEditingLesson = false;
            _lessonToEdit = null;
            // Resetta i campi di input
            NewLessonDate = DateTime.Today;
            NewLessonDuration = TimeSpan.Zero;
        }

        private void RemoveLesson(object? parameter)
        {
            if (parameter is Lesson lessonToRemove)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to remove the lesson on {lessonToRemove.Date.ToShortDateString()}?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Lessons.Remove(lessonToRemove);
                    // Aggiornare BilledHours nel Contract se necessario
                }
            }
        }

        // --- Proprietà del Contratto (Wrappers) ---
        public string Company { get { return Contract?.Company ?? ""; } set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); } } }
        public string ContractNumber { get { return Contract?.ContractNumber ?? ""; } set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); } } }
        public decimal HourlyRate { get { return Contract?.HourlyRate ?? 0; } set { if (Contract != null && Contract.HourlyRate != value) { Contract.HourlyRate = value; OnPropertyChanged(); } } }
        public int TotalHours { get { return Contract?.TotalHours ?? 0; } set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); } } }
        public int BilledHours { get { return Contract?.BilledHours ?? 0; } set { if (Contract != null && Contract.BilledHours != value) { Contract.BilledHours = value; OnPropertyChanged(); } } }
        public DateTime StartDate { get { return Contract?.StartDate ?? DateTime.MinValue; } set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); } } }
        public DateTime EndDate { get { return Contract?.EndDate ?? DateTime.MinValue; } set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); } } }
    }
}