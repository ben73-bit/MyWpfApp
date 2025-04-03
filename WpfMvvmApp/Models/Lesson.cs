// WpfMvvmApp/Models/Lesson.cs
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace WpfMvvmApp.Models
{
    public class Lesson : INotifyPropertyChanged
    {
        private DateTime _date;
        private TimeSpan _duration;
        // Modificato: Campo e proprietà Contract resi nullable per risolvere CS8618
        private Contract? _contract;
        private bool _isConfirmed;

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        [Required(ErrorMessage = "Duration is required.")]
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        // Modificato: Proprietà resa nullable. L'attributo [Required] si applica comunque
        // quando la validazione viene eseguita esplicitamente (es. prima di salvare).
        // Il ViewModel dovrebbe assicurarsi che venga impostato.
        [Required(ErrorMessage = "Contract is required.")]
        public Contract? Contract
        {
            get => _contract;
            set => SetProperty(ref _contract, value);
        }

        public bool IsConfirmed
        {
            get => _isConfirmed;
            set => SetProperty(ref _isConfirmed, value);
        }

        public Lesson()
        {
            _date = DateTime.Today;
            _duration = TimeSpan.FromHours(1); // Default più sensato
            _isConfirmed = false;
            // _contract rimane null qui, verrà impostato dal ViewModel quando si aggiunge la lezione
        }

        // --- Implementazione INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // --- Fine Implementazione INotifyPropertyChanged ---
    }
}