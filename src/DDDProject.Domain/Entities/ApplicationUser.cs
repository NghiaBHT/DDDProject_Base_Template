using Microsoft.AspNetCore.Identity;
using System; // Add this for Guid

namespace DDDProject.Domain.Entities;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser<Guid>
{
    // Add custom properties here
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
} 