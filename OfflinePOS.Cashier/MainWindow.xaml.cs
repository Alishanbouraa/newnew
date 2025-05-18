// File: OfflinePOS.Cashier/MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Cashier.ViewModels;
using OfflinePOS.Cashier.Views;
using OfflinePOS.Core.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        public MainWindow(IServiceProvider serviceProvider, User currentUser, ILogger<MainWindow> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize the component to load the XAML file
            InitializeComponent();

            // Set user information
            UserText.Text = $"User: {_currentUser.FullName} ({_currentUser.Role})";

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Checking drawer status...";

                // Check if drawer is open
                var drawerViewModel = _serviceProvider.GetRequiredService<DrawerViewModel>();
                await drawerViewModel.InitializeAsync();

                // If no drawer is open, show drawer view first
                if (!drawerViewModel.IsDrawerOpen)
                {
                    _logger.LogInformation("No open drawer found, navigating to DrawerView");
                    StatusText.Text = "No open drawer. Please open a drawer to continue.";
                    await NavigateToViewAsync<DrawerView, DrawerViewModel>();
                }
                else
                {
                    // Otherwise, show sales view
                    _logger.LogInformation("Open drawer found, navigating to SalesView");
                    StatusText.Text = "Drawer open. Ready for sales.";
                    await NavigateToViewAsync<SalesView, SalesViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading main window");
                StatusText.Text = "Error initializing application";
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to the specified view and initializes its ViewModel asynchronously
        /// </summary>
        /// <typeparam name="TView">View type</typeparam>
        /// <typeparam name="TViewModel">ViewModel type</typeparam>
        public async Task NavigateToViewAsync<TView, TViewModel>()
            where TView : UserControl
            where TViewModel : ViewModelBase
        {
            try
            {
                _logger.LogInformation($"Navigating to {typeof(TView).Name}");

                // Get the ViewModel from service provider
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

                // Create the view with the ViewModel
                var view = (TView)Activator.CreateInstance(typeof(TView), viewModel);

                // Set it as the current content
                MainContent.Content = view;

                // If the ViewModel has an InitializeAsync method, properly await it
                if (viewModel.GetType().GetMethod("InitializeAsync") != null)
                {
                    try
                    {
                        var initMethod = viewModel.GetType().GetMethod("InitializeAsync");
                        var task = (Task)initMethod.Invoke(viewModel, null);

                        // Actually await the task properly
                        await task.ConfigureAwait(true); // Stay on UI thread
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error initializing {typeof(TViewModel).Name}");
                        StatusText.Text = $"Initialization error: {ex.Message}";
                    }
                }

                _logger.LogInformation($"Successfully navigated to {typeof(TView).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {typeof(TView).Name}");
                StatusText.Text = $"Navigation error: {ex.Message}";
                MessageBox.Show($"Error loading view: {ex.Message}", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to the specified view and initializes its ViewModel (non-async version for backward compatibility)
        /// </summary>
        /// <typeparam name="TView">View type</typeparam>
        /// <typeparam name="TViewModel">ViewModel type</typeparam>
        public void NavigateToView<TView, TViewModel>()
            where TView : UserControl
            where TViewModel : ViewModelBase
        {
            try
            {
                _logger.LogInformation($"Navigating to {typeof(TView).Name}");

                // Get the ViewModel from service provider
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

                // Create the view with the ViewModel
                var view = (TView)Activator.CreateInstance(typeof(TView), viewModel);

                // Set it as the current content
                MainContent.Content = view;

                // Async initialization will be handled by the view's Loaded event
                _logger.LogInformation($"Successfully navigated to {typeof(TView).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {typeof(TView).Name}");
                StatusText.Text = $"Navigation error: {ex.Message}";
                MessageBox.Show($"Error loading view: {ex.Message}", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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