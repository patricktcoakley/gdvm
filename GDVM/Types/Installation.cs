namespace GDVM.Types;

/// <summary>
///     Represents the successful outcome of an installation operation.
/// </summary>
public abstract record InstallationOutcome
{
    public record NewInstallation(string ReleaseNameWithRuntime, IReadOnlyList<string>? InfoMessages = null) : InstallationOutcome;

    public record AlreadyInstalled(string ReleaseNameWithRuntime, IReadOnlyList<string>? InfoMessages = null) : InstallationOutcome;
}

/// <summary>
///     Represents the possible errors that can occur during installation.
/// </summary>
public abstract record InstallationError
{
    public record NotFound(IReadOnlyList<string>? ErrorMessages = null) : InstallationError;

    public record Failed(IReadOnlyList<string>? ErrorMessages = null) : InstallationError;
}
