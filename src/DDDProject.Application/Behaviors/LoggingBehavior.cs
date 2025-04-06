using MediatR;
using Microsoft.Extensions.Logging;

namespace DDDProject.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging requests and responses.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> // Ensure TRequest is a MediatR request
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Request logging
        _logger.LogInformation("Handling {RequestName}. Request details: {@Request}", requestName, request);

        try
        {
            var response = await next();

            // Response logging
            _logger.LogInformation("Handled {RequestName} successfully. Response details: {@Response}", requestName, response);

            return response;
        }
        catch (Exception ex)
        {
            // Exception logging
            _logger.LogError(ex, "An error occurred while handling {RequestName}. Request details: {@Request}", requestName, request);
            throw; // Re-throw the exception after logging
        }
    }
} 