using System.Reflection;
using DDDProject.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DDDProject.Application.Services.Authentication;

namespace DDDProject.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register AutoMapper (scans Application assembly for profiles)
        // Ensure profiles exist in this assembly or add scans for other assemblies
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Register FluentValidation validators (scans Application assembly)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            // Register services from assembly containing requests/handlers (Application assembly)
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Register pipeline behaviors
            // Order matters: Logging -> Validation
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(DDDProject.Application.Behaviors.ValidationBehavior<,>));
        });

        // Register Application Services
        services.AddScoped<IAuthService, AuthService>();

        // Optional: Register other application-specific services here
        // e.g., services.AddScoped<IUserService, UserService>();

        return services;
    }
} 