// OfflinePOS.Cashier/Views/LoginView.xaml.cs
using OfflinePOS.Core.Models;
using OfflinePOS.Core.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Cashier.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Wire up password changed event since PasswordBox doesn't support binding
            PasswordBox.PasswordChanged += OnPasswordChanged;

            // Handle window loaded to set focus to username
            Loaded += OnWindowLoaded;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.Username))
            {
                PasswordBox.Password = string.Empty;
                Keyboard.Focus(PasswordBox);
            }
        }
    }
}