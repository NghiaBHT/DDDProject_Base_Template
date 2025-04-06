using DDDProject.Application.Contracts.Authentication;
using DDDProject.Domain.Common; // For Result
using MediatR;
using System;

namespace DDDProject.Application.Features.Authentication.Commands.Register;

// Command: Represents the intention to register a user
public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<Result<Guid>>; // Returns Result<Guid> 