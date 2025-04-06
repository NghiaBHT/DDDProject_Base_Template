using DDDProject.Application.Features.Authentication.Commands.ConfirmEmail;
using DDDProject.Domain.Common;
using DDDProject.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options; // For mocking UserManager options

namespace DDDProject.Application.UnitTests.Features.Authentication.Commands;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<ConfirmEmailCommandHandler>> _mockLogger;
    private readonly ConfirmEmailCommandHandler _handler;

    // Reusing MockUserManager helper from RegisterCommandHandlerTests structure
    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class, new()
    {
        var store = new Mock<IUserStore<TUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());
        var userManager = new Mock<UserManager<TUser>>(
            store.Object, options.Object, new Mock<IPasswordHasher<TUser>>().Object, 
            new List<IUserValidator<TUser>>(), new List<IPasswordValidator<TUser>>(), 
            new Mock<ILookupNormalizer>().Object, new Mock<IdentityErrorDescriber>().Object, 
            new Mock<IServiceProvider>().Object, new Mock<ILogger<UserManager<TUser>>>().Object);
        return userManager;
    }

    public ConfirmEmailCommandHandlerTests()
    {
        _mockUserManager = MockUserManager<ApplicationUser>();
        _mockLogger = new Mock<ILogger<ConfirmEmailCommandHandler>>();
        _handler = new ConfirmEmailCommandHandler(
            _mockUserManager.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserExistsAndCodeIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = Guid.Parse(userId), UserName = "test@test.com", Email = "test@test.com" };
        var rawToken = "valid-raw-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var command = new ConfirmEmailCommand(userId, encodedToken);

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.ConfirmEmailAsync(user, rawToken)) // Expecting decoded token
                        .ReturnsAsync(IdentityResult.Success)
                        .Verifiable();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockUserManager.Verify(); // Verify ConfirmEmailAsync was called
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("any-token"));
        var command = new ConfirmEmailCommand(userId, encodedToken);

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null); // User not found

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Unable to confirm email.");
        _mockUserManager.Verify(u => u.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never); // Ensure confirm not called
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCodeIsInvalidFormat()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = Guid.Parse(userId), UserName = "test@test.com", Email = "test@test.com" };
        var invalidEncodedToken = "this-is-not-base64url"; // Invalid format
        var command = new ConfirmEmailCommand(userId, invalidEncodedToken);

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Invalid confirmation code format.");
        _mockUserManager.Verify(u => u.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenConfirmEmailAsyncFails()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = Guid.Parse(userId), UserName = "test@test.com", Email = "test@test.com" };
        var rawToken = "invalid-token-for-user";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var command = new ConfirmEmailCommand(userId, encodedToken);
        var identityErrors = new List<IdentityError> { new IdentityError { Code = "InvalidToken", Description = "Invalid token." } };

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.ConfirmEmailAsync(user, rawToken))
                        .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray())); // Confirm fails

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(identityErrors.First().Description);
    }
} 