// OfflinePOS.DataAccess/Services/DrawerService.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service for managing cash drawer operations
    /// </summary>
    public class DrawerService : IDrawerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DrawerService> _logger;

        /// <summary>
        /// Initializes a new instance of the DrawerService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public DrawerService(IUnitOfWork unitOfWork, ILogger<DrawerService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DrawerOperation> OpenDrawerAsync(int userId, decimal startingBalance)
        {
            try
            {
                // Check if user already has an open drawer
                var existingDrawer = await GetOpenDrawerForUserAsync(userId);
                if (existingDrawer != null)
                {
                    throw new InvalidOperationException($"User already has an open drawer (ID: {existingDrawer.Id})");
                }

                // Create new drawer operation
                var drawerOperation = new DrawerOperation
                {
                    UserId = userId,
                    StartingBalance = startingBalance,
                    EndingBalance = 0,
                    ExpectedBalance = startingBalance,
                    Difference = 0,
                    Status = "Open",
                    OpenTime = DateTime.Now,
                    CloseTime = null,
                    Notes = "Drawer opened"
                };

                await _unitOfWork.DrawerOperations.AddAsync(drawerOperation);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Drawer opened for user {UserId}", userId);
                return drawerOperation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening drawer for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DrawerOperation> CloseDrawerAsync(int drawerOperationId, decimal endingBalance, string notes)
        {
            try
            {
                var drawerOperation = await _unitOfWork.DrawerOperations.GetByIdAsync(drawerOperationId);
                if (drawerOperation == null)
                    throw new InvalidOperationException($"Drawer operation with ID {drawerOperationId} not found");

                if (drawerOperation.Status != "Open")
                    throw new InvalidOperationException("Cannot close a drawer that is not open");

                // Calculate expected balance
                drawerOperation.ExpectedBalance = await CalculateExpectedBalanceAsync(drawerOperationId);

                // Update drawer operation
                drawerOperation.EndingBalance = endingBalance;
                drawerOperation.Difference = endingBalance - drawerOperation.ExpectedBalance;
                drawerOperation.Status = "Closed";
                drawerOperation.CloseTime = DateTime.Now;
                drawerOperation.Notes = notes;

                await _unitOfWork.DrawerOperations.UpdateAsync(drawerOperation);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Drawer closed: {DrawerOperationId}", drawerOperationId);
                return drawerOperation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing drawer {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DrawerTransaction> RecordCashInAsync(int drawerOperationId, decimal amount,
            string reason, string reference, int userId)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            try
            {
                var drawerOperation = await _unitOfWork.DrawerOperations.GetByIdAsync(drawerOperationId);
                if (drawerOperation == null)
                    throw new InvalidOperationException($"Drawer operation with ID {drawerOperationId} not found");

                if (drawerOperation.Status != "Open")
                    throw new InvalidOperationException("Cannot record transactions for a closed drawer");

                // Create drawer transaction
                var drawerTransaction = new DrawerTransaction
                {
                    DrawerOperationId = drawerOperationId,
                    Amount = amount,
                    Type = "CashIn",
                    Reason = reason,
                    Reference = reference,
                    TransactionTime = DateTime.Now,
                    RecordedById = userId,
                    CreatedById = userId
                };

                await _unitOfWork.DrawerTransactions.AddAsync(drawerTransaction);

                // Update expected balance
                drawerOperation.ExpectedBalance += amount;
                await _unitOfWork.DrawerOperations.UpdateAsync(drawerOperation);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cash in recorded: {Amount} for drawer {DrawerOperationId}",
                    amount, drawerOperationId);

                return drawerTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording cash in for drawer {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DrawerTransaction> RecordCashOutAsync(int drawerOperationId, decimal amount,
            string reason, string reference, int userId)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            try
            {
                var drawerOperation = await _unitOfWork.DrawerOperations.GetByIdAsync(drawerOperationId);
                if (drawerOperation == null)
                    throw new InvalidOperationException($"Drawer operation with ID {drawerOperationId} not found");

                if (drawerOperation.Status != "Open")
                    throw new InvalidOperationException("Cannot record transactions for a closed drawer");

                // Check if there's enough cash in the drawer
                var expectedBalance = await CalculateExpectedBalanceAsync(drawerOperationId);
                if (expectedBalance < amount)
                    throw new InvalidOperationException($"Not enough cash in drawer. Expected balance: {expectedBalance}, Requested: {amount}");

                // Create drawer transaction
                var drawerTransaction = new DrawerTransaction
                {
                    DrawerOperationId = drawerOperationId,
                    Amount = amount,
                    Type = "CashOut",
                    Reason = reason,
                    Reference = reference,
                    TransactionTime = DateTime.Now,
                    RecordedById = userId,
                    CreatedById = userId
                };

                await _unitOfWork.DrawerTransactions.AddAsync(drawerTransaction);

                // Update expected balance
                drawerOperation.ExpectedBalance -= amount;
                await _unitOfWork.DrawerOperations.UpdateAsync(drawerOperation);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cash out recorded: {Amount} for drawer {DrawerOperationId}",
                    amount, drawerOperationId);

                return drawerTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording cash out for drawer {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DrawerOperation> GetOpenDrawerForUserAsync(int userId)
        {
            try
            {
                var drawerOperations = await _unitOfWork.DrawerOperations.GetAsync(
                    d => d.UserId == userId && d.Status == "Open");

                return drawerOperations.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open drawer for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DrawerOperation>> GetDrawerOperationsForUserAsync(
            int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Adjust end date to include the full day
                var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

                return await _unitOfWork.DrawerOperations.GetAsync(
                    d => d.UserId == userId && d.OpenTime >= startDate && d.OpenTime <= adjustedEndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drawer operations for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DrawerTransaction>> GetDrawerTransactionsAsync(int drawerOperationId)
        {
            try
            {
                return await _unitOfWork.DrawerTransactions.GetAsync(
                    dt => dt.DrawerOperationId == drawerOperationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drawer transactions for drawer {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<decimal> CalculateExpectedBalanceAsync(int drawerOperationId)
        {
            try
            {
                var drawerOperation = await _unitOfWork.DrawerOperations.GetByIdAsync(drawerOperationId);
                if (drawerOperation == null)
                    throw new InvalidOperationException($"Drawer operation with ID {drawerOperationId} not found");

                decimal expected = drawerOperation.StartingBalance;

                // Get all drawer transactions
                var drawerTransactions = await _unitOfWork.DrawerTransactions.GetAsync(
                    dt => dt.DrawerOperationId == drawerOperationId);

                // Add cash-in transactions
                expected += drawerTransactions
                    .Where(dt => dt.Type == "CashIn")
                    .Sum(dt => dt.Amount);

                // Subtract cash-out transactions
                expected -= drawerTransactions
                    .Where(dt => dt.Type == "CashOut")
                    .Sum(dt => dt.Amount);

                // Get all cash sales transactions
                var transactions = await _unitOfWork.Transactions.GetAsync(
                    t => t.DrawerOperationId == drawerOperationId &&
                         t.PaymentMethod == "Cash" &&
                         t.Status != "Cancelled");

                // Add cash sales
                expected += transactions.Sum(t => t.PaidAmount);

                return expected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating expected balance for drawer {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }
    }
}