using DDDProject.Application.Features.Authentication.Commands.Register;
using DDDProject.Domain.Common; // For Roles, Result
using DDDProject.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options;

namespace DDDProject.Application.UnitTests.Features.Authentication.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<RegisterCommandHandler>> _mockLogger;
    private readonly RegisterCommandHandler _handler;

    // Helper to create Mock<UserManager<TUser>>
    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        // Add other required store interfaces if needed by the methods being tested
        // For example, if FindByEmailAsync is used, mock IUserEmailStore<TUser>
        var emailStore = store.As<IUserEmailStore<TUser>>();

        var options = new Mock<IOptions<IdentityOptions>>();
        var identityOptions = new IdentityOptions(); // Basic options
        // Configure options as needed for the test (e.g., SignIn.RequireConfirmedAccount)
        identityOptions.SignIn.RequireConfirmedAccount = true; // Assume email confirmation is on for testing SendEmail
        options.Setup(o => o.Value).Returns(identityOptions);

        var passwordHasher = new Mock<IPasswordHasher<TUser>>();
        var userValidators = new List<IUserValidator<TUser>>();
        var passwordValidators = new List<IPasswordValidator<TUser>>();

        var userManager = new Mock<UserManager<TUser>>(
            store.Object,
            options.Object, // Use mocked options
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<TUser>>>().Object);

        // Default setups for common methods (can be overridden in tests)
        userManager.Setup(u => u.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.AddToRoleAsync(It.IsAny<TUser>(), It.IsAny<string>()))
                   .ReturnsAsync(IdentityResult.Success);
         userManager.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<TUser>()))
                   .ReturnsAsync("valid_token"); 
         userManager.Setup(u => u.GetUserIdAsync(It.IsAny<TUser>()))
                   .ReturnsAsync((TUser user) => (user as ApplicationUser)?.Id.ToString() ?? Guid.NewGuid().ToString()); // Simple mock ID generation

        return userManager;
    }

    public RegisterCommandHandlerTests()
    {
        _mockUserManager = MockUserManager<ApplicationUser>();
        _mockEmailSender = new Mock<IEmailSender>();
        _mockLogger = new Mock<ILogger<RegisterCommandHandler>>();

        _handler = new RegisterCommandHandler(
            _mockUserManager.Object,
            _mockEmailSender.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessAndUserId_WhenRegistrationSucceeds()
    {
        // Arrange
        var command = new RegisterCommand("Test", "User", "test@example.com", "Password123!");
        var expectedUserId = Guid.NewGuid();
        var createdUser = new ApplicationUser { Id = expectedUserId }; // Capture created user

        // Setup UserManager CreateAsync to capture the user and return success
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .Callback<ApplicationUser, string>((user, pwd) => 
            {
                // Assign the expected ID to the user instance created within the handler
                user.Id = expectedUserId; 
            })
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();
        
        _mockUserManager.Setup(u => u.GetUserIdAsync(It.Is<ApplicationUser>(u => u.Id == expectedUserId)))
            .ReturnsAsync(expectedUserId.ToString()); // Ensure GetUserId returns the correct ID for the created user
        
        // Setup AddToRoleAsync to return success
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Id == expectedUserId), Roles.User))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUserId);

        _mockUserManager.Verify(); // Verify CreateAsync and AddToRoleAsync were called
        _mockEmailSender.Verify(x =>
            x.SendEmailAsync(command.Email, "Confirm your email", It.IsAny<string>()),
            Times.Once); // Verify email was sent (since RequireConfirmedAccount is true in mock setup)
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserManagerCreateFails()
    {
        // Arrange
        var command = new RegisterCommand("Test", "User", "test@example.com", "Password123!");
        var identityErrors = new List<IdentityError> { new IdentityError { Code = "DuplicateUserName", Description = "Username already taken." } };

        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e == identityErrors.First().Description);
        _mockUserManager.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never); // Ensure role not added
        _mockEmailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never); // Ensure email not sent
    }

    [Fact]
    public async Task Handle_ShouldLogWarningAndStillSucceed_WhenAddToRoleFails()
    {
         // Arrange
        var command = new RegisterCommand("Test", "User", "rolefail@example.com", "Password123!");
        var expectedUserId = Guid.NewGuid();

         // Setup CreateAsync to succeed and capture user
         _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .Callback<ApplicationUser, string>((user, pwd) => user.Id = expectedUserId)
            .ReturnsAsync(IdentityResult.Success);
        
        _mockUserManager.Setup(u => u.GetUserIdAsync(It.Is<ApplicationUser>(u => u.Id == expectedUserId)))
            .ReturnsAsync(expectedUserId.ToString());

        // Setup AddToRoleAsync to FAIL
        var roleErrors = new List<IdentityError> { new IdentityError { Code = "RoleError", Description = "Failed to add role." } };
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Id == expectedUserId), Roles.User))
            .ReturnsAsync(IdentityResult.Failed(roleErrors.ToArray()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Registration itself should succeed
        result.Value.Should().Be(expectedUserId);
        
        // Verify logger warning for role assignment failure was called (implementation detail, optional)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning, 
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to assign default role")), 
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!
            ),
            Times.Once);
            
        _mockEmailSender.Verify(x =>
            x.SendEmailAsync(command.Email, "Confirm your email", It.IsAny<string>()),
            Times.Once); // Email should still be sent
    }

     [Fact]
    public async Task Handle_ShouldNotSendConfirmationEmail_WhenRequireConfirmedAccountIsFalse()
    {
        // Arrange
        // Create a separate UserManager mock with RequireConfirmedAccount = false
        var mockUserManagerNoConfirm = MockUserManager<ApplicationUser>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var identityOptions = new IdentityOptions { SignIn = { RequireConfirmedAccount = false } }; // Override option
        options.Setup(o => o.Value).Returns(identityOptions);
        // Recreate UserManager mock with updated options (simplistic way shown here)
         var store = new Mock<IUserStore<ApplicationUser>>();
         mockUserManagerNoConfirm = new Mock<UserManager<ApplicationUser>>(
            store.Object, options.Object, new Mock<IPasswordHasher<ApplicationUser>>().Object, null, null, 
            new Mock<ILookupNormalizer>().Object, new Mock<IdentityErrorDescriber>().Object, new Mock<IServiceProvider>().Object, 
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object);
         mockUserManagerNoConfirm.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
         mockUserManagerNoConfirm.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        // Create handler with this specific UserManager mock
        var handler = new RegisterCommandHandler(
            mockUserManagerNoConfirm.Object,
            _mockEmailSender.Object,
            _mockLogger.Object);
            
        var command = new RegisterCommand("Test", "User", "noemail@example.com", "Password123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEmailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never); // Verify email was NOT sent
    }
}
