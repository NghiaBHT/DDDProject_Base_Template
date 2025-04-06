using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Contracts.Common;
using DDDProject.Application.Services.Authentication;
using DDDProject.Domain.Common; // For Roles & Result<T>
using DDDProject.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // For IdentityDbContext
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;
using Microsoft.AspNetCore.Http;
using DDDProject.Infrastructure.Persistence; // Need ApplicationDbContext
using MediatR;
using DDDProject.Application.UnitTests.Helpers; // Add using for Helpers
// using DDDProject.Domain.Common.Errors; // Not used here
using DDDProject.Application.Contracts.Authentication; // Needed for ConfirmEmailRequest

namespace DDDProject.Application.UnitTests.Services.Authentication;

public class AuthServiceTests : IAsyncLifetime, IDisposable
{
    // Keep Mocks owned by the test class for verification
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly Mock<IEmailSender> _mockEmailSender = new();
    private readonly Mock<ILogger<AuthService>> _mockLogger = new();
    private readonly Mock<IPublisher> _mockPublisher = new(); // Keep for potential verification, passed to factory

    private IServiceProvider _serviceProvider = default!;
    private IServiceScope _scope = default!;
    private ApplicationDbContext _dbContext = default!;
    private UserManager<ApplicationUser> _userManager = default!;
    private RoleManager<IdentityRole<Guid>> _roleManager = default!;
    private SignInManager<ApplicationUser> _signInManager = default!;
    private AuthService _authService = default!;

    // Use IAsyncLifetime for async setup/teardown compatible with xUnit
    public async Task InitializeAsync()
    {
        await InitializeTestScopeAsync();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    // Helper to initialize a fresh scope, provider, and seeded data for each test
    private async Task InitializeTestScopeAsync()
    {
        // Dispose previous scope if exists
        _scope?.Dispose();

        // Create ServiceProvider using the factory, passing our mocks
        _serviceProvider = TestServiceProviderFactory.CreateServiceProvider(
            _mockJwtTokenGenerator,
            _mockEmailSender,
            _mockPublisher,
            services =>
            {
                // Register the specific logger mock instance for AuthService
                services.AddSingleton(_mockLogger.Object);
            });

        // Create a new scope for the test
        _scope = _serviceProvider.CreateAsyncScope();

        // Resolve services from the scope's ServiceProvider
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        _signInManager = _scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        // Seed essential data (like roles)
        await SeedRequiredRolesAsync(_roleManager);

        // Resolve the service under test directly from the scope
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>() as AuthService ?? 
            throw new InvalidOperationException("Failed to resolve AuthService");

        // Ensure DB is created for InMemory provider
        await _dbContext.Database.EnsureCreatedAsync();
    }

    private async Task SeedRequiredRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
        }
        if (!await roleManager.RoleExistsAsync(Roles.User))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.User));
        }
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndAssignRole_WhenRequestIsValid()
    {
        // Arrange
        await InitializeTestScopeAsync(); // Ensure fresh scope and DB
        var request = new RegisterRequest("Test", "User", "test@example.com", "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var userInDb = await _userManager.FindByEmailAsync(request.Email);
        userInDb.Should().NotBeNull();
        userInDb!.FirstName.Should().Be(request.FirstName);
        userInDb.LastName.Should().Be(request.LastName);

        var roles = await _userManager.GetRolesAsync(userInDb);
        roles.Should().ContainSingle(r => r == Roles.User);

        // Verify email is sent, less specific about the body HTML
        _mockEmailSender.Verify(x =>
            x.SendEmailAsync(request.Email, "Confirm your email", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenUserCreationFails()
    {
        // Arrange
        await InitializeTestScopeAsync();
        // Seed a user with the same email to cause failure
        var existingUser = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com", EmailConfirmed = true };
        var identityResult = await _userManager.CreateAsync(existingUser, "Password123!");
        identityResult.Succeeded.Should().BeTrue(); // Pre-condition check

        var request = new RegisterRequest("Test", "User", "test@example.com", "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check if the Errors collection contains the expected Identity error message
        result.Errors.Should().Contain(e => e.Contains("is already taken")); // Example check for duplicate email/username
        _mockEmailSender.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValidAndEmailConfirmed()
    {
        // Arrange
        await InitializeTestScopeAsync();
        const string email = "test@example.com";
        const string password = "Password123!";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true }; // Ensure email is confirmed
        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, Roles.User);

        var request = new LoginRequest(email, password);
        var expectedToken = "valid_jwt_token"; // The token our mock generator returns
        _mockJwtTokenGenerator.SetReturnsDefault(Task.FromResult(expectedToken)); // Re-apply workaround

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Token.Should().Be(expectedToken);
        result.Value.UserId.Should().Be(user.Id.ToString());
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        await InitializeTestScopeAsync();
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check for the specific error string returned by AuthService
        result.Errors.Should().Contain("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsInvalid()
    {
        // Arrange
        await InitializeTestScopeAsync();
        const string email = "test@example.com";
        const string correctPassword = "Password123!";
        const string incorrectPassword = "WrongPassword!";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        await _userManager.CreateAsync(user, correctPassword);

        var request = new LoginRequest(email, incorrectPassword);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check for the specific error string returned by AuthService
        result.Errors.Should().Contain("Invalid credentials.");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        await InitializeTestScopeAsync();
        const string email = "test@example.com";
        const string password = "Password123!";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false }; // Start with unconfirmed email
        await _userManager.CreateAsync(user, password);
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); // Encode the token
        var request = new ConfirmEmailRequest(user.Id.ToString(), encodedToken); // Use encoded token in request

        // Act
        var result = await _authService.ConfirmEmailAsync(request); // Pass request object

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        await InitializeTestScopeAsync();
        var nonExistentUserId = Guid.NewGuid().ToString();
        var token = "some_token";
        var request = new ConfirmEmailRequest(nonExistentUserId, token); // Create request object

        // Act
        var result = await _authService.ConfirmEmailAsync(request); // Pass request object

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check for the specific error string returned by AuthService (doesn't reveal user existence)
        result.Errors.Should().Contain("Unable to confirm email.");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnFailure_WhenTokenIsInvalid()
    {
        // Arrange
        await InitializeTestScopeAsync();
        const string email = "test@example.com";
        const string password = "Password123!";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };
        await _userManager.CreateAsync(user, password);
        var invalidToken = "invalid_token";
        var request = new ConfirmEmailRequest(user.Id.ToString(), invalidToken); // Create request object

        // Act
        var result = await _authService.ConfirmEmailAsync(request); // Pass request object

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check for the specific error string when token decoding fails
        result.Errors.Should().Contain("Invalid confirmation code format."); // Updated expected error
        var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser!.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldReturnFailure_WhenCodeIsInvalidFormat() // Renamed test
    {
        // Arrange
        await InitializeTestScopeAsync();
        const string email = "test@example.com";
        const string password = "Password123!";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };
        await _userManager.CreateAsync(user, password);
        var invalidFormatToken = "this is definitely not base64 url encoded";
        var request = new ConfirmEmailRequest(user.Id.ToString(), invalidFormatToken); // Create request object

        // Act
        var result = await _authService.ConfirmEmailAsync(request); // Pass request object

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Check for the specific error string returned by AuthService
        result.Errors.Should().Contain("Invalid confirmation code format.");
        var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser!.EmailConfirmed.Should().BeFalse();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        // Clean up mocks if necessary, though usually handled by GC
        GC.SuppressFinalize(this);
    }
} 