using ConsoleAppFramework;
using Fgvm.Cli.Error;
using Fgvm.Environment;
using Fgvm.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace Fgvm.Cli.Command;

public sealed class RemoveCommand(
    IHostSystem hostSystem,
    IReleaseManager releaseManager,
    IPathService pathService,
    IAnsiConsole console,
    ILogger<RemoveCommand> logger)
{
    /// <summary>
    ///     Remove an installed Godot version.
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
                console.MarkupLine(Messages.NoInstallationsToRemove);
                return;
            }

            var filteredInstallations = releaseManager.FilterReleasesByQuery(query, installed).ToArray();
            if (filteredInstallations.Length == 0)
            {
                var queryJoin = string.Join(' ', query);
                logger.ZLogInformation($"Query didn't find any installations: {queryJoin}.");
                console.MarkupLine(Messages.NoVersionsMatchingQuery(queryJoin));
                return;
            }

            IEnumerable<string> versionsToDelete;
            if (filteredInstallations.Length == 1)
            {
                var versionToRemove = filteredInstallations[0];
                logger.ZLogInformation($"Automatically removing single matched version: {versionToRemove}.");
                console.MarkupLine(Messages.FoundExactMatch(versionToRemove));
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
                    console.MarkupLine(Messages.SuccessfullyRemoved(selectionPath));
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
                Messages.SomethingWentWrong("when trying to remove installations", pathService)
            );

            throw;
        }
    }
}
