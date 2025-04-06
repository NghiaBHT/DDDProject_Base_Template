using FluentValidation;
using MediatR;
using ValidationException = DDDProject.Application.Exceptions.ValidationException; // Use custom exception

namespace DDDProject.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior for running FluentValidation validators.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> // Ensure TRequest is a MediatR request
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            // No validators registered for this request type
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Run validators asynchronously and collect failures
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Throw custom validation exception
            throw new ValidationException(failures);
        }

        // Validation passed, proceed with the request handler
        return await next();
    }
} 