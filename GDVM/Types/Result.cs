namespace GDVM.Types;

/// <summary>
///     Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
/// <typeparam name="E">The type of the error value</typeparam>
public abstract record Result<T, E>
{
    public sealed record Success(T Value) : Result<T, E>;

    public sealed record Failure(E Error) : Result<T, E>;
}
