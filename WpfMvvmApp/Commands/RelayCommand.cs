// WpfMvvmApp/Commands/RelayCommand.cs
using System;
using System.Windows.Input;

namespace WpfMvvmApp.Commands // Assicurati che il namespace sia corretto
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute; // Delegato per l'azione da eseguire
        private readonly Predicate<object?>? _canExecute; // Delegato per verificare se l'azione può essere eseguita (opzionale)

        /// <summary>
        /// Crea un nuovo comando che può sempre essere eseguito.
        /// </summary>
        /// <param name="execute">Logica di esecuzione.</param>
        public RelayCommand(Action<object?> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Crea un nuovo comando.
        /// </summary>
        /// <param name="execute">Logica di esecuzione.</param>
        /// <param name="canExecute">Logica dello stato abilitato.</param>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Evento che viene sollevato quando cambia la possibilità di eseguire il comando.
        /// WPF si sottoscrive automaticamente a questo evento tramite CommandManager.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determina se il comando può essere eseguito.
        /// </summary>
        /// <param name="parameter">Parametro passato al comando.</param>
        /// <returns>True se il comando può essere eseguito, altrimenti False.</returns>
        public bool CanExecute(object? parameter)
        {
            // Se _canExecute è null, il comando può sempre essere eseguito.
            // Altrimenti, chiama il delegato _canExecute per determinare lo stato.
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Esegue la logica del comando.
        /// </summary>
        /// <param name="parameter">Parametro passato al comando.</param>
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Metodo per forzare la rivalutazione dello stato CanExecute.
        /// Utile se lo stato CanExecute dipende da qualcosa che non viene
        /// rilevato automaticamente da CommandManager.RequerySuggested.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // Forza WPF a richiedere lo stato CanExecute
            CommandManager.InvalidateRequerySuggested();
        }
    }
}