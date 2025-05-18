// OfflinePOS.Cashier/ViewModels/ViewModelBase.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.MVVM;
using System;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace OfflinePOS.Cashier.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels in the cashier application
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        protected readonly ILogger _logger;
        private bool _isLoading;
        private string _loadingMessage;
        private string _errorMessage;
        /// <summary>
        /// Event raised when navigation is requested
        /// </summary>
        public event EventHandler<NavigationEventArgs> NavigationRequested;
        /// <summary>
        /// Flag indicating if the ViewModel is loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Message to display during loading
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Error message to display
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Initializes a new instance of the ViewModelBase class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        protected ViewModelBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a task and handles loading state and errors
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="operation">Task to execute</param>
        /// <param name="loadingMessage">Message to display during loading</param>
        /// <param name="errorMessage">Message to display on error</param>
        /// <param name="onSuccess">Action to perform on success</param>
        /// <returns>Result of the operation or default value on error</returns>
        protected async Task<T> ExecuteWithLoadingAsync<T>(
            Func<Task<T>> operation,
            string loadingMessage,
            string errorMessage,
            Action<T> onSuccess = null)
        {
            IsLoading = true;
            LoadingMessage = loadingMessage;
            ErrorMessage = string.Empty;

            try
            {
                var result = await operation();
                onSuccess?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);
                ErrorMessage = $"{errorMessage}: {ex.Message}";
                return default;
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }
        /// <summary>
        /// Raises the NavigationRequested event
        /// </summary>
        /// <param name="viewName">Name of the view to navigate to</param>
        protected void RequestNavigation(string viewName)
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(viewName));
        }

        /// <summary>
        /// Class to hold navigation event arguments
        /// </summary>
        public class NavigationEventArgs : EventArgs
        {
            /// <summary>
            /// Name of the view to navigate to
            /// </summary>
            public string ViewName { get; }

            /// <summary>
            /// Initializes a new instance of the NavigationEventArgs class
            /// </summary>
            /// <param name="viewName">Name of the view to navigate to</param>
            public NavigationEventArgs(string viewName)
            {
                ViewName = viewName ?? throw new ArgumentNullException(nameof(viewName));
            }
        }
        /// <summary>
        /// Executes a task and handles loading state and errors
        /// </summary>
        /// <param name="operation">Task to execute</param>
        /// <param name="loadingMessage">Message to display during loading</param>
        /// <param name="errorMessage">Message to display on error</param>
        /// <param name="onSuccess">Action to perform on success</param>
        /// <returns>True if operation succeeded, false otherwise</returns>
        protected async Task<bool> ExecuteWithLoadingAsync(
            Func<Task> operation,
            string loadingMessage,
            string errorMessage,
            Action onSuccess = null)
        {
            IsLoading = true;
            LoadingMessage = loadingMessage;
            ErrorMessage = string.Empty;

            try
            {
                await operation();
                onSuccess?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);
                ErrorMessage = $"{errorMessage}: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }
    }
}