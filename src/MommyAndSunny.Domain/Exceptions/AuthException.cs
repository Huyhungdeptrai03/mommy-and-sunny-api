namespace MommyAndSunny.Domain.Exceptions;

public sealed class AuthException(string code, int statusCode, string message) : DomainException(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
