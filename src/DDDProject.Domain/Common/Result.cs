using System;
using System.Collections.Generic;
using System.Linq;

namespace DDDProject.Domain.Common;

/// <summary>
/// Represents the outcome of an operation, indicating success or failure.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IEnumerable<string> Errors { get; } // Changed to IEnumerable<string> for multiple errors

    protected Result(bool isSuccess, IEnumerable<string>? errors = null)
    {
        if (isSuccess && errors != null && errors.Any())
        {
            throw new InvalidOperationException("Successful result cannot have errors.");
        }
        if (!isSuccess && (errors == null || !errors.Any()))
        {
            throw new InvalidOperationException("Failed result must have at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors ?? Enumerable.Empty<string>();
    }

    // Factory methods for creating results
    public static Result Success()
    {
        return new Result(true);
    }

    public static Result Failure(string error)
    {
        return new Result(false, new[] { error });
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }

    // Implicit conversion from string array for convenience
    public static implicit operator Result(string[] errors) => Failure(errors);
    // Implicit conversion from single string for convenience
    public static implicit operator Result(string error) => Failure(error);

    // Generic version
    public static Result<T> Success<T>(T value)
    {
        return new Result<T>(true, value);
    }

    public static Result<T> Failure<T>(string error)
    {
        return new Result<T>(false, default, new[] { error });
    }

    public static Result<T> Failure<T>(IEnumerable<string> errors)
    {
        return new Result<T>(false, default, errors);
    }
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the value returned on success.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    protected internal Result(bool isSuccess, TValue? value, IEnumerable<string>? errors = null)
        : base(isSuccess, errors)
    {
         if (isSuccess && value == null)
         {
              // Allow null for reference types if the signature allows it
              // This check might be too strict depending on whether TValue can be null
              // Consider using attributes or constraints if null success values are intended
              // throw new InvalidOperationException("Successful result must have a non-null value.");
         }
        _value = value;
    }

    // Implicit conversion from TValue for convenience
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
