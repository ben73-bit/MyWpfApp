using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WpfMvvmApp.Models
{
    public class UniqueContractsAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is List<Contract> contracts)
            {
                // Verifica se ci sono contratti duplicati basati sul numero di contratto
                var duplicateContracts = contracts
                    .GroupBy(c => c.ContractNumber)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateContracts.Any())
                {
                    return new ValidationResult($"The following contract numbers are duplicated: {string.Join(", ", duplicateContracts)}");
                }
            }
            return ValidationResult.Success;
        }
    }
}