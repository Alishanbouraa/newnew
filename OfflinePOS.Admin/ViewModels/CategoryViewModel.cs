// File: OfflinePOS.Admin/ViewModels/CategoryViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for category management
    /// </summary>
    public class CategoryViewModel : ViewModelCommandBase
    {
        private readonly ICategoryService _categoryService;
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider; // Add service provider

        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;
        private string _searchText;
        private KeyValuePair<string, string> _selectedCategoryType;
        private bool _isBusy;
        private string _statusMessage;

        /// <summary>
        /// Collection of categories
        /// </summary>
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>
        /// Currently selected category
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        /// <summary>
        /// Search text for finding categories
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Selected category type for filtering (Product/Expense)
        /// </summary>
        public KeyValuePair<string, string> SelectedCategoryType
        {
            get => _selectedCategoryType;
            set
            {
                if (SetProperty(ref _selectedCategoryType, value))
                {
                    // Refresh categories when type changes
                    SearchCategories(null);
                }
            }
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
        /// Status message to display
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Collection of available category types
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> CategoryTypes { get; } =
            new ObservableCollection<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("All", ""),
                new KeyValuePair<string, string>("Product", "Product"),
                new KeyValuePair<string, string>("Expense", "Expense")
            };

        /// <summary>
        /// Command for searching categories
        /// </summary>
        public ICommand SearchCategoriesCommand { get; }

        /// <summary>
        /// Command for refreshing the category list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command for adding a new category
        /// </summary>
        public ICommand AddCategoryCommand { get; }

        /// <summary>
        /// Command for editing a category
        /// </summary>
        public ICommand EditCategoryCommand { get; }

        /// <summary>
        /// Command for deleting a category
        /// </summary>
        public ICommand DeleteCategoryCommand { get; }

        /// <summary>
        /// Initializes a new instance of the CategoryViewModel class
        /// </summary>
        /// <param name="categoryService">Category service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public CategoryViewModel(
            ICategoryService categoryService,
            ILogger<CategoryViewModel> logger,
            User currentUser,
            IServiceProvider serviceProvider)
            : base(logger)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize collections
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            SearchCategoriesCommand = CreateCommand(SearchCategories);
            RefreshCommand = CreateCommand(async _ => await LoadDataAsync());
            AddCategoryCommand = CreateCommand(AddCategory);
            EditCategoryCommand = CreateCommand(EditCategory, CanEditCategory);
            DeleteCategoryCommand = CreateCommand(DeleteCategory, CanDeleteCategory);

            // Set default category type
            SelectedCategoryType = CategoryTypes.First();
        }

        /// <summary>
        /// Loads categories from the repository
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading categories...";

                IEnumerable<Category> categories;

                if (string.IsNullOrEmpty(SelectedCategoryType.Value))
                {
                    // Load all categories
                    categories = await _categoryService.GetAllCategoriesAsync();
                }
                else
                {
                    // Load categories by type
                    categories = await _categoryService.GetCategoriesByTypeAsync(SelectedCategoryType.Value);
                }

                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                StatusMessage = $"Loaded {Categories.Count} categories";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading categories: {ex.Message}";
                _logger.LogError(ex, "Error loading categories");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Searches for categories matching the search text
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void SearchCategories(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Searching categories...";

                // Get all categories (filtered by type if selected)
                IEnumerable<Category> categories;

                if (string.IsNullOrEmpty(SelectedCategoryType.Value))
                {
                    categories = await _categoryService.GetAllCategoriesAsync();
                }
                else
                {
                    categories = await _categoryService.GetCategoriesByTypeAsync(SelectedCategoryType.Value);
                }

                // Filter by search text if provided
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.ToLower();
                    categories = categories.Where(c =>
                        c.Name.ToLower().Contains(searchTerm) ||
                        (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
                }

                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                StatusMessage = $"Found {Categories.Count} categories";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching categories: {ex.Message}";
                _logger.LogError(ex, "Error searching categories with term: {SearchTerm}", SearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Opens the Add Category dialog
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void AddCategory(object parameter)
        {
            try
            {
                // Create new category
                var category = new Category
                {
                    Type = SelectedCategoryType.Value == "" ? "Product" : SelectedCategoryType.Value,
                    IsActive = true,
                    CreatedById = _currentUser.Id,
                    CreatedDate = DateTime.Now
                };

                // Get the typed logger from service provider
                var logger = _serviceProvider.GetService(typeof(ILogger<CategoryDialogViewModel>)) as
                    ILogger<CategoryDialogViewModel>;

                // Show category dialog
                var dialog = new CategoryDialogView(new CategoryDialogViewModel(
                    _categoryService,
                    logger,
                    _currentUser,
                    category,
                    true))
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh categories if dialog was successful
                if (result == true)
                {
                    LoadDataAsync();
                    StatusMessage = "Category added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding category: {ex.Message}";
                _logger.LogError(ex, "Error adding category");
            }
        }

        /// <summary>
        /// Opens the Edit Category dialog for the selected category
        /// </summary>
        /// <param name="parameter">Category to edit</param>
        private async void EditCategory(object parameter)
        {
            var category = parameter as Category ?? SelectedCategory;
            if (category == null)
                return;

            try
            {
                // Get the complete category
                var completeCategory = await _categoryService.GetCategoryByIdAsync(category.Id);
                if (completeCategory == null)
                {
                    StatusMessage = "Category not found";
                    return;
                }

                // Get the typed logger from service provider
                var logger = _serviceProvider.GetService(typeof(ILogger<CategoryDialogViewModel>)) as
                    ILogger<CategoryDialogViewModel>;

                // Show category dialog
                var dialog = new CategoryDialogView(new CategoryDialogViewModel(
                    _categoryService,
                    logger,
                    _currentUser,
                    completeCategory,
                    false))
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh categories if dialog was successful
                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Category updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing category: {ex.Message}";
                _logger.LogError(ex, "Error editing category {CategoryId}", category.Id);
            }
        }

        /// <summary>
        /// Deletes the selected category after confirmation
        /// </summary>
        /// <param name="parameter">Category to delete</param>
        private async void DeleteCategory(object parameter)
        {
            var category = parameter as Category ?? SelectedCategory;
            if (category == null)
                return;

            var result = MessageBox.Show($"Are you sure you want to delete category '{category.Name}'?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Deleting category...";

                    bool success = await _categoryService.DeleteCategoryAsync(category.Id);

                    if (success)
                    {
                        Categories.Remove(category);
                        StatusMessage = $"Category '{category.Name}' deleted successfully";
                    }
                    else
                    {
                        StatusMessage = $"Failed to delete category '{category.Name}'";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting category: {ex.Message}";
                    _logger.LogError(ex, "Error deleting category {CategoryId}", category.Id);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Determines if a category can be edited
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if a category can be edited, false otherwise</returns>
        private bool CanEditCategory(object parameter)
        {
            return parameter is Category || SelectedCategory != null;
        }

        /// <summary>
        /// Determines if a category can be deleted
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if a category can be deleted, false otherwise</returns>
        private bool CanDeleteCategory(object parameter)
        {
            return parameter is Category || SelectedCategory != null;
        }
    }
}