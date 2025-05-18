// OfflinePOS.Admin/Diagnostics/MainWindowDiagnostics.cs
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Windows;

namespace OfflinePOS.Admin.Diagnostics
{
    /// <summary>
    /// Provides diagnostic utilities for analyzing MainWindow issues
    /// </summary>
    public static class MainWindowDiagnostics
    {
        /// <summary>
        /// Verifies the MainWindow's XAML elements and logs their status
        /// </summary>
        /// <param name="mainWindow">The MainWindow instance to verify</param>
        /// <param name="logger">Logger for recording results</param>
        public static void VerifyXamlElements(MainWindow mainWindow, ILogger logger)
        {
            try
            {
                // Get all fields in the MainWindow class
                var fields = typeof(MainWindow).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                logger.LogInformation($"Found {fields.Length} fields in MainWindow");

                // Try to access known XAML elements 
                var navigationListBox = FindElementByName<System.Windows.Controls.ListBox>(mainWindow, "NavigationListBox");
                logger.LogInformation($"NavigationListBox found: {navigationListBox != null}");

                var userNameTextBlock = FindElementByName<System.Windows.Controls.TextBlock>(mainWindow, "UserNameTextBlock");
                logger.LogInformation($"UserNameTextBlock found: {userNameTextBlock != null}");

                var pageTitleTextBlock = FindElementByName<System.Windows.Controls.TextBlock>(mainWindow, "PageTitleTextBlock");
                logger.LogInformation($"PageTitleTextBlock found: {pageTitleTextBlock != null}");

                // Check if Application.Current.MainWindow is set correctly
                logger.LogInformation($"Application.Current.MainWindow is this window: {Application.Current.MainWindow == mainWindow}");

                // Verify window properties
                logger.LogInformation($"Window Visibility: {mainWindow.Visibility}");
                logger.LogInformation($"Window IsVisible: {mainWindow.IsVisible}");
                logger.LogInformation($"Window IsLoaded: {mainWindow.IsLoaded}");

                // Check resource dictionaries
                var resourceCount = mainWindow.Resources?.MergedDictionaries?.Count ?? 0;
                logger.LogInformation($"Window has {resourceCount} merged resource dictionaries");

                // Check application resources
                var appResourceCount = Application.Current.Resources?.MergedDictionaries?.Count ?? 0;
                logger.LogInformation($"Application has {appResourceCount} merged resource dictionaries");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in VerifyXamlElements");
            }
        }

        /// <summary>
        /// Finds a named element within a window's template
        /// </summary>
        /// <typeparam name="T">Type of element to find</typeparam>
        /// <param name="window">Window to search</param>
        /// <param name="name">Name of the element to find</param>
        /// <returns>Element if found, null otherwise</returns>
        private static T FindElementByName<T>(Window window, string name) where T : DependencyObject
        {
            try
            {
                return (T)window.FindName(name);
            }
            catch
            {
                return null;
            }
        }
    }
}