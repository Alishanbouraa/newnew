// File: OfflinePOS.Core/MVVM/ViewModelCommandBase.cs

using OfflinePOS.Core.MVVM;
using Microsoft.Extensions.Logging;
using System;
using System.Windows.Input;

namespace OfflinePOS.Core.MVVM
{
    /// <summary>
    /// Base class for ViewModels with common command functionality
    /// </summary>
    public abstract class ViewModelCommandBase : ObservableObject
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the ViewModelCommandBase class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        protected ViewModelCommandBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a command that handles exceptions internally
        /// </summary>
        /// <param name="execute">Action to execute</param>
        /// <param name="canExecute">Function that determines if command can execute</param>
        /// <param name="onException">Action to execute on exception</param>
        /// <returns>A command with exception handling</returns>
        protected ICommand CreateCommand(
            Action<object> execute,
            Func<object, bool> canExecute = null,
            Action<Exception> onException = null)
        {
            return new RelayCommand(
                parameter =>
                {
                    try
                    {
                        execute(parameter);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Command execution error");
                        onException?.Invoke(ex);
                    }
                },
                canExecute);
        }

        /// <summary>
        /// Creates an asynchronous command that handles exceptions internally
        /// </summary>
        /// <param name="execute">Function to execute</param>
        /// <param name="canExecute">Function that determines if command can execute</param>
        /// <param name="onException">Action to execute on exception</param>
        /// <returns>An async command with exception handling</returns>
        protected ICommand CreateAsyncCommand(
            Func<object, System.Threading.Tasks.Task> execute,
            Func<object, bool> canExecute = null,
            Action<Exception> onException = null)
        {
            return new AsyncRelayCommand(
                async parameter =>
                {
                    try
                    {
                        await execute(parameter);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Async command execution error");
                        onException?.Invoke(ex);
                    }
                },
                canExecute);
        }
    }
}