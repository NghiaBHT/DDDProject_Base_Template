using MediatR;

namespace DDDProject.Domain.Abstractions;

/// <summary>
/// Marker interface for domain events. Inherits from INotification for MediatR.
/// </summary>
public interface IDomainEvent : INotification
{
} 