using DDDProject.Domain.Abstractions;

namespace DDDProject.Domain.Common;

/// <summary>
/// Base class for entities.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IEntity<TId>
    where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity(TId id)
    {
        // Ensure Id is not default value for value types, or null for reference types
        if (EqualityComparer<TId>.Default.Equals(id, default))
        {
            throw new ArgumentException("The entity identifier cannot be the default value.", nameof(id));
        }
        Id = id;
    }

    // EF Core requires a parameterless constructor for proxies
    // It can be protected or private
    protected Entity()
    {
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears the domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    // Optional: Override Equals and GetHashCode for entity comparison based on Id
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType() || obj is not Entity<TId> other)
        {
            return false;
        }

        // If Id is the default value, reference equality is the only option
        if (EqualityComparer<TId>.Default.Equals(Id, default) || EqualityComparer<TId>.Default.Equals(other.Id, default))
        {
            return ReferenceEquals(this, other);
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        // If Id is default, use the base hash code (based on reference)
        if (EqualityComparer<TId>.Default.Equals(Id, default))
        {
            return base.GetHashCode();
        }
        return Id.GetHashCode() * 41; // Use a prime number
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
} 