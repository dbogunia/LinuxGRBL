namespace LaserGRBL.Core.Abstractions;

public sealed record OperationError(string Message, string? Detail = null, Exception? Exception = null);

public sealed record OperationResult(bool Succeeded, OperationError? Error = null)
{
    public static OperationResult Success() => new(true);

    public static OperationResult Failure(string message, string? detail = null, Exception? exception = null) =>
        new(false, new OperationError(message, detail, exception));
}

public sealed record OperationResult<T>(bool Succeeded, T? Value = default, OperationError? Error = null)
{
    public static OperationResult<T> Success(T value) => new(true, value);

    public static OperationResult<T> Failure(string message, string? detail = null, Exception? exception = null) =>
        new(false, default, new OperationError(message, detail, exception));
}
