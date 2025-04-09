using DDDProject.Domain.Common;
using DDDProject.Domain.Common.Enums;
using DDDProject.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DDDProject.API.Controllers;

/// <summary>
/// Provides endpoints for managing user information.
/// Requires authentication for all operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userManager">ASP.NET Core Identity UserManager service.</param>
    /// <param name="roleManager">ASP.NET Core Identity RoleManager service.</param>
    public UserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Retrieves a list of all registered users along with their roles and permissions.
    /// </summary>
    /// <remarks>Requires the 'permission:ViewUsers' policy.</remarks>
    /// <returns>A list of user information objects.</returns>
    /// <response code="200">Returns the list of users.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have the required 'ViewUsers' permission.</response>
    [HttpGet]
    [Authorize(Policy = "permission:ViewUsers")] // Require ViewUsers permission
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<object>();

        foreach (var user in users)
        {
            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Get user permissions from role claims
            var roleClaims = new List<Claim>();
            foreach (var role in roles)
            {
                var identityRole = await _roleManager.FindByNameAsync(role);
                if (identityRole != null)
                {
                    var claims = await _roleManager.GetClaimsAsync(identityRole);
                    roleClaims.AddRange(claims);
                }
            }
            
            var permissions = roleClaims
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            userList.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnd,
                user.LockoutEnabled,
                user.AccessFailedCount,
                Roles = roles,
                Permissions = permissions
            });
        }

        return Ok(userList);
    }

    /// <summary>
    /// Retrieves the profile information for the currently authenticated user.
    /// </summary>
    /// <returns>The profile information of the current user, including roles and permissions.</returns>
    /// <response code="200">Returns the current user's information.</response>
    /// <response code="401">If the user is not authenticated or the token is invalid.</response>
    /// <response code="404">If the user associated with the token could not be found.</response>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Get the user ID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("User ID not found in token");
        }

        // Parse the user ID
        if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
        {
            return BadRequest("Invalid user ID format in token");
        }

        // Get the user from the database
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound("User not found");
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get user permissions from claims
        var permissions = User.Claims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .ToList();

        // Return user information
        var userInfo = new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.TwoFactorEnabled,
            Roles = roles,
            Permissions = permissions
        };

        return Ok(userInfo);
    }

    /// <summary>
    /// Retrieves the details of a specific user by their unique identifier.
    /// </summary>
    /// <param name="id">The GUID identifier of the user to retrieve.</param>
    /// <remarks>Requires the 'permission:ViewUsers' policy.</remarks>
    /// <returns>The detailed information of the specified user.</returns>
    /// <response code="200">Returns the specified user's information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have the required 'ViewUsers' permission.</response>
    /// <response code="404">If a user with the specified ID is not found.</response>
    [HttpGet("{id}")]
    [Authorize(Policy = "permission:ViewUsers")] // Require ViewUsers permission
    public async Task<IActionResult> GetUserById(Guid id)
    {
        // Get the user from the database
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound("User not found");
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        
        // Get user permissions from role claims
        var roleClaims = new List<Claim>();
        foreach (var role in roles)
        {
            var identityRole = await _roleManager.FindByNameAsync(role);
            if (identityRole != null)
            {
                var claims = await _roleManager.GetClaimsAsync(identityRole);
                roleClaims.AddRange(claims);
            }
        }
        
        var permissions = roleClaims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        // Return user information
        var userInfo = new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd,
            user.LockoutEnabled,
            user.AccessFailedCount,
            Roles = roles,
            Permissions = permissions
        };

        return Ok(userInfo);
    }
} 