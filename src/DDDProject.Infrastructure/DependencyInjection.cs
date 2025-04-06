using DDDProject.Domain.Abstractions;
using DDDProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DDDProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Guard.Against.NullOrWhiteSpace(connectionString, message: "Database connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString,
                npgsqlOptionsAction: sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    // Optional: Configure resilience
                    // sqlOptions.EnableRetryOnFailure(
                    //     maxRetryCount: 5,
                    //     maxRetryDelay: TimeSpan.FromSeconds(30),
                    //     errorCodesToAdd: null);
                });

            // Optional: Enable sensitive data logging only in development
            // var environment = sp.GetRequiredService<IHostEnvironment>();
            // if (environment.IsDevelopment())
            // {
            //     options.EnableSensitiveDataLogging();
            // }
        });

        // Register UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register Generic Repository
        // Note: Specific repositories (e.g., IOrderRepository) should be registered if needed
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        // Configure AutoMapper
        // It scans the assembly containing this code (Infrastructure)
        // You might need to add scans for Application/Domain assemblies if profiles reside there.
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // Scrutor: Scan for and register services (e.g., domain services, application services if defined here)
        // Example: Register all classes implementing ITransientService as transient
        // services.Scan(scan => scan
        //     .FromAssemblyOf<DependencyInjection>()
        //     .AddClasses(classes => classes.AssignableTo<ITransientService>())
        //     .AsImplementedInterfaces()
        //     .WithTransientLifetime());

        // Example: Register specific services if needed
        // services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}

// Simple Guard Clause helper (can be expanded or replaced with a library like Ardalis.GuardClauses)
internal static class Guard
{
    internal static class Against
    {
        internal static void NullOrWhiteSpace(string? value, string? message = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message ?? "Value cannot be null or whitespace.");
            }
        }
    }
} 