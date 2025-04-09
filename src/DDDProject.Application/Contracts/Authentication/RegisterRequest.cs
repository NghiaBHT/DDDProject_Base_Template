using System.ComponentModel.DataAnnotations;

namespace DDDProject.Application.Contracts.Authentication;

/// <summary>
/// RegisterRequest
/// </summary>
/// <param name="FirstName"></param>
/// <param name="LastName"></param>
/// <param name="Email"></param>
/// <param name="Password"></param>
public record RegisterRequest(
    [Required][StringLength(100)] string FirstName,
    [Required][StringLength(100)] string LastName,
    [Required][EmailAddress] string Email,
    [Required] string Password // Add ConfirmPassword if desired, validated via data annotation or FluentValidation
); 