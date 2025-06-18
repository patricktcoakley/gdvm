using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class RemoveCommand(
    IHostSystem hostSystem,
    IReleaseManager releaseManager,
    IPathService pathService,
    Messages messages,
    IAnsiConsole console,
    ILogger<RemoveCommand> logger)
{
    /// <summary>
    ///     Removes Godot installations. Automatically removes when exactly one version matches the query,
    ///     or prompts the user to select from multiple matches.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="query"></param>
    public async Task Remove(CancellationToken cancellationToken = default, [Argument] params string[] query)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var installed = hostSystem
                .ListInstallations()
                .ToArray();

            if (installed.Length == 0)
            {
                hostSystem.RemoveSymbolicLinks();
                console.MarkupLine("[orange1] No installations available to remove. [/]");
                return;
            }

            var filteredInstallations = releaseManager.FilterReleasesByQuery(query, installed).ToArray();
            if (filteredInstallations.Length == 0)
            {
                var queryJoin = string.Join(' ', query);
                logger.ZLogInformation($"Query didn't find any installations: {queryJoin}.");
                console.MarkupLine($"[orange1]Couldn't find any versions with query `{queryJoin}`. Please try again. [/]");
                return;
            }

            IEnumerable<string> versionsToDelete;
            if (filteredInstallations.Length == 1)
            {
                var versionToRemove = filteredInstallations[0];
                logger.ZLogInformation($"Automatically removing single matched version: {versionToRemove}.");
                console.MarkupLine($"[yellow]Found exactly one version matching your query: {versionToRemove}[/]");
                versionsToDelete = [versionToRemove];
            }
            else
            {
                versionsToDelete = await Prompts.Remove.ShowVersionRemovalPrompt(filteredInstallations, console, cancellationToken);
            }

            foreach (var selectionPath in versionsToDelete.Select(selection => Path.Combine(pathService.RootPath, selection)))
            {
                if (Directory.Exists(selectionPath))
                {
                    Directory.Delete(selectionPath, true);
                    logger.ZLogInformation($"Removed {selectionPath}.");
                    console.MarkupLine($"Successfully removed {selectionPath} :wastebasket: ");
                }
                else
                {
                    logger.ZLogWarning($"Directory {selectionPath} does not exist, skipping removal.");
                }
            }

            if (!hostSystem.ListInstallations().Any())
            {
                hostSystem.RemoveSymbolicLinks();
            }
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled removal.");
            console.MarkupLine(Messages.UserCancelled("removal"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error removing installations: {e.Message}");
            console.MarkupLine(
                messages.SomethingWentWrong("when trying to remove installations")
            );

            throw;
        }
    }
}
