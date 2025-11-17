namespace Fgvm.Types;

/// <summary>
///     Represents the possible errors that can occur during network operations.
/// </summary>
public abstract record NetworkError
{
    public record RequestFailed(string Url, int StatusCode, string? Body = null) : NetworkError;

    public record Exception(string Message, string? Details = null) : NetworkError;
}
