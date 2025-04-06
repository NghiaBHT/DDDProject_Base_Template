using DDDProject.Application.Contracts.Authentication;
using DDDProject.Domain.Common; // For Result
using DDDProject.Domain.Entities;
using DDDProject.Application.Services.Authentication; // For IJwtTokenGenerator
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace DDDProject.Application.Features.Authentication.Queries.Login;

public class LoginQueryHandler : IRequestHandler<LoginQuery, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<LoginQueryHandler> _logger;

    public LoginQueryHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<LoginQueryHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
         _logger.LogInformation("Handling login query for {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Email} not found.", request.Email);
            return Result.Failure<LoginResponse>("Invalid credentials.");
        }

        // Check email confirmation if required
        if (_userManager.Options.SignIn.RequireConfirmedEmail &&
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {Email}.", request.Email);
            return Result.Failure<LoginResponse>("Email not confirmed.");
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
                errorReason = "Login not allowed (e.g., email not confirmed).";
            }
            else
            {
                 _logger.LogWarning("Login failed: Invalid password attempt for user {Email}.", request.Email);
            }
            return Result.Failure<LoginResponse>(errorReason);
        }

        // Login succeeded, generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = await _jwtTokenGenerator.GenerateTokenAsync(user, roles);

        _logger.LogInformation("Login successful for user {Email}", request.Email);

        var response = new LoginResponse(
            user.Id,
            user.Email!,
            user.FirstName!,
            user.LastName!,
            token
        );

        return Result<LoginResponse>.Success(response);
    }
} 