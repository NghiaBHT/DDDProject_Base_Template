using DDDProject.Domain.Common; // For Result
using MediatR;

namespace DDDProject.Application.Features.Authentication.Commands.ConfirmEmail;

// Command: Represents the intention to confirm a user's email
public record ConfirmEmailCommand(
    string UserId,
    string Code) : IRequest<Result>; // Returns Result (no value needed on success) 