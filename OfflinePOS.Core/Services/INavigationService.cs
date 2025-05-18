// File: OfflinePOS.Core/Services/INavigationService.cs

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service interface for handling navigation between views
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to the specified view
        /// </summary>
        /// <param name="viewName">Name of the view to navigate to</param>
        void NavigateTo(string viewName);

        /// <summary>
        /// Logs out of the application
        /// </summary>
        void Logout();
    }
}