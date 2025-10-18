namespace GDVM.Types;

/// <summary>
///     Information about the current symlink configuration.
/// </summary>
public readonly record struct SymlinkInfo(string SymlinkPath, string? MacAppSymlinkPath = null);

/// <summary>
///     Represents the possible errors that can occur with symlink operations.
/// </summary>
public abstract record SymlinkError
{
    public record NoVersionSet : SymlinkError;

    public record InvalidSymlink(string SymlinkPath, string Target) : SymlinkError;
}
