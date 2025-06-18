using ConsoleAppFramework;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;
using Messages = GDVM.Error.Messages;

namespace GDVM.Command;

public sealed class LocalCommand(IVersionManagementService versionManagementService, Messages messages, IAnsiConsole console, ILogger<LocalCommand> logger)
{
    /// <summary>
    ///     Sets the Godot version for the current project using `.gdvm-version` file.
    ///     If no arguments are provided and a project version is detected but not installed, it will automatically install that version.
    ///     If arguments are provided, it will find the best matching version and install it if necessary.
    ///     Use --interactive to select from already installed versions.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="interactive">-i, Creates a prompt to select from installed versions for the local project.</param>
    public async Task Local([Argument] string[]? query = null, bool interactive = false)
    {
        try
        {
            var godotRelease = await versionManagementService.SetLocalVersionAsync(query, interactive);
            console.MarkupLine($"[green]Set local version to {godotRelease.ReleaseNameWithRuntime}.[/]");
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting local version: {e.Message}");
            console.MarkupLine(messages.SomethingWentWrong("when trying to set the local version"));
            throw;
        }
    }
}
