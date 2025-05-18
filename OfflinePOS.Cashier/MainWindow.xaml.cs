// OfflinePOS.Cashier/MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using System;
using System.Windows;

namespace OfflinePOS.Cashier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User _currentUser;
        private readonly ILogger<MainWindow> _logger;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="currentUser">Current authenticated user</param>
        /// <param name="logger">Logger</param>
        public MainWindow(
            IServiceProvider serviceProvider,
            User currentUser,
            ILogger<MainWindow> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize the component to load the XAML file
            InitializeComponent();

            // Set user information
            UserText.Text = $"User: {_currentUser.FullName} ({_currentUser.Role})";
        }

        /// <summary>
        /// Updates the status bar text
        /// </summary>
        /// <param name="statusMessage">Status message to display</param>
        public void UpdateStatus(string statusMessage)
        {
            StatusText.Text = statusMessage;
        }
    }
}