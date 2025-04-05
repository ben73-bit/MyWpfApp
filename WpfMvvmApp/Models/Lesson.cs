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
        private string _uid = Guid.NewGuid().ToString(); // ID univoco, inizializzato
        private DateTime _startDateTime;
        private TimeSpan _duration;
        private Contract? _contract;
        private bool _isConfirmed;
        private string? _summary;
        private string? _description;
        private string? _location;

        // Identificatore Univoco (per iCal e potenziale sync)
        // Non serve notifica se non cambia mai dopo la creazione
        public string Uid
        {
            get => _uid;
            // Rendere il setter privato o internal se non deve essere modificato dall'esterno
            set => SetProperty(ref _uid, value);
        }

        // MODIFICATO: Da Date a StartDateTime (include Data e Ora Inizio)
        [Required(ErrorMessage = "Start date and time is required.")]
        public DateTime StartDateTime
        {
            get => _startDateTime;
            set => SetProperty(ref _startDateTime, value);
        }

        // Durata rimane TimeSpan
        [Required(ErrorMessage = "Duration is required.")]
        // Aggiungere validazione per durata > 0
        [Range(typeof(TimeSpan), "00:00:01", "23:59:59", ErrorMessage = "Duration must be positive.")]
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        // Contratto di riferimento (nullable come prima)
        [Required(ErrorMessage = "Contract is required.")]
        public Contract? Contract
        {
            get => _contract;
            set => SetProperty(ref _contract, value);
        }

        // Stato conferma
        public bool IsConfirmed
        {
            get => _isConfirmed;
            set => SetProperty(ref _isConfirmed, value);
        }

        // --- Campi Aggiuntivi per iCal Mapping (Opzionali) ---

        // Titolo/Sommario dell'evento
        public string? Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        // Descrizione/Note
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        // Luogo
        public string? Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        // --- Costruttore ---
        public Lesson()
        {
            // Imposta valori di default sensati
            _startDateTime = DateTime.Now; // Default a ora corrente
            _duration = TimeSpan.FromHours(1);
            _isConfirmed = false;
            // _uid viene già inizializzato
            // Contract verrà impostato dal ViewModel
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
    }
}