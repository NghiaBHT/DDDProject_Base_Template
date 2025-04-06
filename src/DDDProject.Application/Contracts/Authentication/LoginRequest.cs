using System.ComponentModel.DataAnnotations;

namespace DDDProject.Application.Contracts.Authentication;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
); 