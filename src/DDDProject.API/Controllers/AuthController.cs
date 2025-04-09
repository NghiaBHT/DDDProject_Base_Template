using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Features.Authentication.Commands.ConfirmEmail;
using DDDProject.Application.Features.Authentication.Commands.Register;
using DDDProject.Application.Features.Authentication.Queries.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Add for StatusCodes
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DDDProject.API.Controllers;

/// <summary>
/// Handles authentication related operations like user registration, login, and email confirmation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow access to login/register without prior authentication
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance for sending commands and queries.</param>
    public AuthController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="request">The registration details containing first name, last name, email, and password.</param>
    /// <returns>The ID of the newly registered user upon successful registration.</returns>
    /// <response code="201">Returns the newly created user's ID.</response>
    /// <response code="400">If the request data is invalid (e.g., email already exists, password doesn't meet requirements).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)] // Return Guid
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), new { userId = result.Value }, result.Value)
            : BadRequest(new { Errors = result.Errors });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token upon successful login.
    /// </summary>
    /// <param name="request">The login credentials containing email and password.</param>
    /// <returns>An authentication response containing the JWT token and user details.</returns>
    /// <response code="200">Returns the authentication token and user info.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="401">If the credentials are invalid or the email is not confirmed.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // If credentials invalid or email not confirmed
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var query = new LoginQuery(request.Email, request.Password);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { Errors = result.Errors });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirms a user's email address using the provided user ID and confirmation code.
    /// </summary>
    /// <param name="request">The request containing the user ID and email confirmation code.</param>
    /// <returns>An OK result if the email is confirmed successfully.</returns>
    /// <response code="200">If the email confirmation is successful.</response>
    /// <response code="400">If the user ID or confirmation code is invalid or expired.</response>
    [HttpPost("confirm-email")] // Or HttpGet if using query parameters
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request) // Or [FromQuery]
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Code);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok()
            : BadRequest(new { Errors = result.Errors });
    }

    // TODO: Add endpoints for ForgotPassword, ResetPassword if needed
} 