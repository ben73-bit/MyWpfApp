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

        // Explicit IDataErrorInfo implementation
        string IDataErrorInfo.Error => null!;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName))
                    return string.Empty;

                string result = string.Empty;
                if (Contract != null) // Controllo di null
                {
                    ValidationContext context = new(Contract) { MemberName = columnName };
                    List<ValidationResult> results = new();
                    bool isValid = Validator.TryValidateProperty(
                        Contract.GetType().GetProperty(columnName)?.GetValue(Contract),
                        context,
                        results
                    );
                    if (!isValid)
                    {
                        result = string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                    }
                }
                return result;
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
                ValidationContext context = new(Contract);
                List<ValidationResult> results = new();
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
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
                    OnPropertyChanged(nameof(IsValid)); // Notifica che IsValid è cambiata
                }
            }
        }
    }
}