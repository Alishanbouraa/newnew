// OfflinePOS.DataAccess/Services/CustomerService.cs
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
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IUnitOfWork unitOfWork, ILogger<CustomerService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            try
            {
                return await _unitOfWork.Customers.GetAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all customers");
                throw;
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Customers.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer by ID {CustomerId}", id);
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            try
            {
                // Validate customer name is unique
                var existing = await _unitOfWork.Customers.GetAsync(
                    c => c.Name == customer.Name && c.IsActive);

                if (existing.Any())
                    throw new InvalidOperationException($"A customer with name '{customer.Name}' already exists");

                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Customer created: {CustomerName}", customer.Name);
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {CustomerName}", customer.Name);
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            try
            {
                // Check if customer exists
                var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(customer.Id);
                if (existingCustomer == null)
                    return false;

                // Check if name is unique
                var duplicateNameCheck = await _unitOfWork.Customers.GetAsync(
                    c => c.Name == customer.Name && c.Id != customer.Id && c.IsActive);

                if (duplicateNameCheck.Any())
                    throw new InvalidOperationException($"Another customer with name '{customer.Name}' already exists");

                await _unitOfWork.Customers.UpdateAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Customer updated: {CustomerName}", customer.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(id);
                if (customer == null)
                    return false;

                // Check for pending transactions
                var hasUnpaidTransactions = await _unitOfWork.Transactions.ExistsAsync(
                    t => t.CustomerId == id && t.RemainingBalance > 0 && t.Status != "Cancelled");

                if (hasUnpaidTransactions)
                    throw new InvalidOperationException("Cannot delete customer with pending transactions");

                // Soft delete - set IsActive to false
                customer.IsActive = false;
                await _unitOfWork.Customers.UpdateAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Customer soft deleted: {CustomerId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await GetAllCustomersAsync();

            try
            {
                return await _unitOfWork.Customers.GetAsync(
                    c => (c.Name.Contains(searchTerm) ||
                         (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm)) ||
                         (c.Address != null && c.Address.Contains(searchTerm))) &&
                         c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> SettleDebtAsync(int customerId, decimal amount, string paymentMethod, string reference, int userId)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
                if (customer == null)
                    throw new InvalidOperationException($"Customer with ID {customerId} not found");

                if (customer.CurrentBalance < amount)
                    throw new InvalidOperationException($"Payment amount ({amount}) exceeds customer balance ({customer.CurrentBalance})");

                // Update customer balance
                customer.CurrentBalance -= amount;
                await _unitOfWork.Customers.UpdateAsync(customer);

                // Find transactions with remaining balance, oldest first
                var transactions = (await _unitOfWork.Transactions.GetAsync(
                    t => t.CustomerId == customerId && t.RemainingBalance > 0 && t.Status != "Cancelled"))
                    .OrderBy(t => t.TransactionDate)
                    .ToList();

                // Apply payment to transactions
                decimal remainingPayment = amount;
                foreach (var transaction in transactions)
                {
                    if (remainingPayment <= 0)
                        break;

                    decimal paymentToApply = Math.Min(remainingPayment, transaction.RemainingBalance);

                    transaction.PaidAmount += paymentToApply;
                    transaction.RemainingBalance -= paymentToApply;

                    // Update transaction status if fully paid
                    if (transaction.RemainingBalance == 0)
                    {
                        transaction.Status = "Completed";
                    }

                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                    remainingPayment -= paymentToApply;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Debt settled for customer {CustomerId}: {Amount}", customerId, amount);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error settling debt for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<Transaction>> GetCustomerTransactionsAsync(int customerId)
        {
            try
            {
                return await _unitOfWork.Transactions.GetAsync(t => t.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for customer {CustomerId}", customerId);
                throw;
            }
        }
    }
}