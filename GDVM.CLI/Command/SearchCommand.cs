using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class SearchCommand(
    IReleaseManager releaseManager,
    IPathService pathService,
    IAnsiConsole console,
    ILogger<SearchCommand> logger)
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
            // Use SearchRemoteReleases for both empty and non-empty queries to get chronological sorting
            var filteredReleaseNames = await releaseManager.SearchRemoteReleases(query, cancellationToken);

            var panel = new Panel(string.Join("\n", filteredReleaseNames))
            {
                Header = new PanelHeader(query.Length == 0 ? Messages.AvailableVersionsHeader : "[green]List Of Available Versions[/]"),
                Width = 40
            };

            console.Write(panel);
        }

        catch (Exception e)
        {
            logger.ZLogError($"Error searching releases: {e.Message}");
            console.MarkupLine(
                Messages.SomethingWentWrong("when trying to search releases", pathService)
            );

            throw;
        }
    }
}
