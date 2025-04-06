using DDDProject.Domain.Common; // For Result
using DDDProject.Domain.Entities; // For ApplicationUser
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DDDProject.Application.Features.Authentication.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling email confirmation command for user ID {UserId}", request.UserId);

        // Note: UserId in the command is string, matching Identity's default FindByIdAsync
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {            
            _logger.LogWarning("ConfirmEmail failed: User not found for ID {UserId}", request.UserId);
            // Avoid revealing user existence details
            return Result.Failure("Unable to confirm email.");
        }

        try
        {
            // Decode the code
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user {Email}", user.Email);
                return Result.Success();
            }
            else
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Email confirmation failed for user {Email}: {Errors}", user.Email, string.Join(", ", errors));
                return Result.Failure(errors);
            }
        }
        catch (FormatException ex)
        {
             _logger.LogWarning(ex, "ConfirmEmail failed: Invalid Base64 code format for user ID {UserId}. Code: {Code}", request.UserId, request.Code);
             return Result.Failure("Invalid confirmation code format.");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "An unexpected error occurred during email confirmation for user ID {UserId}", request.UserId);
             return Result.Failure("An unexpected error occurred during email confirmation.");
        }
    }
} 