using FluentValidation.Results;

namespace DDDProject.Application.Exceptions;

/// <summary>
/// Custom exception for validation errors.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    // Constructor that accepts FluentValidation failures
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this() // Calls the default constructor
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    /// <summary>
    /// Gets the validation errors dictionary.
    /// Key: Property name, Value: Array of error messages.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
} 