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
// Assicurati che RelayCommand sia accessibile (es. usando WpfMvvmApp.Commands)
// using WpfMvvmApp.Commands;

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
                    OnPropertyChanged(nameof(Company));
                    OnPropertyChanged(nameof(ContractNumber));
                    OnPropertyChanged(nameof(HourlyRate));
                    OnPropertyChanged(nameof(TotalHours));
                    OnPropertyChanged(nameof(BilledHours));
                    OnPropertyChanged(nameof(StartDate));
                    OnPropertyChanged(nameof(EndDate));
                    OnPropertyChanged(nameof(IsValid));
                    LoadLessons();
                    ResetLessonInputFields();
                    IsEditingLesson = false; // Assicura reset stato modifica
                    _lessonToEdit = null;
                }
            }
        }

        // Wrappers proprietà Contratto
        public string Company { get => Contract?.Company ?? ""; set { if (Contract != null && Contract.Company != value) { Contract.Company = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public string ContractNumber { get => Contract?.ContractNumber ?? ""; set { if (Contract != null && Contract.ContractNumber != value) { Contract.ContractNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public decimal HourlyRate { get => Contract?.HourlyRate ?? 0; set { if (Contract != null && Contract.HourlyRate != value) { Contract.HourlyRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int TotalHours { get => Contract?.TotalHours ?? 0; set { if (Contract != null && Contract.TotalHours != value) { Contract.TotalHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public int BilledHours { get => Contract?.BilledHours ?? 0; set { if (Contract != null && Contract.BilledHours != value) { Contract.BilledHours = value; OnPropertyChanged(); /* TODO: Aggiornare IsValid? */ } } }
        public DateTime StartDate { get => Contract?.StartDate ?? DateTime.MinValue; set { if (Contract != null && Contract.StartDate != value) { Contract.StartDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }
        public DateTime EndDate { get => Contract?.EndDate ?? DateTime.MinValue; set { if (Contract != null && Contract.EndDate != value) { Contract.EndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } } }

        // Collezione lezioni per UI
        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>();

        // Proprietà input/modifica lezioni
        public DateTime NewLessonDate { get => _newLessonDate; set => SetProperty(ref _newLessonDate, value); }
        public TimeSpan NewLessonDuration
        {
            get => _newLessonDuration;
            set { if (SetProperty(ref _newLessonDuration, value)) { (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        }

        public bool IsEditingLesson
        {
            get => _isEditingLesson;
            private set
            {
                if (SetProperty(ref _isEditingLesson, value))
                {
                    // Aggiorna CanExecute dei comandi quando lo stato di modifica cambia
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    // Aggiorna anche il comando Toggle
                    (ToggleLessonConfirmationCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Comandi ---
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        // NUOVO: Comando per confermare/deconfermare
        public ICommand ToggleLessonConfirmationCommand { get; }
        // RIMOSSO: ConfirmLessonCommand

        // Costruttore
        public ContractViewModel(Contract? contract)
        {
            _contract = contract;

            // Inizializza comandi
            AddLessonCommand = new RelayCommand(ExecuteAddOrUpdateLesson, CanExecuteAddOrUpdateLesson);
            EditLessonCommand = new RelayCommand(ExecuteEditLesson, CanExecuteEditOrRemoveLesson); // Usa CanExecute aggiornato
            RemoveLessonCommand = new RelayCommand(ExecuteRemoveLesson, CanExecuteEditOrRemoveLesson); // Usa CanExecute aggiornato
            CancelEditLessonCommand = new RelayCommand(ExecuteCancelEditLesson, CanExecuteCancelEditLesson);
            // NUOVO: Inizializza il comando Toggle
            ToggleLessonConfirmationCommand = new RelayCommand(ExecuteToggleLessonConfirmation, CanExecuteToggleLessonConfirmation);

            LoadLessons();
        }

        // Metodo helper caricamento lezioni
        private void LoadLessons()
        {
            Lessons.Clear();
            if (_contract?.Lessons != null)
            {
                foreach (var lesson in _contract.Lessons.OrderBy(l => l.Date)) { Lessons.Add(lesson); }
            }
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value; OnPropertyChanged(propertyName); return true;
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
            bool isValid = string.IsNullOrEmpty(propertyName)
                ? Validator.TryValidateObject(Contract, context, results, true)
                : Validator.TryValidateProperty(Contract.GetType().GetProperty(propertyName!)?.GetValue(Contract), context, results);
            if (isValid) return string.Empty;
            return results.First().ErrorMessage ?? "Validation Error";
        }
        public bool IsValid => Contract != null && string.IsNullOrEmpty(GetValidationError());
        // --- Fine IDataErrorInfo ---

        // --- Metodi Esecuzione Comandi Lezione ---
        private void ExecuteAddOrUpdateLesson(object? parameter)
        {
            if (Contract == null) return;
            if (IsEditingLesson && _lessonToEdit != null)
            {
                _lessonToEdit.Date = NewLessonDate; _lessonToEdit.Duration = NewLessonDuration;
            }
            else
            {
                var newLesson = new Lesson { Date = NewLessonDate, Duration = NewLessonDuration, Contract = Contract, IsConfirmed = false };
                Lessons.Add(newLesson); Contract.Lessons.Add(newLesson);
            }
            ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null;
        }
        private bool CanExecuteAddOrUpdateLesson(object? parameter) { return Contract != null && NewLessonDuration > TimeSpan.Zero && !IsEditingLesson; } // Aggiunto !IsEditingLesson

        private void ExecuteEditLesson(object? parameter)
        { if (parameter is Lesson lessonToEdit) { IsEditingLesson = true; _lessonToEdit = lessonToEdit; NewLessonDate = lessonToEdit.Date; NewLessonDuration = lessonToEdit.Duration; } }
        private void ExecuteRemoveLesson(object? parameter)
        {
            if (parameter is Lesson lessonToRemove && Contract?.Lessons != null)
            {
                var result = MessageBox.Show($"Remove lesson on {lessonToRemove.Date:d}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) { Lessons.Remove(lessonToRemove); Contract.Lessons.Remove(lessonToRemove); /* TODO: Aggiornare BilledHours? */ }
            }
        }
        private void ExecuteCancelEditLesson(object? parameter) { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteCancelEditLesson(object? parameter) { return IsEditingLesson; }

        // NUOVO: Logica per Confermare/Deconfermare una Lezione
        private void ExecuteToggleLessonConfirmation(object? parameter)
        {
            if (parameter is Lesson lessonToToggle)
            {
                lessonToToggle.IsConfirmed = !lessonToToggle.IsConfirmed; // Inverti lo stato

                // --- Punto di estensione per effetti collaterali futuri ---
                if (lessonToToggle.IsConfirmed) { /* Logica post-conferma (es. aggiorna BilledHours) */ }
                else { /* Logica post-deconferma (es. aggiorna BilledHours) */ }
                // ---------------------------------------------------------
            }
        }

        // NUOVO: CanExecute per il comando Toggle
        private bool CanExecuteToggleLessonConfirmation(object? parameter)
        { return parameter is Lesson && !IsEditingLesson; } // Abilitato solo se non si sta modificando

        // Modificato: CanExecute condiviso solo per Edit/Remove
        private bool CanExecuteEditOrRemoveLesson(object? parameter)
        { return parameter is Lesson && !IsEditingLesson; } // Abilitato solo se non si sta modificando

        // --- Metodi Helper ---
        private void ResetLessonInputFields() { NewLessonDate = DateTime.Today; NewLessonDuration = TimeSpan.FromHours(1); }
    }
}