using MediatR;

namespace DDDProject.Application.Products.GetProductById;

/// <summary>
/// Query to get a product by its unique identifier.
/// </summary>
/// <param name="ProductId">The ID of the product to retrieve.</param>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse?>; // Returns ProductResponse or null if not found 