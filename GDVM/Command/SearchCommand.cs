using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class SearchCommand(IReleaseManager releaseManager, ILogger<SearchCommand> logger)
{
    /// <summary>
    ///     A remote search that takes an optional query and displays a filtered list of Godot releases available to download.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="query"></param>
    public async Task Search(CancellationToken cancellationToken = default, [Argument] params string[] query)
    {
        try
        {
            Panel panel;
            if (query.Length == 0)
            {
                var releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();

                await File.WriteAllLinesAsync(Paths.ReleasesPath, releaseNames, cancellationToken);
                panel = new Panel(string.Join("\n", releaseNames))
                {
                    Header = new PanelHeader("[green]List Of Available Versions[/]"),
                    Width = 40
                };

                AnsiConsole.Write(panel);
                return;
            }

            var filteredReleaseNames = await releaseManager.SearchRemoteReleases(query, cancellationToken);

            panel = new Panel(string.Join("\n", filteredReleaseNames))
            {
                Header = new PanelHeader("[green]List Of Available Versions[/]"),
                Width = 40
            };

            AnsiConsole.Write(panel);
        }

        catch (Exception e)
        {
            logger.ZLogError($"Error searching releases: {e.Message}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to search releases")
            );

            throw;
        }
    }
}
