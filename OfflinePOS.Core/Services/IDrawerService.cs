// OfflinePOS.Core/Services/IDrawerService.cs
using OfflinePOS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing cash drawer operations
    /// </summary>
    public interface IDrawerService
    {
        /// <summary>
        /// Opens a new drawer session
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startingBalance">Starting cash balance</param>
        /// <returns>Opened drawer operation</returns>
        Task<DrawerOperation> OpenDrawerAsync(int userId, decimal startingBalance);

        /// <summary>
        /// Closes a drawer session
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <param name="endingBalance">Ending cash balance</param>
        /// <param name="notes">Closing notes</param>
        /// <returns>Closed drawer operation</returns>
        Task<DrawerOperation> CloseDrawerAsync(int drawerOperationId, decimal endingBalance, string notes);

        /// <summary>
        /// Records a cash-in transaction
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <param name="amount">Amount added</param>
        /// <param name="reason">Reason for adding cash</param>
        /// <param name="reference">Reference information</param>
        /// <param name="userId">User who recorded the transaction</param>
        /// <returns>Created drawer transaction</returns>
        Task<DrawerTransaction> RecordCashInAsync(int drawerOperationId, decimal amount,
            string reason, string reference, int userId);

        /// <summary>
        /// Records a cash-out transaction
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <param name="amount">Amount removed</param>
        /// <param name="reason">Reason for removing cash</param>
        /// <param name="reference">Reference information</param>
        /// <param name="userId">User who recorded the transaction</param>
        /// <returns>Created drawer transaction</returns>
        Task<DrawerTransaction> RecordCashOutAsync(int drawerOperationId, decimal amount,
            string reason, string reference, int userId);

        /// <summary>
        /// Gets the current open drawer for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Open drawer operation, or null if none found</returns>
        Task<DrawerOperation> GetOpenDrawerForUserAsync(int userId);

        /// <summary>
        /// Gets drawer operations for a specific user in a date range
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Drawer operations</returns>
        Task<IEnumerable<DrawerOperation>> GetDrawerOperationsForUserAsync(
            int userId, System.DateTime startDate, System.DateTime endDate);

        /// <summary>
        /// Gets drawer transactions for a drawer operation
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <returns>Drawer transactions</returns>
        Task<IEnumerable<DrawerTransaction>> GetDrawerTransactionsAsync(int drawerOperationId);

        /// <summary>
        /// Calculates the expected balance for a drawer operation
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <returns>Expected balance</returns>
        Task<decimal> CalculateExpectedBalanceAsync(int drawerOperationId);
    }
}