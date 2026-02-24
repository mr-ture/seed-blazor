namespace MIF.SharedKernel.Application;

/// <summary>
/// Represents an operation outcome using success/failure instead of exceptions for expected errors.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    /// <summary>
    /// Gets the error for failed results. For successful results, this is <see cref="Error.None"/>.
    /// </summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Cannot have success result with error");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Cannot have failure result without error");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with no return value.
    /// </summary>
    public static Result Success() => new(true, Error.None);
    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result that contains a value.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    /// <summary>
    /// Creates a failed typed result.
    /// </summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
/// A typed result that carries a value when successful.
/// </summary>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the operation value. This may be <see langword="null"/> when the result is a failure.
    /// </summary>
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Converts a value to a successful <see cref="Result{T}"/>.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Standard error type used by <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (used for successful results).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
    /// <summary>
    /// Represents a null input or missing required value.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "Value cannot be null");

    /// <summary>
    /// Creates a not-found error for an entity key.
    /// </summary>
    public static Error NotFound(string entity, object key) =>
        new("Error.NotFound", $"{entity} with key {key} was not found");

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string message) =>
        new("Error.Validation", message);

    /// <summary>
    /// Creates a conflict error (for example, duplicate keys or concurrency conflicts).
    /// </summary>
    public static Error Conflict(string message) =>
        new("Error.Conflict", message);
}
