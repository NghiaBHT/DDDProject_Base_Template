using Microsoft.EntityFrameworkCore;
using DDDProject.Infrastructure.Persistence; // Namespace for ApplicationDbContext
using System;
using MediatR; // Added for IPublisher
using Moq; // Added for Mock<IPublisher>

namespace DDDProject.Infrastructure.Tests;

public static class DbContextFactory
{
    /// <summary>
    /// Creates a new instance of ApplicationDbContext using the InMemory database provider.
    /// Each call creates a context connected to a unique in-memory database instance.
    /// </summary>
    /// <returns>A new ApplicationDbContext instance.</returns>
    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        // Configure options for InMemory database
        // Use a unique database name for each context instance to ensure test isolation
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock IPublisher (no behavior needed for repo tests)
        var mockPublisher = new Mock<IPublisher>();

        // Create and return the context instance, passing the options and mock publisher
        var context = new ApplicationDbContext(options, mockPublisher.Object);

        // Optional: Ensure the database is created (though InMemory usually handles this implicitly)
        // context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Destroys the in-memory database associated with the given context instance.
    /// Useful for cleanup after tests if needed, though separate database names per test
    /// often make explicit destruction unnecessary.
    /// </summary>
    /// <param name="context">The context instance whose database should be destroyed.</param>
    public static void Destroy(ApplicationDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
} 