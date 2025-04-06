namespace DDDProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a Product entity is not found.
/// </summary>
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(Guid productId)
        : base($"The product with the identifier {productId} was not found.")
    {
    }
} 