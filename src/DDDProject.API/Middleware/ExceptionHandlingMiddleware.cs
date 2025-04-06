using System.Net;
using System.Text.Json;
using DDDProject.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DDDProject.API.Middleware;

/// <summary>
/// Global exception handling middleware.
/// </summary>
public class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        object response;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                response = new ValidationProblemDetails(validationException.Errors)
                {
                    Title = "Validation Failed",
                    Status = (int)statusCode,
                    Detail = validationException.Message,
                    Instance = context.Request.Path
                };
                break;

            // Add cases for other custom domain/application exceptions
            // case NotFoundException notFoundException:
            //     statusCode = HttpStatusCode.NotFound;
            //     response = new ProblemDetails { ... };
            //     break;

            default: // Unhandled exceptions
                statusCode = HttpStatusCode.InternalServerError;
                response = new ProblemDetails
                {
                    Title = "An unexpected error occurred",
                    Status = (int)statusCode,
                    Detail = "An internal server error has occurred.", // Don't expose exception details in production
                    Instance = context.Request.Path
                };
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
} 