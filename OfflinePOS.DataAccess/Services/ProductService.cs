// OfflinePOS.DataAccess/Services/ProductService.cs
using Microsoft.EntityFrameworkCore;
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
    /// Service for managing products
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        /// <summary>
        /// Initializes a new instance of the ProductService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _unitOfWork.Products.GetAsync(p => p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _unitOfWork.Products.GetAsync(p => p.CategoryId == categoryId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category {CategoryId}", categoryId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Products.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID {ProductId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> GetProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return null;

            try
            {
                var products = await _unitOfWork.Products.GetAsync(
                    p => (p.BoxBarcode == barcode || p.ItemBarcode == barcode) && p.IsActive);

                return products.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode {Barcode}", barcode);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> CreateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            try
            {
                // Check if product with the same barcodes already exists
                if (!string.IsNullOrEmpty(product.BoxBarcode))
                {
                    var existingByBoxBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.BoxBarcode == product.BoxBarcode && p.IsActive);

                    if (existingByBoxBarcode)
                        throw new InvalidOperationException($"A product with box barcode {product.BoxBarcode} already exists");
                }

                if (!string.IsNullOrEmpty(product.ItemBarcode))
                {
                    var existingByItemBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.ItemBarcode == product.ItemBarcode && p.IsActive);

                    if (existingByItemBarcode)
                        throw new InvalidOperationException($"A product with item barcode {product.ItemBarcode} already exists");
                }

                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product created: {ProductName}", product.Name);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", product.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            try
            {
                // Check if product exists
                var existingProduct = await _unitOfWork.Products.GetByIdAsync(product.Id);
                if (existingProduct == null)
                    return false;

                // Check if barcodes are unique
                if (!string.IsNullOrEmpty(product.BoxBarcode))
                {
                    var existingByBoxBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.BoxBarcode == product.BoxBarcode && p.Id != product.Id && p.IsActive);

                    if (existingByBoxBarcode)
                        throw new InvalidOperationException($"Another product with box barcode {product.BoxBarcode} already exists");
                }

                if (!string.IsNullOrEmpty(product.ItemBarcode))
                {
                    var existingByItemBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.ItemBarcode == product.ItemBarcode && p.Id != product.Id && p.IsActive);

                    if (existingByItemBarcode)
                        throw new InvalidOperationException($"Another product with item barcode {product.ItemBarcode} already exists");
                }

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductName}", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                    return false;

                // Soft delete - set IsActive to false
                product.IsActive = false;
                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product soft deleted: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Enumerable.Empty<Product>();

            try
            {
                var products = await _unitOfWork.Products.GetAsync(
                    p => (p.Name.Contains(searchTerm) ||
                          p.BoxBarcode == searchTerm ||
                          p.ItemBarcode == searchTerm) &&
                          p.IsActive);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}