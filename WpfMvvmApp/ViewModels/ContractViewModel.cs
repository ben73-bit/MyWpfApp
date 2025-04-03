// WpfMvvmApp/ViewModels/ContractViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WpfMvvmApp.Models;
// Assicurati che RelayCommand sia accessibile
// using WpfMvvmApp.Commands; // O dove si trova RelayCommand

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        // Campi privati
        private Contract? _contract;
        private DateTime _newLessonDate = DateTime.Today;
        private TimeSpan _newLessonDuration = TimeSpan.FromHours(1);
        private Lesson? _lessonToEdit;
        private bool _isEditingLesson;

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
                    // Notifica cambiamenti delle proprietà dipendenti
                    OnPropertyChanged(nameof(Company));
                    OnPropertyChanged(nameof(ContractNumber));
                    OnPropertyChanged(nameof(HourlyRate));
                    OnPropertyChanged(nameof(TotalHours));
                    OnPropertyChanged(nameof(BilledHours));
                    OnPropertyChanged(nameof(StartDate));
                    OnPropertyChanged(nameof(EndDate));
                    OnPropertyChanged(nameof(IsValid));
                    // Aggiorna la collezione di lezioni quando il contratto cambia
                    LoadLessons();
                    // Resetta stato modifica lezioni
                    ResetLessonInputFields();
                    IsEditingLesson = false;
                    _lessonToEdit = null;
                }
            }
        }

        // Wrappers proprietà Contratto
        public string Company { get => Contract?.Company ?? ""; set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public string ContractNumber { get => Contract?.ContractNumber ?? ""; set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public decimal HourlyRate { get => Contract?.HourlyRate ?? 0; set { if (Contract != null && Contract.HourlyRate != value) { Contract.HourlyRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int TotalHours { get => Contract?.TotalHours ?? 0; set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int BilledHours { get => Contract?.BilledHours ?? 0; set { if (Contract != null && Contract.BilledHours != value) { Contract.BilledHours = value; OnPropertyChanged(); /* Potrebbe influenzare IsValid? */ } } }
        public DateTime StartDate { get => Contract?.StartDate ?? DateTime.MinValue; set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public DateTime EndDate { get => Contract?.EndDate ?? DateTime.MinValue; set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }

        // Collezione lezioni per UI
        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        // Proprietà input/modifica lezioni
        public DateTime NewLessonDate
        {
            get => _newLessonDate;
            set => SetProperty(ref _newLessonDate, value);
        }

        public TimeSpan NewLessonDuration
        {
            get => _newLessonDuration;
            set
            {
                if (SetProperty(ref _newLessonDuration, value))
                {
                    // Rimuovi il cast generico
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsEditingLesson
        {
            get => _isEditingLesson;
            private set
            {
                if (SetProperty(ref _isEditingLesson, value))
                {
                    // Aggiorna CanExecute dei comandi (senza cast generici)
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ConfirmLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Comandi ---
        // Inizializzati usando la versione non generica di RelayCommand
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        public ICommand ConfirmLessonCommand { get; }

        // Costruttore
        public ContractViewModel(Contract? contract)
        {
            _contract = contract;

            // Inizializza comandi usando la versione non generica
            AddLessonCommand = new RelayCommand(ExecuteAddOrUpdateLesson, CanExecuteAddOrUpdateLesson);
            EditLessonCommand = new RelayCommand(ExecuteEditLesson, CanExecuteEditRemoveConfirmLesson); // Passa il metodo non generico
            RemoveLessonCommand = new RelayCommand(ExecuteRemoveLesson, CanExecuteEditRemoveConfirmLesson); // Passa il metodo non generico
            CancelEditLessonCommand = new RelayCommand(ExecuteCancelEditLesson, CanExecuteCancelEditLesson);
            ConfirmLessonCommand = new RelayCommand(ExecuteConfirmLesson, CanExecuteConfirmLesson); // Passa il metodo non generico

            LoadLessons();
        }

        // Metodo helper caricamento lezioni
        private void LoadLessons()
        {
            Lessons.Clear();
            // Ora possiamo accedere a _contract.Lessons perché l'abbiamo aggiunto al modello
            if (_contract?.Lessons != null)
            {
                foreach (var lesson in _contract.Lessons.OrderBy(l => l.Date))
                {
                    Lessons.Add(lesson);
                }
            }
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // --- Fine Implementazione INotifyPropertyChanged ---

        // --- Implementazione IDataErrorInfo (per validazione Contratto) ---
        string IDataErrorInfo.Error => GetValidationError();
        string IDataErrorInfo.this[string columnName] => GetValidationError(columnName);

        private string GetValidationError(string? propertyName = null)
        {
            if (Contract == null) return string.Empty;
            var context = new ValidationContext(Contract) { MemberName = propertyName };
            var results = new List<ValidationResult>();
            bool isValid;
            if (string.IsNullOrEmpty(propertyName))
            {
                isValid = Validator.TryValidateObject(Contract, context, results, true);
            }
            else
            {
                var propertyInfo = Contract.GetType().GetProperty(propertyName);
                if (propertyInfo == null) return string.Empty;
                var value = propertyInfo.GetValue(Contract);
                isValid = Validator.TryValidateProperty(value, context, results);
            }
            if (isValid) return string.Empty;
            return results.First().ErrorMessage ?? "Validation Error";
        }

        public bool IsValid => Contract != null && string.IsNullOrEmpty(GetValidationError());
        // --- Fine IDataErrorInfo ---


        // --- Metodi Esecuzione Comandi Lezione (con object? parameter) ---

        private void ExecuteAddOrUpdateLesson(object? parameter) // Firma non generica
        {
            if (Contract == null) return;

            if (IsEditingLesson && _lessonToEdit != null)
            {
                _lessonToEdit.Date = NewLessonDate;
                _lessonToEdit.Duration = NewLessonDuration;
                // Non serve workaround grazie a INPC in Lesson
            }
            else
            {
                var newLesson = new Lesson
                {
                    Date = NewLessonDate,
                    Duration = NewLessonDuration,
                    Contract = Contract, // Associa al contratto
                    IsConfirmed = false
                };
                Lessons.Add(newLesson); // Aggiungi a ObservableCollection (UI)
                Contract.Lessons.Add(newLesson); // Aggiungi a List nel modello
            }

            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }

        private bool CanExecuteAddOrUpdateLesson(object? parameter) // Firma non generica
        {
            return Contract != null && NewLessonDuration > TimeSpan.Zero;
        }

        private void ExecuteEditLesson(object? parameter) // Firma non generica
        {
            // Cast del parametro a Lesson
            if (parameter is Lesson lessonToEdit)
            {
                IsEditingLesson = true;
                _lessonToEdit = lessonToEdit;
                NewLessonDate = lessonToEdit.Date;
                NewLessonDuration = lessonToEdit.Duration;
            }
        }

        private void ExecuteRemoveLesson(object? parameter) // Firma non generica
        {
            // Cast del parametro a Lesson
            if (parameter is Lesson lessonToRemove && Contract?.Lessons != null)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the lesson on {lessonToRemove.Date:d}?",
                                             "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Lessons.Remove(lessonToRemove);
                    Contract.Lessons.Remove(lessonToRemove); // Rimuovi anche dal modello
                    // TODO: Aggiornare BilledHours?
                }
            }
        }

        private void ExecuteCancelEditLesson(object? parameter) // Firma non generica
        {
            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }

        private bool CanExecuteCancelEditLesson(object? parameter) // Firma non generica
        {
            return IsEditingLesson;
        }

        private void ExecuteConfirmLesson(object? parameter) // Firma non generica
        {
            // Cast del parametro a Lesson
            if (parameter is Lesson lessonToConfirm)
            {
                lessonToConfirm.IsConfirmed = true;
                // Rivaluta CanExecute per questo comando (senza cast generico)
                (ConfirmLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                // TODO: Logica aggiuntiva?
            }
        }

        private bool CanExecuteConfirmLesson(object? parameter) // Firma non generica
        {
            // Cast del parametro a Lesson
            if (parameter is Lesson lessonToConfirm)
            {
                // Puoi confermare solo se la lezione NON è confermata e NON siamo in modifica
                return !lessonToConfirm.IsConfirmed && !IsEditingLesson;
            }
            return false; // Non puoi confermare se il parametro non è una Lesson
        }

        // CanExecute condiviso per Edit/Remove/Confirm (prima del check IsConfirmed)
        private bool CanExecuteEditRemoveConfirmLesson(object? parameter) // Firma non generica
        {
            // Puoi interagire solo se il parametro è una Lesson e non siamo in modifica
            return parameter is Lesson && !IsEditingLesson;
        }

        // --- Metodi Helper ---
        private void ResetLessonInputFields()
        {
            NewLessonDate = DateTime.Today;
            NewLessonDuration = TimeSpan.FromHours(1);
        }
    }
}