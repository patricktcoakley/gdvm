using GDVM.Environment;
using GDVM.Godot;
using GDVM.Prompts;
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
    Task<VersionResolutionResult> ResolveVersionForLaunchAsync(bool forceInteractive = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the global Godot version by creating/updating symlinks.
    /// </summary>
    /// <param name="query">Version query to search for</param>
    /// <param name="forceInteractive">Force interactive selection from installed versions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The release that was set</returns>
    Task<Release> SetGlobalVersionAsync(string[] query, bool forceInteractive = false, CancellationToken cancellationToken = default);

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
    IAnsiConsole console,
    ILogger<VersionManagementService> logger) : IVersionManagementService
{
    /// <summary>
    ///     Resolves the appropriate Godot version for launching in the current directory.
    ///     Handles project-specific version detection, compatibility checking, and automatic installation prompts.
    /// </summary>
    public async Task<VersionResolutionResult> ResolveVersionForLaunchAsync(bool forceInteractive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToList();

            // Check for project-specific version first (unless interactive mode is requested)
            var projectInfo = ProjectManager.FindProjectInfo();

            if (projectInfo is not null && !forceInteractive)
            {
                return await ResolveProjectVersionAsync(projectInfo, installed, cancellationToken);
            }

            if (forceInteractive)
            {
                return await ResolveInteractiveVersionAsync(installed, cancellationToken);
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
            console.MarkupLine("[red]Error resolving Godot version for launch.[/]");
            return VersionResolutionResult.Failed();
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
                throw new InvalidOperationException("No installations found. Please install one or more versions first.");
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
                throw new InvalidOperationException("Invalid Godot version.");
            }

            var symlinkTargetPath = Path.Combine(pathService.RootPath, godotRelease.ReleaseNameWithRuntime);
            symlinkTargetPath = Path.Combine(symlinkTargetPath, godotRelease.ExecName);
            hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);

            logger.ZLogInformation($"Successfully set version to {godotRelease.ReleaseNameWithRuntime}.");
            console.MarkupLine($"[green]Successfully set version to {godotRelease.ReleaseNameWithRuntime}. [/]");

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

            var godotRelease = releaseManager.TryCreateRelease(versionToSet) ?? throw new InvalidOperationException("Invalid Godot version.");

            // Create the `.gdvm-version` file
            CreateOrUpdateVersionFile(godotRelease.ReleaseNameWithRuntime);

            var versionFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".gdvm-version");
            var fileExists = File.Exists(versionFilePath);

            logger.ZLogInformation($"Successfully set local version to {godotRelease.ReleaseNameWithRuntime}.");
            console.MarkupLine($"[dim]{(fileExists ? "Updated" : "Created")} `.gdvm-version` file in current directory.[/]");

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

            // No compatible version found, attempt installation if prompted
            if (!promptForInstallation || !await PromptForInstallationAsync(projectVersion, isDotNet, cancellationToken))
            {
                return null;
            }

            console.MarkupLine($"[dim]Installing {projectVersion}{(isDotNet ? " (.NET)" : "")}...[/]");

            // Build the query for installation
            var installQuery = isDotNet
                ? new[] { projectVersion, "mono" }
                : new[] { projectVersion };

            var installedRelease = await installationService.InstallByQueryAsync(installQuery, cancellationToken: cancellationToken);

            if (!installedRelease.IsSuccess)
            {
                return null;
            }

            // Re-check for a compatible version after installation
            installed = hostSystem.ListInstallations().ToList();
            compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, isDotNet, installed);
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
        ProjectManager.CreateVersionFile(version, directory);
    }

    private async Task<VersionResolutionResult> ResolveProjectVersionAsync(ProjectManager.ProjectInfo projectInfo, List<string> installed,
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
            console.MarkupLine($"[yellow]Project specifies {projectVersion}{(projectInfo.IsDotNet ? " [.NET]" : "")} but it's not installed.[/]");
            console.MarkupLine(
                $"[dim]Run 'gdvm install {projectVersion}{(projectInfo.IsDotNet ? " mono" : "")}' or 'gdvm local {projectVersion}{(projectInfo.IsDotNet ? " mono" : "")}' to install it.[/]");

            return VersionResolutionResult.NotFound();
        }

        var compatibleInstalled = await FindOrInstallCompatibleVersionAsync(projectVersion, projectInfo.IsDotNet, false, cancellationToken);
        if (compatibleInstalled is null)
        {
            console.MarkupLine($"[red]Failed to install {projectVersion}{(projectInfo.IsDotNet ? " [.NET]" : "")}.[/]");
            console.MarkupLine($"[dim]You can manually install with: gdvm install {projectVersion}{(projectInfo.IsDotNet ? " mono" : "")}[/]");
            return VersionResolutionResult.Failed();
        }

        // Re-get installed versions and resolve again
        var updatedInstalled = hostSystem.ListInstallations().ToList();
        var newCompatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, projectInfo.IsDotNet, updatedInstalled);

        if (newCompatibleVersion is null || releaseManager.TryCreateRelease(newCompatibleVersion) is not { } newProjectGodotRelease)
        {
            console.MarkupLine("[red]Installation succeeded but version not found in installed list.[/]");
            return VersionResolutionResult.Failed();
        }

        var (execPath, workingDirectory) = GetExecutionPaths(newProjectGodotRelease);

        console.MarkupLine(
            $"[green]Successfully installed and using: {projectVersion}{(projectInfo.IsDotNet ? " (.NET)" : "")} → {newCompatibleVersion}[/]");

        return VersionResolutionResult.Found(execPath, workingDirectory, newCompatibleVersion, true);
    }

    private async Task<VersionResolutionResult> ResolveInteractiveVersionAsync(List<string> installed, CancellationToken cancellationToken)
    {
        if (installed.Count == 0)
        {
            logger.ZLogError($"Tried to launch when no version is set and no installations available.");
            console.MarkupLine("[red]No Godot versions installed.[/]");
            return VersionResolutionResult.NotFound();
        }

        var selection = await Prompts.Godot.ShowGodotSelectionPrompt(installed, console, cancellationToken);

        if (releaseManager.TryCreateRelease(selection) is not { } godotRelease)
        {
            logger.ZLogError($"Invalid Godot version selected: {selection}");
            console.MarkupLine($"[red]Invalid Godot version: {selection}[/]");
            return VersionResolutionResult.InvalidVersion();
        }

        var (execPath, workingDirectory) = GetExecutionPaths(godotRelease);
        return VersionResolutionResult.Found(execPath, workingDirectory, selection);
    }

    private VersionResolutionResult ResolveSymlinkVersion()
    {
        if (!Path.Exists(pathService.SymlinkPath))
        {
            logger.ZLogError($"Tried to launch when no version is set.");
            console.MarkupLine("[red]No current Godot version set.[/]");
            return VersionResolutionResult.NotFound();
        }

        var symlinkInfo = new FileInfo(pathService.SymlinkPath);
        var execPath = symlinkInfo.ResolveLinkTarget(true)!.FullName;
        var workingDirectory = "";
        var versionName = "Unknown";

        if (symlinkInfo.LinkTarget is not { } target)
        {
            return VersionResolutionResult.Found(execPath, workingDirectory, versionName);
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

        return VersionResolutionResult.Found(execPath, workingDirectory, versionName);
    }

    private VersionResolutionResult CreateVersionResolutionResult(string compatibleVersion, ProjectManager.ProjectInfo projectInfo, string projectVersion,
        bool isProjectVersion = false)
    {
        if (releaseManager.TryCreateRelease(compatibleVersion) is not { } projectGodotRelease)
        {
            logger.ZLogError($"Invalid project version: {compatibleVersion}");
            console.MarkupLine($"[red]Invalid project version: {compatibleVersion}[/]");
            return VersionResolutionResult.InvalidVersion();
        }

        var (execPath, workingDirectory) = GetExecutionPaths(projectGodotRelease);

        console.MarkupLine($"[dim]Using project version: {projectVersion}{(projectInfo.IsDotNet ? " (.NET)" : "")} → {compatibleVersion}[/]");
        return VersionResolutionResult.Found(execPath, workingDirectory, compatibleVersion, isProjectVersion);
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
        // Interactive mode
        if (forceInteractive || query == null)
        {
            return await HandleInteractiveModeAsync(installed, cancellationToken);
        }

        if (query.Length == 0)
        {
            return await HandleAutoDetectionModeAsync(installed, cancellationToken);
        }

        return await HandleQueryModeAsync(query, installed, cancellationToken);
    }

    private async Task<string> HandleInteractiveModeAsync(string[] installed, CancellationToken cancellationToken)
    {
        if (installed.Length != 0)
        {
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        logger.ZLogWarning($"No versions installed for interactive selection.");
        throw new InvalidOperationException("No installations found. Install a version first with: gdvm install <version>");
    }

    private async Task<string> HandleAutoDetectionModeAsync(string[] installed, CancellationToken cancellationToken)
    {
        // Check for existing `.gdvm-version` file or `project.godot`
        var projectInfo = ProjectManager.FindProjectInfo();
        if (projectInfo is not null)
        {
            return await HandleProjectInfoAsync(projectInfo, installed, cancellationToken);
        }

        if (installed.Length == 0)
        {
            logger.ZLogWarning($"No versions installed and no `.gdvm-version` file found.");
            throw new InvalidOperationException("No installations found and no `.gdvm-version` file. Install a version first with: gdvm install <version>");
        }

        // No `.gdvm-version` found, prompt for selection
        return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
    }

    private async Task<string> HandleProjectInfoAsync(ProjectManager.ProjectInfo projectInfo, string[] installed, CancellationToken cancellationToken)
    {
        var projectVersion = projectInfo.Version;

        // Try to find a compatible installed version
        var compatibleVersion = releaseManager.FindCompatibleVersion(projectVersion, projectInfo.IsDotNet, installed);

        if (compatibleVersion is not null)
        {
            // Compatible version already installed, create the file quietly
            return compatibleVersion;
        }

        // Not installed, need to install it
        logger.ZLogInformation($"Project version {projectVersion} is not installed, attempting to install it.");
        console.MarkupLine($"[yellow]Project specifies {projectVersion}{(projectInfo.IsDotNet ? " (.NET)" : "")} but it's not installed.[/]");
        console.MarkupLine($"[dim]Installing {projectVersion}{(projectInfo.IsDotNet ? " (.NET)" : "")}...[/]");

        // Build the query for installation
        var installQuery = projectInfo.IsDotNet
            ? new[] { projectVersion, "mono" }
            : new[] { projectVersion };

        var installedRelease = await installationService.InstallByQueryAsync(installQuery, cancellationToken: cancellationToken);
        if (!installedRelease.IsSuccess)
        {
            console.MarkupLine($"[red]Failed to install {projectVersion}{(projectInfo.IsDotNet ? " (.NET)" : "")}.[/]");

            if (installed.Length <= 0)
            {
                throw new InvalidOperationException("No versions installed. Install a version first with: gdvm install <version>");
            }

            console.MarkupLine("[dim]Choose from installed versions:[/]");
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        console.MarkupLine($"[green]Successfully installed {installedRelease.ReleaseNameWithRuntime}.[/]");
        return installedRelease.ReleaseNameWithRuntime;
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
        console.MarkupLine($"[yellow]No installed version found matching '{string.Join(" ", query)}'.[/]");
        console.MarkupLine($"[dim]Installing {string.Join(" ", query)}...[/]");

        // Try to install
        var installedRelease = await installationService.InstallByQueryAsync(query, cancellationToken: cancellationToken);
        if (!installedRelease.IsSuccess)
        {
            console.MarkupLine($"[red]Failed to install version matching '{string.Join(" ", query)}'.[/]");

            if (installed.Length <= 0)
            {
                throw new InvalidOperationException("Installation failed and no versions available.");
            }

            console.MarkupLine("[dim]Choose from installed versions:[/]");
            return await Set.ShowSetVersionPrompt(installed, console, cancellationToken);
        }

        // Re-check installed versions after installation
        var updatedInstalled = hostSystem.ListInstallations().ToArray();
        foundVersion = releaseManager.TryFindReleaseByQuery(query, updatedInstalled)?.ReleaseNameWithRuntime;

        if (foundVersion == null)
        {
            console.MarkupLine("[red]Installation succeeded but version not found in installed list.[/]");
            throw new InvalidOperationException("Installation succeeded but version not found in installed list.");
        }

        console.MarkupLine($"[green]Successfully installed {installedRelease.ReleaseNameWithRuntime}.[/]");
        return foundVersion;
    }

    /// <summary>
    ///     Creates a confirmation prompt for automatic installation.
    /// </summary>
    private async Task<bool> PromptForInstallationAsync(string projectVersion, bool isDotNet, CancellationToken cancellationToken)
    {
        try
        {
            var runtimeText = isDotNet ? " [.NET]" : "";

            var confirmPrompt =
                new ConfirmationPrompt($"[yellow]Project requires {projectVersion}{runtimeText} but it's not installed.[/]\n[green]Would you like to install it now?[/]")
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
