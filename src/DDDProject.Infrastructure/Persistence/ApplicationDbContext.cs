using System.Reflection;
using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Common;
using DDDProject.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace DDDProject.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private IDbContextTransaction? _currentTransaction;

    // DbSet properties for Aggregate Roots
    public DbSet<Product> Products { get; set; }
    // Add other DbSets here e.g., public DbSet<Order> Orders { get; set; }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IPublisher publisher)
        : base(options)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations defined in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Example: Configure Value Objects (if using OwnsMany or similar)
        // modelBuilder.Entity<Order>().OwnsMany(o => o.OrderItems, oi => { ... });

        // Example: Configure soft delete query filter
        // modelBuilder.Entity<SomeEntity>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    // Override SaveChangesAsync to dispatch domain events and handle auditing
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    #region IUnitOfWork Implementation

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return; // Transaction already started
        }
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await (_currentTransaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    #endregion

    #region Private Methods

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            // Using a fixed user for now, replace with actual user context later
            const string currentUser = "System";
            var utcNow = DateTime.UtcNow;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = currentUser;
                    entry.Property(nameof(IAuditableEntity.CreatedOnUtc)).CurrentValue = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = currentUser;
                    entry.Property(nameof(IAuditableEntity.ModifiedOnUtc)).CurrentValue = utcNow;
                    break;
            }
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = ChangeTracker.Entries<Entity<Guid>>() // Assuming Guid IDs, adjust if needed
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList(); // Copy events before clearing
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }

    #endregion
} 