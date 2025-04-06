using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Common;
using DDDProject.Domain.Events; // For ProductCreatedDomainEvent
using DDDProject.Domain.ValueObjects; // For Money and Sku

namespace DDDProject.Domain.Entities;

public class Product : AuditableEntity<Guid>, IAggregateRoot
{
    // Private constructor for EF Core and factory
    private Product(
        Guid id,
        string name,
        string description,
        Money price,
        Sku sku)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Sku = sku;
    }

    // Required parameterless constructor for EF Core proxies
    private Product() { }

    public string Name { get; private set; } // Can be set through methods if needed
    public string Description { get; private set; }
    public Money Price { get; private set; } // Value Object
    public Sku Sku { get; private set; }     // Value Object

    // Factory method for creation
    public static Product Create(
        string name,
        string description,
        Money price,
        Sku sku)
    {
        // Basic validation (more complex validation can be in domain services or command handlers)
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (price.Amount <= 0)
            throw new ArgumentException("Product price must be positive.", nameof(price));
        // Add more validation as needed

        var product = new Product(Guid.NewGuid(), name, description, price, sku);

        // Raise a domain event
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id, product.Sku.Value));

        return product;
    }

    // Example method to update price (encapsulates logic)
    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new ArgumentException("Product price must be positive.", nameof(newPrice));
        if (newPrice.Currency != Price.Currency)
            throw new InvalidOperationException("Cannot change currency of existing product price.");

        Price = newPrice;
        // Raise event if needed: RaiseDomainEvent(new ProductPriceChangedEvent(...));
    }
} 