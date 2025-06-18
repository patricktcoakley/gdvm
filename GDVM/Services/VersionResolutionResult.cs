namespace GDVM.Services;

public enum VersionResolutionStatus
{
    Found,
    NotFound,
    Failed,
    InvalidVersion
}

/// <summary>
///     Result of version resolution for launching Godot
/// </summary>
public readonly record struct VersionResolutionResult(
    string ExecutablePath,
    string WorkingDirectory,
    string VersionName,
    bool IsProjectVersion,
    VersionResolutionStatus Status)
{
    public bool IsSuccess => Status == VersionResolutionStatus.Found;

    public static VersionResolutionResult Found(string executablePath, string workingDirectory, string versionName, bool isProjectVersion = false) =>
        new(executablePath, workingDirectory, versionName, isProjectVersion, VersionResolutionStatus.Found);

    public static VersionResolutionResult NotFound() =>
        new(string.Empty, string.Empty, string.Empty, false, VersionResolutionStatus.NotFound);

    public static VersionResolutionResult Failed() =>
        new(string.Empty, string.Empty, string.Empty, false, VersionResolutionStatus.Failed);

    public static VersionResolutionResult InvalidVersion() =>
        new(string.Empty, string.Empty, string.Empty, false, VersionResolutionStatus.InvalidVersion);
}
