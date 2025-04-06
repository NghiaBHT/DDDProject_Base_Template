using System.Linq.Expressions;

namespace DDDProject.Domain.Abstractions;

/// <summary>
/// Generic repository interface for domain entities.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId> // Constrain to reference types implementing IEntity
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity); // Update is often synchronous in EF Core
    void Remove(TEntity entity); // Remove is often synchronous in EF Core
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
} 