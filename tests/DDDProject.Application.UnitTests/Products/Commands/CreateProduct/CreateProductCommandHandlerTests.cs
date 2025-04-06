using Moq;
using FluentAssertions;
using Xunit;
using DDDProject.Application.Products.CreateProduct;
using DDDProject.Domain.Abstractions; // For IUnitOfWork
using DDDProject.Domain.Repositories; // For IProductRepository
using DDDProject.Domain.Entities; // For Product
using DDDProject.Domain.ValueObjects; // For Sku, Money
using System;
using System.Threading;
using System.Threading.Tasks;
using DDDProject.Application.UnitTests.TestHelpers; // Added using for MockFactory
using TestHelpers = DDDProject.Application.UnitTests.TestHelpers; // Alias to resolve ambiguity

namespace DDDProject.Application.UnitTests.Products.Commands.CreateProduct;

public class CreateProductCommandHandlerTests
{
    // Use the factory methods for mock creation
    private readonly Mock<IProductRepository> _productRepositoryMock = TestHelpers.MockFactory.GetMockProductRepository();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = TestHelpers.MockFactory.GetMockUnitOfWork();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        // Arrange: Common setup for all tests in this class
        // Mocks are now initialized directly using the factory
        _handler = new CreateProductCommandHandler(_productRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnProductId_WhenSkuIsUnique()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product", "Test Description", 99.99m, "USD", "TESTSKU001");
        var cancellationToken = CancellationToken.None;

        // Setup mock for SkuExistsAsync to return false (SKU is unique)
        // This might be the default setup in MockFactory, but explicit setup here is fine too.
        _productRepositoryMock
            .Setup(repo => repo.SkuExistsAsync(It.Is<Sku>(s => s.Value == command.Sku), cancellationToken))
            .ReturnsAsync(false);

        // Setup AddAsync to capture the added product (optional, but good practice)
        Product? addedProduct = null;
        _productRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Callback<Product, CancellationToken>((product, ct) => addedProduct = product)
            .Returns(Task.CompletedTask); // Ensure AddAsync setup returns a Task

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        // 1. Verify AddAsync was called exactly once with a Product whose properties match the command
        _productRepositoryMock.Verify(
            repo => repo.AddAsync(It.Is<Product>(p =>
                p.Name == command.Name &&
                p.Description == command.Description &&
                p.Price.Currency == command.Currency &&
                p.Price.Amount == command.Amount &&
                p.Sku.Value == command.Sku),
            cancellationToken),
            Times.Once);

        // 2. Verify SaveChangesAsync was called exactly once (using the mock from the factory)
        _unitOfWorkMock.Verify(
            uow => uow.SaveChangesAsync(cancellationToken),
            Times.Once);

        // 3. Verify the returned Guid is not empty
        result.Should().NotBe(Guid.Empty);

        // 4. (Optional) Verify the Guid matches the Id of the product passed to AddAsync
        if (addedProduct != null)
        {
            result.Should().Be(addedProduct.Id);
        }
    }

    // TODO: Add more test cases:
    // - Handle_Should_ThrowInvalidOperationException_WhenSkuIsNotUnique
    // - Handle_Should_ThrowArgumentException_WhenSkuIsInvalid (if Sku.Create can return null or throw)
} 