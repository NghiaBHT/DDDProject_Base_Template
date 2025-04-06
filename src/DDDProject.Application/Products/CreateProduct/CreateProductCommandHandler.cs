using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Entities;
using DDDProject.Domain.Repositories;
using DDDProject.Domain.ValueObjects;
using MediatR;

namespace DDDProject.Application.Products.CreateProduct;

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Create Value Objects
        var price = new Money(request.Currency, request.Amount);
        var sku = Sku.Create(request.Sku);

        if (sku is null)
        {
            // Handle invalid SKU based on domain rules (e.g., throw exception)
            // This might also be caught by FluentValidation earlier
            throw new ArgumentException("Invalid SKU format or value.", nameof(request.Sku));
        }

        // Check for SKU uniqueness (Domain rule enforced by Application/Infrastructure)
        if (await _productRepository.SkuExistsAsync(sku, cancellationToken))
        {
            // Consider a specific exception type here
            throw new InvalidOperationException($"A product with SKU '{sku.Value}' already exists.");
        }

        // Create Product entity using factory
        var product = Product.Create(
            request.Name,
            request.Description,
            price,
            sku);

        // Add to repository
        await _productRepository.AddAsync(product, cancellationToken);

        // Save changes and dispatch events
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
} 