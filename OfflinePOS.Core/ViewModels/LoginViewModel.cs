// OfflinePOS.Core/ViewModels/LoginViewModel.cs
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Core.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly Action<User> _onLoginSuccess;

        private string _username;
        private string _errorMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password { private get; set; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthService authService, Action<User> onLoginSuccess)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _onLoginSuccess = onLoginSuccess ?? throw new ArgumentNullException(nameof(onLoginSuccess));

            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrEmpty(Password) &&
                   !IsLoading;
        }

        private async Task ExecuteLoginAsync(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;
                IsLoading = true;

                var user = await _authService.AuthenticateAsync(Username, Password);

                if (user != null)
                {
                    _onLoginSuccess(user);
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                Password = string.Empty; // Clear password for security
            }
        }
    }
}