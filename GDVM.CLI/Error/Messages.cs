using GDVM.Environment;

namespace GDVM.Error;

/// <summary>
///     CLI-specific user-facing messages with markup formatting
/// </summary>
public static class Messages
{
    // Prompts
    public static string TypeToSearch => "[aqua]Type to search...[/]";
    public static string SelectARuntime => "[green]Standard or Mono (.NET Support)?[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
    public static string UnexpectedError => "[red]Unexpected error: version resolution succeeded but result is not success.[/]";
    public static string UnknownResolutionError => "[red]Unknown error occurred while resolving version.[/]";
    public static string UnknownSymlinkError => "[red]Unknown error occurred while checking current version[/]";
    public static string MoreChoicesText => "[grey](Move up and down to see more versions)[/]";

    // Symlinks
    public static string NoVersionSet => "[yellow]No Godot version is currently set[/]";
    public static string NoCurrentVersionSet => "[red]No current Godot version set.[/]";
    public static string NoInstallationsToRemove => "[orange1] No installations available to remove. [/]";
    public static string SetAsDefaultVersionNote => "[dim]Set as default version.[/]";
    public static string AutoSetAsDefaultNote => "[dim]Set as default version since no other versions are installed.[/]";
    public static string InstallationSucceededButNotFound => "[red]Installation succeeded but version not found in installed list.[/]";
    public static string ErrorResolvingVersion => "[red]Error resolving Godot version for launch.[/]";

    // Version management
    public static string NoVersionsInstalled => "[red]No Godot versions installed.[/]";
    public static string CreatedVersionFile => "[dim]Created `.gdvm-version` file in current directory.[/]";
    public static string UpdatedVersionFile => "[dim]Updated `.gdvm-version` file in current directory.[/]";
    public static string ChooseFromInstalled => "[dim]Choose from installed versions:[/]";

    // Godot
    public static string MultipleArgsError => "[red]Error: Multiple arguments detected. Please pass all Godot arguments as a single quoted string.[/]";
    public static string ArgsExplanation => "[dim]This ensures proper argument parsing and avoids shell interpretation issues.[/]";

    // List
    public static string ListPanelHeader => "List Of Installed Versions";

    // Local
    public static string InteractiveWithQueryWarning => "[yellow]Warning: Cannot use --interactive with version query. Defaulting to interactive selection.[/]";

    // Search
    public static string AvailableVersionsHeader => "[green]List Of Available Versions[/]";

    // Exceptions
    public static string NoInstallationsFound => "No installations found. Please install one or more versions first.";
    public static string InvalidGodotVersion => "Invalid Godot version.";
    public static string NoInstallationsAndNoVersionFile => "No installations found and no `.gdvm-version` file. Install a version first with: gdvm install <version>";
    public static string NoVersionsInstalledPrompt => "No versions installed. Install a version first with: `gdvm install <version>`";
    public static string InstallationFailedNoVersions => "Installation failed and no versions available.";
    public static string UnknownInstallationOutcome => "Unknown installation outcome";
    public static string UnknownInstallationResultType => "Unknown installation result type";
    public static string CurrentVersionSetTo(string symlinkPath) => $"[green]Current version set to:[/] {symlinkPath}";
    public static string CurrentMacOSAppSetTo(string macAppSymlinkPath) => $"\n[green]Current macOS App set to:[/] {macAppSymlinkPath}";
    public static string ConfigurationError(string message) => $"[red]Configuration error: {message}[/]";
    public static string ExceptionMessage(string message) => $"[red]{message}.[/]";
    public static string SelectAVersionTo(string what) => $"[green]Select a version to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
    public static string SelectVersionsTo(string what) => $"[green]Select the versions to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";

    // Cancellation
    public static string UserCancelled(string what) => $"[red]User cancelled {what} operation :prohibited:[/]";

    // Errors
    public static string SomethingWentWrong(string when, string logPath) =>
        $"[red]Something went wrong {when} 💣[/]\n[red]Please use [hotpink_1]gdvm logs[/] for more information or check {logPath}.[/]";

    public static string SomethingWentWrong(string when, IPathService pathService) =>
        SomethingWentWrong(when, pathService.LogPath);

    public static string SomethingWentWrong(string when) =>
        $"[red]Something went wrong {when} 💣[/]\n[red]Please use [hotpink_1]gdvm logs[/] for more information.[/]";

    public static string InvalidSymlink(string path, string target) =>
        $"[red]Invalid symlink detected:[/] {path} -> {target}\n[red]Symlink appears to be invalid[/]";

    public static string InvalidSymlinkWarn(string symlinkPath) =>
        $"[orange1]WARN: Symlink for {symlinkPath} was created but appears to be invalid. Removing it.[/]";

    // Installation
    public static string NewInstallation(string releaseNameWithRuntime) => $"[green]Successfully installed {releaseNameWithRuntime}[/]";
    public static string AlreadyInstalled(string releaseNameWithRuntime) => $"[yellow]{releaseNameWithRuntime} is already installed[/]";
    public static string InstallationNotFound(string version) => $"[red]Version {version} could not be found[/]";
    public static string InstallationFailed(string reason) => $"[red]Installation failed: {reason}[/]";
    public static string InstallationSuccessBase(string releaseNameWithRuntime) => $"Finished installing {releaseNameWithRuntime}! :party_popper:";
    public static string SuccessfullyInstalled(string releaseNameWithRuntime) => $"[green]Successfully installed {releaseNameWithRuntime}.[/]";

    // Version resolution
    public static string VersionResolutionNotFound(string version) => $"[red]Version {version} could not be found[/]";
    public static string VersionResolutionFailed(string reason) => $"[red]Version resolution failed: {reason}[/]";
    public static string InvalidVersion(string version) => $"[red]The version '{version}' is invalid[/]";
    public static string InvalidProjectVersion(string compatibleVersion) => $"[red]Invalid project version: {compatibleVersion}[/]";
    public static string SuccessfullySetVersion(string releaseNameWithRuntime) => $"[green]Successfully set version to {releaseNameWithRuntime}. [/]";
    public static string SetLocalVersion(string releaseNameWithRuntime) => $"[green]Set local version to {releaseNameWithRuntime}.[/]";

    public static string ProjectSpecifiesVersion(string projectVersion, string runtimeDisplaySuffix) =>
        $"[yellow]Project specifies {projectVersion}{runtimeDisplaySuffix}.[/]";

    public static string ProjectVersionNotInstalled(string projectVersion, string runtimeDisplaySuffix) =>
        $"[yellow]Project specifies {projectVersion}{runtimeDisplaySuffix} but it's not installed.[/]";

    public static string InstallingProjectVersion(string projectVersion, string runtimeDisplaySuffix) =>
        $"[dim]Installing {projectVersion}{runtimeDisplaySuffix}...[/]";

    public static string FailedToInstallProjectVersion(string projectVersion, string runtimeDisplaySuffix) =>
        $"[red]Failed to install {projectVersion}{runtimeDisplaySuffix}.[/]";

    public static string InstallationInstructions(string projectVersion, bool isDotNet) =>
        $"[dim]Run 'gdvm install {projectVersion}{(isDotNet ? " mono" : "")}' or 'gdvm local {projectVersion}{(isDotNet ? " mono" : "")}' to install it.[/]";

    public static string ManualInstallInstructions(string projectVersion, bool isDotNet) =>
        $"[dim]You can manually install with: gdvm install {projectVersion}{(isDotNet ? " mono" : "")}[/]";

    public static string SuccessfullyInstalledAndUsing(string projectVersion, string runtimeDisplaySuffix, string newCompatibleVersion) =>
        $"[green]Successfully installed and using: {projectVersion}{runtimeDisplaySuffix} → {newCompatibleVersion}[/]";

    public static string UsingProjectVersion(string projectVersion, string runtimeDisplaySuffix, string compatibleVersion) =>
        $"[dim]Using project version: {projectVersion}{runtimeDisplaySuffix} → {compatibleVersion}[/]";

    public static string NoInstalledVersionMatching(string query) => $"[yellow]No installed version found matching '{query}'.[/]";
    public static string Installing(string query) => $"[dim]Installing {query}...[/]";
    public static string FailedToInstallMatching(string query) => $"[red]Failed to install version matching '{query}'.[/]";

    public static string InstallingAutoDetected(string projectVersion, string runtimeDisplaySuffix) =>
        $"[dim]Installing {projectVersion}...{runtimeDisplaySuffix}[/]";

    // Project confirmation
    public static string ProjectRequiresInstall(string projectVersion, string runtimeText) =>
        $"[yellow]Project requires {projectVersion}{runtimeText} but it's not installed.[/]\n[green]Would you like to install it now?[/]";

    public static string ArgsExampleUsage(string args) => $"[yellow]Example: gdvm godot --args \"{args}\"[/]";
    public static string AutoDetectedProject(string fileName) => $"[dim]Auto-detected project file: {fileName}[/]";
    public static string LaunchedGodotDetached(string versionName, int processId) => $"[green]Launched Godot {versionName} in detached mode (PID: {processId}).[/]";

    public static string RunningAttachedMode(string versionName) =>
        $"[yellow]Note: Running Godot {versionName} in attached mode due to arguments requiring terminal output.[/]";

    // Remove
    public static string FoundExactMatch(string versionToRemove) => $"[yellow]Found exactly one version matching your query: {versionToRemove}[/]";
    public static string NoVersionsMatchingQuery(string queryJoin) => $"[orange1]Couldn't find any versions with query `{queryJoin}`. Please try again. [/]";
    public static string SuccessfullyRemoved(string selectionPath) => $"Successfully removed {selectionPath} :wastebasket: ";
    public static string LogLevelOutOfRange(string level) => $"{level} is not valid. Accepted values include: default, debug, info, warning, error, or critical";
    public static string LogPathNotFound(string logPath) => $"Path to logs {logPath} doesn't exist.";
    public static string UnableToGetRelease(string version) => $"Unable to get release with selection `{version}`.";
}
