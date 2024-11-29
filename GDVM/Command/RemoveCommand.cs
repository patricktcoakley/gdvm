using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class RemoveCommand(IHostSystem hostSystem, IReleaseManager releaseManager, ILogger<RemoveCommand> logger)
{
    /// <summary>
    ///     Prompts the user to remove multiple installations, or takes an exact version string for a version of Godot.
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
                // Always remove symlinks in case they were corrupted or there is a mismatch between installations
                hostSystem.RemoveSymbolicLinks();
                AnsiConsole.MarkupLine("[orange1] No installations available to remove. [/]");
                return;
            }

            var filteredInstallations = releaseManager.FilterReleasesByQuery(query, installed).ToArray();
            if (filteredInstallations.Length == 0)
            {
                var queryJoin = string.Join(' ', query);
                logger.ZLogInformation($"Query didn't find any installations: {queryJoin}.");
                AnsiConsole.MarkupLine($"[orange1]Couldn't find any versions with query `{queryJoin}`. Please try again. [/]");
                return;
            }

            var versionsToDelete = await Prompts.Remove.ShowVersionRemovalPrompt(filteredInstallations, cancellationToken);

            foreach (var selectionPath in versionsToDelete.Select(selection => Path.Combine(Paths.RootPath, selection)))
            {
                Directory.Delete(selectionPath, true);
                logger.ZLogInformation($"Removed {selectionPath}.");
                AnsiConsole.MarkupLine($"Successfully removed {selectionPath} :wastebasket: ");
            }

            if (!hostSystem.ListInstallations().Any())
            {
                hostSystem.RemoveSymbolicLinks();
            }
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled removal.");
            AnsiConsole.MarkupLine(Messages.UserCancelled("removal"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error removing installations: {e.Message}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to remove installations")
            );

            throw;
        }
    }
}
