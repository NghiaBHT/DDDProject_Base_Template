namespace DDDProject.Application.Products.GetProductById;

/// <summary>
/// Data Transfer Object for returning product details.
/// </summary>
public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal PriceAmount,   // Flattened from Money VO
    string PriceCurrency, // Flattened from Money VO
    string Sku            // Flattened from Sku VO
); 