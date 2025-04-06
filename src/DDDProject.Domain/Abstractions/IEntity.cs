namespace DDDProject.Domain.Abstractions;

/// <summary>
/// Defines an entity with a unique identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; }
} 