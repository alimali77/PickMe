namespace PickMe.Api.Common;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
    public string? Code { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Ok() => new() { Success = true };
    public static ApiResponse<T> Fail(string code, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        => new() { Success = false, Code = code, Message = message, Errors = errors };
}

public sealed record EmptyPayload;
