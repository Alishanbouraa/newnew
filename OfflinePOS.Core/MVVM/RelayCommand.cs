using System;
using System.Windows.Input;

namespace OfflinePOS.Core.MVVM
{
    /// <summary>
    /// Implementation of ICommand for binding commands to methods in ViewModels
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// Event raised when conditions affecting the execution status have changed
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Creates a new RelayCommand that can always execute
        /// </summary>
        /// <param name="execute">Action to execute</param>
        public RelayCommand(Action<object> execute) : this(execute, null) { }

        /// <summary>
        /// Creates a new RelayCommand
        /// </summary>
        /// <param name="execute">Action to execute</param>
        /// <param name="canExecute">Function that determines if command can execute</param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines if the command can execute
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if command can execute, false otherwise</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}