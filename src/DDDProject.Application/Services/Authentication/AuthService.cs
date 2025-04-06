using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Contracts.Common;
using DDDProject.Domain.Common; // For Roles
using DDDProject.Domain.Entities;
using DDDProject.Application.Services.Authentication; // Use local IJwtTokenGenerator
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // For IEmailSender
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace DDDProject.Application.Services.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailSender emailSender,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Attempting login for user {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Email} not found.", request.Email);
            return Result<LoginResponse>.Failure("Invalid credentials.");
        }

        // Check email confirmation if required
        if (_userManager.Options.SignIn.RequireConfirmedEmail &&
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {Email}.", request.Email);
            // Consider returning a specific error or status code for this
            return Result<LoginResponse>.Failure("Email not confirmed.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            string errorReason = "Invalid credentials.";
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed: User {Email} locked out.", request.Email);
                errorReason = "Account locked out.";
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login failed: User {Email} not allowed to sign in (e.g., email not confirmed).", request.Email);
                // Might be redundant if RequireConfirmedEmail check above is active
                errorReason = "Login not allowed (e.g., email not confirmed)."; 
            }
            else
            {
                 _logger.LogWarning("Login failed: Invalid password attempt for user {Email}.", request.Email);
            }
            return Result<LoginResponse>.Failure(errorReason);
        }

        // Login succeeded, generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = await _jwtTokenGenerator.GenerateTokenAsync(user, roles); // Now async

        _logger.LogInformation("Login successful for user {Email}", request.Email);

        var response = new LoginResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            token
        );

        return Result<LoginResponse>.Success(response);
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting registration for user {Email}", request.Email);

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
            return Result<Guid>.Failure(errors);
        }

        _logger.LogInformation("User {Email} created successfully.", request.Email);

        // Assign default role
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
             _logger.LogWarning("Failed to assign default role 'User' to {Email}: {Errors}", request.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
             // Decide if registration should fail if role assignment fails
             // For now, let's treat it as a partial success but log the warning
        }
        else
        {
             _logger.LogInformation("Assigned default role 'User' to {Email}.", request.Email);
        }

        // Send confirmation email (if required)
        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
             await SendConfirmationEmailAsync(user);
        }

        return Result<Guid>.Success(user.Id);
    }

     public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        _logger.LogInformation("Attempting email confirmation for user ID {UserId}", request.UserId);

        if (!Guid.TryParse(request.UserId, out var userIdGuid))
        {
             _logger.LogWarning("ConfirmEmail failed: Invalid UserId format {UserId}", request.UserId);
             return Result.Failure("Invalid user ID format.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("ConfirmEmail failed: User not found for ID {UserId}", request.UserId);
            // Don't reveal that the user doesn't exist
            return Result.Failure("Unable to confirm email."); 
        }

        try
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

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
            _logger.LogWarning(ex, "ConfirmEmail failed: Invalid Base64 code format for user ID {UserId}", request.UserId);
            return Result.Failure("Invalid confirmation code format.");
        }
    }

    // Helper to send confirmation email
    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        try
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            
            // TODO: Construct the actual callback URL based on your UI/API structure
            // This URL should point to an endpoint/page that calls ConfirmEmailAsync
            // For an API-only approach, this might be a frontend URL with params
            var callbackUrl = $"http://localhost:YOUR_FRONTEND_PORT/confirm-email?userId={userId}&code={code}"; // Placeholder

            _logger.LogInformation("Generated confirmation code for {Email}: {Code} - Callback: {CallbackUrl}", user.Email, code, callbackUrl);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

             _logger.LogInformation("Confirmation email sent successfully to {Email}.", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email to {Email}.", user.Email);
            // Decide how to handle this - maybe log and continue registration?
        }
    }
} 