// OfflinePOS.Core/Services/ICustomerService.cs
using OfflinePOS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    public interface ICustomerService
    {
        /// <summary>
        /// Gets all active customers
        /// </summary>
        Task<IEnumerable<Customer>> GetAllCustomersAsync();

        /// <summary>
        /// Gets a customer by ID
        /// </summary>
        Task<Customer> GetCustomerByIdAsync(int id);

        /// <summary>
        /// Creates a new customer
        /// </summary>
        Task<Customer> CreateCustomerAsync(Customer customer);

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        Task<bool> UpdateCustomerAsync(Customer customer);

        /// <summary>
        /// Deletes a customer by ID
        /// </summary>
        Task<bool> DeleteCustomerAsync(int id);

        /// <summary>
        /// Searches for customers by name or contact info
        /// </summary>
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);

        /// <summary>
        /// Settles customer debt
        /// </summary>
        Task<bool> SettleDebtAsync(int customerId, decimal amount, string paymentMethod, string reference, int userId);

        /// <summary>
        /// Gets transaction history for a customer
        /// </summary>
        Task<IEnumerable<Transaction>> GetCustomerTransactionsAsync(int customerId);
    }
}