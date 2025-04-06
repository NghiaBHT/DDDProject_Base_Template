namespace DDDProject.Domain.Common;

/// <summary>
/// Defines properties for auditable entities.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedOnUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedOnUtc { get; set; }
    string? ModifiedBy { get; set; }
} 