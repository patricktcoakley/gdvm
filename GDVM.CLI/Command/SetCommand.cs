using ConsoleAppFramework;
using GDVM.Error;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;
using Messages = GDVM.Error.Messages;

namespace GDVM.Command;

public sealed class SetCommand(IVersionManagementService versionManagementService, IAnsiConsole console, ILogger<SetCommand> logger)
{
    /// <summary>
    ///     Sets the selected version of Godot.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="interactive">-i, Creates a prompt to select from installed versions to set as global.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task Set([Argument] string[]? query = null, bool interactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await versionManagementService.SetGlobalVersionAsync(query ?? [], interactive, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled setting version.");
            console.MarkupLine(Messages.UserCancelled("setting version"));
            throw;
        }
        catch (InvalidSymlinkException e)
        {
            logger.ZLogError($"Symlink created but appears invalid: {e.SymlinkPath}.");
            console.MarkupLine($"[orange1]WARN: Symlink for {e.SymlinkPath} was created but appears to be invalid. Removing it.[/]");

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting a version: {e.Message}");
            console.MarkupLine(
                Messages.SomethingWentWrong("when trying to set the version")
            );

            throw;
        }
    }
}
