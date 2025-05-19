// File: OfflinePOS.Admin/ViewModels/CategoryDialogViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for the Category Dialog (Add/Edit)
    /// </summary>
    public class CategoryDialogViewModel : ViewModelCommandBase
    {
        private readonly ICategoryService _categoryService;
        private readonly User _currentUser;

        private Category _category;
        private bool _isNewCategory;
        private string _windowTitle;
        private string _errorMessage;
        private bool _isBusy;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Category being added or edited
        /// </summary>
        public Category Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        /// <summary>
        /// Flag indicating if this is a new category
        /// </summary>
        public bool IsNewCategory
        {
            get => _isNewCategory;
            set => SetProperty(ref _isNewCategory, value);
        }

        /// <summary>
        /// Title of the dialog window
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
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
        /// Flag indicating if a background operation is in progress
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Collection of available category types
        /// </summary>
        public ObservableCollection<string> CategoryTypes { get; } = new ObservableCollection<string>
        {
            "Product",
            "Expense"
        };

        /// <summary>
        /// Command for saving the category
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the CategoryDialogViewModel class
        /// </summary>
        /// <param name="categoryService">Category service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="category">Category to edit, or null for a new category</param>
        /// <param name="isNewCategory">Flag indicating if this is a new category</param>
        public CategoryDialogViewModel(
            ICategoryService categoryService,
            ILogger<CategoryDialogViewModel> logger,
            User currentUser,
            Category category,
            bool isNewCategory)
            : base(logger)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Set category and flags
            Category = category ?? throw new ArgumentNullException(nameof(category));
            IsNewCategory = isNewCategory;

            // Set window title
            WindowTitle = IsNewCategory ? "Add New Category" : "Edit Category";

            // Initialize commands
            SaveCommand = CreateCommand(SaveCategory, CanSaveCategory);
            CancelCommand = CreateCommand(Cancel);
        }

        /// <summary>
        /// Saves the category
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        // File: OfflinePOS.Admin/ViewModels/CategoryDialogViewModel.cs
        private async void SaveCategory(object parameter)
        {
            if (!ValidateCategory())
                return;

            try
            {
                IsBusy = true;

                // Always ensure Description is not null
                Category.Description ??= string.Empty;

                if (IsNewCategory)
                {
                    // Create new category
                    Category.CreatedById = _currentUser.Id;
                    await _categoryService.CreateCategoryAsync(Category);
                }
                else
                {
                    // Update existing category
                    Category.LastUpdatedById = _currentUser.Id;
                    Category.LastUpdatedDate = DateTime.Now;
                    await _categoryService.UpdateCategoryAsync(Category);
                }

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving category: {ex.Message}";
                _logger.LogError(ex, "Error saving category");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Validates the category data
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        // File: OfflinePOS.Admin/ViewModels/CategoryDialogViewModel.cs
        private bool ValidateCategory()
        {
            // Clear previous error
            ErrorMessage = string.Empty;

            // Check required fields
            if (string.IsNullOrWhiteSpace(Category.Name))
            {
                ErrorMessage = "Category name is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Category.Type))
            {
                ErrorMessage = "Category type is required";
                return false;
            }

            // Ensure Description is never null to satisfy database constraint
            if (Category.Description == null)
            {
                Category.Description = string.Empty;
            }

            return true;
        }
        /// <summary>
        /// Cancels the operation and closes the dialog
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void Cancel(object parameter)
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Determines if the category can be saved
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if the category can be saved, false otherwise</returns>
        private bool CanSaveCategory(object parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Category?.Name);
        }
    }
}