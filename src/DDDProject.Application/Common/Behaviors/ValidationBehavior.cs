using DDDProject.Domain.Common; // For Result
using FluentValidation;
using FluentValidation.Results; // For ValidationResult
using MediatR;
using Microsoft.Extensions.Logging; // Optional: For logging
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DDDProject.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior to automatically validate requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response from the handler (must be a Result type).</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result // Constraint TResponse to be a Result or Result<T>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>>? _logger; // Optional logger

    // Inject validators for the specific TRequest and optional logger
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>>? logger = null)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            // No validators registered for this request type
            _logger?.LogTrace("No validators configured for {RequestType}", typeof(TRequest).Name);
            return await next();
        }

        _logger?.LogDebug("Running validation for {RequestType}...", typeof(TRequest).Name);

        // Create a validation context
        var context = new ValidationContext<TRequest>(request);

        // Run all validators asynchronously and collect results
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Aggregate validation failures
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (failures.Any())
        {
            _logger?.LogWarning("Validation failed for {RequestType}. Errors: {ValidationErrors}",
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            // Validation failed, return a Failure result
            // We need to create the correct Result type (Result or Result<T>)
            return CreateValidationResult<TResponse>(failures);
        }

        // Validation succeeded, proceed to the next handler in the pipeline
        _logger?.LogDebug("Validation successful for {RequestType}. Proceeding to handler.", typeof(TRequest).Name);
        return await next();
    }

    // Helper method to create the appropriate Failure Result (Result or Result<T>)
    private static TResponse CreateValidationResult<TResponse>(List<ValidationFailure> failures)
        where TResponse : Result // Use TResponse from the class
    {
        var errorMessages = failures.Select(f => f.ErrorMessage).ToList();

        // Check if the required response is the non-generic Result
        if (typeof(TResponse) == typeof(Result))
        {
            // Create and cast Result.Failure to TResponse (which is Result here)
            // Cast needed: Result -> object -> TResponse
            return (TResponse)(object)Result.Failure(errorMessages);
        }

        // Handle the generic Result<T> case
        // Get the T from Result<T> (TResponse)
        Type valueType = typeof(TResponse).GetGenericArguments()[0];

        // Get the static generic method Result.Failure<T>(IEnumerable<string>)
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
            .MakeGenericMethod(valueType);

        if (failureMethod == null)
        {
            // This should ideally not happen if TResponse is correctly constrained
            throw new InvalidOperationException($"Could not find static generic Failure method on Result type for {typeof(TResponse).Name}");
        }

        // Invoke Result.Failure<T>(errors) and cast the result (which is object) to TResponse
        var failureResult = failureMethod.Invoke(null, new object[] { errorMessages });
        if (failureResult == null)
        {
            throw new InvalidOperationException("Result.Failure returned null unexpectedly.");
        }
        return (TResponse)failureResult;
    }
} 