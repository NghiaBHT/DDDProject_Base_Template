using DDDProject.Domain.Abstractions;

namespace DDDProject.Domain.Events;

/// <summary>
/// Domain event raised when a new product is created.
/// </summary>
/// <param name="ProductId">The ID of the created product.</param>
/// <param name="Sku">The SKU of the created product.</param>
public record ProductCreatedDomainEvent(Guid ProductId, string Sku) : IDomainEvent; 