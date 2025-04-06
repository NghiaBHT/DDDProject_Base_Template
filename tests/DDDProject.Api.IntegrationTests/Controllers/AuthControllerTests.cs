using DDDProject.API.Controllers;
using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Contracts.Common;
using DDDProject.Application.Services.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DDDProject.Api.IntegrationTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        // If AuthController takes ILogger, mock it here too
        _controller = new AuthController(_mockAuthService.Object); // Add mockLogger.Object if needed
    }

    // --- Register Tests ---

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturnOkWithUserId()
    {
        // Arrange
        var request = new RegisterRequest("Test", "User", "test@example.com", "Pass123!");
        var expectedUserId = Guid.NewGuid();
        var successResult = Result<Guid>.Success(expectedUserId);

        _mockAuthService.Setup(s => s.RegisterAsync(request)).ReturnsAsync(successResult);

        // Act
        var actionResult = await _controller.Register(request);

        // Assert
        // Expect CreatedAtAction (HTTP 201) on successful registration
        actionResult.Should().BeOfType<CreatedAtActionResult>(); 
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult?.StatusCode.Should().Be((int)System.Net.HttpStatusCode.Created);
        createdResult?.Value.Should().Be(expectedUserId);
        _mockAuthService.Verify(s => s.RegisterAsync(request), Times.Once);
    }

    [Fact]
    public async Task Register_WhenServiceReturnsFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("Test", "Fail", "fail@example.com", "Pass123!");
        var errorMessages = new List<string> { "Email already exists" };
        var failureResult = Result<Guid>.Failure(errorMessages);

        _mockAuthService.Setup(s => s.RegisterAsync(request)).ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Register(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        // Check if errors are returned within an anonymous object
        badRequestResult?.Value.Should().BeEquivalentTo(new { Errors = errorMessages }); 
        _mockAuthService.Verify(s => s.RegisterAsync(request), Times.Once);
    }

    // --- Login Tests ---

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithLoginResponse()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Pass123!");
        var loginResponse = new LoginResponse(Guid.NewGuid(), request.Email, "Test", "User", "jwt.token.here");
        var successResult = Result<LoginResponse>.Success(loginResponse);

        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(successResult);

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult?.Value.Should().Be(loginResponse);
        _mockAuthService.Verify(s => s.LoginAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("wrong@example.com", "wrongpass");
        var errorMessages = new List<string> { "Invalid credentials." };
        var failureResult = Result<LoginResponse>.Failure(errorMessages);

        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(failureResult);

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        // Expect Unauthorized (HTTP 401) for failed login attempts
        actionResult.Should().BeOfType<UnauthorizedObjectResult>(); 
        var unauthorizedResult = actionResult as UnauthorizedObjectResult;
        // Check if errors are returned within an anonymous object
        unauthorizedResult?.Value.Should().BeEquivalentTo(new { Errors = errorMessages }); 
        _mockAuthService.Verify(s => s.LoginAsync(request), Times.Once);
    }

    // TODO: Add tests for ConfirmEmail action
} 