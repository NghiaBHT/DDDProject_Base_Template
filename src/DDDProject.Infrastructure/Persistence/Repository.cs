using System.Linq.Expressions;
using DDDProject.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DDDProject.Infrastructure.Persistence;

/// <summary>
/// Generic repository implementation using EF Core.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = DbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        // Use FindAsync for primary key lookups
        return await DbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        // EF Core tracks changes, so just setting the state is enough if attached.
        // If detached, Attach and then set state.
        DbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        // Use AnyAsync for efficient existence check
        // Note: Requires primary key property name knowledge or a generic way to build the expression
        // This simple version fetches the entity, which is less efficient.
        // A more optimized version might look like:
        // return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
        // But this requires TEntity to have a publicly accessible Id property matching the name 'Id'.
        // The current IEntity<TId> only guarantees the getter.

        // Less efficient, but works with the current IEntity definition:
        return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }
} 