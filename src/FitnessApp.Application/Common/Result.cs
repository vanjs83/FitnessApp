namespace FitnessApp.Application.Common;

public enum ResultError
{
    None,
    NotFound,
    Forbidden,
    Validation,
    Conflict
}

/// <summary>
/// Outcome of a command/query that can fail in ways the controller must map to
/// different HTTP status codes (NotFound, Forbid, BadRequest...).
/// </summary>
public class Result
{
    public bool Succeeded { get; }
    public ResultError Error { get; }
    public string? Message { get; }

    protected Result(bool succeeded, ResultError error, string? message)
    {
        Succeeded = succeeded;
        Error = error;
        Message = message;
    }

    public static Result Success() => new(true, ResultError.None, null);
    public static Result Fail(ResultError error, string? message = null) => new(false, error, message);

    public static Result NotFound(string? message = null) => Fail(ResultError.NotFound, message);
    public static Result Forbidden(string? message = null) => Fail(ResultError.Forbidden, message);
    public static Result Conflict(string? message = null) => Fail(ResultError.Conflict, message);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, ResultError.None, null) => Value = value;
    private Result(ResultError error, string? message) : base(false, error, message) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Fail(ResultError error, string? message = null) => new(error, message);

    public static new Result<T> NotFound(string? message = null) => Fail(ResultError.NotFound, message);
    public static new Result<T> Forbidden(string? message = null) => Fail(ResultError.Forbidden, message);
    public static new Result<T> Conflict(string? message = null) => Fail(ResultError.Conflict, message);
}
