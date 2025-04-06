// WpfMvvmApp/Models/Lesson.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace WpfMvvmApp.Models
{
    public class Lesson : INotifyPropertyChanged
    {
        // Campi esistenti
        private string _uid = Guid.NewGuid().ToString();
        private DateTime _startDateTime;
        private TimeSpan _duration;
        private Contract? _contract;
        private bool _isConfirmed;
        private string? _summary;
        private string? _description;
        private string? _location;

        // NUOVI CAMPI PER FATTURAZIONE
        private bool _isBilled;
        private string? _invoiceNumber;
        private DateTime? _invoiceDate;

        // Proprietà Esistenti
        public string Uid { get => _uid; set => SetProperty(ref _uid, value); }

        [Required(ErrorMessage = "Start date and time is required.")]
        public DateTime StartDateTime { get => _startDateTime; set => SetProperty(ref _startDateTime, value); }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(typeof(TimeSpan), "00:00:01", "23:59:59", ErrorMessage = "Duration must be positive.")]
        public TimeSpan Duration { get => _duration; set => SetProperty(ref _duration, value); }

        [Required(ErrorMessage = "Contract is required.")]
        public Contract? Contract { get => _contract; set => SetProperty(ref _contract, value); }

        public bool IsConfirmed { get => _isConfirmed; set => SetProperty(ref _isConfirmed, value); }

        public string? Summary { get => _summary; set => SetProperty(ref _summary, value); }
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
        public string? Location { get => _location; set => SetProperty(ref _location, value); }


        // --- NUOVE PROPRIETA' PER FATTURAZIONE ---
        public bool IsBilled
        {
            get => _isBilled;
            set => SetProperty(ref _isBilled, value);
        }

        public string? InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public DateTime? InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }
        // --- FINE NUOVE PROPRIETA' ---


        // Costruttore
        public Lesson()
        {
            _startDateTime = DateTime.Now;
            _duration = TimeSpan.FromHours(1);
            _isConfirmed = false;
            _isBilled = false; // Default a non fatturato
        }

        // Implementazione INotifyPropertyChanged (invariata)
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
    }
}