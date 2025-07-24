using ConsoleAppFramework;
using GDVM.Error;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class LocalCommand(IVersionManagementService versionManagementService, IAnsiConsole console, ILogger<LocalCommand> logger)
{
    /// <summary>
    ///     Sets the Godot version for the current project using `.gdvm-version` file.
    ///     If no arguments are provided and a project version is detected but not installed, it will automatically install that version.
    ///     If arguments are provided, it will find the best matching version and install it if necessary.
    ///     Use --interactive to select from already installed versions.
    /// </summary>
    /// <param name="interactive">-i, Creates a prompt to select from installed versions for the local project.</param>
    /// <param name="query">Version query arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task Local(bool interactive = false, [Argument] string[]? query = null, CancellationToken cancellationToken = default)
    {
        // Warn if both query and interactive are provided, then default to interactive
        if (query is { Length: > 0 } && interactive)
        {
            console.MarkupLine(Messages.InteractiveWithQueryWarning);
            query = null; // Clear query to use interactive mode
        }

        try
        {
            var godotRelease = await versionManagementService.SetLocalVersionAsync(query, interactive, cancellationToken);
            console.MarkupLine(Messages.SetLocalVersion(godotRelease.ReleaseNameWithRuntime));
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled setting local version.");
            console.MarkupLine(Messages.UserCancelled("setting local version"));
            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting local version: {e.Message}");
            console.MarkupLine(Messages.SomethingWentWrong("when trying to set the local version"));
            throw;
        }
    }
}
