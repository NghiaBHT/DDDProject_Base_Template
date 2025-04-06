using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Entities;
using DDDProject.Domain.ValueObjects; // For Sku

namespace DDDProject.Domain.Repositories;

/// <summary>
/// Repository specific to the Product aggregate root.
/// Inherits generic methods and can add product-specific queries.
/// </summary>
public interface IProductRepository : IRepository<Product, Guid>
{
    /// <summary>
    /// Checks if a product with the given SKU already exists.
    /// </summary>
    /// <param name="sku">The SKU to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the SKU exists, false otherwise.</returns>
    Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken = default);

    // Add other product-specific query methods here if needed
    // e.g., Task<List<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
} 