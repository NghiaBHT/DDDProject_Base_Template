using Moq;
using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Repositories;
using DDDProject.Domain.Entities; // Needed for Product, etc.
using DDDProject.Domain.ValueObjects; // Needed for Sku, Money, etc.
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace DDDProject.Application.UnitTests.TestHelpers
{
    public static class MockFactory
    {
        /// <summary>
        /// Creates a Moq mock for IUnitOfWork with SaveChangesAsync setup.
        /// </summary>
        /// <returns>A Mock<IUnitOfWork>.</returns>
        public static Mock<IUnitOfWork> GetMockUnitOfWork()
        {
            var mock = new Mock<IUnitOfWork>();

            // Default setup: SaveChangesAsync completes successfully and returns 1 (representing one change).
            mock.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return mock;
        }

        /// <summary>
        /// Creates a Moq mock for IProductRepository.
        /// Add common setups here or create specialized versions.
        /// </summary>
        /// <returns>A Mock<IProductRepository>.</returns>
        public static Mock<IProductRepository> GetMockProductRepository()
        {
            var mock = new Mock<IProductRepository>();

            // --- Examples of common setups (Uncomment and adapt as needed) ---

            // // Setup GetByIdAsync to return null by default
            // mock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            //     .ReturnsAsync((Product?)null);

            // // Setup SkuExistsAsync to return false by default
            // mock.Setup(repo => repo.SkuExistsAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            //     .ReturnsAsync(false);

            // // Setup AddAsync to do nothing by default (can add verification later)
            // mock.Setup(repo => repo.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            //     .Returns(Task.CompletedTask);

            // TODO: Add Setup for methods returning simulated data (e.g., GetAllAsync)
            // Example using a hypothetical TestDataBuilder:
            // var sampleProducts = TestDataBuilder.GetSampleProducts(count: 5);
            // mock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            //     .ReturnsAsync(sampleProducts);

            return mock;
        }

        // --- Data Simulation / Test Data Builders ---

        /// <summary>
        /// Generates a list of sample Product entities for testing.
        /// </summary>
        /// <param name="count">The number of products to generate.</param>
        /// <returns>A list of sample Product entities.</returns>
        public static List<Product> GetSampleProducts(int count = 3)
        {
            var products = new List<Product>();
            for (int i = 1; i <= count; i++)
            {
                // Attempt to create Sku, handle potential null (though validation might prevent this)
                var sku = Sku.Create($"SAMPLE{i:000}");
                if (sku is null)
                {
                    // Skip or throw if a valid SKU cannot be created for test data
                    continue; // Or throw new InvalidOperationException(...)
                }

                // Create Product
                var product = Product.Create(
                    $"Sample Product {i}",
                    $"Description for product {i}",
                    new Money("USD", 10.0m * i),
                    sku
                );

                // Note: Product IDs are typically generated upon creation or saving.
                // If your Product.Create doesn't assign an ID, the ID might be Guid.Empty initially.
                // Mocks needing specific IDs might require additional setup.

                products.Add(product);
            }
            return products;
        }

        // Add more factory methods for other common mocks (ICategoryRepository, IOrderRepository, etc.)
    }
} 