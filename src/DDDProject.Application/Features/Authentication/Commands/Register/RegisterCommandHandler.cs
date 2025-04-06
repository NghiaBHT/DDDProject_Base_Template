using DDDProject.Domain.Common; // For Roles and Result
using DDDProject.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // For IEmailSender
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace DDDProject.Application.Features.Authentication.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender; // Assuming email confirmation is needed
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<RegisterCommandHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling registration command for {Email}", request.Email);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));
            return Result.Failure<Guid>(errors);
        }

         _logger.LogInformation("User {Email} created successfully. Assigning role and sending confirmation email if needed.", request.Email);

        // Assign default role
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
             _logger.LogWarning("Failed to assign default role 'User' to {Email}: {Errors}", request.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
             // Consider if this should cause the entire registration to fail
        }
        else
        {
            _logger.LogInformation("Assigned default role 'User' to {Email}.", request.Email);
        }


        // Send confirmation email (if required by Identity options)
        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            await SendConfirmationEmailAsync(user);
        }


        return Result<Guid>.Success(user.Id);
    }

    // Extracted helper method from AuthService - could be moved to a shared service if reused elsewhere
    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
         try
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // TODO: IMPORTANT! Replace with your actual frontend confirmation URL structure
            // The URL must point to a page/route capable of calling the ConfirmEmail endpoint/handler
            var callbackUrl = $"http://localhost:YOUR_FRONTEND_PORT/confirm-email?userId={userId}&code={code}"; // Placeholder

            _logger.LogInformation("Generated confirmation code for {Email}. Callback URL: {CallbackUrl}", user.Email, callbackUrl); // Don't log the code itself in production

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

             _logger.LogInformation("Confirmation email sent successfully to {Email}.", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email to {Email}.", user.Email);
            // Decide how to handle this - log and continue, or maybe wrap the Handle method in a transaction?
        }
    }
} 