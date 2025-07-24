using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using GDVM.Progress;
using GDVM.Prompts;
using GDVM.Types;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Services;

/// <summary>
///     Service interface for managing Godot version operations
/// </summary>
public interface IVersionManagementService
{
    /// <summary>
    ///     Resolves the appropriate Godot version for launching in the current directory.
    ///     Handles project-specific version detection, compatibility checking, and automatic installation prompts.
    /// </summary>
    /// <param name="forceInteractive">Force interactive selection even if project version is detected</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version resolution result containing execution path and working directory, or failure status</returns>
    Task<Result<VersionResolutionOutcome, VersionResolutionError>> ResolveVersionForLaunchAsync(bool forceInteractive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resolves version for launching using only explicit .gdvm-version files (not project.godot auto-detection)
    /// </summary>
    /// <param name="forceInteractive">Force interactive selection even if .gdvm-version file is found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version resolution result containing execution path and working directory, or failure status</returns>
    Task<Result<VersionResolutionOutcome, VersionResolutionError>> ResolveVersionForLaunchExplicitAsync(bool forceInteractive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the global Godot version by creating/updating symlinks.
    /// </summary>
    /// <param name="query">Version query to search for</param>
    /// <param name="forceInteractive">Force interactive selection from installed versions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The release that was set</returns>
    Task<Release> SetGlobalVersionAsync(string[] query, bool forceInteractive = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resolves an interactive version selection to a version resolution result.
    /// </summary>
    /// <param name="selection">The version selection from interactive prompt</param>
    /// <returns>Version resolution result</returns>
    Result<VersionResolutionOutcome, VersionResolutionError> ResolveInteractiveVersion(string selection);

    /// <summary>
    ///     Sets the local project version by managing the `.gdvm-version` file.
    ///     Handles project detection, installation if needed, and file creation/updates.
    /// </summary>
    /// <param name="query">Version query to search for (null for auto-detection)</param>
    /// <param name="forceInteractive">Force interactive selection from installed versions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The release that was set</returns>
    Task<Release> SetLocalVersionAsync(string[]? query = null, bool forceInteractive = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds or installs a compatible version for the given project requirements.
    /// </summary>
    /// <param name="projectVersion">The version specified in the project</param>
    /// <param name="isDotNet">Whether the project uses .NET</param>
    /// <param name="promptForInstallation">Whether to prompt user for automatic installation if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The compatible version if found/installed, null otherwise</returns>
    Task<string?> FindOrInstallCompatibleVersionAsync(string projectVersion, bool isDotNet, bool promptForInstallation = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates or updates a `.gdvm-version` file in the specified directory.
    /// </summary>
    /// <param name="version">The version to write to the file</param>
    /// <param name="directory">The directory to create the file in (null for current directory)</param>
    void CreateOrUpdateVersionFile(string version, string? directory = null);
}

/// <summary>
///     Service for managing Godot version operations including resolution, installation, and configuration
/// </summary>
public class VersionManagementService(
    IHostSystem hostSystem,
    IReleaseManager releaseManager,
    IInstallationService installationService,
    IPathService pathService,
    IProjectManager projectManager,
    IAnsiConsole console,
    ILogger<VersionManagementService> logger) : IVersionManagementService
{
    /// <summary>
    ///     Resolves the appropriate Godot version for launching in the current directory.
    ///     Handles project-specific version detection, compatibility checking, and automatic installation prompts.
    /// </summary>
    public async Task<Result<VersionResolutionOutcome, VersionResolutionError>> ResolveVersionForLaunchAsync(bool forceInteractive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToList();

            // Check for project-specific version first (unless interactive mode is requested)
            var projectInfo = projectManager.FindProjectInfo();

            if (projectInfo is not null && !forceInteractive)
            {
                return await ResolveProjectVersionAsync(projectInfo, installed, cancellationToken);
            }

            if (forceInteractive)
            {
                return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
                    new VersionResolutionOutcome.InteractiveRequired(installed));
            }

            return ResolveSymlinkVersion();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error resolving version for launch");
            console.MarkupLine(Messages.ErrorResolvingVersion);
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.Failed($"Error resolving version for launch: {e.Message}"));
        }
    }

    /// <summary>
    ///     Resolves version for launching using only explicit .gdvm-version files (not project.godot auto-detection)
    /// </summary>
    public async Task<Result<VersionResolutionOutcome, VersionResolutionError>> ResolveVersionForLaunchExplicitAsync(bool forceInteractive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToList();

            // Check for explicit .gdvm-version file only (not project.godot auto-detection)
            var projectInfo = projectManager.FindExplicitProjectInfo();

            if (projectInfo is not null && !forceInteractive)
            {
                return await ResolveProjectVersionAsync(projectInfo, installed, cancellationToken);
            }

            if (forceInteractive)
            {
                return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
                    new VersionResolutionOutcome.InteractiveRequired(installed));
            }

            return ResolveSymlinkVersion();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error resolving version for launch");
            console.MarkupLine(Messages.ErrorResolvingVersion);
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.Failed($"Error resolving explicit version for launch: {e.Message}"));
        }
    }

    /// <summary>
    ///     Sets the global Godot version by creating/updating symlinks.
    /// </summary>
    public async Task<Release> SetGlobalVersionAsync(string[] query, bool forceInteractive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToArray();
            if (installed.Length == 0)
            {
                logger.ZLogWarning($"Tried to set a version when there were none installed.");
                throw new InvalidOperationException(Messages.NoInstallationsFound);
            }

            var versionToSet = forceInteractive || query.Length == 0
                // Drop to prompt when query is empty or interactive mode is forced
                ? await Set.ShowSetVersionPrompt(installed, console, cancellationToken)
                // Try to find the first release that matches the query or throw
                : releaseManager.FilterReleasesByQuery(query, installed).FirstOrDefault()
                  ?? throw new InvalidOperationException($"Unable to find Godot release with query `{string.Join(", ", query)}`");

            var godotRelease = releaseManager.TryCreateRelease(versionToSet);
            if (godotRelease == null)
            {
                throw new InvalidOperationException(Messages.InvalidGodotVersion);
            }

            var symlinkTargetPath = Path.Combine(pathService.RootPath, godotRelease.ReleaseNameWithRuntime);
            symlinkTargetPath = Path.Combine(symlinkTargetPath, godotRelease.ExecName);
            hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);

            logger.ZLogInformation($"Successfully set version to {godotRelease.ReleaseNameWithRuntime}.");
            console.MarkupLine(Messages.SuccessfullySetVersion(godotRelease.ReleaseNameWithRuntime));

            return godotRelease;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting a version: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Sets the local project version by managing the `.gdvm-version` file.
    ///     Handles project detection, installation if needed, and file creation/updates.
    /// </summary>
    public async Task<Release> SetLocalVersionAsync(string[]? query = null, bool forceInteractive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToArray();
            var versionToSet = await DetermineVersionToSetAsync(query, forceInteractive, installed, cancellationToken);

            var godotRelease = releaseManager.TryCreateRelease(versionToSet) ?? throw new InvalidOperationException(Messages.InvalidGodotVersion);

            // Create the `.gdvm-version` file
            CreateOrUpdateVersionFile(godotRelease.ReleaseNameWithRuntime);

            var versionFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".gdvm-version");
            var fileExists = File.Exists(versionFilePath);

            logger.ZLogInformation($"Successfully set local version to {godotRelease.ReleaseNameWithRuntime}.");
            console.MarkupLine(fileExists ? Messages.UpdatedVersionFile : Messages.CreatedVersionFile);

            return godotRelease;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting local version: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Finds or installs a compatible version for the given project requirements.
    /// </summary>
    public async Task<string?> FindOrInstallCompatibleVersionAsync(string projectVersion, bool isDotNet, bool promptForInstallation = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToList();

            // First try to find a compatible installed version
            var compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, isDotNet, installed);
            if (compatibleVersion is not null)
            {
                return compatibleVersion;
            }

            // No compatible version found, attempt installation if prompting is enabled
            if (!promptForInstallation)
            {
                return null;
            }

            if (!await PromptForInstallationAsync(projectVersion, isDotNet, cancellationToken))
            {
                return null;
            }

            console.MarkupLine(Messages.InstallingAutoDetected(projectVersion, isDotNet ? " (.NET)" : ""));

            // Build the query for installation
            // Parse the project version to extract base version (remove runtime suffix)
            var baseVersion = projectVersion;
            if (baseVersion.EndsWith("-mono"))
            {
                baseVersion = baseVersion.Replace("-mono", "");
            }
            else if (baseVersion.EndsWith("-standard"))
            {
                baseVersion = baseVersion.Replace("-standard", "");
            }

            var installQuery = isDotNet
                ? new[] { baseVersion, "mono" }
                : new[] { baseVersion };

            var installedRelease = await installationService.InstallByQueryAsync(installQuery, new Progress<OperationProgress<InstallationStage>>(), cancellationToken: cancellationToken);

            if (installedRelease is not Result<InstallationOutcome, InstallationError>.Success installSuccess)
            {
                return null;
            }

            // Re-check for a compatible version after installation
            installed = hostSystem.ListInstallations().ToList();
            compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, isDotNet, installed);

            // If installation succeeded but compatibility check failed, return the installed version name
            if (compatibleVersion == null)
            {
                var releaseNameWithRuntime = installSuccess.Value switch
                {
                    InstallationOutcome.NewInstallation(var name) => name,
                    InstallationOutcome.AlreadyInstalled(var name) => name,
                    _ => throw new InvalidOperationException(Messages.UnknownInstallationOutcome)
                };

                return releaseNameWithRuntime;
            }

            return compatibleVersion;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error finding or installing compatible version {projectVersion}");
            return null;
        }
    }

    /// <summary>
    ///     Creates or updates a `.gdvm-version` file in the specified directory.
    /// </summary>
    public void CreateOrUpdateVersionFile(string version, string? directory = null)
    {
        projectManager.CreateVersionFile(version, directory);
    }

    public Result<VersionResolutionOutcome, VersionResolutionError> ResolveInteractiveVersion(string selection)
    {
        if (releaseManager.TryCreateRelease(selection) is not { } godotRelease)
        {
            logger.ZLogError($"Invalid Godot version selected: {selection}");
            console.MarkupLine($"[red]Invalid Godot version: {selection}[/]");
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.InvalidVersion(selection));
        }

        var (execPath, workingDirectory) = GetExecutionPaths(godotRelease);
        return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
            new VersionResolutionOutcome.Found(execPath, workingDirectory, selection, false));
    }

    private async Task<Result<VersionResolutionOutcome, VersionResolutionError>> ResolveProjectVersionAsync(ProjectManager.ProjectInfo projectInfo,
        List<string> installed,
        CancellationToken cancellationToken)
    {
        var projectVersion = projectInfo.Version;

        // Try to find a compatible installed version
        var compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, projectInfo.IsDotNet, installed);

        if (compatibleVersion is not null)
        {
            return CreateVersionResolutionResult(compatibleVersion, projectInfo, projectVersion, true);
        }

        logger.ZLogWarning($"Project version {projectVersion} is not installed.");

        // Prompt user for automatic installation
        if (!await PromptForInstallationAsync(projectVersion, projectInfo.IsDotNet, cancellationToken))
        {
            console.MarkupLine(Messages.ProjectVersionNotInstalled(projectVersion, projectInfo.RuntimeDisplaySuffix));
            console.MarkupLine(Messages.InstallationInstructions(projectVersion, projectInfo.IsDotNet));

            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.NotFound(projectVersion));
        }

        var compatibleInstalled = await FindOrInstallCompatibleVersionAsync(projectVersion, projectInfo.IsDotNet, false, cancellationToken);
        if (compatibleInstalled is null)
        {
            console.MarkupLine(Messages.FailedToInstallProjectVersion(projectVersion, projectInfo.RuntimeDisplaySuffix));
            console.MarkupLine(Messages.ManualInstallInstructions(projectVersion, projectInfo.IsDotNet));
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.Failed($"Failed to install {projectVersion}{projectInfo.RuntimeDisplaySuffix}"));
        }

        // Re-get installed versions and resolve again
        var updatedInstalled = hostSystem.ListInstallations().ToList();
        var newCompatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, projectInfo.IsDotNet, updatedInstalled);

        if (newCompatibleVersion is null || releaseManager.TryCreateRelease(newCompatibleVersion) is not { } newProjectGodotRelease)
        {
            console.MarkupLine(Messages.InstallationSucceededButNotFound);
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.Failed($"Installation succeeded but version {projectVersion} not found in installed list"));
        }

        var (execPath, workingDirectory) = GetExecutionPaths(newProjectGodotRelease);

        console.MarkupLine(Messages.SuccessfullyInstalledAndUsing(projectVersion, projectInfo.RuntimeDisplaySuffix, newCompatibleVersion));

        return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
            new VersionResolutionOutcome.Found(execPath, workingDirectory, newCompatibleVersion, true));
    }

    private Result<VersionResolutionOutcome, VersionResolutionError> ResolveSymlinkVersion()
    {
        if (!Path.Exists(pathService.SymlinkPath))
        {
            logger.ZLogError($"Tried to launch when no version is set.");
            console.MarkupLine(Messages.NoCurrentVersionSet);
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.NotFound("No current version set"));
        }

        var symlinkInfo = new FileInfo(pathService.SymlinkPath);
        var execPath = symlinkInfo.ResolveLinkTarget(true)!.FullName;
        var workingDirectory = "";
        var versionName = "Unknown";

        if (symlinkInfo.LinkTarget is not { } target)
        {
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
                new VersionResolutionOutcome.Found(execPath, workingDirectory, versionName, false));
        }

        var split = target.Split(Path.DirectorySeparatorChar)[..^1];
        workingDirectory = string.Join(Path.DirectorySeparatorChar, split);

        // Extract version name from the symlink target path
        var targetParts = target.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Look for a part that looks like a version (contains numbers and hyphens)
        foreach (var part in targetParts)
        {
            if (string.IsNullOrEmpty(part) ||
                !part.Contains('-') ||
                !part.Any(char.IsDigit) ||
                part.Equals("MacOS", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Contents", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            versionName = part;
            break;
        }

        return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
            new VersionResolutionOutcome.Found(execPath, workingDirectory, versionName, false));
    }

    private Result<VersionResolutionOutcome, VersionResolutionError> CreateVersionResolutionResult(string compatibleVersion, ProjectManager.ProjectInfo projectInfo,
        string projectVersion,
        bool isProjectVersion = false)
    {
        if (releaseManager.TryCreateRelease(compatibleVersion) is not { } projectGodotRelease)
        {
            logger.ZLogError($"Invalid project version: {compatibleVersion}");
            console.MarkupLine(Messages.InvalidProjectVersion(compatibleVersion));
            return new Result<VersionResolutionOutcome, VersionResolutionError>.Failure(
                new VersionResolutionError.InvalidVersion(compatibleVersion));
        }

        var (execPath, workingDirectory) = GetExecutionPaths(projectGodotRelease);

        console.MarkupLine(Messages.UsingProjectVersion(projectVersion, projectInfo.RuntimeDisplaySuffix, compatibleVersion));
        return new Result<VersionResolutionOutcome, VersionResolutionError>.Success(
            new VersionResolutionOutcome.Found(execPath, workingDirectory, compatibleVersion, isProjectVersion));
    }

    private (string execPath, string workingDirectory) GetExecutionPaths(Release release)
    {
        var workingDirectory = Path.Combine(pathService.RootPath, release.ReleaseNameWithRuntime);
        var execPath = Path.Combine(workingDirectory, release.ExecName);

        // Handle macOS app bundles by pointing to the actual executable inside
        if (execPath.EndsWith(".app"))
        {
            execPath = Path.Combine(execPath, "Contents", "MacOS", "Godot");
        }

        return (execPath, workingDirectory);
    }

    private async Task<string> DetermineVersionToSetAsync(string[]? query, bool forceInteractive, string[] installed, CancellationToken cancellationToken)
    {
        if (query == null || query.Length == 0)
        {
            return await HandleAutoDetectionModeAsync(installed, forceInteractive, cancellationToken);
        }

        return await HandleQueryModeAsync(query, installed, cancellationToken);
    }


    private async Task<string> HandleAutoDetectionModeAsync(string[] installed, bool forceInteractive, CancellationToken cancellationToken)
    {
        // Check for existing `.gdvm-version` file or `project.godot`
        var projectInfo = projectManager.FindProjectInfo();
        if (projectInfo is not null)
        {
            return await HandleProjectInfoAsync(projectInfo, installed, forceInteractive, cancellationToken);
        }

        if (installed.Length == 0)
        {
            logger.ZLogWarning($"No versions installed and no `.gdvm-version` file found.");
            throw new InvalidOperationException(Messages.NoInstallationsAndNoVersionFile);
        }

        // No `.gdvm-version` found, prompt for selection
        return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
    }

    private async Task<string> HandleProjectInfoAsync(ProjectManager.ProjectInfo projectInfo, string[] installed, bool forceInteractive,
        CancellationToken cancellationToken)
    {
        var projectVersion = projectInfo.Version;

        // If interactive mode is forced, show selection regardless of compatible versions
        if (forceInteractive)
        {
            if (installed.Length <= 0)
            {
                throw new InvalidOperationException(Messages.NoVersionsInstalledPrompt);
            }

            console.MarkupLine(Messages.ProjectSpecifiesVersion(projectVersion, projectInfo.RuntimeDisplaySuffix));
            console.MarkupLine(Messages.ChooseFromInstalled);
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        // Try to find a compatible installed version
        var compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, projectInfo.IsDotNet, installed);

        if (compatibleVersion is not null)
        {
            // Compatible version already installed, create the file quietly
            return compatibleVersion;
        }

        // Not installed, auto-install
        logger.ZLogInformation($"Project version {projectVersion} is not installed, automatically installing it.");
        console.MarkupLine(Messages.ProjectVersionNotInstalled(projectVersion, projectInfo.RuntimeDisplaySuffix));
        console.MarkupLine(Messages.InstallingProjectVersion(projectVersion, projectInfo.RuntimeDisplaySuffix));

        // Build the query for installation
        // Parse the project version to extract base version (remove runtime suffix)
        var baseVersion = projectVersion;
        if (baseVersion.EndsWith("-mono"))
        {
            baseVersion = baseVersion.Replace("-mono", "");
        }
        else if (baseVersion.EndsWith("-standard"))
        {
            baseVersion = baseVersion.Replace("-standard", "");
        }

        var installQuery = projectInfo.IsDotNet
            ? new[] { baseVersion, "mono" }
            : new[] { baseVersion };

        var installedRelease = await installationService.InstallByQueryAsync(installQuery, new Progress<OperationProgress<InstallationStage>>(), cancellationToken: cancellationToken);
        if (installedRelease is not Result<InstallationOutcome, InstallationError>.Success installSuccess)
        {
            console.MarkupLine(Messages.FailedToInstallProjectVersion(projectVersion, projectInfo.RuntimeDisplaySuffix));

            if (installed.Length <= 0)
            {
                throw new InvalidOperationException("No versions installed. Install a version first with: gdvm install <version>");
            }

            console.MarkupLine(Messages.ChooseFromInstalled);
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        var releaseNameWithRuntime = installSuccess.Value switch
        {
            InstallationOutcome.NewInstallation(var name) => name,
            InstallationOutcome.AlreadyInstalled(var name) => name,
            _ => throw new InvalidOperationException(Messages.UnknownInstallationOutcome)
        };

        console.MarkupLine(Messages.SuccessfullyInstalled(releaseNameWithRuntime));
        return releaseNameWithRuntime;
    }

    private async Task<string> HandleQueryModeAsync(string[] query, string[] installed, CancellationToken cancellationToken)
    {
        // Use query to find version - try to find exact match first
        var foundVersion = releaseManager.TryFindReleaseByQuery(query, installed)?.ReleaseNameWithRuntime;

        if (foundVersion is not null)
        {
            return foundVersion;
        }

        logger.ZLogInformation($"Version matching '{string.Join(" ", query)}' not installed, attempting to install it.");
        console.MarkupLine(Messages.NoInstalledVersionMatching(string.Join(" ", query)));
        console.MarkupLine(Messages.Installing(string.Join(" ", query)));

        // Try to install
        var installedRelease = await installationService.InstallByQueryAsync(query, new Progress<OperationProgress<InstallationStage>>(), cancellationToken: cancellationToken);
        if (installedRelease is not Result<InstallationOutcome, InstallationError>.Success installSuccess)
        {
            console.MarkupLine(Messages.FailedToInstallMatching(string.Join(" ", query)));

            if (installed.Length <= 0)
            {
                throw new InvalidOperationException(Messages.InstallationFailedNoVersions);
            }

            console.MarkupLine(Messages.ChooseFromInstalled);
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        // Re-check installed versions after installation
        var updatedInstalled = hostSystem.ListInstallations().ToArray();
        foundVersion = releaseManager.TryFindReleaseByQuery(query, updatedInstalled)?.ReleaseNameWithRuntime;

        if (foundVersion == null)
        {
            console.MarkupLine(Messages.InstallationSucceededButNotFound);
            throw new InvalidOperationException(Messages.InstallationSucceededButNotFound);
        }

        var releaseNameWithRuntime = installSuccess.Value switch
        {
            InstallationOutcome.NewInstallation(var name) => name,
            InstallationOutcome.AlreadyInstalled(var name) => name,
            _ => throw new InvalidOperationException(Messages.UnknownInstallationOutcome)
        };

        console.MarkupLine(Messages.SuccessfullyInstalled(releaseNameWithRuntime));
        return foundVersion;
    }

    /// <summary>
    ///     Creates a confirmation prompt for automatic installation.
    /// </summary>
    private async Task<bool> PromptForInstallationAsync(string projectVersion, bool isDotNet, CancellationToken cancellationToken)
    {
        try
        {
            var runtimeText = isDotNet ? " [[.NET]]" : "";

            var confirmPrompt =
                new ConfirmationPrompt(Messages.ProjectRequiresInstall(projectVersion, runtimeText))
                {
                    DefaultValue = true
                };

            return await confirmPrompt.ShowAsync(console, cancellationToken);
        }
        catch (Exception)
        {
            // If prompting fails (e.g., non-interactive environment), default to false
            return false;
        }
    }
}
