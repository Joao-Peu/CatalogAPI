namespace CatalogAPI.Application.Abstractions;

public sealed class Error(string code, string message)
{
    public string Code { get; } = code;
    public string Message { get; } = message;

    public static readonly Error None = new("", "");

    public static implicit operator Result(Error error)
        => Result.Failure(error);
}
