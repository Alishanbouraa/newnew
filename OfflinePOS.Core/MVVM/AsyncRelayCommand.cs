using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Core.MVVM
{
    /// <summary>
    /// Implementation of ICommand for binding asynchronous methods in ViewModels
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Event raised when conditions affecting the execution status have changed
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Creates a new AsyncRelayCommand that can always execute
        /// </summary>
        /// <param name="execute">Function to execute</param>
        public AsyncRelayCommand(Func<object, Task> execute) : this(execute, null) { }

        /// <summary>
        /// Creates a new AsyncRelayCommand
        /// </summary>
        /// <param name="execute">Function to execute</param>
        /// <param name="canExecute">Function that determines if command can execute</param>
        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute)
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
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
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