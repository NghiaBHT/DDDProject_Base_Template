using DDDProject.API.Middleware; // For custom middleware
using DDDProject.Application;
using DDDProject.Domain.Entities; // Add this
using DDDProject.Infrastructure;
using DDDProject.Infrastructure.Persistence; // Add this
using Microsoft.AspNetCore.Authentication.JwtBearer; // Add this
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // Optional: For health checks later
using Microsoft.AspNetCore.Identity; // Add this
using Microsoft.IdentityModel.Tokens; // Add this
using NLog;
using NLog.Web;
using System.Text; // Add this
using Microsoft.EntityFrameworkCore;
using DDDProject.API.Authorization; // Add this
using Microsoft.AspNetCore.Authorization; // Add this
using Microsoft.Extensions.DependencyInjection; // Add this
using Microsoft.Extensions.Logging; // Add this
using DDDProject.Infrastructure.Authentication; // For JWT services
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using DDDProject.Infrastructure.Services;
using DDDProject.Application.Services.Authentication; // For IOptions

// --- Logging Configuration ---
// Capture the NLog logger
var startupLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
startupLogger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Clear default providers and configure NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // --- Dependency Injection ---
    // Add services from Application and Infrastructure layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    // --- Configure JWT Settings --- <<<<<<<<<< NEW >>>>>>>>>>
    var jwtSettings = new JwtSettings();
    builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);
    // Register JwtSettings using IOptions
    builder.Services.AddSingleton(Options.Create(jwtSettings));
    // Register the generator service as Scoped
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    // <<<<<<<<<< END NEW SECTION >>>>>>>>>>
    builder.Services.AddSingleton<IEmailSender, LoggerEmailSender>();
    // --- Add Identity --- <<<<<<<<<< UPDATED SECTION >>>>>>>>>>
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // Configure password settings (example)
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 1;

        // Configure lockout settings (example)
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Configure user settings (example)
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // For password reset, email confirmation tokens etc.
    // <<<<<<<<<< END UPDATED SECTION >>>>>>>>>>

    // --- Add Authentication (JWT) --- <<<<<<<<<< NEW SECTION >>>>>>>>>>
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer, // Use bound settings
            ValidAudience = jwtSettings.Audience, // Use bound settings
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)) // Use bound settings
        };
    });
    // <<<<<<<<<< END NEW SECTION >>>>>>>>>>

    // Add standard ASP.NET Core services
    builder.Services.AddControllers();
    builder.Services.AddRazorPages();
    builder.Services.AddEndpointsApiExplorer(); // Needed for Swagger/OpenAPI if added later
    // builder.Services.AddSwaggerGen(); // Add Swagger generation if needed

    // --- Add Authorization --- <<<<<<<<<< UPDATED SECTION >>>>>>>>>>
    // Register the custom handler
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    // Register the custom policy provider
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

    // Keep the base AddAuthorization call if you still need role-based or other policies
    builder.Services.AddAuthorization(); // Or potentially remove this if ONLY using permission policies
    // <<<<<<<<<< END Authorization SECTION >>>>>>>>>>

    // Add custom middleware
    builder.Services.AddTransient<ExceptionHandlingMiddleware>();

    // Optional: Add Health Checks
    // builder.Services.AddHealthChecks()
    //    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    // --- Build the Application ---
    var app = builder.Build();

    // --- Seed Database --- <<<<<<<<<< REVISED SECTION >>>>>>>>>>
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        // Get the specific logger type needed by the seeder
        var seederLogger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var environment = services.GetRequiredService<IWebHostEnvironment>(); // Get environment

        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Apply migrations only if NOT in Testing environment
            if (!environment.IsEnvironment("Testing")) 
            {
                startupLogger.Info("Applying database migrations...");
                await dbContext.Database.MigrateAsync();
                startupLogger.Info("Database migrations applied successfully.");
            }
            else
            {
                // For testing environment, ensure DB is created (handled by IntegrationTestBase, but good practice)
                // await dbContext.Database.EnsureCreatedAsync(); 
                startupLogger.Info("Skipping migrations for Testing environment.");
            }

            // Seed data (might need adjustment based on whether migrations ran)
            startupLogger.Info("Initializing database seeding...");
            await DataSeeder.InitializeDatabaseAsync(services, seederLogger); // Pass the specific logger
            startupLogger.Info("Database seeding finished."); // Use startupLogger
        }
        catch (Exception ex)
        {
            // Use startupLogger for errors during startup
            startupLogger.Error(ex, "An error occurred during database initialization (migration or seeding).");
            throw; // Re-throw to stop application start if critical
        }
    }
    // <<<<<<<<<< END REVISED SECTION >>>>>>>>>>

    // --- Configure the HTTP request pipeline ---

    // Use custom exception handling middleware first
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Configure the HTTP request pipeline based on environment
    if (app.Environment.IsDevelopment())
    {
        // app.UseSwagger();
        // app.UseSwaggerUI();
        // Add developer-specific middleware like detailed error pages if not using custom middleware for all errors
        // app.UseDeveloperExceptionPage();
    }
    else
    {
        // Production error handling (covered by our middleware, but could add HSTS, etc.)
        // app.UseExceptionHandler("/Error");
        // app.UseHsts();
    }

    // Standard middleware
    // app.UseHttpsRedirection(); // Consider enabling if needed

    // --- Add Authentication & Authorization Middleware --- <<<<<<<<<< NEW >>>>>>>>>>
    app.UseAuthentication(); // Add Authentication middleware
    app.UseAuthorization(); // Add Authorization middleware

    // Map controllers and pages
    app.MapControllers();
    app.MapRazorPages();

    // Optional: Map health checks endpoint
    // app.MapHealthChecks("/_health", new HealthCheckOptions
    // {
    //     ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Requires AspNetCore.HealthChecks.UI.Client
    // });

    // --- Run the Application ---
    startupLogger.Info("Starting web host..."); // Use startupLogger
    app.Run();
}
catch (Exception ex)
{
    // Use startupLogger for critical startup errors
    startupLogger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit
    LogManager.Shutdown();
}

// Make Program public for testing purposes
public partial class Program { }
