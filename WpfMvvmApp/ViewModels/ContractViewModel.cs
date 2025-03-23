using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Contract? _contract; // Il campo può essere null

        public Contract? Contract // La proprietà può essere null
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
                }
            }
        }

        public ContractViewModel(Contract? contract) // Il parametro può essere null
        {
            _contract = contract;
            //Non lancio più l'eccezione per non bloccare il programma
            //Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Implementazione IDataErrorInfo migliorata
        string IDataErrorInfo.Error => null!;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName) || Contract == null)
                    return string.Empty;

                // Mappa le proprietà del ViewModel alle proprietà del modello Contract
                string propertyToValidate = columnName;
                object valueToValidate;

                // Ottieni il valore corretto in base alla proprietà
                switch (columnName)
                {
                    case nameof(Company):
                        valueToValidate = Contract.Company;
                        propertyToValidate = nameof(Contract.Company);
                        break;
                    case nameof(ContractNumber):
                        valueToValidate = Contract.ContractNumber;
                        propertyToValidate = nameof(Contract.ContractNumber);
                        break;
                    case nameof(HourlyRate):
                        valueToValidate = Contract.HourlyRate;
                        propertyToValidate = nameof(Contract.HourlyRate);
                        break;
                    case nameof(TotalHours):
                        valueToValidate = Contract.TotalHours;
                        propertyToValidate = nameof(Contract.TotalHours);
                        break;
                    case nameof(BilledHours):
                        valueToValidate = Contract.BilledHours;
                        propertyToValidate = nameof(Contract.BilledHours);
                        break;
                    case nameof(StartDate):
                        valueToValidate = Contract.StartDate;
                        propertyToValidate = nameof(Contract.StartDate);
                        break;
                    case nameof(EndDate):
                        valueToValidate = Contract.EndDate;
                        propertyToValidate = nameof(Contract.EndDate);
                        break;
                    default:
                        return string.Empty;
                }

                // Valida la proprietà
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

        // Nuova proprietà per verificare se il contratto è valido
        public bool IsValid
        {
            get
            {
                if (Contract == null)
                    return false;

                // Verifica la validità di tutte le proprietà rilevanti
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
                if (Contract != null)
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
                if (Contract != null)
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
                if (Contract != null)
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
                if (Contract != null)
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
                if (Contract != null)
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
                if (Contract != null)
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
                if (Contract != null)
                {
                    Contract.EndDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }
    }
}