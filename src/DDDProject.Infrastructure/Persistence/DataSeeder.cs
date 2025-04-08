using DDDProject.Domain.Common; // Add this
using DDDProject.Domain.Common.Enums;
using DDDProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DDDProject.Infrastructure.Persistence;

public static class DataSeeder
{
    // Change logger type to ILogger<ApplicationDbContext>
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger<ApplicationDbContext> logger)
    {
        logger.LogInformation("Attempting to seed database...");

        // Scope the services
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        try
        {
            var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            // var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>(); // Needed if seeding users

            await SeedRolesAsync(roleManager, logger);
            await SeedPermissionsAsync(roleManager, logger);
            // await SeedAdminUserAsync(userManager, roleManager, logger); // Optional: Seed a default admin user

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
            // Consider throwing or handling the exception appropriately
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger<ApplicationDbContext> logger)
    {
        logger.LogInformation("Seeding roles...");
        string[] roleNames = { Roles.Admin, Roles.User }; // Roles comes from Domain.Common now

        foreach (var roleName in roleNames)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    // Log errors
                    logger.LogError("Failed to create role '{RoleName}'. Errors: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role '{RoleName}' already exists.", roleName);
            }
        }
    }

    private static async Task SeedPermissionsAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger<ApplicationDbContext> logger)
    {
        logger.LogInformation("Seeding permissions for roles...");

        var adminRole = await roleManager.FindByNameAsync(Roles.Admin);
        var userRole = await roleManager.FindByNameAsync(Roles.User);

        if (adminRole == null || userRole == null)
        {
            logger.LogError("Admin or User role not found. Cannot seed permissions.");
            return;
        }

        // Get all permissions from the enum
        var allPermissions = Enum.GetValues(typeof(Permission)).Cast<Permission>();

        // Assign all permissions to Admin role
        await AssignPermissionsToRoleAsync(roleManager, adminRole, allPermissions, logger);

        // Assign specific permissions to User role
        var userPermissions = new[]
        {
            Permission.ViewUsers,
            Permission.ViewFeatureX,
            Permission.ViewReportB
            // Add other permissions for the User role here
        };
        await AssignPermissionsToRoleAsync(roleManager, userRole, userPermissions, logger);
    }

    private static async Task AssignPermissionsToRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        IdentityRole<Guid> role,
        IEnumerable<Permission> permissions,
        ILogger<ApplicationDbContext> logger)
    {
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var currentPermissionClaims = currentClaims
            .Where(c => c.Type == PermissionClaimTypes.Permission) // PermissionClaimTypes from Domain.Common
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in permissions)
        {
            var permissionName = permission.ToString();
            if (!currentPermissionClaims.Contains(permissionName))
            {
                // Use correct Claim constructor
                var claim = new Claim(PermissionClaimTypes.Permission, permissionName);
                var result = await roleManager.AddClaimAsync(role, claim);
                if (result.Succeeded)
                {
                    logger.LogInformation("Assigned permission '{PermissionName}' to role '{RoleName}'.", permissionName, role.Name);
                }
                else
                {
                    logger.LogError("Failed to assign permission '{PermissionName}' to role '{RoleName}'. Errors: {Errors}",
                        permissionName, role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    // Optional: Method to seed a default admin user
    // private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    // {
    //    // Implementation to create a default admin user and assign the Admin role
    // }
}

// REMOVE Roles class definition from here
// public static class Roles
// {
//     public const string Admin = "Admin";
//     public const string User = "User";
// } 