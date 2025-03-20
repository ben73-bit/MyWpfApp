using System;
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
    }
}