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
                    LoadLessons(); // Carica le lezioni E notifica le ore calcolate
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
        public int TotalHours
        {
            get => Contract?.TotalHours ?? 0;
            set
            {
                if (Contract != null && Contract.TotalHours != value)
                {
                    Contract.TotalHours = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                    NotifyCalculatedHoursChanged(); // Le ore residue dipendono da TotalHours
                }
            }
        }
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
                    (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (EditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelEditLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ToggleLessonConfirmationCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Proprietà Calcolate per le Ore ---
        public double TotalInsertedHours => Lessons.Sum(l => l.Duration.TotalHours);
        public double TotalConfirmedHours => Lessons.Where(l => l.IsConfirmed).Sum(l => l.Duration.TotalHours);
        public double RemainingHours => (Contract?.TotalHours ?? 0) - TotalConfirmedHours;

        // --- Comandi ---
        public ICommand AddLessonCommand { get; }
        public ICommand EditLessonCommand { get; }
        public ICommand RemoveLessonCommand { get; }
        public ICommand CancelEditLessonCommand { get; }
        public ICommand ToggleLessonConfirmationCommand { get; }

        // Costruttore
        public ContractViewModel(Contract? contract)
        {
            _contract = contract;
            AddLessonCommand = new RelayCommand(ExecuteAddOrUpdateLesson, CanExecuteAddOrUpdateLesson);
            EditLessonCommand = new RelayCommand(ExecuteEditLesson, CanExecuteEditOrRemoveLesson);
            RemoveLessonCommand = new RelayCommand(ExecuteRemoveLesson, CanExecuteEditOrRemoveLesson);
            CancelEditLessonCommand = new RelayCommand(ExecuteCancelEditLesson, CanExecuteCancelEditLesson);
            ToggleLessonConfirmationCommand = new RelayCommand(ExecuteToggleLessonConfirmation, CanExecuteToggleLessonConfirmation);
            LoadLessons();
        }

        // Metodo helper caricamento lezioni
        private void LoadLessons()
        {
            Lessons.Clear();
            if (_contract?.Lessons != null)
            {
                foreach (var lesson in _contract.Lessons.OrderBy(l => l.Date))
                {
                    // RIMOSSO: Aggancio evento lesson.PropertyChanged -= Lesson_PropertyChanged;
                    Lessons.Add(lesson);
                }
            }
            NotifyCalculatedHoursChanged(); // Notifica dopo aver caricato/pulito
        }

        // RIMOSSO: Gestore Eventi Lesson_PropertyChanged
        // private void Lesson_PropertyChanged(object? sender, PropertyChangedEventArgs e) { ... }

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

        // --- Implementazione IDataErrorInfo ---
        string IDataErrorInfo.Error => GetValidationError();
        string IDataErrorInfo.this[string columnName] => GetValidationError(columnName);
        private string GetValidationError(string? propertyName = null)
        {
            if (Contract == null) return string.Empty;
            var context = new ValidationContext(Contract) { MemberName = propertyName };
            var results = new List<ValidationResult>();
            bool isValid = false;
            if (string.IsNullOrEmpty(propertyName)) { isValid = Validator.TryValidateObject(Contract, context, results, true); }
            else
            {
                var propertyInfo = Contract.GetType().GetProperty(propertyName);
                if (propertyInfo != null) { var value = propertyInfo.GetValue(Contract); isValid = Validator.TryValidateProperty(value, context, results); }
            }
            if (isValid || results.Count == 0) return string.Empty;
            return results.First().ErrorMessage ?? "Validation Error";
        }
        public bool IsValid => Contract != null && string.IsNullOrEmpty(GetValidationError());
        // --- Fine IDataErrorInfo ---

        // --- Metodi Esecuzione Comandi Lezione ---
        private void ExecuteAddOrUpdateLesson(object? parameter)
        {
            if (Contract == null) return;
            bool changed = false; // Flag per sapere se notificare

            if (IsEditingLesson && _lessonToEdit != null)
            {
                var oldDuration = _lessonToEdit.Duration;
                _lessonToEdit.Date = NewLessonDate;
                _lessonToEdit.Duration = NewLessonDuration;
                // Se la durata è cambiata, dobbiamo notificare
                if (oldDuration != _lessonToEdit.Duration) { changed = true; }
            }
            else
            {
                var newLesson = new Lesson { Date = NewLessonDate, Duration = NewLessonDuration, Contract = Contract, IsConfirmed = false };
                // RIMOSSO: Aggancio evento newLesson.PropertyChanged += Lesson_PropertyChanged;
                Lessons.Add(newLesson);
                Contract.Lessons.Add(newLesson);
                changed = true; // L'aggiunta cambia sempre i totali
            }

            if (changed) { NotifyCalculatedHoursChanged(); } // Notifica se aggiunto o durata cambiata

            ResetLessonInputFields();
            IsEditingLesson = false;
            _lessonToEdit = null;
        }
        private bool CanExecuteAddOrUpdateLesson(object? parameter) { return Contract != null && NewLessonDuration > TimeSpan.Zero && !IsEditingLesson; }

        private void ExecuteEditLesson(object? parameter)
        { if (parameter is Lesson lessonToEdit) { IsEditingLesson = true; _lessonToEdit = lessonToEdit; NewLessonDate = lessonToEdit.Date; NewLessonDuration = lessonToEdit.Duration; } }

        private void ExecuteRemoveLesson(object? parameter)
        {
            if (parameter is Lesson lessonToRemove && Contract?.Lessons != null)
            {
                var result = MessageBox.Show($"Remove lesson on {lessonToRemove.Date:d}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // RIMOSSO: Sgancio evento lessonToRemove.PropertyChanged -= Lesson_PropertyChanged;
                    bool removedFromVM = Lessons.Remove(lessonToRemove);
                    Contract.Lessons.Remove(lessonToRemove);

                    if (removedFromVM) { NotifyCalculatedHoursChanged(); } // Notifica dopo la rimozione
                }
            }
        }
        private void ExecuteCancelEditLesson(object? parameter) { ResetLessonInputFields(); IsEditingLesson = false; _lessonToEdit = null; }
        private bool CanExecuteCancelEditLesson(object? parameter) { return IsEditingLesson; }

        private void ExecuteToggleLessonConfirmation(object? parameter)
        {
            if (parameter is Lesson lessonToToggle)
            {
                var oldState = lessonToToggle.IsConfirmed;
                lessonToToggle.IsConfirmed = !lessonToToggle.IsConfirmed;

                // MODIFICATO: Chiama NotifyCalculatedHoursChanged direttamente qui
                if (oldState != lessonToToggle.IsConfirmed)
                {
                    NotifyCalculatedHoursChanged();
                }

                // --- Punto di estensione per effetti collaterali futuri ---
                if (lessonToToggle.IsConfirmed) { /* Logica post-conferma */ }
                else { /* Logica post-deconferma */ }
                // ---------------------------------------------------------
            }
        }
        private bool CanExecuteToggleLessonConfirmation(object? parameter) { return parameter is Lesson && !IsEditingLesson; }
        private bool CanExecuteEditOrRemoveLesson(object? parameter) { return parameter is Lesson && !IsEditingLesson; }

        // --- Metodi Helper ---
        private void ResetLessonInputFields() { NewLessonDate = DateTime.Today; NewLessonDuration = TimeSpan.FromHours(1); }

        // Metodo helper per notificare l'aggiornamento delle ore calcolate
        private void NotifyCalculatedHoursChanged()
        {
            OnPropertyChanged(nameof(TotalInsertedHours));
            OnPropertyChanged(nameof(TotalConfirmedHours));
            OnPropertyChanged(nameof(RemainingHours));
        }
    }
}