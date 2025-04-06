using System;

namespace DDDProject.Application.Contracts.Authentication;

public record LoginResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Token // The JWT
); 