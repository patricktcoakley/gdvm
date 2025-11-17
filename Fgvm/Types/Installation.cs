namespace Fgvm.Types;

/// <summary>
///     Represents the successful outcome of an installation operation.
/// </summary>
public abstract record InstallationOutcome
{
    public record NewInstallation(string ReleaseNameWithRuntime) : InstallationOutcome;

    public record AlreadyInstalled(string ReleaseNameWithRuntime) : InstallationOutcome;
}

/// <summary>
///     Represents the possible errors that can occur during installation.
/// </summary>
public abstract record InstallationError
{
    public record NotFound(string Version) : InstallationError;

    public record Failed(string Reason) : InstallationError;
}
