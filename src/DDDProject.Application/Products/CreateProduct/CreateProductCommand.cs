using MediatR;

namespace DDDProject.Application.Products.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
/// <param name="Name">Product name.</param>
/// <param name="Description">Product description.</param>
/// <param name="Amount">Price amount.</param>
/// <param name="Currency">Price currency code (e.g., "USD").</param>
/// <param name="Sku">Stock Keeping Unit.</param>
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Amount,
    string Currency,
    string Sku
) : IRequest<Guid>; // Returns the Id of the created product 