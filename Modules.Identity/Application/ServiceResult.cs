namespace Modules.Identity.Application;

public sealed record ServiceResult<T>(bool Succeeded, string? Error, T? Data)
{
    public static ServiceResult<T> Ok(T data) => new(true, null, data);
    public static ServiceResult<T> Fail(string error) => new(false, error, default);
}
