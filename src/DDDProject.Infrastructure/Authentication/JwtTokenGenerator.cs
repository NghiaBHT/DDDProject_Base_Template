using DDDProject.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using DDDProject.Infrastructure.Authentication; // Use local JwtSettings
using DDDProject.Domain.Common; // For PermissionClaimTypes & Roles
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using DDDProject.Application.Services.Authentication; // Use interface from Application

namespace DDDProject.Infrastructure.Authentication;

// REMOVE Interface definition from here
// // Update IJwtTokenGenerator interface to be async
// public interface IJwtTokenGenerator
// {
//     Task<string> GenerateTokenAsync(
//         ApplicationUser user,
//         IEnumerable<string> roles,
//         IEnumerable<Claim> additionalClaims = null);
// }

// Implements interface from Application layer
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager; // Inject RoleManager

    // Add RoleManager to constructor
    public JwtTokenGenerator(
        IOptions<JwtSettings> jwtOptions,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _jwtSettings = jwtOptions.Value;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Make GenerateToken async
    public async Task<string> GenerateTokenAsync(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<Claim> additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), // Alt standard claim for ID
            new(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty), // Standard claim for username
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims (using ClaimTypes.Role)
        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // Fetch and add permission claims associated with the user's roles
        var permissionClaims = await GetPermissionClaimsForUserRolesAsync(roles ?? Enumerable.Empty<string>());
        claims.AddRange(permissionClaims);

        // Add any other custom claims passed in
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.LifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Implement helper method to get permission claims
    private async Task<IEnumerable<Claim>> GetPermissionClaimsForUserRolesAsync(IEnumerable<string> roleNames)
    {
        var allRoleClaims = new List<Claim>();
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                allRoleClaims.AddRange(claims.Where(c => c.Type == PermissionClaimTypes.Permission));
            }
        }
        return allRoleClaims;
    }
} 