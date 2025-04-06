namespace DDDProject.Domain.Common;

/// <summary>
/// Base class for entities that require auditing information.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : IEquatable<TId>
{
    protected AuditableEntity(TId id) : base(id)
    {
    }

    protected AuditableEntity() // For EF Core
    {
    }

    public DateTime CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
    public string? ModifiedBy { get; set; }
} 