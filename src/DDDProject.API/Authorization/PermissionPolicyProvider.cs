using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using DDDProject.Domain.Common;

namespace DDDProject.API.Authorization;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        // DefaultAuthorizationPolicyProvider is internal; we create a new instance.
        // It handles policies like [Authorize] without arguments.
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

    // Dynamically create policies based on the permission name
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if the policy name follows the convention "permission:..."
        if (policyName.StartsWith(PermissionClaimTypes.Permission, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            
            // Extract just the permission name (remove the "permission:" prefix)
            string permissionName = policyName.Substring(PermissionClaimTypes.Permission.Length + 1);
            
            // Add requirement with the permission name
            policy.AddRequirements(new PermissionRequirement(permissionName));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        // If the policy name doesn't match our convention, fall back to the default provider.
        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
} 