using DDDProject.Application.Contracts.Authentication; // For LoginResponse
using DDDProject.Domain.Common; // For Result
using MediatR;

namespace DDDProject.Application.Features.Authentication.Queries.Login;

// Query: Represents the intention to log a user in
public record LoginQuery(
    string Email,
    string Password) : IRequest<Result<LoginResponse>>; // Returns Result<LoginResponse> 