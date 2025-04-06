using DDDProject.Application.Services.Authentication;
using DDDProject.Domain.Entities;
using DDDProject.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.DataProtection;

namespace DDDProject.Application.UnitTests.Helpers;

/// <summary>
/// Factory for creating IServiceProvider instances configured for testing purposes,
/// including an in-memory DbContext and mocked external dependencies.
/// </summary>
public static class TestServiceProviderFactory
{
    /// <summary>
    /// Creates a new IServiceProvider with a unique in-memory database and common services/mocks.
    /// </summary>
    /// <param name="jwtMock">Optional pre-configured mock for IJwtTokenGenerator.</param>
    /// <param name="emailMock">Optional pre-configured mock for IEmailSender.</param>
    /// <param name="publisherMock">Optional pre-configured mock for IPublisher.</param>
    /// <param name="configureServices">Optional action to further configure services (e.g., add test-specific mocks or services).</param>
    /// <returns>A configured IServiceProvider.</returns>
    public static IServiceProvider CreateServiceProvider(
        Mock<IJwtTokenGenerator>? jwtMock = null,
        Mock<IEmailSender>? emailMock = null,
        Mock<IPublisher>? publisherMock = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString(); // Unique DB for each provider instance

        // Ensure mocks are created if not provided
        jwtMock ??= new Mock<IJwtTokenGenerator>();
        emailMock ??= new Mock<IEmailSender>();
        publisherMock ??= new Mock<IPublisher>();

        // Mock DataProtectionProvider
        var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
        var mockDataProtector = new Mock<IDataProtector>();

        mockDataProtectionProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(mockDataProtector.Object);

        // Setup mock protector to bypass actual protection/unprotection for tests
        mockDataProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns((byte[] data) => data); // Return original data
        mockDataProtector
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns((byte[] data) => data); // Return original data

        // Add DbContext with In-Memory provider
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        // Register mock DataProtectionProvider
        services.AddSingleton<IDataProtectionProvider>(mockDataProtectionProvider.Object);

        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            // Default options suitable for testing
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add Mocks (use provided instances or the newly created ones)
        services.AddSingleton(jwtMock.Object); // Register the mocked object instance
        services.AddSingleton(emailMock.Object);
        services.AddSingleton(publisherMock.Object);
        // Note: Logger<T> is often registered by AddLogging(). If specific mock needed, add via configureServices.

        // Add common infrastructure
        services.AddLogging(); // Registers ILoggerFactory, ILogger<T>
        services.AddHttpContextAccessor(); // Needed for SignInManager/UserManager
        services.AddAuthentication(); // Needed for SignInManager dependencies

        // Add the AuthService itself
        services.AddScoped<IAuthService, AuthService>(); // Register the service

        // Allow specific tests to add/override services
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
