namespace GDVM.Services;

public enum InstallationStatus
{
    NewInstallation,
    AlreadyInstalled,
    NotFound,
    Failed
}

public readonly record struct InstallationResult(string ReleaseNameWithRuntime, InstallationStatus Status)
{
    public bool IsSuccess => Status is InstallationStatus.NewInstallation or InstallationStatus.AlreadyInstalled;

    public static InstallationResult NewInstallation(string releaseNameWithRuntime) =>
        new(releaseNameWithRuntime, InstallationStatus.NewInstallation);

    public static InstallationResult AlreadyInstalled(string releaseNameWithRuntime) =>
        new(releaseNameWithRuntime, InstallationStatus.AlreadyInstalled);

    public static InstallationResult NotFound() =>
        new(string.Empty, InstallationStatus.NotFound);

    public static InstallationResult Failed() =>
        new(string.Empty, InstallationStatus.Failed);
}
