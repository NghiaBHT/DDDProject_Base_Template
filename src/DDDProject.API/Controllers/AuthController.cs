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

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow access to login/register without prior authentication
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthController(ISender mediator)
    {
        _mediator = mediator;
    }

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