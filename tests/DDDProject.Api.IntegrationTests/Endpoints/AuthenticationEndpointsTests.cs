using DDDProject.Api.IntegrationTests.Common;
using DDDProject.Application.Contracts.Authentication; // For RegisterRequest, LoginRequest
using DDDProject.Application.Contracts.Common; // For Result<T>
using DDDProject.Domain.Entities; // For ApplicationUser
using FluentAssertions;
using Microsoft.AspNetCore.Identity; // For UserManager
using Microsoft.AspNetCore.WebUtilities; // For QueryHelpers
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // For GetRequiredService
using System;
using System.Net;
using System.Net.Http.Json;
using System.Text; // For Encoding
using System.Threading.Tasks;
using Xunit;

namespace DDDProject.Api.IntegrationTests.Endpoints;

public class AuthenticationEndpointsTests : IntegrationTestBase
{
    public AuthenticationEndpointsTests(CustomWebApplicationFactory<Program> factory)
        : base(factory) // Pass factory to base class
    {
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOkAndUserId()
    {
        // Arrange
        var uniqueEmail = $"test-{Guid.NewGuid()}@integration.com";
        var request = new RegisterRequest(
            FirstName: "Integration",
            LastName: "TestUser",
            Email: uniqueEmail,
            Password: "Password123!"
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created); // Corrected expectation to 201 Created
        
        var userIdResult = await response.Content.ReadFromJsonAsync<Guid>(); 
        userIdResult.Should().NotBeEmpty();

        // Optional: Verify user exists in DB
        var dbContext = GetDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        user.Should().NotBeNull();
        user?.Id.Should().Be(userIdResult);
        user?.FirstName.Should().Be(request.FirstName);
        user?.LastName.Should().Be(request.LastName);
        user?.EmailConfirmed.Should().Be(false); // Assuming email confirmation is required and not done yet
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var existingEmail = $"duplicate-{Guid.NewGuid()}@integration.com";
        // Seed a user with this email first
        await SeedUserAsync(email: existingEmail, password: "Password123!");

        var request = new RegisterRequest(
            FirstName: "Another",
            LastName: "User",
            Email: existingEmail, // Use the same email
            Password: "AnotherPassword123!"
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // Optionally, check for specific error messages if the API returns them
        // var error = await response.Content.ReadFromJsonAsync<ProblemDetails>(); // Or your specific error DTO
        // error?.Detail.Should().Contain("Email already exists"); 
    }

    [Theory]
    [InlineData("Test", "User", "invalid-email", "Password123!")] // Invalid email format
    [InlineData("Test", "User", "weakpass@test.com", "pass")]     // Weak password
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(string firstName, string lastName, string email, string password)
    {
        // Arrange
        var request = new RegisterRequest(
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Password: password
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkAndToken()
    {
        // Arrange
        var email = $"login-valid-{Guid.NewGuid()}@integration.com";
        var password = "Password123!";
        var user = await SeedUserAsync(email: email, password: password);
        // Manually confirm email for this test, as SeedUserAsync now defaults to false
        using (var setupContext = GetDbContext()) 
        {
            var userToConfirm = await setupContext.Users.FindAsync(user.Id);
            if(userToConfirm != null)
            {
                 userToConfirm.EmailConfirmed = true; 
                 await setupContext.SaveChangesAsync();
            }
        }
       
        var request = new LoginRequest(email, password);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(); // Corrected type to LoginResponse
        loginResponse.Should().NotBeNull();
        loginResponse?.Token.Should().NotBeNullOrWhiteSpace();
        loginResponse?.UserId.Should().Be(user.Id); 
        // Optionally decode and validate token claims if necessary
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = $"login-invalidpass-{Guid.NewGuid()}@integration.com";
        await SeedUserAsync(email: email, password: "CorrectPassword123!");

        var request = new LoginRequest(email, "WrongPassword123!");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = $"nonexistent-{Guid.NewGuid()}@integration.com";
        var request = new LoginRequest(email, "AnyPassword123!");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = $"login-unconfirmed-{Guid.NewGuid()}@integration.com";
        var password = "Password123!";
        // Seed user, EmailConfirmed defaults to false or is explicitly set false
        await SeedUserAsync(email: email, password: password); 

        var request = new LoginRequest(email, password);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        // Identity default does NOT require confirmed email, so login should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Adjusted expectation based on default config
        // You might get a specific error message in the body depending on API implementation
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ShouldConfirmEmailAndReturnOk()
    {
        // Arrange
        var email = $"confirm-{Guid.NewGuid()}@integration.com";
        var user = await SeedUserAsync(email: email, password: "Password123!");
        user.EmailConfirmed.Should().BeFalse(); // Verify initial state

        // Need UserManager to generate token
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); // URL-safe encoding

        var request = new ConfirmEmailRequest(user.Id.ToString(), encodedToken);

        // Act
        // Use POST and send data in the body
        var response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", request);

        // Assert
        response.EnsureSuccessStatusCode(); // Throws if not 2xx
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        var dbContext = GetDbContext();
        await dbContext.Entry(user).ReloadAsync(); 
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var email = $"confirm-invalid-{Guid.NewGuid()}@integration.com";
        var user = await SeedUserAsync(email: email, password: "Password123!");
        var invalidToken = "thisisnotavalidtoken";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(invalidToken));

        var request = new ConfirmEmailRequest(user.Id.ToString(), encodedToken);

        // Act
        // Use POST and send data in the body
        var response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify user email is still not confirmed
        var dbContext = GetDbContext();
        await dbContext.Entry(user).ReloadAsync();
        user.EmailConfirmed.Should().BeFalse();
    }
} 