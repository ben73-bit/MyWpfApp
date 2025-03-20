using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WpfMvvmApp.Models;

namespace WpfMvvmApp.Models
{
    public class User
    {
        [Required(ErrorMessage = "Username is required.")]
        public required string Username { get; set; }

        [UniqueContractsAttribute(ErrorMessage = "The list of contracts contains duplicate contract numbers.")]
        public List<Contract> Contracts { get; set; } = new List<Contract>(); // Relazione con i contratti
    }
}