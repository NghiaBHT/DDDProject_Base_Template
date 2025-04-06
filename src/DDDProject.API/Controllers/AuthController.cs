using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Add for StatusCodes
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DDDProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow access to login/register without prior authentication
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)] // Return Guid
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), new { userId = result.Value } , result.Value) // Return UserId
            : BadRequest(new { Errors = result.Errors });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // If credentials invalid or email not confirmed
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        // Distinguish between bad credentials and other failures if needed
        if (!result.IsSuccess)
        {
             // Simple unauthorized for credential/confirmation issues
            return Unauthorized(new { Errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpPost("confirm-email")] // Or HttpGet if using query parameters
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
     public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request) // Or [FromQuery]
    {
        var result = await _authService.ConfirmEmailAsync(request);

        return result.IsSuccess
            ? Ok()
            : BadRequest(new { Errors = result.Errors });
    }

     // TODO: Add endpoints for ForgotPassword, ResetPassword if needed
} 