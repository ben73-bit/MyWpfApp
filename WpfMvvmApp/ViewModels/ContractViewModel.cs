using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Aggiunto
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Contract? _contract;

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
                }
            }
        }

        public ObservableCollection<Lesson> Lessons { get; } = new ObservableCollection<Lesson>(); // Aggiunto

        public ContractViewModel(Contract? contract)
        {
            _contract = contract;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string IDataErrorInfo.Error => null!;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName) || Contract == null)
                    return string.Empty;

                string propertyToValidate = columnName;
                object? valueToValidate = null; // Inizializza a null

                // Ottieni il valore corretto in base alla proprietà
                var propertyInfo = Contract.GetType().GetProperty(columnName);
                if (propertyInfo != null)
                {
                    valueToValidate = propertyInfo.GetValue(Contract);
                    propertyToValidate = propertyInfo.Name; // Usa il nome effettivo della proprietà del modello
                }
                else
                {
                     // Potrebbe essere una proprietà del ViewModel, non del Model
                     // Gestisci questo caso se necessario, altrimenti restituisci stringa vuota
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

        public string Company
        {
            get { return Contract?.Company ?? ""; }
            set
            {
                if (Contract != null && Contract.Company != value)
                {
                    Contract.Company = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public string ContractNumber
        {
            get { return Contract?.ContractNumber ?? ""; }
            set
            {
                if (Contract != null && Contract.ContractNumber != value)
                {
                    Contract.ContractNumber = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public decimal HourlyRate
        {
            get { return Contract?.HourlyRate ?? 0; }
            set
            {
                if (Contract != null && Contract.HourlyRate != value)
                {
                    Contract.HourlyRate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public int TotalHours
        {
            get { return Contract?.TotalHours ?? 0; }
            set
            {
                if (Contract != null && Contract.TotalHours != value)
                {
                    Contract.TotalHours = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public int BilledHours
        {
            get { return Contract?.BilledHours ?? 0; }
            set
            {
                if (Contract != null && Contract.BilledHours != value)
                {
                    Contract.BilledHours = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public DateTime StartDate
        {
            get { return Contract?.StartDate ?? DateTime.MinValue; }
            set
            {
                if (Contract != null && Contract.StartDate != value)
                {
                    Contract.StartDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public DateTime EndDate
        {
            get { return Contract?.EndDate ?? DateTime.MinValue; }
            set
            {
                if (Contract != null && Contract.EndDate != value)
                {
                    Contract.EndDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }
    }
}