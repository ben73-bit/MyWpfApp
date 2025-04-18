// WpfMvvmApp/Models/Lesson.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using WpfMvvmApp.Properties; // AGGIUNGI using per Resources

namespace WpfMvvmApp.Models
{
    public class Lesson : INotifyPropertyChanged
    {
        // ... (campi privati) ...
        private string _uid = Guid.NewGuid().ToString();
        private DateTime _startDateTime;
        private TimeSpan _duration;
        private Contract? _contract;
        private bool _isConfirmed;
        private string? _summary;
        private string? _description;
        private string? _location;
        private bool _isBilled;
        private string? _invoiceNumber;
        private DateTime? _invoiceDate;


        // --- Proprietà ---
        public string Uid { get => _uid; set => SetProperty(ref _uid, value); }

        // MODIFICATO: Required
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FieldRequired")]
        public DateTime StartDateTime { get => _startDateTime; set => SetProperty(ref _startDateTime, value); }

        // MODIFICATO: Required e Range
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FieldRequired")]
        [Range(typeof(TimeSpan), "00:00:01", "23:59:59", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_TimeSpanPositive")]
        public TimeSpan Duration
        {
            get => _duration;
            set { if (SetProperty(ref _duration, value)) { OnPropertyChanged(nameof(Amount)); } }
        }

        // MODIFICATO: Required (JsonIgnore rimane)
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FieldRequired")]
        [JsonIgnore]
        public Contract? Contract
        {
            get => _contract;
            set { if (SetProperty(ref _contract, value)) { OnPropertyChanged(nameof(Amount)); } }
        }

        public bool IsConfirmed { get => _isConfirmed; set => SetProperty(ref _isConfirmed, value); }
        public string? Summary { get => _summary; set => SetProperty(ref _summary, value); }
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
        public string? Location { get => _location; set => SetProperty(ref _location, value); }
        public bool IsBilled { get => _isBilled; set => SetProperty(ref _isBilled, value); }
        public string? InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }
        public DateTime? InvoiceDate { get => _invoiceDate; set => SetProperty(ref _invoiceDate, value); }

        public decimal Amount { get { /*...*/ return 0m; } } // Implementazione esistente

        // Costruttore
        public Lesson() { /*...*/ }

        // Implementazione INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
    }
}