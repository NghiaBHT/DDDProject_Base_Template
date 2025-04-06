using DDDProject.Domain.Common.Enums;
using DDDProject.Domain.Entities;
using DDDProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace DDDProject.Api.IntegrationTests.Common;

// Note: Your API project (DDDProject.API) must have InternalsVisibleTo("DDDProject.Api.IntegrationTests") 
// in its .csproj file for the WebApplicationFactory<Program> to work correctly if Program is internal.
// Alternatively, if you have a public Startup class, use that instead of Program.

[Collection("IntegrationTests")] // Optional: Use collection fixture if needed for shared setup
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime // Implement IAsyncLifetime for async setup/teardown
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly HttpClient HttpClient;
    private IServiceScope _scope = null!;

    protected IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        HttpClient = Factory.CreateClient();
    }

    // Async initialization (creates DB schema)
    public virtual async Task InitializeAsync()
    {
        _scope = Factory.Services.CreateScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync(); // Ensure the DB schema is created

        // Seed default roles required by the application/tests
        await SeedRequiredRolesAsync();
    }

    // Async cleanup (dispose scope)
    public virtual Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    // Helper to get a fresh DbContext instance within the current scope
    protected ApplicationDbContext GetDbContext()
    {
        // Ensure scope is created if accessed before InitializeAsync (though unlikely with IAsyncLifetime)
        _scope ??= Factory.Services.CreateScope(); 
        return _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
    
    // --- Replicated/Adapted Seeding Helpers --- 
    // These helpers now use GetDbContext() to operate on the scoped context

    protected async Task<ApplicationUser> SeedUserAsync(
        string email = "testuser@example.com",
        string firstName = "Test",
        string lastName = "User",
        string? password = null)
    {
        var context = GetDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString("D")
        };

        if (!string.IsNullOrEmpty(password))
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            user.PasswordHash = hasher.HashPassword(user, password);
        }

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    protected async Task<IdentityRole<Guid>> SeedRoleAsync(string roleName)
    {
        var context = GetDbContext();
        var normalizedName = roleName.ToUpperInvariant();

        // Check if role already exists
        var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName);
        if (existingRole != null)
        {
            return existingRole; // Return existing role if found
        }

        // Role doesn't exist, create it
        var role = new IdentityRole<Guid>(roleName)
        {
            Id = Guid.NewGuid(),
            NormalizedName = normalizedName,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    protected async Task<IdentityRoleClaim<Guid>> SeedRoleClaimAsync(IdentityRole<Guid> role, Claim claim)
    {
        var context = GetDbContext();
        var roleClaim = new IdentityRoleClaim<Guid>
        {
            RoleId = role.Id,
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };
        context.RoleClaims.Add(roleClaim);
        await context.SaveChangesAsync();
        return roleClaim;
    }

    // Add a helper method specifically for seeding required roles
    private async Task SeedRequiredRolesAsync()
    {
        // Use Roles from Domain.Common
        await SeedRoleAsync(Domain.Common.Roles.Admin);
        await SeedRoleAsync(Domain.Common.Roles.User);
    }
} 