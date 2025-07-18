namespace GDVM.Types;

/// <summary>
///     Represents the successful outcome of version resolution.
/// </summary>
public abstract record VersionResolutionOutcome
{
    public record Found(string ExecutablePath, string WorkingDirectory, string VersionName, bool IsProjectVersion, IReadOnlyList<string>? InfoMessages = null)
        : VersionResolutionOutcome;

    public record InteractiveRequired(IReadOnlyList<string> AvailableVersions) : VersionResolutionOutcome;
}

/// <summary>
///     Represents the possible errors that can occur during version resolution.
/// </summary>
public abstract record VersionResolutionError
{
    public record NotFound(IReadOnlyList<string>? ErrorMessages = null) : VersionResolutionError;

    public record Failed(IReadOnlyList<string>? ErrorMessages = null) : VersionResolutionError;

    public record InvalidVersion(IReadOnlyList<string>? ErrorMessages = null) : VersionResolutionError;
}
