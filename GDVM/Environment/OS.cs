namespace GDVM.Environment;

/// <summary>
///     Gives us a constant value we can use for things like pattern matching where `OSPlatform` would not work
///     since it isn't available at compile time.
/// </summary>
/// <returns></returns>
public enum OS
{
    Windows,
    Linux,
    MacOS,
    FreeBSD,
    Unknown
}
