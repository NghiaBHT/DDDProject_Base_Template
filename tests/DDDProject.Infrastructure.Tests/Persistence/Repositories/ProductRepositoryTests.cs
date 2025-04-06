using DDDProject.Domain.Entities;
using DDDProject.Domain.ValueObjects;
using DDDProject.Infrastructure.Persistence;
using DDDProject.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;

namespace DDDProject.Infrastructure.Tests.Persistence.Repositories;

public class ProductRepositoryTests
{
    // Helper method to create a valid Sku for tests
    private Sku CreateValidSku(string value = "TESTSKU123")
    {
        var sku = Sku.Create(value);
        if (sku == null)
            throw new InvalidOperationException("Could not create valid Sku for test");
        return sku;
    }

    [Fact]
    public async Task AddAsync_ShouldAddProductToDatabase()
    {
        // Arrange
        // Get a context instance using the factory
        await using var context = DbContextFactory.CreateInMemoryDbContext();

        // Instantiate the concrete repository with the context
        var productRepository = new ProductRepository(context);

        // Create a new product entity
        var newProduct = Product.Create(
            "Test Product",
            "Test Description",
            new Money("USD", 19.99m),
            CreateValidSku("ADDTEST001")
        );

        // Act
        // Add the product using the repository method
        await productRepository.AddAsync(newProduct);

        // Save changes using the context (simulating Unit of Work SaveChanges)
        await context.SaveChangesAsync();

        // Assert
        // Verify the product exists in the context's DbSet after saving
        var productInDb = await context.Products.FirstOrDefaultAsync(p => p.Id == newProduct.Id);

        productInDb.Should().NotBeNull();
        productInDb.Should().BeEquivalentTo(newProduct, options => options
            .Excluding(p => p.DomainEvents)); // Exclude DomainEvents from comparison
    }

    // TODO: Add more tests for other repository methods (GetByIdAsync, SkuExistsAsync, Update, Delete, etc.)
} 