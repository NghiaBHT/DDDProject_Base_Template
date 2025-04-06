using DDDProject.API.Middleware; // For custom middleware
using DDDProject.Application;
using DDDProject.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // Optional: For health checks later
using NLog;
using NLog.Web;

// --- Logging Configuration ---
// Get NLog logger early for startup messages
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Clear default providers and configure NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // --- Dependency Injection ---
    // Add services from Application and Infrastructure layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Add standard ASP.NET Core services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer(); // Needed for Swagger/OpenAPI if added later
    // builder.Services.AddSwaggerGen(); // Add Swagger generation if needed

    // Add custom middleware
    builder.Services.AddTransient<ExceptionHandlingMiddleware>();

    // Optional: Add Health Checks
    // builder.Services.AddHealthChecks()
    //    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    // --- Build the Application ---
    var app = builder.Build();

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
    app.UseAuthorization(); // Add authentication middleware before this if needed

    // Map controllers
    app.MapControllers();

    // Optional: Map health checks endpoint
    // app.MapHealthChecks("/_health", new HealthCheckOptions
    // {
    //     ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Requires AspNetCore.HealthChecks.UI.Client
    // });

    // --- Run the Application ---
    logger.Info("Starting web host...");
    app.Run();
}
catch (Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}

// Make Program internal for testing purposes if needed
// public partial class Program { }
