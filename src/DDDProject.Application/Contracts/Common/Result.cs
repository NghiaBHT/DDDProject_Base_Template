using System.Collections.Generic;
using System.Linq;

namespace DDDProject.Application.Contracts.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public List<string> Errors { get; }

    protected Result(bool isSuccess, List<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? new List<string>();
    }

    public static Result Success() => new Result(true, null);
    public static Result Failure(string error) => new Result(false, new List<string> { error });
    public static Result Failure(IEnumerable<string> errors) => new Result(false, errors?.ToList());

    // Implicit conversion for convenience (optional)
    public static implicit operator bool(Result result) => result.IsSuccess;
}

public class Result<T> : Result
{
    public T Value { get; }

    protected Result(T value, bool isSuccess, List<string> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new Result<T>(value, true, null);
    public new static Result<T> Failure(string error) => new Result<T>(default, false, new List<string> { error });
    public new static Result<T> Failure(IEnumerable<string> errors) => new Result<T>(default, false, errors?.ToList());

    // Implicit conversion for convenience (optional)
    // public static implicit operator T(Result<T> result) => result.Value;
} 