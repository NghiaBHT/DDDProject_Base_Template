using DDDProject.Domain.Entities;
using DDDProject.Domain.Repositories;
using DDDProject.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DDDProject.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        // Check if any product exists with the given Sku value
        return await DbContext.Set<Product>()
            .AnyAsync(p => p.Sku == sku, cancellationToken);
    }

    // Implement other IProductRepository methods if any were added
} 