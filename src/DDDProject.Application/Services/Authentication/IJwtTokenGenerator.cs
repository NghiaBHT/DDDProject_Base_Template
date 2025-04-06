using DDDProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

// Move to Application layer
namespace DDDProject.Application.Services.Authentication; // Update namespace

public interface IJwtTokenGenerator
{
    Task<string> GenerateTokenAsync(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<Claim> additionalClaims = null);
} 