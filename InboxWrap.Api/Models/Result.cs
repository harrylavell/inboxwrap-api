namespace InboxWrap.Models;

public class Result
{
    public bool Success { get; }
    public string? Error { get; }

    public bool Failure => !Success;

    protected Result(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool success, T? value, string? error)
        : base(success, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static new Result<T> Fail(string error) => new(false, default, error);
}

public class Result<TValue, TError>
{
    public bool Success { get; }
    public bool Failure => !Success;
    public TError? Error { get; }
    public TValue? Value { get; }

    private Result(bool success, TValue? value, TError? error)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public static Result<TValue, TError> Ok(TValue value) => new(true, value, default);
    public static Result<TValue, TError> Fail(TError error) => new(false, default, error);
}
