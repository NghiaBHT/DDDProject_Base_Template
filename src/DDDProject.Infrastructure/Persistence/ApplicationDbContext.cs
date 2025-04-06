using System.Reflection;
using DDDProject.Domain.Abstractions;
using DDDProject.Domain.Common;
using DDDProject.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace DDDProject.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private IDbContextTransaction? _currentTransaction;

    // DbSet properties for Aggregate Roots
    // public DbSet<User> Users { get; set; }
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
        base.OnModelCreating(modelBuilder);

        // Apply all configurations defined in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Example: Configure Value Objects (if using OwnsMany or similar)
        // modelBuilder.Entity<Order>().OwnsMany(o => o.OrderItems, oi => { ... });

        // Example: Configure soft delete query filter
        // modelBuilder.Entity<SomeEntity>().HasQueryFilter(e => !e.IsDeleted);

        // Potentially customize Identity table names/schema here if needed
        // Example:
        // modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        // modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        // modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        // modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        // modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        // modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        // modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
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
        // Adjust the entry type if ApplicationUser is now the base entity with events,
        // or keep tracking your custom Entity<Guid> if ApplicationUser doesn't have events.
        // Assuming ApplicationUser doesn't directly use the DomainEvents pattern here.
        var entitiesWithEvents = ChangeTracker.Entries<Entity<Guid>>() // Track custom base Entity
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }

    #endregion
} 